# Pipeline Performance & Stability Benchmark Example

Demonstrates a high-throughput, sustained data pipeline constructed with **DotnetKit.DataflowBuilder** and continuously monitored using **DotnetKit.MetricFlow (1.0.55-preview.0.1)** to assess **performance**, **memory allocations**, **throughput stability**, and **execution hierarchies**.

---

## Architecture Overview

This example simulates an infinite data ingestion scenario with natural backpressure:

```
[Infinite IEnumerable<SensorMessage>] (TrackStream)
         ↓ (SendAllAsync with backpressure)
[BufferBlock (bounded capacity: 1000)]
         ↓
[Stage 1: Enrichment (TransformBlock, Parallel CPU-bound)]
         ↓
[Stage 2: Quality Filter (FilterBlock with FailureCounter)]
         ↓
[Stage 3: Async Validation (TransformBlock, Parallel Async)]
         ↓
[Stage 4: Dynamic Batcher (BatchBlock, size: 100)]
         ↓
[Stage 5: Terminal Batch Sink (ActionBlock, Parallel Sink)]
```

### Key Components & MetricFlow Features:
- **`InfiniteDataGenerator` & `TrackStream`**: Produces a continuous stream of `SensorMessage` objects, natively monitored via `TrackStream` for stream duration, item yields, and throughput velocity.
- **`SendAllAsync`**: Feeds the sequence into the entry `BufferBlock`. If downstream processing slows down, bounded capacity applies backpressure asynchronously to avoid unbounded queue growth.
- **`MetricTracker` (MetricFlow 1.0.55-preview.0.1)**:
  - **`ConsoleMetricSink` (`AddConsoleSink`)**: Real-time formatted and ANSI colorized console logging directly attached via fluent extensions with duration percentiles (`p50`, `p90`, `p95`, `p99`), configurable units (`MemoryUnit`, `DurationUnit`, `ThroughputUnit`), and timestamps.
  - **Threshold Rules**: Dynamic latency color-coding via `AddThreshold<DurationSnapshot>` (warning in yellow, critical in red).
  - **Execution-Lifecycle Triggers**: Tail sampling triggers via `ConfigureSinkTriggers` emitting snapshots on failure or slow outlier operations (>50ms).
  - **Automatic Periodic Flush**: Background scheduled telemetry flushing with shutdown guarantees via `EnablePeriodicFlush`.
  - **Failure Counter**: Distinguishes logical filter rejections from runtime exceptions via `FailureCounter`.
  - **Hierarchy Counter**: Enables nested execution correlation and hierarchical tree snapshots via `HierarchyCounter` and `ShowHierarchicalTree`.
  - **Memory Tracking**: Real-time current memory monitoring (physical working set and managed heap) alongside per-stage per-operation allocation profiling via `MemoryCounter`.
  - **Throughput & Item Tracking**: Real-time velocity (`items/sec`) and cumulative processed items via `GetTotalItemsProcessed()`.
  - **Dimensional Breakdown**: Categorizes message throughput by `device_id` and computed `alert_status`.
- **Graceful Shutdown**: When the user presses any key (or sends `Ctrl+C`), the generator stops producing items, remaining in-flight items drain completely through all pipeline stages, and `tracker.FlushSinks()` emits the complete telemetry report.

---

## How to Run

Run the benchmark using either .NET 8 or .NET 10:

```bash
# Run with .NET 8
dotnet run --project examples/PipelineBenchmark -f net8.0

# Run with .NET 10
dotnet run --project examples/PipelineBenchmark -f net10.0
```

Press **any key** or **Ctrl+C** to stop the pipeline and display the comprehensive MetricFlow telemetry summary.
