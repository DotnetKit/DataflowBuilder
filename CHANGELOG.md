# Changelog

All notable changes to **DataflowBuilder** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Multi-Source Pipelines**:
  - `DataFlowPipelineBuilder.FromSources<T1, T2>()` builder to ingest and coordinate two independent input streams into a single pipeline.
  - `IPipeline<TSource1, TSource2>` abstraction providing asynchronous `Send1Async` and `Send2Async` ingestion methods with unified completion.
  - Multi-source example project (`examples/MultiBookReaderExample`).
- **Grouping & Projection Blocks**:
  - Keyed batch grouping block `GroupBlock<TGroupKey, TInput>` for aggregating stream elements by key with range-based windowing.
  - One-to-many projection blocks (`ProcessMany`, `ProcessManyAsync`).
  - Configurable source block default buffer sizes and block parallelism (`maxDegreeOfParallelism`).
- **Documentation & Architecture**:
  - Complete XML API documentation comments for all public interfaces, classes, methods, and extensions across `DotnetKit.DataflowBuilder`.
  - DocFX-powered static documentation site and interactive API reference.
  - GitHub Actions workflow (`docs.yml`) for automated documentation builds and GitHub Pages deployment.
  - Architecture guide (`ARCHITECTURE.md`) covering memory model, block mapping, and fault isolation.
  - Guides for Getting Started, Pipeline Concepts, Multi-Source Pipelines, Parallelism & Performance, Error Handling, and Custom Blocks.
  - Project Roadmap (`ROADMAP.md`).
- **Development & Code Quality**:
  - Modern C# & ReSharper development guidelines skill (`.agents/skills/resharper-csharp`) and automated formatting script.
  - Recommended modern `.editorconfig` ruleset.

### Changed
- Target latest LTS runtime (`net8.0`) across all libraries, examples, and test projects.
- Enhanced word splitting behavior and token handling in `BookReaderExample`.

### Performance
- **Zero-Allocation Pipeline Ingestion**: Replaced `async`/`await` state machine wrappers in `SendAsync`, `Send1Async`, and `Send2Async` with direct task returns, eliminating heap state machine allocations on every message sent into the pipeline.
- **Single-Stage Pipeline Completion**: Avoided allocating `Task.WhenAll` promise objects in `CompleteAsync()` when the source and destination blocks are the same block.
- **Synchronization Context Decoupling**: Added `.ConfigureAwait(false)` to asynchronous delegate wrappers in `DataFlowPipelineBuilder` and `GroupBlock` to prevent context switches and avoid deadlock vulnerabilities in UI and single-threaded runtimes.
- **Pre-sized Collections**: Pre-sized remaining batch collections in `GroupBlock` flush routines to prevent array resizing overhead.
- **Socket and Resource Optimization**: Reused `static readonly HttpClient` with decompression in sample services to prevent socket exhaustion.
- **Early-Exit Partitioning**: Added empty collection short-circuit in `EnumerableExtensions.GroupByRange`.

### Fixed
- **GroupBlock Data Loss on Completion**: Fixed silent data loss in `GroupBlock<TGroupKey, TInput>` by automatically flushing buffered partial groups and propagating completion/fault downstream upon input completion.
- **GroupBlock Race Condition**: Synchronized internal dictionary and list state mutations in `GroupBlock<TGroupKey, TInput>` to guarantee thread safety under concurrent parallelism (`maxDegreeOfParallelism > 1`).
- **Group Extension Parameter Inversion**: Decoupled `boundedCapacity` and `maxDegreeOfParallelism` in `PipelineBuilderExtensions.Group` to allow configuring group batch sizes independently from concurrency.
- **BasePropagatorBlock Encapsulation Allocation**: Cached the encapsulated `DataflowBlock.Encapsulate` instance in `BasePropagatorBlock<TInput, TOutput>` to eliminate repeated object allocations per dataflow connector call, and corrected fault propagation on completion.
- **API Method Typo**: Added `CreateAsyncActionBlock` with corrected spelling in `IPipelineBuilder` and `DataFlowPipelineBuilder`, preserving `CreateAscynActionBlock` as an `[Obsolete]` forwarding member for backward compatibility.
- **Inactive Test Suite Activation**: Restored missing `[TestMethod]` attribute on `Should_Build_Pipeline_With_Source_And_Target()` in `DataflowBuilder.Tests.cs`.
- **Generic Type Constraint**: Enforced `where TGroupKey : notnull` on `GroupBlock<TGroupKey, TInput>` and `PipelineBuilderExtensions.Group` to resolve compiler warning `CS8714`.

---

## [1.1.0] - 2024-12-05

### Added
- Initial public release of `DotnetKit.DataflowBuilder`.
- Fluent pipeline builder for single sources (`DataFlowPipelineBuilder.FromSource<T>()`).
- Synchronous and asynchronous block processing stages (`Process`, `ProcessAsync`).
- Batching block support (`Batch`).
- Terminal action targets (`ToTarget`, `ToTargetAsync`).
- Bounded buffer capacities with backpressure support.
- Automatic completion propagation and fault trapping across pipeline blocks.
- Sample project `BookReaderExample` demonstrating pipeline execution.

### Changed
- Removed legacy .NET 5 target in favor of modern .NET.
- Updated package metadata, MIT licensing, and project repository URLs.
