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
    private readonly object _syncLock = new();

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
            TInput[]? batchToEmit = null;

            lock (_syncLock)
            {
                if (!_groups.TryGetValue(groupKey, out var list))
                {
                    list = new List<TInput>(boundedCapacity);
                    _groups.Add(groupKey, list);
                }
                list.Add(input);

                if (list.Count >= boundedCapacity)
                {
                    batchToEmit = list.ToArray();
                    list.Clear();
                    _groups.Remove(groupKey);
                }
            }

            if (batchToEmit != null)
            {
                await _outputBlock.SendAsync(batchToEmit).ConfigureAwait(false);
            }
        }, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = boundedCapacity,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        });

        _ = FlushOnCompletionAsync();
    }

    /// <summary>
    /// Flushes any buffered groups remaining in the dictionary upon input completion and signals completion downstream.
    /// </summary>
    private async Task FlushOnCompletionAsync()
    {
        try
        {
            await _inputBlock.Completion.ConfigureAwait(false);

            List<TInput[]> remainingBatches = new();
            lock (_syncLock)
            {
                foreach (var (_, list) in _groups)
                {
                    if (list.Count > 0)
                    {
                        remainingBatches.Add(list.ToArray());
                    }
                }
                _groups.Clear();
            }

            foreach (var batch in remainingBatches)
            {
                await _outputBlock.SendAsync(batch).ConfigureAwait(false);
            }

            _outputBlock.Complete();
        }
        catch (Exception ex)
        {
            ((IDataflowBlock)_outputBlock).Fault(ex);
        }
    }

    /// <inheritdoc />
    public override void Complete()
    {
        _inputBlock.Complete();
    }

    /// <inheritdoc />
    public override void Fault(Exception exception)
    {
        _inputBlock.Fault(exception);
        ((IDataflowBlock)_outputBlock).Fault(exception);
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