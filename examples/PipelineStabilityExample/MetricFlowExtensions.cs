using DotnetKit.MetricFlow.Abstractions;
using DotnetKit.MetricFlow.Counters;
using DotnetKit.MetricFlow.Sinks.Console;

namespace PipelineStabilityExample;

/// <summary>
/// Provides extension methods for MetricFlow, including ConsoleSink registration and memory aggregation.
/// </summary>
public static class MetricFlowExtensions
{
    /// <summary>
    /// Registers a <see cref="ConsoleMetricSink"/> on the metric tracker using the specified configuration.
    /// </summary>
    /// <typeparam name="TTracker">The metric tracker type.</typeparam>
    /// <param name="tracker">The tracker to attach the console sink to.</param>
    /// <param name="configure">Optional delegate to configure console sink options.</param>
    /// <returns>The same tracker instance for fluent chaining.</returns>
    public static TTracker AddConsoleSink<TTracker>(
        this TTracker tracker,
        Action<ConsoleMetricSinkOptions>? configure = null) where TTracker : MetricTrackerBase
    {
        var options = new ConsoleMetricSinkOptions();
        configure?.Invoke(options);
        tracker.RegisterSink(new ConsoleMetricSink(options));
        return tracker;
    }

    /// <summary>
    /// Computes the aggregate allocated bytes across all pipeline stages instrumented by MetricFlow memory counters.
    /// </summary>
    /// <param name="tracker">The metric tracker instance.</param>
    /// <returns>Total allocated bytes across all active memory snapshots.</returns>
    public static long GetTotalAllocatedBytes(this IMetricTracker tracker)
    {
        return tracker.GetAllSnapshots()
            .OfType<MemorySnapshot>()
            .Sum(snapshot => snapshot.TotalAllocatedBytes);
    }
}
