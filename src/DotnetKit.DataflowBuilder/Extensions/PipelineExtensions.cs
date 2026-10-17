using System.Threading.Channels;
using DotnetKit.DataflowBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace DotnetKit.DataflowBuilder;

/// <summary>
/// Provides extension methods for ingesting data streams into <see cref="IPipeline{TSource}"/> instances.
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Asynchronously streams all elements from an <see cref="IAsyncEnumerable{T}"/> into the pipeline.
    /// </summary>
    /// <typeparam name="TSource">The type of elements accepted by the pipeline.</typeparam>
    /// <param name="pipeline">The target pipeline.</param>
    /// <param name="source">The asynchronous stream of elements to send.</param>
    /// <param name="completeAfterSend">Whether to automatically call <see cref="IPipeline{TSource}.CompleteAsync"/> after all items are dispatched.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when all elements have been sent (and completed, if <paramref name="completeAfterSend"/> is true).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pipeline"/> or <paramref name="source"/> is null.</exception>
    public static async Task SendAllAsync<TSource>(
        this IPipeline<TSource> pipeline,
        IAsyncEnumerable<TSource> source,
        bool completeAfterSend = false,
        CancellationToken cancellationToken = default
    )
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await pipeline.SendAsync(item, cancellationToken).ConfigureAwait(false);
        }

        if (completeAfterSend)
        {
            await pipeline.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously sends all elements from an <see cref="IEnumerable{T}"/> into the pipeline.
    /// </summary>
    /// <typeparam name="TSource">The type of elements accepted by the pipeline.</typeparam>
    /// <param name="pipeline">The target pipeline.</param>
    /// <param name="source">The sequence of elements to send.</param>
    /// <param name="completeAfterSend">Whether to automatically call <see cref="IPipeline{TSource}.CompleteAsync"/> after all items are dispatched.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when all elements have been sent (and completed, if <paramref name="completeAfterSend"/> is true).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pipeline"/> or <paramref name="source"/> is null.</exception>
    public static async Task SendAllAsync<TSource>(
        this IPipeline<TSource> pipeline,
        IEnumerable<TSource> source,
        bool completeAfterSend = false,
        CancellationToken cancellationToken = default
    )
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await pipeline.SendAsync(item, cancellationToken).ConfigureAwait(false);
        }

        if (completeAfterSend)
        {
            await pipeline.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously reads all elements from a <see cref="ChannelReader{T}"/> and sends them into the pipeline until the channel is completed.
    /// </summary>
    /// <typeparam name="TSource">The type of elements accepted by the pipeline.</typeparam>
    /// <param name="pipeline">The target pipeline.</param>
    /// <param name="channelReader">The channel reader producing items.</param>
    /// <param name="completeAfterSend">Whether to automatically complete the pipeline once the channel completes. Defaults to true.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when all channel items have been forwarded to the pipeline.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pipeline"/> or <paramref name="channelReader"/> is null.</exception>
    public static async Task SendFromChannelAsync<TSource>(
        this IPipeline<TSource> pipeline,
        ChannelReader<TSource> channelReader,
        bool completeAfterSend = true,
        CancellationToken cancellationToken = default
    )
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        if (channelReader == null)
            throw new ArgumentNullException(nameof(channelReader));

        while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (channelReader.TryRead(out var item))
            {
                await pipeline.SendAsync(item, cancellationToken).ConfigureAwait(false);
            }
        }

        if (completeAfterSend)
        {
            await pipeline.CompleteAsync().ConfigureAwait(false);
        }
    }
}
