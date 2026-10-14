using System.Threading.Tasks.Dataflow;
using DotnetKit.DataflowBuilder.Abstractions;

namespace DotnetKit.DataflowBuilder;

/// <summary>
/// Implements an executable single-source dataflow pipeline encapsulating an ingress target block and an egress destination block.
/// </summary>
/// <typeparam name="TSource">The type of elements accepted by the pipeline entry.</typeparam>
public class DataflowPipeline<TSource> : IPipeline<TSource>
{
    private readonly ITargetBlock<TSource> _sourceBlock;
    private readonly IDataflowBlock _destinationBlock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataflowPipeline{TSource}"/> class.
    /// </summary>
    /// <param name="sourceBlock">The entry target block that receives incoming data items.</param>
    /// <param name="destinationBlock">The final block in the pipeline chain whose completion signals overall pipeline termination.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceBlock"/> or <paramref name="destinationBlock"/> is null.</exception>
    public DataflowPipeline(ITargetBlock<TSource> sourceBlock, IDataflowBlock destinationBlock)
    {
        _sourceBlock = sourceBlock ?? throw new ArgumentNullException(nameof(sourceBlock));
        _destinationBlock = destinationBlock ?? throw new ArgumentNullException(nameof(destinationBlock));
    }

    /// <summary>
    /// Asynchronously sends a data item into the entry block of the pipeline.
    /// </summary>
    /// <param name="dataItem">The item to send.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the item is accepted by the pipeline.</returns>
    public Task SendAsync(TSource dataItem, CancellationToken cancellationToken = default)
        => _sourceBlock.SendAsync(dataItem, cancellationToken);

    /// <summary>
    /// Signals completion to the entry block and waits asynchronously until all pipeline stages have completed.
    /// </summary>
    /// <returns>A task that represents completion of both the source and destination blocks.</returns>
    public Task CompleteAsync()
    {
        _sourceBlock.Complete();
        if (ReferenceEquals(_sourceBlock, _destinationBlock))
        {
            return _sourceBlock.Completion;
        }

        return Task.WhenAll(_sourceBlock.Completion, _destinationBlock.Completion);
    }
}

/// <summary>
/// Implements an executable multi-source dataflow pipeline that receives items from two distinct sources,
/// joining them internally for downstream pipeline processing.
/// </summary>
/// <typeparam name="TSource1">The type of elements accepted by the first source.</typeparam>
/// <typeparam name="TSource2">The type of elements accepted by the second source.</typeparam>
public class DataflowPipeline<TSource1, TSource2> : IPipeline<TSource1, TSource2>
{
    private readonly JoinBlock<TSource1, TSource2> _sourceBlock;
    private readonly IDataflowBlock _destinationBlock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataflowPipeline{TSource1, TSource2}"/> class.
    /// </summary>
    /// <param name="sourceBlock">The join block joining input streams from both sources.</param>
    /// <param name="destinationBlock">The final block in the pipeline chain whose completion signals overall termination.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceBlock"/> or <paramref name="destinationBlock"/> is null.</exception>
    public DataflowPipeline(JoinBlock<TSource1, TSource2> sourceBlock, IDataflowBlock destinationBlock)
    {
        _sourceBlock = sourceBlock ?? throw new ArgumentNullException(nameof(sourceBlock));
        _destinationBlock = destinationBlock ?? throw new ArgumentNullException(nameof(destinationBlock));
    }

    /// <summary>
    /// Asynchronously sends a data item to the first source input.
    /// </summary>
    /// <param name="dataItem">The item to send to the first source.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the item is accepted.</returns>
    public Task Send1Async(TSource1 dataItem, CancellationToken cancellationToken = default)
        => _sourceBlock.Target1.SendAsync(dataItem, cancellationToken);

    /// <summary>
    /// Asynchronously sends a data item to the second source input.
    /// </summary>
    /// <param name="dataItem">The item to send to the second source.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the item is accepted.</returns>
    public Task Send2Async(TSource2 dataItem, CancellationToken cancellationToken = default)
        => _sourceBlock.Target2.SendAsync(dataItem, cancellationToken);

    /// <summary>
    /// Signals completion to both source targets and waits asynchronously for all pipeline stages to complete.
    /// </summary>
    /// <returns>A task representing the overall completion of the pipeline.</returns>
    public Task CompleteAsync()
    {
        _sourceBlock.Target1.Complete();
        _sourceBlock.Target2.Complete();
        _sourceBlock.Complete();

        if (ReferenceEquals(_sourceBlock, _destinationBlock))
        {
            return _sourceBlock.Completion;
        }

        return Task.WhenAll(_sourceBlock.Completion, _destinationBlock.Completion);
    }

    /// <summary>
    /// Sending combined tuples directly is not supported in a multi-source pipeline; use <see cref="Send1Async"/> and <see cref="Send2Async"/> instead.
    /// </summary>
    /// <param name="dataItem">The tuple payload.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="NotSupportedException">Always thrown because inputs must be posted via <see cref="Send1Async"/> and <see cref="Send2Async"/>.</exception>
    public Task SendAsync(Tuple<TSource1, TSource2> dataItem, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use Send1Async and Send2Async instead for multisource pipeline");
    }
}
