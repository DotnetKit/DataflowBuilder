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