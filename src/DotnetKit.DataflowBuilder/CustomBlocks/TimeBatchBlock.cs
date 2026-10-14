using System.Threading.Tasks.Dataflow;
using DotnetKit.DataflowBuilder.Abstractions;

namespace DotnetKit.DataflowBuilder.CustomBlocks;

/// <summary>
/// A custom propagator block that groups incoming items into batches of up to <c>batchSize</c> items,
/// or flushes earlier if a specified <c>timeout</c> duration has elapsed since the first buffered item.
/// </summary>
/// <typeparam name="T">The type of incoming data items.</typeparam>
public class TimeBatchBlock<T> : BasePropagatorBlock<T, T[]>
{
    private readonly ITargetBlock<T> _inputBlock;
    private readonly BufferBlock<T[]> _outputBlock;
    private readonly List<T> _currentBatch;
    private readonly int _batchSize;
    private readonly TimeSpan _timeout;
    private readonly object _syncLock = new();
    private readonly CancellationTokenSource _cts;
    private Timer? _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeBatchBlock{T}"/> class.
    /// </summary>
    /// <param name="batchSize">The maximum number of items per batch.</param>
    /// <param name="timeout">The maximum time to wait before emitting a partial batch.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="batchSize"/> is less than 1 or <paramref name="timeout"/> is negative.</exception>
    public TimeBatchBlock(
        int batchSize,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout cannot be negative.");

        _batchSize = batchSize;
        _timeout = timeout;
        _currentBatch = new List<T>(batchSize);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _outputBlock = new BufferBlock<T[]>(new DataflowBlockOptions
        {
            BoundedCapacity = batchSize,
            CancellationToken = _cts.Token
        });

        _inputBlock = new ActionBlock<T>(async input =>
        {
            T[]? batchToEmit = null;

            lock (_syncLock)
            {
                if (_currentBatch.Count == 0 && _timeout > TimeSpan.Zero)
                {
                    _timer?.Dispose();
                    _timer = new Timer(OnTimerElapsed, null, _timeout, Timeout.InfiniteTimeSpan);
                }

                _currentBatch.Add(input);

                if (_currentBatch.Count >= _batchSize)
                {
                    _timer?.Dispose();
                    _timer = null;
                    batchToEmit = _currentBatch.ToArray();
                    _currentBatch.Clear();
                }
            }

            if (batchToEmit != null)
            {
                await _outputBlock.SendAsync(batchToEmit).ConfigureAwait(false);
            }
        }, new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = batchSize,
            MaxDegreeOfParallelism = 1,
            CancellationToken = _cts.Token
        });

        _ = FlushOnCompletionAsync();
    }

    private void OnTimerElapsed(object? state)
    {
        T[]? batchToEmit = null;

        lock (_syncLock)
        {
            if (_currentBatch.Count > 0)
            {
                batchToEmit = _currentBatch.ToArray();
                _currentBatch.Clear();
            }
            _timer?.Dispose();
            _timer = null;
        }

        if (batchToEmit != null)
        {
            _ = _outputBlock.SendAsync(batchToEmit);
        }
    }

    private async Task FlushOnCompletionAsync()
    {
        try
        {
            await _inputBlock.Completion.ConfigureAwait(false);

            T[]? remainingBatch = null;
            lock (_syncLock)
            {
                _timer?.Dispose();
                _timer = null;

                if (_currentBatch.Count > 0)
                {
                    remainingBatch = _currentBatch.ToArray();
                    _currentBatch.Clear();
                }
            }

            if (remainingBatch != null)
            {
                await _outputBlock.SendAsync(remainingBatch).ConfigureAwait(false);
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
        lock (_syncLock)
        {
            _timer?.Dispose();
            _timer = null;
        }
        _inputBlock.Fault(exception);
        ((IDataflowBlock)_outputBlock).Fault(exception);
    }

    /// <summary>
    /// Gets the entry target block.
    /// </summary>
    public override ITargetBlock<T> Target => _inputBlock;

    /// <summary>
    /// Gets the egress source block.
    /// </summary>
    public override ISourceBlock<T[]> Source => _outputBlock;
}
