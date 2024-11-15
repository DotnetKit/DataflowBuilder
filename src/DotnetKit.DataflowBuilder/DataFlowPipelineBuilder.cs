using DotnetKit.DataflowBuilder.Abstractions;
using DotnetKit.DataflowBuilder.Core;
using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder;

public static class DataFlowPipelineBuilder
{
    public static IFluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>> FromSource<TSource>(CancellationToken cancellationToken = default)
    {
        var initialBlock = new BufferBlock<TSource>(new DataflowBlockOptions()
        {
            BoundedCapacity = 1,
            CancellationToken = cancellationToken
        });

        return new FluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>>(new DataFlowPipelineBuilder<TSource>(initialBlock, cancellationToken));
    }
}

public partial class DataFlowPipelineBuilder<TSource> : IPipelineBuilder<TSource>
{
    public CancellationToken CancellationToken { get; }
    private readonly List<IDataflowBlock> _blocks = new();
    public ISourceBlock<TSource> SourceBlock { get; }

    public DataFlowPipelineBuilder(IDataflowBlock initialBlock, CancellationToken cancellationToken)
    {
        SourceBlock = initialBlock as ISourceBlock<TSource> ?? null!;
        if (SourceBlock == null)
        {
            throw new InvalidCastException("initialBlock instance cloud not be casted to ISourceBlock<TSource>");
        }
        _blocks.Add(initialBlock);
        CancellationToken = cancellationToken;
    }

    public void AddPropagatorBlock<TInput, TOutput>(IPropagatorBlock<TInput, TOutput> propagatorBlock)
    {
        if (_blocks.Last() is ISourceBlock<TInput> sourceBlock)
        {
            sourceBlock.LinkTo(propagatorBlock, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
        }
        else
        {
            throw new System.Exception("Cannot link to a non-source block");
        }
        _blocks.Add(propagatorBlock);
    }

    public void AddTargetBlock<TInput>(ITargetBlock<TInput> targetBlock)
    {
        if (_blocks.Last() is ISourceBlock<TInput> sourceBlock)
        {
            sourceBlock.LinkTo(targetBlock, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
        }
        else
        {
            throw new System.Exception("Cannot link to a non-source block");
        }
        _blocks.Add(targetBlock);
    }

    public IPipeline<TSource> Build()
    {
        return new DataflowPipeline<TSource>(_blocks.First() as ITargetBlock<TSource> ?? null!, _blocks.Last());
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