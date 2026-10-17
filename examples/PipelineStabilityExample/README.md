# Pipeline Stability & Performance Benchmark Example

Demonstrates a high-throughput, sustained data pipeline constructed with **DotnetKit.DataflowBuilder** and continuously monitored using **DotnetKit.MetricFlow** to assess **performance**, **memory allocations**, and **throughput stability**.

---

## Architecture Overview

This example simulates an infinite data ingestion scenario with natural backpressure:

```
[Infinite IEnumerable<SensorMessage>]
         ↓ (SendAllAsync with cancellation)
[BufferBlock (bounded capacity: 1000)]
         ↓
[Stage 1: Enrichment (TransformBlock, Parallel CPU-bound)]
         ↓
[Stage 2: Quality Filter (FilterBlock)]
         ↓
[Stage 3: Async Validation (TransformBlock, Parallel Async)]
         ↓
[Stage 4: Dynamic Batcher (BatchBlock, size: 100)]
         ↓
[Stage 5: Terminal Batch Sink (ActionBlock, Parallel Sink)]
```

### Key Components:
- **`InfiniteDataGenerator`**: Produces a continuous, non-terminating stream of `SensorMessage` objects via `IEnumerable<SensorMessage>` and `yield return` until the cancellation token is signaled.
- **`SendAllAsync`**: Feeds the sequence into the entry `BufferBlock`. If downstream processing slows down, bounded capacity applies backpressure asynchronously to avoid unbounded queue growth.
- **`MetricTracker` (MetricFlow 1.0.54)**:
  - **`ConsoleMetricSink` (`AddConsoleSink`)**: Real-time formatted and ANSI colorized console logging for pipeline metrics with duration percentiles (`p50`, `p90`, `p95`, `p99`), configurable units (`MemoryUnit`, `DurationUnit`, `ThroughputUnit`), and timestamps.
  - **Latency / Duration**: Minimum, maximum, average execution time, and quantiles per stage.
  - **Throughput**: Calculated processing velocity (`items/sec` and operations/sec).
  - **Memory Tracking**: Per-stage heap allocations and total memory consumption via `MemoryCounter`.
  - **Dimensional Breakdown**: Categorizes message throughput and operations by `device_id`.
  - **Exception / Failure Rate**: Captures any unhandled block errors or failures.
- **Graceful Shutdown**: When the user presses any key (or sends `Ctrl+C`), the generator stops producing items, remaining in-flight items drain completely through all pipeline stages, and `tracker.FlushSinks()` emits the complete telemetry report.

---

## How to Run

Run the benchmark using either .NET 8 or .NET 10:

```bash
# Run with .NET 8
dotnet run --project examples/PipelineStabilityExample -f net8.0

# Run with .NET 10
dotnet run --project examples/PipelineStabilityExample -f net10.0
```

Press **any key** or **Ctrl+C** to stop the pipeline and display the comprehensive MetricFlow telemetry summary.
