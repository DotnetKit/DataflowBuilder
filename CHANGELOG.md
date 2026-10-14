# Changelog

All notable changes to **DataflowBuilder** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- Complete XML documentation for all public interfaces, classes, methods, and extensions across `DotnetKit.DataflowBuilder`.
- DocFX-powered static documentation site and API reference.
- GitHub Actions workflow (`docs.yml`) for automated documentation builds and GitHub Pages deployment.
- Architecture guide (`ARCHITECTURE.md`) detailing memory model, block mapping, and fault isolation.
- Comprehensive guides for Getting Started, Pipeline Concepts, Multi-Source Pipelines, Parallelism & Performance, Error Handling, and Custom Blocks.
- Project Roadmap (`ROADMAP.md`).

### Fixed
- Fixed generic type constraint warning `CS8714` on `GroupBlock<TGroupKey, TInput>` and `PipelineBuilderExtensions.Group` by enforcing `where TGroupKey : notnull`.

---

## [1.1.0] - 2024-11-20

### Added
- Multi-source pipeline builder `DataFlowPipelineBuilder.FromSources<T1, T2>()`.
- `IPipeline<TSource1, TSource2>` supporting independent stream ingestion via `Send1Async` and `Send2Async`.
- Keyed batch grouping block `GroupBlock<TGroupKey, TInput>`.
- Parallelism configurations (`maxDegreeOfParallelism`) on transform and target blocks.
- One-to-many projection blocks (`ProcessMany`, `ProcessManyAsync`).

---

## [1.0.0] - 2024-05-15

### Added
- Initial release of `DotnetKit.DataflowBuilder`.
- Fluent pipeline builder for single sources (`FromSource<T>`).
- Synchronous and asynchronous transformations (`Process`, `ProcessAsync`).
- Batching block (`Batch`).
- Terminal action targets (`ToTarget`, `ToTargetAsync`).
- Bounded buffer capacities with backpressure.
- Automatic completion propagation and fault trapping.
