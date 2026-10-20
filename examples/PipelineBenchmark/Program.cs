using System.Diagnostics;
using DotnetKit.MetricFlow;
using PipelineBenchmark;

Console.WriteLine(
    "================================================================================"
);
Console.WriteLine(
    "        DotnetKit.DataflowBuilder - High-Throughput & Pipeline Benchmark        "
);
Console.WriteLine(
    "================================================================================"
);
Console.WriteLine("  Architecture:");
Console.WriteLine("    [Infinite IEnumerable StreamSource] -> [BufferBlock (capacity: 1000)]");
Console.WriteLine("      -> [Stage 1: Parallel Enrichment (TransformBlock)]");
Console.WriteLine("      -> [Stage 2: Sensor Quality Filter (FilterBlock)]");
Console.WriteLine("      -> [Stage 3: Parallel Async Validation (TransformBlock)]");
Console.WriteLine("      -> [Stage 4: Dynamic Batcher (BatchBlock, size: 100)]");
Console.WriteLine("      -> [Stage 5: Terminal Batch Sink (ActionBlock)]");
Console.WriteLine();
Console.WriteLine("  Observability via MetricFlow v1.0.56:");
Console.WriteLine(
    "    - Live Ticker       : Real-time, flicker-free single-line in-place console monitor"
);
Console.WriteLine("    - Stream Tracking   : Native TrackStream on continuous source stream");
Console.WriteLine(
    "    - Table Display     : Correlated multi-stage tabular view with cell-level thresholds"
);
Console.WriteLine(
    "    - Dimension Trees   : Categorical sub-row breakdown of devices and alert distribution"
);
Console.WriteLine(
    "    - Throughput        : Real-time velocity (items/sec) across all pipeline stages"
);
Console.WriteLine("    - Memory Tracking   : Live RAM/Heap & per-operation allocation efficiency");
Console.WriteLine(
    "    - Failure Tracking  : Exception vs logical failure tracking via FailureCounter"
);
Console.WriteLine(
    "    - Final Sink Report : Comprehensive MetricFlow ConsoleMetricSink report at shutdown"
);
Console.WriteLine();
Console.WriteLine("  Press [ANY KEY] or [Ctrl+C] to stop the pipeline and display results...");
Console.WriteLine(
    "================================================================================"
);

using var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var metricTracker = MetricTrackerFactory.CreateMetricTracker();
using var benchmark = new BenchmarkPipeline(metricTracker);

// Start pipeline
var (pipeline, producerTask) = benchmark.Start(generatorCancellationToken: cancellationToken);

// Background listener for console key press or redirected input
_ = Task.Run(() =>
{
    try
    {
        if (Console.IsInputRedirected)
        {
            Console.ReadLine();
        }
        else
        {
            Console.ReadKey(intercept: true);
        }
    }
    catch
    {
        // Gracefully ignore console reading issues
    }
    finally
    {
        cts.Cancel();
    }
});

var initialGen0 = GC.CollectionCount(0);
var initialGen1 = GC.CollectionCount(1);
var initialGen2 = GC.CollectionCount(2);
var stopwatch = Stopwatch.StartNew();
using var currentProcess = Process.GetCurrentProcess();

// Live in-place progress monitor (flicker-free single-line dashboard displaying current memory)
while (!cts.IsCancellationRequested)
{
    try
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    var elapsed = stopwatch.Elapsed;
    var sinkThroughput = benchmark.Tracker.GetThroughputSnapshot("Stage4_BatchSink");
    var totalItems = benchmark.TotalItemsProcessed;
    var currentIps = sinkThroughput?.ItemsPerSecond ?? 0;

    currentProcess.Refresh();
    var currentWorkingSetMb = currentProcess.WorkingSet64 / (1024.0 * 1024.0);
    var currentHeapMb = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0);

    Console.Write(
        $"\r[Elapsed: {elapsed:mm\\:ss}] Sunk: {totalItems:N0} | Rate: {currentIps:N0}/s | RAM: {currentWorkingSetMb:N1} MB (Heap: {currentHeapMb:N1} MB)    "
    );
}

Console.WriteLine();
Console.WriteLine("\n[Shutdown initiated] Stopping source generator and flushing pipeline...");

try
{
    await producerTask.ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Normal termination when cancelled
}

// Ensure pipeline drains all in-flight items cleanly
try
{
    await pipeline.CompleteAsync().ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Normal termination if cancelled
}
stopwatch.Stop();

Console.WriteLine("[Shutdown complete] All in-flight items have drained through the pipeline.\n");

var finalGen0 = GC.CollectionCount(0) - initialGen0;
var finalGen1 = GC.CollectionCount(1) - initialGen1;
var finalGen2 = GC.CollectionCount(2) - initialGen2;

currentProcess.Refresh();
var finalWorkingSetMb = currentProcess.WorkingSet64 / (1024.0 * 1024.0);
var peakWorkingSetMb = currentProcess.PeakWorkingSet64 / (1024.0 * 1024.0);
var finalHeapMb = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0);

Console.WriteLine(
    "================================================================================"
);
Console.WriteLine(
    "       FINAL PIPELINE TELEMETRY REPORT (via MetricFlow Console Table Mode)      "
);
Console.WriteLine(
    "================================================================================"
);
benchmark.Tracker.FlushSinks();

Console.WriteLine();
Console.WriteLine(
    "================================================================================"
);
Console.WriteLine(
    "                         RUNTIME STABILITY & GC METRICS                         "
);
Console.WriteLine(
    "================================================================================"
);
Console.WriteLine($"Total Runtime Duration : {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}");
Console.WriteLine($"Total Items Processed  : {benchmark.TotalItemsProcessed:N0}");
Console.WriteLine($"Total Batches Sunk     : {benchmark.TotalBatchesProcessed:N0}");
var overallRate =
    stopwatch.Elapsed.TotalSeconds > 0
        ? (benchmark.TotalItemsProcessed / stopwatch.Elapsed.TotalSeconds)
        : 0;
Console.WriteLine($"Overall Sink Rate      : {overallRate:N0} items/sec");
Console.WriteLine($"Current Managed Heap   : {finalHeapMb:F2} MB");
Console.WriteLine(
    $"Current Working Set    : {finalWorkingSetMb:F2} MB (Peak: {peakWorkingSetMb:F2} MB)"
);
Console.WriteLine(
    $"GC Collections         : Gen0={finalGen0:N0}, Gen1={finalGen1:N0}, Gen2={finalGen2:N0}"
);
Console.WriteLine(
    "================================================================================"
);
