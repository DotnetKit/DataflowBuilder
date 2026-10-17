# DataflowBuilder

[![Documentation](https://github.com/dotnetkit/dataflowbuilder/actions/workflows/docs.yml/badge.svg)](https://dotnetkit.github.io/DataflowBuilder/)
![internal](https://github.com/dotnetkit/dataflowbuilder/actions/workflows/publish-internal.yml/badge.svg)
![public](https://github.com/dotnetkit/dataflowbuilder/actions/workflows/publish-public.yml/badge.svg)
![Dotnetkit.DataflowBuilder](https://img.shields.io/nuget/v/Dotnetkit.DataflowBuilder)

## Documentation

Full documentation, architecture guides, and API references are available on our [GitHub Pages Documentation Portal](https://dotnetkit.github.io/DataflowBuilder/):

- 📖 [Getting Started Guide](docs/getting-started.md)
- 🧩 [Pipeline Concepts](docs/pipeline-concepts.md)
- 🔀 [Multi-Source Pipelines](docs/multi-source.md)
- ⚡ [Parallelism & Performance](docs/parallelism-and-performance.md)
- 🛡️ [Error Handling & Fault Tolerance](docs/error-handling.md)
- 🧱 [Custom Blocks](docs/custom-blocks.md)
- 🏗️ [Architecture](ARCHITECTURE.md)
- 🗺️ [Roadmap](ROADMAP.md)
- 📝 [Changelog](CHANGELOG.md)

## Overview

`DataflowBuilder` simplifies the creation of dataflow pipelines, making it easier to build, manage, and test complex data processing workflows. With its fluent API, you can quickly set up pipelines that handle various data processing tasks efficiently.

One of the key features of DataflowBuilder is its support for parallel asynchronous tasks executions.

By leveraging dataflow blocks, you can process data concurrently, making efficient use of system resources and improving the performance of your applications.
This is especially useful for CPU-intensive or I/O-bound operations where tasks can benefit from parallelism.

## Features

- Type-safe fluent builder
- Process huge streaming datasets effectively with zero memory leaks
- Implement any asynchronous or synchronous operations (database access, API calls, cloud resource integrations)
- Multi-source extension to implement data streaming joins
- Extensible: add your own block extensions via `BasePropagatorBlock`

## Available blocks

- `FromSource<T>` / `FromSources<T1, T2>` => Entry Source Blocks
- `Process` and `ProcessAsync` => 1-to-1 Transformation blocks (`T` => `T` or `T` => `U`)
- `ProcessMany` and `ProcessManyAsync` => 1-to-N Expansion blocks (`T` => `IEnumerable<U>`)
- `Batch` => Batch input elements (`T` => `T[]`)
- `Group` => Keyed batch grouping (`T` => `T[]`)
- `ToTarget` and `ToTargetAsync` => Terminal action consumer blocks

## What You Can Do with DataflowBuilder ?

`DataflowBuilder` turns complex asynchronous streaming, parallel execution, and backpressure coordination into concise, type-safe pipelines:

| Use Case                         | Solution                                                                                         | Real Example                                                                                                                                                                                                                                            |
| :------------------------------- | :----------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **High-Throughput ETL**          | Filter invalid data, enrich in parallel, batch into chunks, and perform bulk writes.             | `DataFlowPipelineBuilder`<br/>`  .FromSource<RawRecord>()`<br/>`  .Filter(r => r.IsValid)`<br/>`  .ProcessAsync(EnrichAsync, maxDegreeOfParallelism: 8)`<br/>`  .Batch(batchSize: 100)`<br/>`  .ToTargetAsync(BulkInsertAsync)`<br/>`  .Build();`       |
| **Resilient API Calls**          | Process items asynchronously with retry resilience, delay backoff, and fallback callbacks.       | `DataFlowPipelineBuilder`<br/>`  .FromSource<Order>()`<br/>`  .ProcessAsync(ChargeAsync, maxDegreeOfParallelism: 4, retryCount: 3, retryDelay: TimeSpan.FromSeconds(1), onFailed: LogError)`<br/>`  .ToTargetAsync(SendReceiptAsync)`<br/>`  .Build();` |
| **Multi-Stream Reconciliation**  | Join two independent streams with `JoinBlock`, pair items into tuples, and detect discrepancies. | `DataFlowPipelineBuilder`<br/>`  .FromSources<Transaction, Ledger>()`<br/>`  .Process(pair => Reconciler.Match(pair.Item1, pair.Item2))`<br/>`  .Filter(diff => !diff.IsBalanced)`<br/>`  .ToTarget(RecordDiscrepancy)`<br/>`  .Build();`               |
| **Time-Windowed Micro-Batching** | Buffer high-frequency events and flush upon reaching batch capacity OR when timeout expires.     | `DataFlowPipelineBuilder`<br/>`  .FromSource<Metric>()`<br/>`  .Batch(batchSize: 250, timeout: TimeSpan.FromSeconds(5))`<br/>`  .ToTargetAsync(FlushMetricsAsync)`<br/>`  .Build();`                                                                    |
| **Tenant / Keyed Partitioning**  | Group streaming elements by key and emit individual batches when a key reaches capacity.         | `DataFlowPipelineBuilder`<br/>`  .FromSource<AuditEvent>()`<br/>`  .Group(evt => evt.TenantId, boundedCapacity: 50)`<br/>`  .ToTargetAsync(WriteTenantAuditAsync)`<br/>`  .Build();`                                                                    |
| **Channel & Stream Ingestion**   | Ingest bounded/unbounded `ChannelReader<T>` or `IAsyncEnumerable<T>` with auto-completion.       | `var pipeline = DataFlowPipelineBuilder`<br/>`  .FromSource<Log>()`<br/>`  .Process(Sanitize)`<br/>`  .ToChannel(writer)`<br/>`  .Build();`<br/>`await pipeline.SendFromChannelAsync(reader);`                                                          |

## Getting Started

### Installation

To use `DataflowBuilder` in your project, install it via NuGet Package Manager
[DotnetKit.DataflowBuilder NUGET](https://www.nuget.org/packages/DotnetKit.DataflowBuilder)

```bash
Install-Package DotnetKit.DataflowBuilder
```

Or using the .NET CLI:

```bash
dotnet add package DotnetKit.DataflowBuilder
```

### How it works

- Define typed item source and build another blocks with fluent API builder
- Build the pipeline
- Send item

blocks could transform, enrich, group each data (item) sent to the pipeline

#### Sequential pipeline flow

```mermaid
flowchart LR
    Source --> Process1[Enrich] --> Process2[Transform] --> Target
```

#### Pipeline flow with parallelization

```mermaid
flowchart LR
    Source --> Process1[Enrich] --> Process2[Parallel processing] --> Store1
    Process2[Parallel processing] --> Store2
    Process2[Parallel processing] --> Store3
    Store1 --> Target
    Store2 --> Target
    Store3 --> Target
```

### Basic Usage

Here is a simple example to get you started with `DataflowBuilder`.

**Create a Pipeline:**

```csharp
using DotnetKit.DataflowBuilder;
using System.Threading.Tasks;

public async Task RunPipeline()
{
    // build the pipeline
    var pipeline = DataFlowPipelineBuilder.FromSource<int>()
        .Process(a => a * 2)
        .ToTarget(a =>
        {
            Console.WriteLine(a);
        })
        .Build();

    // run the pipeline
    for(var i=1;i<=5;i++)
        {
            await pipeline.SendAsync(1);
        }

    await pipeline.CompleteAsync();
}

```

**Result:**

```bash
2
4
6
8
10
```

### Advanced Usage

`DataflowBuilder` supports advanced operations like batching and grouping.

#### Batching

```csharp
var pipeline = DataFlowPipelineBuilder.FromSource<int>()
    .Batch(5)
    .ToTarget(batch =>
    {
        Console.WriteLine($"Batch received: {string.Join(",", batch)}");
    })
    .Build();

for (int i = 1; i <= 10; i++)
{
    await pipeline.SendAsync(i);
}

await pipeline.CompleteAsync();
```

#### Grouping

```csharp
var pipeline = DataFlowPipelineBuilder.FromSource<char[]>()
    .ProcessMany(chars => chars.GroupBy(c => c).Select(g => g.Key))
    .ToTarget(c =>
    {
        Console.Write(c);
    })
    .Build();

await pipeline.SendAsync("hello world".ToCharArray());
await pipeline.CompleteAsync();
```

#### Multi-source pipeline (JoinBlock implementation)

```csharp
        var expected = new List<string>() { $"HelloWorld-10", $"PingPong-8" };

        var result = new List<string>();
        var pipeline = DataFlowPipelineBuilder.FromSources<string, int>()
            .Process(items => $"{items.Item1}-{items.Item2}")
            .ToTargetAsync(item =>
            {
                result.Add(item);
                return Task.CompletedTask;
            })

            .Build();

        //Order of sending is important for sources
        await pipeline.Send1Async("HelloWorld");
        await pipeline.Send1Async("PingPong");

        await pipeline.Send2Async("HelloWorld".Length);
        await pipeline.Send2Async("PingPong".Length);

        await pipeline.CompleteAsync();

        result.Should().BeEquivalentTo(expected);
```

#### Real world example with parallelization

```mermaid
flowchart TD
    A[Sensor item] -->|Process| B(Enriched sensor)
    B --> |batch| C(process each 1000 items with 3 // tasks )
    C -->|processing task1| D[Bulk insert 1000 sensors]
    C -->|processing task2| E[Bulk insert 1000 sensors]
    C -->|processing task3| F[Bulk insert 1000 sensors]
    D --> |task1 processed| G[Target log event]
    E --> |task2 processed| G[Target ]
    F --> |task3 processed| G[Publish notification]
```

```csharp

//Configuration stage (DI, startup process, or specific lifetime )
//Building sensor bulk insertion pipeline
var pipeline = DataFlowPipelineBuilder.FromSource<SensorEntity>()
    .Process(sensor => sensor.EnrichAsync())
    .Batch(1000)
    .ProcessAsync(async enrichedSensors => {
       await mongoDBClient.BulkInsertAsync(enrichedSensors)
    }, maxDegreeOfParallelism: 3)
    .ToTargetAsync(enrichedSensors =>
    {
     await servicebusDomainTopic.PublishCreatedSensorsNotificationAsync();
    })
    .Build();

//Execution stage (API or Event trigger )
//it could be billion of sensors
await foreach(var sensorPage in GetAllSensorsAsync())
    {
        await _pipeline.SendAsync(sensor);
    }
await _pipeline.CompleteAsync();
```

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## License

This project is licensed under the MIT License.
