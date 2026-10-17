using System.Diagnostics;
using DotnetKit.MetricFlow;
using PipelineStabilityExample;

Console.WriteLine("================================================================================");
Console.WriteLine("    DotnetKit.DataflowBuilder - High-Throughput & Stability Benchmark");
Console.WriteLine("================================================================================");
Console.WriteLine("  Architecture:");
Console.WriteLine("    [Infinite IEnumerable Source] -> [BufferBlock (capacity: 1000)]");
Console.WriteLine("      -> [Stage 1: Parallel Enrichment (TransformBlock)]");
Console.WriteLine("      -> [Stage 2: Sensor Quality Filter (FilterBlock)]");
Console.WriteLine("      -> [Stage 3: Parallel Async Validation (TransformBlock)]");
Console.WriteLine("      -> [Stage 4: Dynamic Batcher (BatchBlock, size: 100)]");
Console.WriteLine("      -> [Stage 5: Terminal Batch Sink (ActionBlock)]");
Console.WriteLine();
Console.WriteLine("  Observability via MetricFlow 1.0.54 ConsoleSink:");
Console.WriteLine("    - Latency      : Min / Max / Average execution durations per stage");
Console.WriteLine("    - Throughput   : Real-time velocity (items/sec) across pipeline stages");
Console.WriteLine("    - Memory       : Per-operation allocations & total volume via MemoryCounter");
Console.WriteLine("    - ConsoleSink  : Formatted & colorized ANSI output directly via MetricFlow");
Console.WriteLine();
Console.WriteLine("  Press [ANY KEY] or [Ctrl+C] to stop the pipeline and display results...");
Console.WriteLine("================================================================================");

using var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var benchmark = new StabilityBenchmarkPipeline();

// Start pipeline, delegating milestone telemetry reporting directly to MetricFlow ConsoleSink
var (pipeline, producerTask) = benchmark.Start(
    generatorCancellationToken: cancellationToken,
    onCheckpoint: items =>
    {
        Console.WriteLine($"\n--- [Milestone: {items:N0} items processed] ---");
        benchmark.Tracker.FlushSinks();
    });

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

// Live console progress monitor
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

    // Real-time memory queried from MetricFlow's MemoryCounter
    var totalAllocatedMb = benchmark.Tracker.GetTotalAllocatedBytes() / (1024.0 * 1024.0);
    var sinkMem = benchmark.Tracker.GetMemorySnapshot("Stage4_BatchSink");
    var avgSinkBytes = sinkMem?.AverageAllocatedBytes ?? 0;

    Console.Write(
        $"\r[Elapsed: {elapsed:mm\\:ss}] Sunk: {totalItems:N0} items | Rate: {currentIps:N0} items/sec | MetricFlow Alloc: {totalAllocatedMb:N1} MB (Sink: {avgSinkBytes:N0} B/op)    "
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
using var currentProcess = Process.GetCurrentProcess();
var workingSetMb = currentProcess.WorkingSet64 / (1024.0 * 1024.0);
var peakWorkingSetMb = currentProcess.PeakWorkingSet64 / (1024.0 * 1024.0);

Console.WriteLine("================================================================================");
Console.WriteLine("          FINAL TELEMETRY REPORT (via MetricFlow ConsoleMetricSink)             ");
Console.WriteLine("================================================================================");
benchmark.Tracker.FlushSinks();

Console.WriteLine();
Console.WriteLine("================================================================================");
Console.WriteLine("                         RUNTIME STABILITY & GC METRICS                         ");
Console.WriteLine("================================================================================");
Console.WriteLine($"Total Runtime Duration : {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}");
Console.WriteLine($"Total Items Processed  : {benchmark.TotalItemsProcessed:N0}");
Console.WriteLine($"Total Batches Sunk     : {benchmark.TotalBatchesProcessed:N0}");
var overallRate = stopwatch.Elapsed.TotalSeconds > 0 ? (benchmark.TotalItemsProcessed / stopwatch.Elapsed.TotalSeconds) : 0;
Console.WriteLine($"Overall Sink Rate      : {overallRate:N0} items/sec");
Console.WriteLine($"MetricFlow Total Alloc : {benchmark.Tracker.GetTotalAllocatedBytes() / (1024.0 * 1024.0):N2} MB");
Console.WriteLine($"Current Working Set    : {workingSetMb:F2} MB");
if (peakWorkingSetMb > 0)
{
    Console.WriteLine($"Peak Working Set       : {peakWorkingSetMb:F2} MB");
}
Console.WriteLine($"GC Collections         : Gen0={finalGen0:N0}, Gen1={finalGen1:N0}, Gen2={finalGen2:N0}");
Console.WriteLine("================================================================================");
