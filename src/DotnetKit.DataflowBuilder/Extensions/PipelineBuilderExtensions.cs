using DotnetKit.DataflowBuilder.Abstractions;
using DotnetKit.DataflowBuilder.Core;
using DotnetKit.DataflowBuilder.CustomBlocks;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder;

/// <summary>
/// Provides fluent extension methods for configuring stages, transformations, batching, grouping, and targets on pipeline builders.
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    /// Appends a synchronous transformation block (<see cref="TransformBlock{TInput, TOutput}"/>) to the pipeline.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type to be transformed.</typeparam>
    /// <typeparam name="TOutput">The transformed element type emitted downstream.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="propagatorFunc">The synchronous transformation function mapping each input to an output.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel processing. Defaults to 1.</param>
    /// <returns>A fluent builder positioned at the output of the new transformation block.</returns>
    /// <example>
    /// <code>
    /// builder.Process(x => x.Trim());
    /// </code>
    /// </example>
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> Process<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, TOutput> propagatorFunc,
        int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateTransformBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends an asynchronous transformation block (<see cref="TransformBlock{TInput, TOutput}"/>) to the pipeline.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type to be transformed.</typeparam>
    /// <typeparam name="TOutput">The transformed element type emitted downstream.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="propagatorFunc">The asynchronous transformation function mapping each input to a task producing an output.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel processing. Defaults to 1.</param>
    /// <returns>A fluent builder positioned at the output of the new transformation block.</returns>
    /// <example>
    /// <code>
    /// builder.ProcessAsync(async id => await repository.GetByIdAsync(id), maxDegreeOfParallelism: 4);
    /// </code>
    /// </example>
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProcessAsync<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, Task<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateAsyncTransformBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends an asynchronous transformation block with configurable retry resilience.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type to be transformed.</typeparam>
    /// <typeparam name="TOutput">The transformed element type emitted downstream.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="propagatorFunc">The asynchronous transformation function.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel processing.</param>
    /// <param name="retryCount">Number of retry attempts upon failure.</param>
    /// <param name="retryDelay">Optional delay between retry attempts.</param>
    /// <param name="onFailed">Optional callback invoked with the item and final exception if all retries are exhausted.</param>
    /// <returns>A fluent builder positioned at the output of the transformation block.</returns>
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProcessAsync<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, Task<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism,
        int retryCount,
        TimeSpan? retryDelay = null,
        Action<TInput, Exception>? onFailed = null)
    {
        Func<TInput, Task<TOutput>> resilientFunc = async input =>
        {
            var attempts = 0;
            while (true)
            {
                try
                {
                    attempts++;
                    return await propagatorFunc(input).ConfigureAwait(false);
                }
                catch (Exception) when (attempts <= retryCount)
                {
                    if (retryDelay.HasValue && retryDelay.Value > TimeSpan.Zero)
                    {
                        await Task.Delay(retryDelay.Value).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    onFailed?.Invoke(input, ex);
                    throw;
                }
            }
        };

        return pipelineBuilder.ProcessAsync(resilientFunc, maxDegreeOfParallelism);
    }

    /// <summary>
    /// Filters pipeline items synchronously, allowing only items that satisfy <paramref name="predicate"/> to pass downstream.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type to filter.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="predicate">A function testing each item for a condition.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel processing. Defaults to 1.</param>
    /// <returns>A fluent builder positioned at the output of the filter stage.</returns>
    public static IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> Filter<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, bool> predicate,
        int maxDegreeOfParallelism = 1)
    {
        return pipelineBuilder.ProcessMany(
            item => predicate(item) ? new[] { item } : Array.Empty<TInput>(),
            maxDegreeOfParallelism);
    }

    /// <summary>
    /// Filters pipeline items asynchronously, allowing only items that satisfy <paramref name="predicate"/> to pass downstream.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type to filter.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="predicate">An asynchronous function testing each item for a condition.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel processing. Defaults to 1.</param>
    /// <returns>A fluent builder positioned at the output of the filter stage.</returns>
    public static IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> FilterAsync<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, Task<bool>> predicate,
        int maxDegreeOfParallelism = 1)
    {
        return pipelineBuilder.ProcessManyAsync<TSource, TInput, TInput>(async item =>
        {
            var matches = await predicate(item).ConfigureAwait(false);
            return matches ? new[] { item } : Array.Empty<TInput>();
        }, maxDegreeOfParallelism);
    }

    /// <summary>
    /// Appends a synchronous one-to-many transformation block (<see cref="TransformManyBlock{TInput, TOutput}"/>) to the pipeline,
    /// expanding each input item into a sequence of output items.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type.</typeparam>
    /// <typeparam name="TOutput">The element type of the expanded sequence.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="propagatorFunc">The function mapping each input item to an enumerable collection of output items.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel processing. Defaults to 1.</param>
    /// <returns>A fluent builder positioned at the output of the new transform-many block.</returns>
    /// <example>
    /// <code>
    /// builder.ProcessMany(line => line.Split(','));
    /// </code>
    /// </example>
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProcessMany<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, IEnumerable<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateTransformManyBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends an asynchronous one-to-many transformation block (<see cref="TransformManyBlock{TInput, TOutput}"/>) to the pipeline,
    /// expanding each input item asynchronously into a sequence of output items.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type.</typeparam>
    /// <typeparam name="TOutput">The element type of the expanded sequence.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="propagatorFunc">The asynchronous function mapping each input item to a task returning an enumerable collection of outputs.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel processing. Defaults to 1.</param>
    /// <returns>A fluent builder positioned at the output of the new transform-many block.</returns>
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProcessManyAsync<TSource, TInput, TOutput>(
       this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
       Func<TInput, Task<IEnumerable<TOutput>>> propagatorFunc,
       int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateAsyncTransformManyBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends a batching block (<see cref="BatchBlock{T}"/>) to the pipeline, grouping incoming elements into arrays of fixed size.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type to be batched.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="batchSize">The maximum number of items per batch. Defaults to 100.</param>
    /// <returns>A fluent builder positioned at the output of the batch block yielding arrays of <typeparamref name="TInput"/>.</returns>
    /// <example>
    /// <code>
    /// builder.Batch(batchSize: 50);
    /// </code>
    /// </example>
    public static IFluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>> Batch<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        int batchSize = 100)
    {
        var block = pipelineBuilder.Current.CreateBatchBlock<TInput>(batchSize);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends a temporal batching block (<see cref="TimeBatchBlock{T}"/>) to the pipeline,
    /// grouping elements into batches of up to <paramref name="batchSize"/> items,
    /// or flushing sooner if <paramref name="timeout"/> expires since the first buffered element.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The incoming element type to be batched.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="batchSize">The maximum number of items per batch.</param>
    /// <param name="timeout">The maximum time to wait before emitting a partial batch.</param>
    /// <returns>A fluent builder yielding arrays of <typeparamref name="TInput"/>.</returns>
    public static IFluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>> Batch<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        int batchSize,
        TimeSpan timeout)
    {
        var block = new TimeBatchBlock<TInput>(batchSize, timeout, pipelineBuilder.Current.CancellationToken);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends a grouping block (<see cref="GroupBlock{TGroupKey, TInput}"/>) to the pipeline, grouping elements by a key selector
    /// and emitting batches when each group's capacity is reached or upon pipeline completion.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TGroupKey">The non-nullable key type used for grouping.</typeparam>
    /// <typeparam name="TInput">The incoming element type to be grouped.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="groupingSelector">A function extracting the grouping key from each element.</param>
    /// <param name="boundedCapacity">The maximum number of items per group before a batch is emitted downstream. Defaults to 100.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism. Defaults to 1.</param>
    /// <returns>A fluent builder positioned at the output of the grouping block yielding arrays of <typeparamref name="TInput"/>.</returns>
    public static IFluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>> Group<TSource, TGroupKey, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, TGroupKey> groupingSelector,
        int boundedCapacity = 100,
        int maxDegreeOfParallelism = 1) where TGroupKey : notnull
    {
        var block = new GroupBlock<TGroupKey, TInput>(
            groupingSelector,
            boundedCapacity,
            maxDegreeOfParallelism,
            pipelineBuilder.Current.CancellationToken);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends an asynchronous terminal action block (<see cref="ActionBlock{TInput}"/>) to consume pipeline items.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The element type consumed by the target block.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="targetAction">The asynchronous action executed for each incoming item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel executions. Defaults to 1.</param>
    /// <returns>A fluent builder configured at the target termination step, ready to call <c>Build()</c>.</returns>
    /// <example>
    /// <code>
    /// builder.ToTargetAsync(async item => await database.SaveAsync(item), maxDegreeOfParallelism: 4);
    /// </code>
    /// </example>
    public static IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> ToTargetAsync<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, Task> targetAction,
        int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateAsyncActionBlock(targetAction, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddTargetBlock(block);
        return new FluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends an asynchronous terminal action block with configurable retry resilience.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The element type consumed by the target block.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="targetAction">The asynchronous action executed for each incoming item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel executions.</param>
    /// <param name="retryCount">Number of retry attempts upon failure.</param>
    /// <param name="retryDelay">Optional delay between retry attempts.</param>
    /// <param name="onFailed">Optional callback invoked with the item and final exception if all retries are exhausted.</param>
    /// <returns>A fluent builder positioned at the target termination step.</returns>
    public static IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> ToTargetAsync<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, Task> targetAction,
        int maxDegreeOfParallelism,
        int retryCount,
        TimeSpan? retryDelay = null,
        Action<TInput, Exception>? onFailed = null)
    {
        Func<TInput, Task> resilientAction = async input =>
        {
            var attempts = 0;
            while (true)
            {
                try
                {
                    attempts++;
                    await targetAction(input).ConfigureAwait(false);
                    return;
                }
                catch (Exception) when (attempts <= retryCount)
                {
                    if (retryDelay.HasValue && retryDelay.Value > TimeSpan.Zero)
                    {
                        await Task.Delay(retryDelay.Value).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    onFailed?.Invoke(input, ex);
                    throw;
                }
            }
        };

        return pipelineBuilder.ToTargetAsync(resilientAction, maxDegreeOfParallelism);
    }

    /// <summary>
    /// Connects the output of the pipeline to a <see cref="ChannelWriter{T}"/>, forwarding all emitted items into the channel.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The element type written to the channel.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="channelWriter">The channel writer receiving items.</param>
    /// <param name="completeChannelOnCompletion">Whether to complete the channel when the pipeline terminates.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel channel writes.</param>
    /// <returns>A fluent builder positioned at the terminal block, ready to call <c>Build()</c>.</returns>
    public static IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> ToChannel<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        ChannelWriter<TInput> channelWriter,
        bool completeChannelOnCompletion = true,
        int maxDegreeOfParallelism = 1)
    {
        if (channelWriter == null) throw new ArgumentNullException(nameof(channelWriter));

        var targetBlock = new ActionBlock<TInput>(async item =>
        {
            await channelWriter.WriteAsync(item, pipelineBuilder.Current.CancellationToken).ConfigureAwait(false);
        }, new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = pipelineBuilder.Current.CancellationToken
        });

        if (completeChannelOnCompletion)
        {
            targetBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    channelWriter.TryComplete(t.Exception);
                }
                else
                {
                    channelWriter.TryComplete();
                }
            }, TaskScheduler.Default);
        }

        pipelineBuilder.Current.AddTargetBlock(targetBlock);
        return new FluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Appends a synchronous terminal action block (<see cref="ActionBlock{TInput}"/>) to consume pipeline items.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The element type consumed by the target block.</typeparam>
    /// <param name="pipelineBuilder">The fluent pipeline builder instance.</param>
    /// <param name="targetAction">The synchronous action executed for each incoming item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallel executions. Defaults to 1.</param>
    /// <returns>A fluent builder configured at the target termination step, ready to call <c>Build()</c>.</returns>
    /// <example>
    /// <code>
    /// builder.ToTarget(item => Console.WriteLine(item));
    /// </code>
    /// </example>
    public static IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> ToTarget<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Action<TInput> targetAction,
        int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateActionBlock(targetAction, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddTargetBlock(block);
        return new FluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    /// <summary>
    /// Builds and finalizes a single-source dataflow pipeline from the configured builder stages.
    /// </summary>
    /// <typeparam name="TSource">The entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The element type consumed at the final target stage.</typeparam>
    /// <param name="pipelineBuilder">The fluent builder terminating at a target block.</param>
    /// <returns>An executable <see cref="IPipeline{TSource}"/> instance.</returns>
    public static IPipeline<TSource> Build<TSource, TInput>(
        this IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder)
    {
        return pipelineBuilder.Current.Build();
    }

    /// <summary>
    /// Builds and finalizes a multi-source dataflow pipeline from the configured builder stages.
    /// </summary>
    /// <typeparam name="TSource1">The first entry element type of the pipeline.</typeparam>
    /// <typeparam name="TSource2">The second entry element type of the pipeline.</typeparam>
    /// <typeparam name="TInput">The element type consumed at the final target stage.</typeparam>
    /// <param name="pipelineBuilder">The fluent builder terminating at a target block.</param>
    /// <returns>An executable <see cref="IPipeline{TSource1, TSource2}"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the underlying builder is not a multi-source builder.</exception>
    public static IPipeline<TSource1, TSource2> Build<TSource1, TSource2, TInput>(
        this IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<Tuple<TSource1, TSource2>>> pipelineBuilder)
    {
        if (pipelineBuilder.Current is DataFlowPipelineBuilder<TSource1, TSource2> joinedPipelineBuilder)
        {
            return joinedPipelineBuilder.Build();
        }
        throw new InvalidOperationException("Tuple inputs could be used only for multi source pipeline");
    }
}
