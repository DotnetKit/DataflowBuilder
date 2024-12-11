using System.Threading.Tasks.Dataflow;
using DotnetKit.DataflowBuilder.Abstractions;
using DotnetKit.DataflowBuilder.Core;

namespace DotnetKit.DataflowBuilder;

public static class DataFlowPipelineBuilder
{
    public static IFluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>> FromSource<TSource>(int sourceBufferSize = 1, CancellationToken cancellationToken = default)
    {
        var initialBlock = new BufferBlock<TSource>(new DataflowBlockOptions()
        {
            BoundedCapacity = sourceBufferSize,
            CancellationToken = cancellationToken
        });

        return new FluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>>(new DataFlowPipelineBuilder<TSource>(initialBlock, cancellationToken));
    }

    public static IFluentBuilder<ISourceBlock<Tuple<TSource1, TSource2>>, DataFlowPipelineBuilder<TSource1, TSource2>> FromSources<TSource1, TSource2>(int sourceBufferSize = 1, CancellationToken cancellationToken = default)
    {
        var initialBlock = new BufferBlock<TSource1>(new DataflowBlockOptions()
        {
            BoundedCapacity = sourceBufferSize,
            CancellationToken = cancellationToken
        });
        var initialBlock2 = new BufferBlock<TSource2>(new DataflowBlockOptions()
        {
            BoundedCapacity = sourceBufferSize,
            CancellationToken = cancellationToken
        });

        return new FluentBuilder<ISourceBlock<Tuple<TSource1, TSource2>>, DataFlowPipelineBuilder<TSource1, TSource2>>(new DataFlowPipelineBuilder<TSource1, TSource2>(initialBlock, initialBlock2, cancellationToken));
    }
}
public interface IPipelineBuilder<TSource1, TSource2> : IPipelineBuilder<Tuple<TSource1, TSource2>>
{
    ISourceBlock<TSource1> SourceBlock1 { get; }
    ISourceBlock<TSource2> SourceBlock2 { get; }
    new IPipeline<TSource1, TSource2> Build();

}

public class DataFlowPipelineBuilder<TSource1, TSource2> : DataFlowPipelineBuilder<Tuple<TSource1, TSource2>>, IPipelineBuilder<TSource1, TSource2>
{
    public ISourceBlock<TSource1> SourceBlock1 { get; }
    public ISourceBlock<TSource2> SourceBlock2 { get; }
    public DataFlowPipelineBuilder(
         ISourceBlock<TSource1> initialBlock1,
         ISourceBlock<TSource2> initialBlock2,
         CancellationToken cancellationToken) : base(new JoinBlock<TSource1, TSource2>(new GroupingDataflowBlockOptions()
         {

             Greedy = true,
             CancellationToken = cancellationToken
         }), cancellationToken)
    {

        SourceBlock1 = initialBlock1;
        SourceBlock2 = initialBlock2;

        if (SourceBlock is JoinBlock<TSource1, TSource2> joinBlock)
        {
            SourceBlock1.LinkTo(joinBlock.Target1, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
            SourceBlock2.LinkTo(joinBlock.Target2, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
        }
    }

    public new IPipeline<TSource1, TSource2> Build()
    {
        return new DataflowPipeline<TSource1, TSource2>(Blocks.First() as JoinBlock<TSource1, TSource2> ?? null!, Blocks.Last());
    }
}
public partial class DataFlowPipelineBuilder<TSource> : IPipelineBuilder<TSource>
{
    public CancellationToken CancellationToken { get; }
    protected readonly List<IDataflowBlock> Blocks = new();
    public ISourceBlock<TSource> SourceBlock { get; }

    public DataFlowPipelineBuilder(IDataflowBlock initialBlock, CancellationToken cancellationToken)
    {
        SourceBlock = initialBlock as ISourceBlock<TSource> ?? null!;
        if (SourceBlock == null)
        {
            throw new InvalidCastException("initialBlock instance cloud not be casted to ISourceBlock<TSource>");
        }
        Blocks.Add(initialBlock);
        CancellationToken = cancellationToken;
    }

    public void AddPropagatorBlock<TInput, TOutput>(IPropagatorBlock<TInput, TOutput> propagatorBlock)
    {
        if (Blocks.Last() is ISourceBlock<TInput> sourceBlock)
        {
            sourceBlock.LinkTo(propagatorBlock, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
        }
        else
        {
            throw new Exception("Cannot link to a non-source block");
        }
        Blocks.Add(propagatorBlock);
    }

    public void AddTargetBlock<TInput>(ITargetBlock<TInput> targetBlock)
    {
        if (Blocks.Last() is ISourceBlock<TInput> sourceBlock)
        {
            sourceBlock.LinkTo(targetBlock, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
        }
        else
        {
            throw new Exception("Cannot link to a non-source block");
        }
        Blocks.Add(targetBlock);
    }

    public virtual IPipeline<TSource> Build()
    {
        return new DataflowPipeline<TSource>(Blocks.First() as ITargetBlock<TSource> ?? null!, Blocks.Last());
    }

    public IPropagatorBlock<TInput, TInput[]> CreateBatchBlock<TInput>(int batchSize)
    {
        return new BatchBlock<TInput>(batchSize, new GroupingDataflowBlockOptions()
        {
            BoundedCapacity = batchSize,
            Greedy = true,
            CancellationToken = CancellationToken
        });
    }

    public ITargetBlock<TInput> CreateActionBlock<TInput>(Action<TInput> targetAction, int maxDegreeOfParallelism)
    {
        return new ActionBlock<TInput>(input =>
       {
           try
           {
               targetAction(input);
           }
           catch (Exception ex)
           {
               if (!SourceBlock.Completion.IsCompleted)
               {
                   SourceBlock.Fault(ex);
               }
               // To prevent the pipeline from crashing, complete it manually with a faulted state
               throw;
           }
       }, new ExecutionDataflowBlockOptions()
       {
           BoundedCapacity = maxDegreeOfParallelism,
           MaxDegreeOfParallelism = maxDegreeOfParallelism,
           CancellationToken = CancellationToken
       });
    }

    public ITargetBlock<TInput> CreateAscynActionBlock<TInput>(
        Func<TInput, Task> targetAction,
        int maxDegreeOfParallelism)
    {
        return new ActionBlock<TInput>(HandleAsyncAction(targetAction)
        , new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken
        });
    }

    public IPropagatorBlock<TInput, TOutput> CreateTransformBlock<TInput, TOutput>(
     Func<TInput, TOutput> propagatorFunc,
     int maxDegreeOfParallelism)
    {
        return new TransformBlock<TInput, TOutput>(propagatorFunc, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken
        });
    }

    public IPropagatorBlock<TInput, TOutput> CreateAsyncTransformBlock<TInput, TOutput>(
        Func<TInput, Task<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism)
    {
        return new TransformBlock<TInput, TOutput>(HandleAsyncFunction(propagatorFunc)
            ,
            new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = maxDegreeOfParallelism,
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = CancellationToken
            });
    }

    public IPropagatorBlock<TInput, TOutput> CreateTransformManyBlock<TInput, TOutput>(
    Func<TInput, IEnumerable<TOutput>> propagatorFunc,
    int maxDegreeOfParallelism = 1)
    {
        return new TransformManyBlock<TInput, TOutput>(propagatorFunc, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken
        });
    }

    public IPropagatorBlock<TInput, TOutput> CreateAsyncTransformManyBlock<TInput, TOutput>(Func<TInput, Task<IEnumerable<TOutput>>> propagatorFunc, int maxDegreeOfParallelism)
    {
        return new TransformManyBlock<TInput, TOutput>(propagatorFunc, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken
        });
    }

    protected Func<TInput, Task> HandleAsyncAction<TInput>(Func<TInput, Task> asyncAction)
    {
        return async (input) =>
        {
            try
            {
                await asyncAction(input);
            }
            catch (Exception ex)
            {
                if (!SourceBlock.Completion.IsCompleted)
                {
                    SourceBlock.Fault(ex);
                }
                // To prevent the pipeline from crashing, complete it manually with a faulted state
                throw;
            }
        };
    }

    protected Func<TInput, Task<TOutput>> HandleAsyncFunction<TInput, TOutput>(Func<TInput, Task<TOutput>> asyncFunction)
    {
        return async (input) =>
        {
            try
            {
                return await asyncFunction(input);
            }
            catch (Exception ex)
            {
                if (!SourceBlock.Completion.IsCompleted)
                {
                    SourceBlock.Fault(ex);
                }
                // To prevent the pipeline from crashing, complete it manually with a faulted state
                throw;
            }
        };
    }
}