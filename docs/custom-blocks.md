# Custom Blocks

DataflowBuilder is designed to be fully extensible. If your workflow requires specialized queuing, deduplication, sliding windows, or custom routing, you can create custom propagator blocks using `BasePropagatorBlock<TInput, TOutput>`.

---

## 1. The `BasePropagatorBlock` Pattern

An `IPropagatorBlock<TInput, TOutput>` acts simultaneously as an `ITargetBlock<TInput>` (receiving data) and an `ISourceBlock<TOutput>` (emitting data).

`BasePropagatorBlock<TInput, TOutput>` encapsulates the boiler-plate coordination by requiring you to implement only two properties:

```csharp
public abstract class BasePropagatorBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>
{
    public abstract ITargetBlock<TInput> Target { get; }
    public abstract ISourceBlock<TOutput> Source { get; }
    public IPropagatorBlock<TInput, TOutput> Pipeline => DataflowBlock.Encapsulate(Target, Source);
    // Automatic pass-through for LinkTo, OfferMessage, ConsumeMessage, Complete, Fault, etc.
}
```

---

## 2. Walkthrough: The Built-in `GroupBlock`

To see how `BasePropagatorBlock` works in practice, examine built-in blocks such as `TimeBatchBlock<T>` (which uses timer-driven flushes) and `GroupBlock<TGroupKey, TInput>`:

```csharp
using DotnetKit.DataflowBuilder.Abstractions;
using System.Threading.Tasks.Dataflow;

public class GroupBlock<TGroupKey, TInput> : BasePropagatorBlock<TInput, TInput[]>
    where TGroupKey : notnull
{
    private readonly ITargetBlock<TInput> _inputBlock;
    private readonly BufferBlock<TInput[]> _outputBlock;
    private readonly Dictionary<TGroupKey, List<TInput>> _groups = new();

    public GroupBlock(
        Func<TInput, TGroupKey> groupingSelector,
        int boundedCapacity = 1,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default)
    {
        _outputBlock = new BufferBlock<TInput[]>(new DataflowBlockOptions
        {
            BoundedCapacity = boundedCapacity,
            CancellationToken = cancellationToken
        });

        _inputBlock = new ActionBlock<TInput>(async input =>
        {
            var key = groupingSelector(input);
            if (!_groups.TryGetValue(key, out var list))
            {
                list = new List<TInput>(boundedCapacity);
                _groups[key] = list;
            }
            list.Add(input);

            if (list.Count >= boundedCapacity)
            {
                await _outputBlock.SendAsync(list.ToArray());
                list.Clear();
                _groups.Remove(key);
            }
        }, new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = boundedCapacity,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        });
    }

    public override ITargetBlock<TInput> Target => _inputBlock;
    public override ISourceBlock<TInput[]> Source => _outputBlock;
}
```

---

## 3. Creating a Custom Filtering Block Example

Suppose you want to create a `FilterBlock<T>` that only allows items passing a predicate to continue downstream:

```csharp
using DotnetKit.DataflowBuilder.Abstractions;
using System.Threading.Tasks.Dataflow;

public class FilterBlock<T> : BasePropagatorBlock<T, T>
{
    private readonly ITargetBlock<T> _target;
    private readonly BufferBlock<T> _source;

    public FilterBlock(Predicate<T> predicate, CancellationToken cancellationToken = default)
    {
        _source = new BufferBlock<T>(new DataflowBlockOptions { CancellationToken = cancellationToken });

        _target = new ActionBlock<T>(async item =>
        {
            if (predicate(item))
            {
                await _source.SendAsync(item);
            }
        }, new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken });
    }

    public override ITargetBlock<T> Target => _target;
    public override ISourceBlock<T> Source => _source;
}
```

---

## 4. Exposing via Fluent Extension Methods

To integrate your custom block into DataflowBuilder's fluent syntax, define an extension method targeting `IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>>`:

```csharp
using DotnetKit.DataflowBuilder;
using DotnetKit.DataflowBuilder.Abstractions;
using DotnetKit.DataflowBuilder.Core;
using System.Threading.Tasks.Dataflow;

public static class CustomPipelineExtensions
{
    public static IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> Filter<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> builder,
        Predicate<TInput> predicate)
    {
        var filterBlock = new FilterBlock<TInput>(predicate, builder.Current.CancellationToken);
        builder.Current.AddPropagatorBlock(filterBlock);
        return new FluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>>(builder.Current);
    }
}
```

Now, developers can use `.Filter(...)` directly within their pipelines:

```csharp
var pipeline = DataFlowPipelineBuilder.FromSource<int>()
    .Filter(x => x % 2 == 0) // Custom block!
    .Process(x => x * 10)
    .ToTarget(Console.WriteLine)
    .Build();
```
