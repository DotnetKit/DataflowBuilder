# DataflowBuilder

**DataflowBuilder** is a high-performance, type-safe fluent wrapper for the .NET Task Parallel Library (TPL) Dataflow (`System.Threading.Tasks.Dataflow`). It simplifies building, orchestrating, and testing complex asynchronous data pipelines with built-in concurrency control, backpressure, and zero-leak memory management.

---

## Key Highlights

- **Fluent Type-Safe Pipeline Builder**: Easily compose transformation, batching, grouping, and consumption blocks with strict type inference across pipeline stages.
- **Asynchronous & Concurrent Execution**: Configure degree of parallelism (`maxDegreeOfParallelism`) independently on each block to maximize CPU and I/O efficiency.
- **Multi-Source Streaming Joins**: Ingest data from dual independent sources and join them into coupled tuples for correlated processing.
- **Controlled Backpressure & Memory Bounds**: Configurable buffer capacities prevent memory bloat even when handling massive streaming workloads.
- **Automatic Fault Trapping**: Comprehensive exception handling ensures errors are trapped and propagated across the pipeline to the completion task without crashing unhandled background tasks.
- **Extensible Architecture**: Create custom propagator blocks easily with `BasePropagatorBlock<TInput, TOutput>`.

---

## Quick Example

```csharp
using DotnetKit.DataflowBuilder;

// Build a pipeline: Intake integers -> Batch by 100 -> Process concurrently -> Save to target
var pipeline = DataFlowPipelineBuilder.FromSource<int>(sourceBufferSize: 50)
    .Batch(batchSize: 100)
    .ProcessAsync(async batch =>
    {
        return await ProcessBatchConcurrentlyAsync(batch);
    }, maxDegreeOfParallelism: 4)
    .ToTargetAsync(async result =>
    {
        await Console.Out.WriteLineAsync($"Processed batch with sum: {result.Sum()}");
    }, maxDegreeOfParallelism: 2)
    .Build();

// Stream items into the pipeline
for (var i = 1; i <= 1000; i++)
{
    await pipeline.SendAsync(i);
}

// Complete the pipeline and await all stages to finish
await pipeline.CompleteAsync();
```

---

## Quick Links

- [Getting Started Guide](docs/getting-started.md)
- [Pipeline Concepts](docs/pipeline-concepts.md)
- [Multi-Source Pipelines](docs/multi-source.md)
- [Parallelism & Performance](docs/parallelism-and-performance.md)
- [Error Handling](docs/error-handling.md)
- [Custom Blocks](docs/custom-blocks.md)
- [Architecture](ARCHITECTURE.md)
- [API Reference](xref:DotnetKit.DataflowBuilder)
- [Roadmap](ROADMAP.md)
- [Changelog](CHANGELOG.md)
