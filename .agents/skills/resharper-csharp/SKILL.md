---
name: resharper-csharp
description: >-
  Provides guidelines, inspection rules, and idioms for writing modern C# adhering to JetBrains ReSharper,
  Roslynator, and .NET compiler code style and quality analyzers. Use this skill when writing, reviewing,
  refactoring, or cleaning up C# code, resolving ReSharper or analyzer warnings, or modernizing .NET codebases.
---

# Modern C# & ReSharper Guidelines Skill

This skill provides guidelines and actionable procedures for developing, reviewing, and refactoring modern C# (.NET 8/9/10, C# 10/12/13) following JetBrains ReSharper, Roslynator, and Microsoft .NET code analysis standards.

---

## 1. Core Idioms & Language Modernization

### File-Scoped Namespaces
Always use file-scoped namespaces to reduce unnecessary indentation:
```csharp
// Prefer
namespace DotnetKit.DataflowBuilder;

// Avoid
namespace DotnetKit.DataflowBuilder
{
}
```

### Primary Constructors
Use primary constructors for classes, records, and readonly structs when constructor parameters map directly to initialization or fields:
```csharp
// Prefer
public class DataflowPipeline<TIn, TOut>(ISourceBlock<TIn> source, ITargetBlock<TOut> target) : IDataflowPipeline<TIn, TOut>
{
    // ...
}

// Avoid
public class DataflowPipeline<TIn, TOut> : IDataflowPipeline<TIn, TOut>
{
    private readonly ISourceBlock<TIn> _source;
    private readonly ITargetBlock<TOut> _target;

    public DataflowPipeline(ISourceBlock<TIn> source, ITargetBlock<TOut> target)
    {
        _source = source;
        _target = target;
    }
}
```

### Collection Expressions
Use C# 12 collection expressions `[]` instead of explicit constructor invocations, `Array.Empty<T>()`, or legacy initializers:
```csharp
// Prefer
IEnumerable<IDataflowBlock> GetBlocks() => [];
List<string> stepNames = ["ingest", "process", "sink"];
int[] bufferSizes = [16, 32, 64];

// Avoid
IEnumerable<IDataflowBlock> GetBlocks() => Array.Empty<IDataflowBlock>();
List<string> stepNames = new List<string> { "ingest", "process", "sink" };
```

### Pattern Matching & Guard Clauses
Prefer modern pattern matching and framework throw helpers:
```csharp
// Prefer
if (obj is null) return;
if (value is not (null or "")) { ... }
if (exception is InvalidOperationException ioe) { ... }
ArgumentNullException.ThrowIfNull(service);
ArgumentException.ThrowIfNullOrWhiteSpace(key);
ObjectDisposedException.ThrowIf(_disposed, this);

// Avoid
if (obj == null) return;
if (!string.IsNullOrEmpty(value)) { ... }
if (service == null) throw new ArgumentNullException(nameof(service));
```

### Target-Typed `new()` & Inferred Types
Use target-typed `new()` when the type is explicit on the left-hand side, or `var` when the type is evident on the right-hand side:
```csharp
// Prefer
private readonly ExecutionDataflowBlockOptions _options = new() { MaxDegreeOfParallelism = 4 };
var cts = new CancellationTokenSource();

// Avoid
private readonly ExecutionDataflowBlockOptions _options = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 4 };
```

### Using Declarations
Prefer `using var` declarations over nested `using (...) { ... }` blocks to minimize nesting:
```csharp
// Prefer
using var stream = File.OpenRead(path);
// work with stream

// Avoid
using (var stream = File.OpenRead(path))
{
    // work with stream
}
```

---

## 2. ReSharper Code Cleanliness & Redundancy Rules

### Redundant Qualifier Elimination
Never qualify namespaces or types when the enclosing namespace or active `using` directives already make the symbol accessible:
```csharp
// Inside namespace DotnetKit.DataflowBuilder:

// Prefer
public IDataflowPipeline AddTransform(TransformBlock<int, string> block);

// Avoid (Flagged by ReSharper: "Qualifier is redundant")
public DotnetKit.DataflowBuilder.IDataflowPipeline AddTransform(System.Threading.Tasks.Dataflow.TransformBlock<int, string> block);
```

### Avoid Default Value Initializations (CA1805)
Do not explicitly initialize fields or auto-properties to their default CLR values (`false`, `0`, `0.0`, `null`, `default`):
```csharp
// Prefer
public bool PropagateCompletion { get; set; }
public int BoundedCapacity { get; set; }

// Avoid (Flagged by CA1805 / ReSharper)
public bool PropagateCompletion { get; set; } = false;
public int BoundedCapacity { get; set; } = 0;
```

### Expression-Bodied Members
Use expression bodies for single-statement methods, simple property getters, and operators:
```csharp
public string Name => name;
public bool IsCompleted => _target.Completion.IsCompleted;
public Task Completion => _target.Completion;
```

### Naming Conventions
- Types and methods: `PascalCase`
- Interfaces: `IPascalCase`
- Private instance fields: `_camelCase`
- Parameters and local variables: `camelCase`
- Constants and static readonly fields: `PascalCase`

---

## 3. Performance, Memory & Async Patterns

### Async & TPL Dataflow Best Practices
- **Library Async**: In reusable library components, always specify `.ConfigureAwait(false)` on awaited tasks unless synchronization context affinity is explicitly required.
- **Cancellation Propagation**: Always accept a `CancellationToken` in async APIs and forward it to downstream async operations and dataflow block options:
  ```csharp
  var options = new ExecutionDataflowBlockOptions
  {
      CancellationToken = cancellationToken,
      BoundedCapacity = boundedCapacity
  };
  ```
- **ValueTask**: Prefer `ValueTask` or `ValueTask<T>` on high-throughput hot paths that frequently complete synchronously (e.g., cached reads, buffer drains).

### Avoid Allocations in Properties (S2365 / CA1859)
Do not instantiate or clone collections inside property getters. Expose read-only views or use explicit methods:
```csharp
// Prefer
public IReadOnlyList<IDataflowBlock> GetBlocks() => _blocks.AsReadOnly();

// Avoid (Flagged by Sonar/ReSharper S2365)
public List<IDataflowBlock> Blocks => new(_blocks);
```

### High-Throughput Scopes (`readonly ref struct` & `in` Parameters)
- Use stack-allocated `readonly ref struct` for tracking and measurement contexts passed into hot paths.
- Pass large readonly structs with the `in` modifier to prevent defensive memory copies.

### LINQ Efficiency
- Avoid multiple enumeration (`CA1851`): materialize query results with `.ToList()` or `.ToArray()` if evaluated repeatedly.
- Use `Length` / `Count` property instead of LINQ `.Count()` where available.
- Prefer `.Any()` over `.Count() > 0`.

---

## 4. ReSharper Suppression Best Practices

When code intentionally deviates from standard conventions (e.g. library reflection bindings, public API backward-compatibility, or framework conventions), suppress explicitly with a scoped reason:

```csharp
/// <summary>
/// Hook invoked implicitly by framework pipeline.
/// </summary>
// ReSharper disable once UnusedMember.Global - Invoked implicitly by consumer pipeline
public async Task ProcessAsync(TIn input)
```

---

## 5. Workflow: Analyzing and Cleaning Code

When instructed to clean up or verify code against ReSharper / Roslyn analyzers:

1. **Format Code**:
   Execute the automated format script:
   ```bash
   ./.agents/skills/resharper-csharp/scripts/clean-code.sh
   ```
   Or run the CLI commands individually:
   ```bash
   dotnet format whitespace
   dotnet format style
   dotnet format analyzers
   ```

2. **Build with Diagnostics**:
   ```bash
   dotnet build -p:EnforceCodeStyleInBuild=true
   ```

3. **Check for Redundant Qualifiers & Code Smells**:
   - Inspect warning messages from `ReSharper`, `Roslynator`, `SonarAnalyzer`, and `CA` rules.
   - Simplify namespaces, eliminate redundant casts, and simplify collection expressions.
   - Verify unit tests:
     ```bash
     dotnet test
     ```
