# DataflowBuilder Documentation

Welcome to the **DataflowBuilder** documentation hub!

DataflowBuilder is designed to make writing high-throughput, fault-tolerant, and memory-safe asynchronous data processing pipelines in .NET as simple and elegant as writing fluent LINQ queries.

---

## Explore the Guides

- **[Getting Started](getting-started.md)**  
  Install DataflowBuilder, understand basic pipeline construction, and run your first dataflow.

- **[Pipeline Concepts](pipeline-concepts.md)**  
  Learn about the core building blocks: Sources, Propagators, Transformers, Batchers, Groupers, and Targets.

- **[Multi-Source Pipelines](multi-source.md)**  
  Orchestrate dual-source streaming pipelines with automatic tuple joining.

- **[Parallelism & Performance](parallelism-and-performance.md)**  
  Tweak degrees of parallelism, configure buffer sizes, and handle backpressure under heavy workloads.

- **[Error Handling & Faults](error-handling.md)**  
  Understand how exceptions are isolated, how pipeline faulting works, and how cancellation tokens are respected.

- **[Custom Blocks](custom-blocks.md)**  
  Extend the pipeline engine with your own custom propagator blocks using `BasePropagatorBlock`.

- **[Architecture](../ARCHITECTURE.md)**  
  Detailed breakdown of DataflowBuilder's design, memory model, and internal encapsulation of TPL Dataflow blocks.

- **[Roadmap](../ROADMAP.md)**  
  Planned features, enhancements, and ecosystem integrations.

- **[Changelog](../CHANGELOG.md)**  
  Release notes and version history.

- **[API Reference](xref:DotnetKit.DataflowBuilder)**  
  Browse fully documented classes, interfaces, methods, and extensions.
