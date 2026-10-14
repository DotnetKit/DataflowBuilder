using DotnetKit.DataflowBuilder.Abstractions;
using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder.CustomBlocks;

/// <summary>
/// A custom propagator block that groups incoming items of type <typeparamref name="TInput"/> by a key of type <typeparamref name="TGroupKey"/>
/// and emits them as batches of <typeparamref name="TInput"/>[] once a group reaches its bounded capacity.
/// </summary>
/// <typeparam name="TGroupKey">The type of the key extracted from incoming items used for grouping. Must be non-nullable.</typeparam>
/// <typeparam name="TInput">The type of incoming data items.</typeparam>
public class GroupBlock<TGroupKey, TInput> : BasePropagatorBlock<TInput, TInput[]> where TGroupKey : notnull
{
    private readonly ITargetBlock<TInput> _inputBlock;
    private readonly BufferBlock<TInput[]> _outputBlock;
    private readonly Dictionary<TGroupKey, List<TInput>> _groups = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupBlock{TGroupKey, TInput}"/> class.
    /// </summary>
    /// <param name="groupingSelector">A function to extract the grouping key from each incoming item.</param>
    /// <param name="boundedCapacity">The maximum number of items per group before a batch is emitted downstream.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism for the input processing block.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public GroupBlock(
        Func<TInput, TGroupKey> groupingSelector,
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
            var groupKey = groupingSelector(input);

            if (!_groups.TryGetValue(groupKey, out var list))
            {
                list = new List<TInput>(boundedCapacity);
                _groups.Add(groupKey, list);
            }
            list.Add(input);

            if (list.Count >= boundedCapacity)
            {
                await _outputBlock.SendAsync(list.ToArray());
                list.Clear();
                _groups.Remove(groupKey);
            }
        }, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = boundedCapacity,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        });
    }

    /// <summary>
    /// Gets the entry target block receiving incoming items to be grouped.
    /// </summary>
    public override ITargetBlock<TInput> Target => _inputBlock;

    /// <summary>
    /// Gets the egress source block emitting grouped batches of items.
    /// </summary>
    public override ISourceBlock<TInput[]> Source => _outputBlock;
}