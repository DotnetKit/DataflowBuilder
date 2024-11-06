# DataflowBuilder

## Overview

`DataflowBuilder` is a library designed to simplify the creation and management of dataflow pipelines using the TPL Dataflow library. It provides a fluent API to build complex data processing pipelines with ease.

One of the key features of DataflowBuilder is its support for parallel asynchronous task execution.
By leveraging dataflow blocks, you can process data concurrently, making efficient use of system resources and improving the performance of your applications. This is especially useful for CPU-intensive or I/O-bound operations where tasks can benefit from parallelism.

## Getting Started

### Installation

To use `DataflowBuilder` in your project, you need to add the necessary NuGet packages. Ensure you have the following packages installed:

- `Microsoft.Tpl.Dataflow`
- `FluentAssertions` (for testing)
- `MSTest.TestFramework` (for testing)

### Basic Usage

Here is a simple example to get you started with `DataflowBuilder`.

1. **Create a Pipeline:**

```csharp
using DataflowBuilder.Core.Pipeline;
using System.Threading.Tasks;
using System.Linq;

public class Example
{
    public async Task RunPipeline()
    {
        var pipeline = DataFlowPipelineBuilder.FromSource<int[]>()
            .ProjectMany(a => a)
            .ToTarget(a =>
            {
                // Process each item
                Console.WriteLine(a);
                return Task.CompletedTask;
            })
            .Build();

        await pipeline.SendAsync(new int[] { 1, 2, 3, 4, 5 });
        await pipeline.CompleteAsync();
    }
}
```

2. **Run the Pipeline:**

```csharp
public static async Task Main(string[] args)
{
    var example = new Example();
    await example.RunPipeline();
}
```

### Advanced Usage

`DataflowBuilder` supports various advanced operations such as batching, grouping, and handling exceptions.

#### Batching

```csharp
var pipeline = DataFlowPipelineBuilder.FromSource<string>()
    .Batch(2)
    .ToTarget(a =>
    {
        Console.WriteLine(string.Concat(a));
        return Task.CompletedTask;
    })
    .Build();

await pipeline.SendAsync("Hello");
await pipeline.SendAsync("World");
await pipeline.CompleteAsync();
```

#### Grouping

```csharp
var pipeline = DataFlowPipelineBuilder.FromSource<char[]>()
    .ProjectMany(a => a.GroupBy(p => p).Select(g => g.ToList()))
    .ProjectMany(a => a.GroupByRange(3))
    .ToTarget(a =>
    {
        Console.WriteLine(string.Join("", a) + "-");
        return Task.CompletedTask;
    })
    .Build();

await pipeline.SendAsync("fffaasss10abcd264daa".ToArray());
await pipeline.CompleteAsync();
```

### Testing

`DataflowBuilder` includes support for testing using MSTest and FluentAssertions. Here is an example test:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Threading.Tasks;

[TestClass]
public class BuilderTests
{
    [TestMethod]
    public async Task Should_Build_Pipeline_With_Batching_And_Projection()
    {
        var expected = "HelloWorld";
        var result = string.Empty;

        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .Batch(2)
            .Project(a => string.Concat(a))
            .ToTarget(a =>
            {
                result = a;
                return Task.CompletedTask;
            })
            .Build();

        await pipeline.SendAsync("Hello");
        await pipeline.SendAsync("World");
        await pipeline.CompleteAsync();

        result.Should().Be(expected);
    }
}
```

## Conclusion

`DataflowBuilder` simplifies the creation of dataflow pipelines, making it easier to build, manage, and test complex data processing workflows. With its fluent API, you can quickly set up pipelines that handle various data processing tasks efficiently.

```

This documentation provides a simple overview and getting started guide for the 

DataflowBuilder

 project. You can expand it further based on your specific requirements and use cases.

medevod: Create documentation here with simple overview, getting started

GitHub Copilot: Sure, here is the documentation with a simple overview and getting started guide:

```markdown
# DataflowBuilder

## Overview

`DataflowBuilder` is a library designed to simplify the creation and management of dataflow pipelines using the TPL Dataflow library. It provides a fluent API to build complex data processing pipelines with ease.

## Getting Started

### Installation

To use `DataflowBuilder` in your project, install it via NuGet Package Manager:

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

```

medevod: could you generate same response but to be integrated in one markdown file

GitHub Copilot: # DataflowBuilder

## Overview

`DataflowBuilder` is a library designed to simplify the creation and management of dataflow pipelines using the TPL Dataflow library. It provides a fluent API to build complex data processing pipelines with ease.

## Getting Started

### Installation

To use `DataflowBuilder` in your project, install it via NuGet Package Manager:

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
