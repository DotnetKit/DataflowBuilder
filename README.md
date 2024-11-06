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
            .Project(a => a * 2)
            .ToTarget(a =>
            {
                Console.WriteLine(a);
                return Task.CompletedTask;
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
        return Task.CompletedTask;
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
    .ProjectMany(chars => chars.GroupBy(c => c).Select(g => g.Key))
    .ToTarget(c =>
    {
        Console.Write(c);
        return Task.CompletedTask;
    })
    .Build();

await pipeline.SendAsync("hello world".ToCharArray());
await pipeline.CompleteAsync();
```

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## License

This project is licensed under the MIT License.
