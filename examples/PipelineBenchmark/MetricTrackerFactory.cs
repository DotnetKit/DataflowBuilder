using DotnetKit.MetricFlow;
using DotnetKit.MetricFlow.Counters;
using DotnetKit.MetricFlow.Sinks.Console;

namespace PipelineBenchmark;

internal static class MetricTrackerFactory
{
    internal static MetricTracker CreateMetricTracker()
    {
        return new MetricTracker(
            "DataflowPipelineBenchmark",
            new Dictionary<string, string>
            {
                ["Pipeline"] = "SensorTelemetryProcessing",
                ["Source"] = "InfiniteIEnumerable",
            }
        )
            .AddThroughputCounter()
            .AddMemoryCounter()
            .AddExceptionCounter()
            .AddFailureCounter()
            .AddDimensionCounter("device_id")
            .AddDimensionCounter(
                "alert_status",
                (_, metadata) =>
                {
                    var isAlert = metadata?.GetValueOrDefault("is_alert") ?? 0;
                    return isAlert == 1 ? "Alert" : "Normal";
                }
            )
            .AddConsoleSink(options =>
            {
                options.DisplayMode = ConsoleDisplayMode.Table;
                options.ShowDimensionBreakdown = true;
                options.Colorize = true;
                options.MemoryUnit = MemoryUnit.Auto;
                options.MemoryDecimals = 2;
                options.DurationUnit = DurationUnit.Auto;
                options.ThroughputUnit = "items/s";

                // Color-code duration thresholds (warning > 5ms, critical > 20ms)
                options.AddThreshold<DurationSnapshot>(
                    warn: TimeSpan.FromMilliseconds(5),
                    critical: TimeSpan.FromMilliseconds(20)
                );
            })
            .ConfigureSinkTriggers(triggers =>
            {
                triggers.EmitOnFailure = true;
                triggers.EmitOnSlowDurationThreshold = TimeSpan.FromMilliseconds(50);
            });
    }
}
