# Getting Started with DataflowBuilder

This guide walks you through setting up DataflowBuilder in your .NET project and creating your first asynchronous dataflow pipeline.

---

## 1. Installation

Install the package via the .NET CLI:

```bash
dotnet add package DotnetKit.DataflowBuilder
```

Or via the NuGet Package Manager Console in Visual Studio:

```powershell
Install-Package DotnetKit.DataflowBuilder
```

---

## 2. Fundamental Workflow

Every DataflowBuilder pipeline follows a simple lifecycle:

1. **Initialize Source**: Choose a single source (`DataFlowPipelineBuilder.FromSource<T>()`) or dual sources (`FromSources<T1, T2>()`).
2. **Chain Propagator Steps**: Add transformations, batching, or grouping blocks fluently.
3. **Attach Target**: Terminate the pipeline with an action block (`ToTarget` or `ToTargetAsync`).
4. **Build**: Instantiate the executable pipeline with `.Build()`.
5. **Ingest Data**: Push items into the pipeline with `await pipeline.SendAsync(item)`.
6. **Complete & Await**: Signal completion and await overall pipeline completion via `await pipeline.CompleteAsync()`.

---

## 3. Basic Example: Transforming and Consuming Data

Here is a straightforward pipeline that takes integer inputs, multiplies them by 2, and logs the results to the console:

```csharp
using DotnetKit.DataflowBuilder;

// 1. Build the pipeline
var pipeline = DataFlowPipelineBuilder.FromSource<int>(sourceBufferSize: 10)
    .Process(number => number * 2)
    .ToTarget(result =>
    {
        Console.WriteLine($"Result: {result}");
    })
    .Build();

// 2. Feed items into the pipeline
for (int i = 1; i <= 5; i++)
{
    await pipeline.SendAsync(i);
}

// 3. Complete and wait for processing to finish
await pipeline.CompleteAsync();
```

**Output:**
```text
Result: 2
Result: 4
Result: 6
Result: 8
Result: 10
```

---

## 4. Asynchronous Transformations with Parallelism

When calling remote APIs, databases, or CPU-bound tasks, use `ProcessAsync` and configure `maxDegreeOfParallelism`:

```csharp
public async Task ProcessOrdersAsync(IEnumerable<string> orderIds)
{
    var pipeline = DataFlowPipelineBuilder.FromSource<string>(sourceBufferSize: 100)
        .ProcessAsync(async orderId =>
        {
            // Simulate asynchronous database or microservice fetch
            return await OrderService.FetchOrderDetailsAsync(orderId);
        }, maxDegreeOfParallelism: 4) // Process up to 4 orders concurrently
        .ToTargetAsync(async orderDetails =>
        {
            // Simulate asynchronous payment or persistence
            await PaymentGateway.ChargeAsync(orderDetails);
        }, maxDegreeOfParallelism: 2)
        .Build();

    foreach (var id in orderIds)
    {
        await pipeline.SendAsync(id);
    }

    await pipeline.CompleteAsync();
}
```

---

## 5. Batching Elements for Bulk Operations

Database writes and cloud notifications are frequently more efficient when sent in batches:

```csharp
var pipeline = DataFlowPipelineBuilder.FromSource<LogMessage>(sourceBufferSize: 500)
    .Batch(batchSize: 50) // Accumulates messages into LogMessage[] arrays of length <= 50
    .ToTargetAsync(async logBatch =>
    {
        await ElasticSearchClient.BulkIndexAsync(logBatch);
    })
    .Build();

// Send log messages
await pipeline.SendAsync(new LogMessage("User logged in"));
// ...
await pipeline.CompleteAsync();
```

---

## 6. Expanding Items with `ProcessMany`

Use `ProcessMany` or `ProcessManyAsync` when an input item needs to be exploded or mapped to multiple downstream items:

```csharp
var pipeline = DataFlowPipelineBuilder.FromSource<string>()
    // Split each comma-separated line into multiple words
    .ProcessMany(line => line.Split(',', StringSplitOptions.RemoveEmptyEntries))
    // Trim each extracted word
    .Process(word => word.Trim())
    .ToTarget(word =>
    {
        Console.WriteLine($"Word: {word}");
    })
    .Build();

await pipeline.SendAsync("apple, banana, cherry");
await pipeline.CompleteAsync();
```

---

## Next Steps

- Explore [Pipeline Concepts](pipeline-concepts.md) for a deeper understanding of block encapsulation and linking.
- Learn how to join independent event streams with [Multi-Source Pipelines](multi-source.md).
- Optimize throughput and memory using [Parallelism & Performance](parallelism-and-performance.md).
