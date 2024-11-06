using System.Threading.Tasks.Dataflow;
using DataflowBuilder.Abstractions;

namespace DataflowBuilder.Core.Pipeline;

public class DataflowPipeline<TSource> : IPipeline<TSource>
{
    private readonly ITargetBlock<TSource> _sourceBlock;
    private readonly IDataflowBlock _destinationBlock;

    public DataflowPipeline(ITargetBlock<TSource> sourceBlock, IDataflowBlock destinationBlock)
    {
        ArgumentNullException.ThrowIfNull(sourceBlock, nameof(sourceBlock));
        ArgumentNullException.ThrowIfNull(destinationBlock, nameof(destinationBlock));

        _sourceBlock = sourceBlock;
        _destinationBlock = destinationBlock;
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