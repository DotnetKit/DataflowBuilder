# Changelog

All notable changes to **DataflowBuilder** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.1.1] - 2026-10-17

### Added
- **TimeBatchBlock (Temporal Window Batching)**:
  - Added `TimeBatchBlock<T>` supporting time-based flushing or maximum item count batching (`Batch(batchSize, timeout)`).
  - Built-in timer flush mechanism that automatically dispatches buffered items when the configured duration expires, regardless of whether the count threshold is met.
  - Automatic completion and fault propagation to ensure buffered items are cleanly flushed upon pipeline completion.
- **Pipeline Ingestion & Streaming Extensions**:
  - `SendAllAsync`: Asynchronous bulk streaming for `IEnumerable<T>` and `IAsyncEnumerable<T>` collections with cancellation token support.
  - `SendFromChannelAsync`: Seamless bridge from `System.Threading.Channels.ChannelReader<T>` with automatic pipeline completion upon channel exhaustion.
  - `ToChannel`: Direct extraction of pipeline output buffers as an asynchronous `ChannelReader<T>`.
- **Fault Tolerance & Resilience**:
  - `ProcessAsync` overload with retry resilience (`retryCount`, `retryDelay`), providing automatic retries with backoff for transient failures before propagating exceptions.
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
  - Use case reference table in root `README.md` mapping use cases to solutions and real-world examples.
  - Mermaid workflow diagrams and structured input/output sample tables in `BookReaderExample` and `MultiBookReaderExample`.
  - Project Roadmap (`ROADMAP.md`).
- **Test Suite & Code Quality**:
  - Dedicated unit test suite `PipelineExtensions.Tests.cs` bringing `PipelineExtensions` to 100% line and branch test coverage.
  - Multi-source concurrent test suite `DateflowBuilder.MultiSource.Tests.cs`.
  - Modern C# & ReSharper development guidelines skill (`.agents/skills/resharper-csharp`) and automated formatting script.
  - Recommended modern `.editorconfig` ruleset.

### Changed
- Promoted `PipelineExtensions` and `PipelineBuilderExtensions` to the root `DotnetKit.DataflowBuilder` namespace, enabling immediate fluent API discovery without additional `using` statements.
- Targeted latest LTS runtime (`net8.0`) across all libraries, examples, and test projects while preserving `netstandard2.1` compatibility.
- Enhanced word splitting behavior and token handling in `BookReaderExample`.
- Modernized GitHub Actions CI/CD workflows: upgraded public deployment to NuGet Trusted Publishing via GitHub Actions OIDC (`NuGet/login@v1`), migrated internal package deployment to standard `GITHUB_TOKEN` permissions, and standardized MinVer pre-release configuration.

### Performance
- **Zero-Allocation Pipeline Ingestion**: Replaced `async`/`await` state machine wrappers in `SendAsync`, `Send1Async`, and `Send2Async` with direct task returns, eliminating heap state machine allocations on every message sent into the pipeline.
- **Single-Stage Pipeline Completion**: Avoided allocating `Task.WhenAll` promise objects in `CompleteAsync()` when the source and destination blocks are the same block.
- **Synchronization Context Decoupling**: Added `.ConfigureAwait(false)` to asynchronous delegate wrappers in `DataFlowPipelineBuilder` and `GroupBlock` to prevent context switches and avoid deadlock vulnerabilities in UI and single-threaded runtimes.
- **Pre-sized Collections**: Pre-sized remaining batch collections in `GroupBlock` flush routines to prevent array resizing overhead.
- **Socket and Resource Optimization**: Reused `static readonly HttpClient` with decompression in sample services to prevent socket exhaustion.
- **Early-Exit Partitioning**: Added empty collection short-circuit in `EnumerableExtensions.GroupByRange`.

### Fixed
- **Multi-Targeting SDK Publishing Issue**: Added `<IsPublishable>false</IsPublishable>` to class library and unit test projects, resolving `NETSDK1129` errors during `dotnet publish`.
- **Packaging Warnings**: Added `<IsPackable>false</IsPackable>` to example projects to prevent unintentional packaging and `NU5104` warnings.
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

[Unreleased]: https://github.com/DotnetKit/DataflowBuilder/compare/v1.1.1...HEAD
[1.1.1]: https://github.com/DotnetKit/DataflowBuilder/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/DotnetKit/DataflowBuilder/releases/tag/v1.1.0
