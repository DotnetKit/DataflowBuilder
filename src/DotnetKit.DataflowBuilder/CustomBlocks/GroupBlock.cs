using DotnetKit.DataflowBuilder.Abstractions;
using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder.CustomBlocks;

public class GroupBlock<TGroupKey, TInput> : BasePropagatorBlock<TInput, TInput[]>
{
    private ITargetBlock<TInput> _inputBlock;
    private BufferBlock<TInput[]> _outputBlock;

    private readonly IPropagatorBlock<TInput, TInput[]> _pipeline;
    private Dictionary<TGroupKey, List<TInput>> _groups = new();

    public GroupBlock(
        Func<TInput, TGroupKey> GroupingSelector,
        int boundedCapacity = 1,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default)
    {
        _outputBlock = new BufferBlock<TInput[]>(new DataflowBlockOptions()
        {
            BoundedCapacity = boundedCapacity,
            CancellationToken = cancellationToken
        });

        _inputBlock = new ActionBlock<TInput>(async (input) =>
        {
            var groupKey = GroupingSelector(input);

            if (!_groups.ContainsKey(groupKey))
            {
                _groups.Add(groupKey, new List<TInput>(boundedCapacity));
            }
            _groups[groupKey].Add(input);

            if (_groups[groupKey].Count >= boundedCapacity)
            {
                await _outputBlock.SendAsync(_groups[groupKey].ToArray());
                _groups[groupKey].Clear();
                _groups.Remove(groupKey);
            }
        }, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = boundedCapacity,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        });
        _pipeline = DataflowBlock.Encapsulate(_inputBlock, _outputBlock);
    }

    public override ITargetBlock<TInput> Target => _inputBlock;
    public override ISourceBlock<TInput[]> Source => _outputBlock;
}