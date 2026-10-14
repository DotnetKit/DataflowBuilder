namespace DotnetKit.DataflowBuilder.Abstractions;

/// <summary>
/// Represents an executable dataflow pipeline that accepts input items of type <typeparamref name="TSource"/>,
/// processes them through interconnected dataflow blocks, and monitors completion across all stages.
/// </summary>
/// <typeparam name="TSource">The type of items accepted by the pipeline source.</typeparam>
public interface IPipeline<TSource>
{
    /// <summary>
    /// Signals the pipeline's input block that no further data will be sent,
    /// and waits asynchronously for all blocks in the pipeline to finish processing.
    /// </summary>
    /// <returns>A task that completes when all pipeline stages have successfully completed or faulted.</returns>
    Task CompleteAsync();

    /// <summary>
    /// Asynchronously sends a data item into the entry block of the pipeline.
    /// </summary>
    /// <param name="dataItem">The data item to post into the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the item has been accepted by the input buffer.</returns>
    Task SendAsync(TSource dataItem, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an executable multi-source dataflow pipeline that joins items from two distinct sources
/// (<typeparamref name="TSource1"/> and <typeparamref name="TSource2"/>) into paired tuples for downstream processing.
/// </summary>
/// <typeparam name="TSource1">The type of items accepted by the first source.</typeparam>
/// <typeparam name="TSource2">The type of items accepted by the second source.</typeparam>
public interface IPipeline<TSource1, TSource2> : IPipeline<Tuple<TSource1, TSource2>>
{
    /// <summary>
    /// Asynchronously sends a data item into the first source input of the pipeline.
    /// </summary>
    /// <param name="dataItem">The data item to send to the first source.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the item has been accepted by the first source buffer.</returns>
    Task Send1Async(TSource1 dataItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a data item into the second source input of the pipeline.
    /// </summary>
    /// <param name="dataItem">The data item to send to the second source.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the item has been accepted by the second source buffer.</returns>
    Task Send2Async(TSource2 dataItem, CancellationToken cancellationToken = default);
}
