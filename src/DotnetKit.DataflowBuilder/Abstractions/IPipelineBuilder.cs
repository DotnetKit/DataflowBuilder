using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder.Abstractions;

/// <summary>
/// Defines a typed pipeline builder for pipelines initiated from an input source of type <typeparamref name="TSource"/>.
/// </summary>
/// <typeparam name="TSource">The initial data item type accepted by the pipeline.</typeparam>
public interface IPipelineBuilder<TSource> : IPipelineBuilder
{
    /// <summary>
    /// Gets the initial source block of the pipeline.
    /// </summary>
    ISourceBlock<TSource> SourceBlock { get; }

    /// <summary>
    /// Builds and encapsulates the configured dataflow blocks into an executable <see cref="IPipeline{TSource}"/>.
    /// </summary>
    /// <returns>An executable dataflow pipeline instance.</returns>
    IPipeline<TSource> Build();
}

/// <summary>
/// Defines core operations and factory methods for assembling, configuring, and linking dataflow blocks within a pipeline.
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    /// Gets the cancellation token associated with the pipeline execution lifecycle.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Links the last block in the pipeline to the specified propagator block and registers it as the current head block.
    /// </summary>
    /// <typeparam name="TInput">The input data type expected by the propagator block.</typeparam>
    /// <typeparam name="TOutput">The output data type produced by the propagator block.</typeparam>
    /// <param name="propagatorBlock">The propagator block to append to the pipeline.</param>
    void AddPropagatorBlock<TInput, TOutput>(IPropagatorBlock<TInput, TOutput> propagatorBlock);

    /// <summary>
    /// Links the last block in the pipeline to the specified terminal target block and registers it.
    /// </summary>
    /// <typeparam name="TInput">The input data type accepted by the target block.</typeparam>
    /// <param name="targetBlock">The target block terminating the pipeline.</param>
    void AddTargetBlock<TInput>(ITargetBlock<TInput> targetBlock);

    /// <summary>
    /// Creates an asynchronous transformation propagator block (<see cref="TransformBlock{TInput, TOutput}"/>) with error handling.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The transformed output item type.</typeparam>
    /// <param name="propagatorFunc">The asynchronous transformation function.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent operations allowed.</param>
    /// <returns>A configured propagator block.</returns>
    IPropagatorBlock<TInput, TOutput> CreateAsyncTransformBlock<TInput, TOutput>(
        Func<TInput, Task<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism);

    /// <summary>
    /// Creates a synchronous terminal action block (<see cref="ActionBlock{TInput}"/>) with error handling.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <param name="targetAction">The synchronous consumer action invoked for each incoming item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent tasks allowed.</param>
    /// <returns>A configured target block.</returns>
    ITargetBlock<TInput> CreateActionBlock<TInput>(
        Action<TInput> targetAction,
        int maxDegreeOfParallelism);

    /// <summary>
    /// Creates an asynchronous terminal action block (<see cref="ActionBlock{TInput}"/>) with error handling.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <param name="targetAction">The asynchronous consumer action invoked for each incoming item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent tasks allowed.</param>
    /// <returns>A configured target block.</returns>
    ITargetBlock<TInput> CreateAscynActionBlock<TInput>(
        Func<TInput, Task> targetAction,
        int maxDegreeOfParallelism);

    /// <summary>
    /// Creates a synchronous transformation propagator block (<see cref="TransformBlock{TInput, TOutput}"/>) with error handling.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The transformed output item type.</typeparam>
    /// <param name="propagatorFunc">The synchronous transformation function.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent operations allowed.</param>
    /// <returns>A configured propagator block.</returns>
    IPropagatorBlock<TInput, TOutput> CreateTransformBlock<TInput, TOutput>(
        Func<TInput, TOutput> propagatorFunc,
        int maxDegreeOfParallelism);

    /// <summary>
    /// Creates a synchronous one-to-many transformation propagator block (<see cref="TransformManyBlock{TInput, TOutput}"/>) with error handling.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The individual output item type yielded by the sequence.</typeparam>
    /// <param name="propagatorFunc">The transformation function returning a sequence of output items.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent operations allowed.</param>
    /// <returns>A configured propagator block.</returns>
    IPropagatorBlock<TInput, TOutput> CreateTransformManyBlock<TInput, TOutput>(
        Func<TInput, IEnumerable<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism);

    /// <summary>
    /// Creates an asynchronous one-to-many transformation propagator block (<see cref="TransformManyBlock{TInput, TOutput}"/>) with error handling.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <typeparam name="TOutput">The individual output item type yielded by the sequence.</typeparam>
    /// <param name="propagatorFunc">The asynchronous transformation function returning a sequence of output items.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent operations allowed.</param>
    /// <returns>A configured propagator block.</returns>
    IPropagatorBlock<TInput, TOutput> CreateAsyncTransformManyBlock<TInput, TOutput>(
        Func<TInput, Task<IEnumerable<TOutput>>> propagatorFunc,
        int maxDegreeOfParallelism);

    /// <summary>
    /// Creates a batching propagator block (<see cref="BatchBlock{T}"/>) that groups incoming items into fixed-size arrays.
    /// </summary>
    /// <typeparam name="TInput">The input item type.</typeparam>
    /// <param name="batchSize">The maximum number of items in each batch.</param>
    /// <returns>A configured batching propagator block.</returns>
    IPropagatorBlock<TInput, TInput[]> CreateBatchBlock<TInput>(int batchSize);
}