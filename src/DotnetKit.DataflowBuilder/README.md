# DataflowBuilder

## Overview

`DataflowBuilder` simplifies the creation of dataflow pipelines, making it easier to build, manage, and test complex data processing workflows. With its fluent API, you can quickly set up pipelines that handle various data processing tasks efficiently.

One of the key features of DataflowBuilder is its support for parallel asynchronous tasks executions.

By leveraging dataflow blocks, you can process data concurrently, making efficient use of system resources and improving the performance of your applications.
This is especially useful for CPU-intensive or I/O-bound operations where tasks can benefit from parallelism.

## Getting Started

### Installation

To use `DataflowBuilder` in your project, install it via NuGet Package Manager (coming soon)

```bash
Install-Package DataflowBuilder
```

Or using the .NET CLI:

```bash
dotnet add package DataflowBuilder
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
using DataflowBuilder.Core.Pipeline;
using System.Threading.Tasks;

public class Example
{
    public async Task RunPipeline()
    {
        var pipeline = DataFlowPipelineBuilder.FromSource<int>()
            .Process(a => a * 2)
            .ToTarget(a =>
            {
                Console.WriteLine(a);
            })
            .Build();

        await pipeline.SendAsync(10);
        await pipeline.CompleteAsync();
    }
}
```

**Run the Pipeline:**

```csharp
public static async Task Main(string[] args)
{
    var example = new Example();
    await example.RunPipeline();
}
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
