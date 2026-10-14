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

### Fixed
- Fixed race condition and potential dictionary corruption in `GroupBlock<TGroupKey, TInput>` by synchronizing dictionary mutations under concurrent parallelism (`maxDegreeOfParallelism > 1`).
- Fixed generic type constraint warning `CS8714` on `GroupBlock<TGroupKey, TInput>` and `PipelineBuilderExtensions.Group` by enforcing `where TGroupKey : notnull`.

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
