# DataflowBuilder Roadmap

This document outlines the strategic direction, upcoming milestones, and planned features for **DataflowBuilder**.

---

## 📍 Current Release: v1.1.x

- [x] Fluent pipeline builder for single-source workflows (`FromSource<T>`)
- [x] Multi-source stream joining (`FromSources<T1, T2>`) with `JoinBlock`
- [x] High-performance transformations (`Process`, `ProcessAsync`, `ProcessMany`, `ProcessManyAsync`)
- [x] Fixed-size batching (`Batch`) and keyed grouping (`Group`)
- [x] Terminal sink integration (`ToTarget`, `ToTargetAsync`)
- [x] Memory safety via bounded capacities and backpressure
- [x] Automated fault isolation and completion propagation
- [x] 100% XML documentation across all exposed public APIs
- [x] Modern DocFX documentation portal with automated GitHub Pages deployment

---

## 🎯 v1.2.0: Telemetry & Multi-Source Expansion

### MetricFlow Integration
- [ ] First-class telemetry integration with [DotnetKit.MetricFlow](https://github.com/DotnetKit/MetricFlow):
  - Block throughput counters (items/sec)
  - Block execution latency percentiles (p50, p95, p99)
  - Active queue depth and buffer saturation tracking
  - Fault and exception counters

### Extended Source Joins
- [ ] 3-way stream joining (`FromSources<T1, T2, T3>()`)
- [ ] N-way heterogeneous stream joining using keyed correlation identifiers

### Time-Based Batching
- [ ] Temporal batch block: Flush batch when `batchSize` is reached OR `maxTimeout` expires (whichever happens first)

---

## 🚀 v1.3.0: Resilience & Modern .NET Ecosystem

### Resilience Policies (Polly Integration)
- [ ] Per-block retry policies with exponential backoff and jitter
- [ ] Circuit breaker support for external I/O blocks (APIs, databases)
- [ ] Per-item timeout policies without cancelling entire pipelines

### Modern Channels Bridge
- [ ] Seamless integration with `System.Threading.Channels`:
  - `FromChannel<T>(ChannelReader<T>)`
  - `ToChannel<T>(ChannelWriter<T>)`
- [ ] `IAsyncEnumerable<T>` streaming adapter (`FromAsyncEnumerable<T>`)

### Distributed Tracing (OpenTelemetry)
- [ ] Native `ActivitySource` instrumentation:
  - Generate OpenTelemetry spans for each stage execution
  - Propagate trace parent and context headers between pipeline stages

---

## 🔮 v2.0.0: Intelligent Scheduling & Zero-Copy Pipelines

### Self-Tuning Concurrency
- [ ] Dynamic concurrency autoscaling: Monitor stage latency and queue pressure to dynamically adjust `MaxDegreeOfParallelism` at runtime.

### Memory Optimization
- [ ] High-throughput zero-copy pipelines leveraging `ReadOnlyMemory<byte>` and `ArrayPool<T>` buffers to minimize garbage collection pauses.

### Pipeline Visualization
- [ ] Runtime topology inspection and graph export (Mermaid / Graphviz DOT) to visualize live pipeline health and item throughput.
