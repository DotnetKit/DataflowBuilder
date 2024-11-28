# DataflowBuilder

![internal](https://github.com/dotnetkit/dataflowbuilder/actions/workflows/publish-internal.yml/badge.svg)
![public](https://github.com/dotnetkit/dataflowbuilder/actions/workflows/publish-public.yml/badge.svg)
![Dotnetkit.DataflowBuilder](https://img.shields.io/nuget/v/Dotnetkit.DataflowBuilder)

## Overview

`DataflowBuilder` simplifies the creation of dataflow pipelines, making it easier to build, manage, and test complex data processing workflows. With its fluent API, you can quickly set up pipelines that handle various data processing tasks efficiently.

One of the key features of DataflowBuilder is its support for parallel asynchronous tasks executions.

By leveraging dataflow blocks, you can process data concurrently, making efficient use of system resources and improving the performance of your applications.
This is especially useful for CPU-intensive or I/O-bound operations where tasks can benefit from parallelism.

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

`DataflowBuilder` supports advanced operations like batching and grouping and supports async tasks as well.

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
