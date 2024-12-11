using System.Threading.Tasks.Dataflow;
using DotnetKit.DataflowBuilder.Abstractions;

namespace DotnetKit.DataflowBuilder;

public class DataflowPipeline<TSource> : IPipeline<TSource>
{
    private readonly ITargetBlock<TSource> _sourceBlock;
    private readonly IDataflowBlock _destinationBlock;

    public DataflowPipeline(ITargetBlock<TSource> sourceBlock, IDataflowBlock destinationBlock)
    {
        _sourceBlock = sourceBlock ?? throw new ArgumentNullException(nameof(sourceBlock));
        _destinationBlock = destinationBlock ?? throw new ArgumentNullException(nameof(destinationBlock));
    }

    public async Task SendAsync(TSource dataItem, CancellationToken cancellationToken = default)
    {
        await _sourceBlock.SendAsync(dataItem, cancellationToken);
    }

    public Task CompleteAsync()
    {
        _sourceBlock.Complete();
        return Task.WhenAll(_sourceBlock.Completion, _destinationBlock.Completion);
    }
}

public class DataflowPipeline<TSource1, TSource2> : IPipeline<TSource1, TSource2>
{
    private readonly JoinBlock<TSource1, TSource2> _sourceBlock;
    private readonly IDataflowBlock _destinationBlock;

    public DataflowPipeline(JoinBlock<TSource1, TSource2> sourceBlock, IDataflowBlock destinationBlock)
    {
        _sourceBlock = sourceBlock ?? throw new ArgumentNullException(nameof(sourceBlock));
        _destinationBlock = destinationBlock ?? throw new ArgumentNullException(nameof(destinationBlock));
    }

    public async Task Send1Async(TSource1 dataItem, CancellationToken cancellationToken = default)
    {
        await _sourceBlock.Target1.SendAsync(dataItem, cancellationToken);
    }
    public async Task Send2Async(TSource2 dataItem, CancellationToken cancellationToken = default)
    {
        await _sourceBlock.Target2.SendAsync(dataItem, cancellationToken);
    }

    public Task CompleteAsync()
    {

        _sourceBlock.Target1.Complete();
        _sourceBlock.Target2.Complete();
        _sourceBlock.Complete();

        return Task.WhenAll(_sourceBlock.Completion, _destinationBlock.Completion);
    }

    public Task SendAsync(Tuple<TSource1, TSource2> dataItem, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use Send1Async and Send2Async instead for multisource pipeline");
    }
}
