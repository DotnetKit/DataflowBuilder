using System.Threading.Tasks.Dataflow;
using DotnetKit.DataflowBuilder.Abstractions;
using DotnetKit.DataflowBuilder.Core;

namespace DotnetKit.DataflowBuilder;

/// <summary>
/// Provides factory methods for initiating fluent dataflow pipelines from single or multiple data sources.
/// </summary>
public static class DataFlowPipelineBuilder
{
    /// <summary>
    /// Creates a fluent pipeline builder with an initial buffer block accepting items of type <typeparamref name="TSource"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of elements accepted at the entrance of the pipeline.</typeparam>
    /// <param name="sourceBufferSize">The bounded capacity of the entry buffer block. Defaults to 1.</param>
    /// <param name="cancellationToken">An optional token to propagate cancellation across the pipeline.</param>
    /// <returns>A fluent builder interface configured with an <see cref="IPipelineBuilder{TSource}"/> context.</returns>
    /// <example>
    /// <code>
    /// var pipeline = DataFlowPipelineBuilder.FromSource&lt;int&gt;(sourceBufferSize: 10)
    ///     .Process(x =&gt; x * 2)
    ///     .ToTarget(Console.WriteLine)
    ///     .Build();
    /// </code>
    /// </example>
    public static IFluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>> FromSource<TSource>(
        int sourceBufferSize = 1,
        CancellationToken cancellationToken = default)
    {
        var initialBlock = new BufferBlock<TSource>(new DataflowBlockOptions()
        {
            BoundedCapacity = sourceBufferSize,
            CancellationToken = cancellationToken
        });

        return new FluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>>(
            new DataFlowPipelineBuilder<TSource>(initialBlock, cancellationToken));
    }

    /// <summary>
    /// Creates a fluent multi-source pipeline builder with two initial buffer blocks joined into pairs.
    /// </summary>
    /// <typeparam name="TSource1">The type of elements accepted by the first pipeline source.</typeparam>
    /// <typeparam name="TSource2">The type of elements accepted by the second pipeline source.</typeparam>
    /// <param name="sourceBufferSize">The bounded capacity for each source buffer block. Defaults to 1.</param>
    /// <param name="cancellationToken">An optional token to propagate cancellation across the pipeline.</param>
    /// <returns>A fluent builder interface configured with a multi-source builder context.</returns>
    /// <example>
    /// <code>
    /// var pipeline = DataFlowPipelineBuilder.FromSources&lt;string, int&gt;()
    ///     .Process(pair =&gt; $"{pair.Item1}: {pair.Item2}")
    ///     .ToTarget(Console.WriteLine)
    ///     .Build();
    /// </code>
    /// </example>
    public static IFluentBuilder<ISourceBlock<Tuple<TSource1, TSource2>>, DataFlowPipelineBuilder<TSource1, TSource2>> FromSources<TSource1, TSource2>(
        int sourceBufferSize = 1,
        CancellationToken cancellationToken = default)
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

        return new FluentBuilder<ISourceBlock<Tuple<TSource1, TSource2>>, DataFlowPipelineBuilder<TSource1, TSource2>>(
            new DataFlowPipelineBuilder<TSource1, TSource2>(initialBlock, initialBlock2, cancellationToken));
    }
}

/// <summary>
/// Defines a multi-source pipeline builder that joins two inputs (<typeparamref name="TSource1"/> and <typeparamref name="TSource2"/>).
/// </summary>
/// <typeparam name="TSource1">The type of the first source.</typeparam>
/// <typeparam name="TSource2">The type of the second source.</typeparam>
public interface IPipelineBuilder<TSource1, TSource2> : IPipelineBuilder<Tuple<TSource1, TSource2>>
{
    /// <summary>
    /// Gets the source block for the first input stream.
    /// </summary>
    ISourceBlock<TSource1> SourceBlock1 { get; }

    /// <summary>
    /// Gets the source block for the second input stream.
    /// </summary>
    ISourceBlock<TSource2> SourceBlock2 { get; }

    /// <summary>
    /// Builds the multi-source dataflow pipeline.
    /// </summary>
    /// <returns>An executable <see cref="IPipeline{TSource1, TSource2}"/> instance.</returns>
    new IPipeline<TSource1, TSource2> Build();
}

/// <summary>
/// Implements a multi-source dataflow pipeline builder joining two data streams with a <see cref="JoinBlock{T1, T2}"/>.
/// </summary>
/// <typeparam name="TSource1">The type of items in the first stream.</typeparam>
/// <typeparam name="TSource2">The type of items in the second stream.</typeparam>
public class DataFlowPipelineBuilder<TSource1, TSource2> : DataFlowPipelineBuilder<Tuple<TSource1, TSource2>>, IPipelineBuilder<TSource1, TSource2>
{
    /// <summary>
    /// Gets the source block for the first input stream.
    /// </summary>
    public ISourceBlock<TSource1> SourceBlock1 { get; }

    /// <summary>
    /// Gets the source block for the second input stream.
    /// </summary>
    public ISourceBlock<TSource2> SourceBlock2 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataFlowPipelineBuilder{TSource1, TSource2}"/> class.
    /// </summary>
    /// <param name="initialBlock1">The initial source block for the first stream.</param>
    /// <param name="initialBlock2">The initial source block for the second stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation.</param>
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

    /// <summary>
    /// Builds the multi-source dataflow pipeline.
    /// </summary>
    /// <returns>An executable <see cref="IPipeline{TSource1, TSource2}"/> instance.</returns>
    public new IPipeline<TSource1, TSource2> Build()
    {
        return new DataflowPipeline<TSource1, TSource2>(Blocks.First() as JoinBlock<TSource1, TSource2> ?? null!, Blocks.Last());
    }
}

/// <summary>
/// Implements a fluent pipeline builder managing a sequence of linked <see cref="IDataflowBlock"/> instances.
/// </summary>
/// <typeparam name="TSource">The type of elements accepted by the entry block of the pipeline.</typeparam>
public class DataFlowPipelineBuilder<TSource> : IPipelineBuilder<TSource>
{
    /// <summary>
    /// Gets the cancellation token associated with the pipeline.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// The collection of blocks participating in the pipeline.
    /// </summary>
    protected readonly List<IDataflowBlock> Blocks = new();

    /// <summary>
    /// Gets the entry source block of the pipeline.
    /// </summary>
    public ISourceBlock<TSource> SourceBlock { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataFlowPipelineBuilder{TSource}"/> class.
    /// </summary>
    /// <param name="initialBlock">The entry block of the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token governing pipeline lifecycle.</param>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="initialBlock"/> cannot be cast to <see cref="ISourceBlock{TSource}"/>.</exception>
    public DataFlowPipelineBuilder(IDataflowBlock initialBlock, CancellationToken cancellationToken)
    {
        SourceBlock = initialBlock as ISourceBlock<TSource> ?? throw new InvalidCastException("initialBlock instance could not be cast to ISourceBlock<TSource>");
        Blocks.Add(initialBlock);
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Links the current head block to the specified propagator block with completion propagation and appends it to the pipeline.
    /// </summary>
    /// <typeparam name="TInput">The input type expected by the propagator block.</typeparam>
    /// <typeparam name="TOutput">The output type produced by the propagator block.</typeparam>
    /// <param name="propagatorBlock">The propagator block to link and append.</param>
    /// <exception cref="InvalidOperationException">Thrown when the previous block is not a source block.</exception>
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
            throw new InvalidOperationException("Cannot link to a non-source block");
        }
        Blocks.Add(propagatorBlock);
    }

    /// <summary>
    /// Links the current head block to the specified target block with completion propagation and appends it to the pipeline.
    /// </summary>
    /// <typeparam name="TInput">The input type accepted by the target block.</typeparam>
    /// <param name="targetBlock">The target block to link and append.</param>
    /// <exception cref="InvalidOperationException">Thrown when the previous block is not a source block.</exception>
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
            throw new InvalidOperationException("Cannot link to a non-source block");
        }
        Blocks.Add(targetBlock);
    }

    /// <summary>
    /// Builds and encapsulates the configured dataflow blocks into an executable <see cref="IPipeline{TSource}"/>.
    /// </summary>
    /// <returns>An executable dataflow pipeline.</returns>
    public virtual IPipeline<TSource> Build()
    {
        return new DataflowPipeline<TSource>(Blocks.First() as ITargetBlock<TSource> ?? null!, Blocks.Last());
    }

    /// <summary>
    /// Creates a batching block that groups consecutive input items into fixed-size arrays.
    /// </summary>
    /// <typeparam name="TInput">The type of input items.</typeparam>
    /// <param name="batchSize">The maximum number of items in each batch.</param>
    /// <returns>A batching propagator block.</returns>
    public IPropagatorBlock<TInput, TInput[]> CreateBatchBlock<TInput>(int batchSize)
    {
        return new BatchBlock<TInput>(batchSize, new GroupingDataflowBlockOptions()
        {
            BoundedCapacity = batchSize,
            Greedy = true,
            CancellationToken = CancellationToken
        });
    }

    /// <summary>
    /// Creates a synchronous action target block with error trapping that faults the pipeline entry upon uncaught exceptions.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <param name="targetAction">The action executed for each item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel execution.</param>
    /// <returns>A configured action block.</returns>
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

    /// <summary>
    /// Creates an asynchronous action target block with error trapping that faults the pipeline entry upon uncaught exceptions.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <param name="targetAction">The asynchronous action executed for each item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel execution.</param>
    /// <returns>A configured action block.</returns>
    public ITargetBlock<TInput> CreateAsyncActionBlock<TInput>(
        Func<TInput, Task> targetAction,
        int maxDegreeOfParallelism)
    {
        return new ActionBlock<TInput>(HandleAsyncAction(targetAction), new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken
        });
    }

    /// <inheritdoc />
    [Obsolete("Use CreateAsyncActionBlock instead. This method has a typo and will be removed in a future release.")]
    public ITargetBlock<TInput> CreateAscynActionBlock<TInput>(
        Func<TInput, Task> targetAction,
        int maxDegreeOfParallelism)
        => CreateAsyncActionBlock(targetAction, maxDegreeOfParallelism);

    /// <summary>
    /// Creates a synchronous transform block mapping an input item to an output item.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The transformed item type.</typeparam>
    /// <param name="propagatorFunc">The transform function.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel execution.</param>
    /// <returns>A configured transform block.</returns>
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

    /// <summary>
    /// Creates an asynchronous transform block mapping an input item to a task yielding an output item.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The transformed item type.</typeparam>
    /// <param name="propagatorFunc">The asynchronous transform function.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel execution.</param>
    /// <returns>A configured transform block.</returns>
    public IPropagatorBlock<TInput, TOutput> CreateAsyncTransformBlock<TInput, TOutput>(
        Func<TInput, Task<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism)
    {
        return new TransformBlock<TInput, TOutput>(HandleAsyncFunction(propagatorFunc), new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken
        });
    }

    /// <summary>
    /// Creates a synchronous transform-many block expanding each input item into a sequence of output items.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The output sequence element type.</typeparam>
    /// <param name="propagatorFunc">The transform function returning an enumerable of outputs.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel execution.</param>
    /// <returns>A configured transform-many block.</returns>
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

    /// <summary>
    /// Creates an asynchronous transform-many block expanding each input item into an asynchronous sequence of output items.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The output sequence element type.</typeparam>
    /// <param name="propagatorFunc">The asynchronous transform function returning an enumerable of outputs.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel execution.</param>
    /// <returns>A configured transform-many block.</returns>
    public IPropagatorBlock<TInput, TOutput> CreateAsyncTransformManyBlock<TInput, TOutput>(
        Func<TInput, Task<IEnumerable<TOutput>>> propagatorFunc,
        int maxDegreeOfParallelism)
    {
        return new TransformManyBlock<TInput, TOutput>(propagatorFunc, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken
        });
    }

    /// <summary>
    /// Wraps an asynchronous action with exception handling that faults the pipeline entry if an unhandled error occurs.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <param name="asyncAction">The delegate to wrap.</param>
    /// <returns>The wrapped delegate.</returns>
    protected Func<TInput, Task> HandleAsyncAction<TInput>(Func<TInput, Task> asyncAction)
    {
        return async (input) =>
        {
            try
            {
                await asyncAction(input).ConfigureAwait(false);
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

    /// <summary>
    /// Wraps an asynchronous function with exception handling that faults the pipeline entry if an unhandled error occurs.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The output item type.</typeparam>
    /// <param name="asyncFunction">The delegate to wrap.</param>
    /// <returns>The wrapped delegate.</returns>
    protected Func<TInput, Task<TOutput>> HandleAsyncFunction<TInput, TOutput>(Func<TInput, Task<TOutput>> asyncFunction)
    {
        return async (input) =>
        {
            try
            {
                return await asyncFunction(input).ConfigureAwait(false);
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