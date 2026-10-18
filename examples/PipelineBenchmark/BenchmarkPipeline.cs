using DotnetKit.DataflowBuilder;
using DotnetKit.DataflowBuilder.Abstractions;
using DotnetKit.MetricFlow;
using DotnetKit.MetricFlow.Counters;
using DotnetKit.MetricFlow.Sinks.Console;

namespace PipelineBenchmark;

/// <summary>
/// Encapsulates the high-throughput, memory, and stability benchmark pipeline orchestrated by DataflowBuilder and instrumented by MetricFlow.
/// </summary>
public sealed class BenchmarkPipeline : IDisposable
{
    private readonly MetricTracker _tracker;
    private long _totalItemsProcessed;
    private long _totalBatchesProcessed;

    /// <summary>
    /// Gets the MetricFlow tracker capturing performance, duration, memory allocations, throughput, and hierarchy.
    /// </summary>
    public MetricTracker Tracker => _tracker;

    /// <summary>
    /// Gets the cumulative count of items that completed all pipeline stages and reached the sink.
    /// </summary>
    public long TotalItemsProcessed => Interlocked.Read(ref _totalItemsProcessed);

    /// <summary>
    /// Gets the cumulative count of batches processed by the sink.
    /// </summary>
    public long TotalBatchesProcessed => Interlocked.Read(ref _totalBatchesProcessed);

    /// <summary>
    /// Initializes a new instance of the <see cref="BenchmarkPipeline"/> class.
    /// </summary>
    public BenchmarkPipeline()
    {
        _tracker = new MetricTracker("DataflowPipelineBenchmark", new Dictionary<string, string>
        {
            ["Pipeline"] = "SensorTelemetryProcessing",
            ["Source"] = "InfiniteIEnumerable"
        })
        .AddThroughputCounter()
        .AddMemoryCounter()
        .AddExceptionCounter()
        .AddFailureCounter()
        .AddDimensionCounter("device_id")
        .AddDimensionCounter("alert_status", (_, metadata) =>
        {
            var isAlert = metadata?.GetValueOrDefault("is_alert") ?? 0;
            return isAlert == 1 ? "Alert" : "Normal";
        })
        .AddConsoleSink(options =>
        {
            options.Colorize = true;
            options.IncludeTimestamp = true;
            options.Prefix = "[PipelineBenchmark]";
            options.MemoryUnit = MemoryUnit.Auto;
            options.MemoryDecimals = 2;
            options.DurationUnit = DurationUnit.Auto;
            options.ThroughputUnit = "items/s";

            // Color-code duration thresholds (warning > 5ms, critical > 20ms)
            options.AddThreshold<DurationSnapshot>(
                warn: TimeSpan.FromMilliseconds(5),
                critical: TimeSpan.FromMilliseconds(20));
        })
        .ConfigureSinkTriggers(triggers =>
        {
            triggers.EmitOnFailure = true;
            triggers.EmitOnSlowDurationThreshold = TimeSpan.FromMilliseconds(50);
        });
    }

    /// <summary>
    /// Constructs and starts the streaming pipeline with backpressure, feeding items from the infinite enumerable generator.
    /// </summary>
    /// <param name="generatorCancellationToken">Cancellation token to signal the infinite generator to stop producing.</param>
    /// <param name="pipelineCancellationToken">Optional cancellation token for hard abortion of pipeline blocks.</param>
    /// <param name="onCheckpoint">Optional callback triggered when milestone item counts are reached.</param>
    /// <returns>A tuple containing the running pipeline and the background producer task.</returns>
    public (IPipeline<SensorMessage> Pipeline, Task ProducerTask) Start(
        CancellationToken generatorCancellationToken,
        CancellationToken pipelineCancellationToken = default,
        Action<long>? onCheckpoint = null)
    {
        var pipeline = DataFlowPipelineBuilder
            .FromSource<SensorMessage>(sourceBufferSize: 1000, cancellationToken: pipelineCancellationToken)

            // Stage 1: CPU-Bound Parallel Enrichment
            .Process(msg =>
            {
                using var scope = _tracker.Track("Stage1_Enrichment", new() { ["device_id"] = msg.DeviceId });
                scope.SetItems(1);

                // Approximate meteorological heat index
                var heatIndex = msg.Temperature + (0.5555 * ((6.11 * Math.Exp(5417.7530 * ((1.0 / 273.16) - (1.0 / (273.15 + msg.Temperature))))) - 10.0));
                var isAlert = msg.Temperature > 38.0 || msg.Humidity > 80.0;
                scope.SetMetadata("is_alert", isAlert ? 1 : 0);

                return new EnrichedMessage(
                    SequenceId: msg.SequenceId,
                    DeviceId: msg.DeviceId,
                    Temperature: msg.Temperature,
                    Humidity: msg.Humidity,
                    HeatIndex: heatIndex,
                    IsAlert: isAlert,
                    EnrichedAt: DateTime.UtcNow,
                    Payload: msg.Payload
                );
            }, maxDegreeOfParallelism: Math.Max(2, Environment.ProcessorCount))

            // Stage 2: Quality Filtering
            .Filter(enriched =>
            {
                using var scope = _tracker.Track("Stage2_QualityFilter");
                scope.SetItems(1);

                // Sanity check: keep readings in valid sensor range (-40°C to +85°C)
                var isValid = enriched.Temperature is >= -40.0 and <= 85.0;
                if (!isValid)
                {
                    scope.SetFailed(true);
                }

                return isValid;
            })

            // Stage 3: Asynchronous Validation (simulating micro-service / I/O latency)
            .ProcessAsync(async enriched =>
            {
                using var scope = _tracker.Track("Stage3_AsyncValidation");
                scope.SetItems(1);

                // Periodic async yield to test async state-machine resumption under load
                if (enriched.SequenceId % 1000 == 0)
                {
                    await Task.Yield();
                }

                return enriched;
            }, maxDegreeOfParallelism: Math.Max(2, Environment.ProcessorCount))

            // Stage 4: Batching items into chunks of 100 for efficient terminal delivery
            .Batch(batchSize: 100)

            // Stage 5: Terminal Target Action (Sink)
            .ToTargetAsync(async batch =>
            {
                using var scope = _tracker.Track("Stage4_BatchSink");
                scope.SetItems(batch.Length);

                var alertCount = 0;
                var tempSum = 0.0;

                for (var i = 0; i < batch.Length; i++)
                {
                    if (batch[i].IsAlert)
                    {
                        alertCount++;
                    }
                    tempSum += batch[i].Temperature;
                }

                _ = new ProcessedBatchResult(
                    BatchNumber: Interlocked.Increment(ref _totalBatchesProcessed),
                    ItemCount: batch.Length,
                    AlertCount: alertCount,
                    AverageTemperature: batch.Length > 0 ? tempSum / batch.Length : 0.0,
                    ProcessedAt: DateTime.UtcNow
                );

                var currentItems = Interlocked.Add(ref _totalItemsProcessed, batch.Length);
                if (onCheckpoint is not null && currentItems % 1_000_000 < batch.Length)
                {
                    onCheckpoint(currentItems);
                }

                // Micro-yield to simulate batched persistence
                if (batch.Length >= 100)
                {
                    await Task.Yield();
                }
            }, maxDegreeOfParallelism: 2)
            .Build();

        // Feed the infinite IEnumerable into the pipeline asynchronously, monitored by TrackStream
        var producerTask = Task.Run(async () =>
        {
            var infiniteStream = InfiniteDataGenerator.Generate(generatorCancellationToken)
                .TrackStream(_tracker, "InfiniteSourceStream");

            try
            {
                await pipeline.SendAllAsync(infiniteStream, completeAfterSend: false, cancellationToken: generatorCancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is triggered
            }
        }, generatorCancellationToken);

        return (pipeline, producerTask);
    }

    /// <summary>
    /// Enables automatic periodic background sink flushes.
    /// </summary>
    /// <param name="interval">The flush interval.</param>
    /// <returns>A disposable token that stops periodic flushes and performs a final flush on disposal.</returns>
    public IDisposable EnablePeriodicFlush(TimeSpan interval) => _tracker.EnablePeriodicFlush(interval);

    /// <summary>
    /// Disposes the underlying tracker.
    /// </summary>
    public void Dispose()
    {
        _tracker.Dispose();
    }
}
