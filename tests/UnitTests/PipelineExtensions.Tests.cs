using System.Threading.Channels;
using DotnetKit.DataflowBuilder;
using DotnetKit.DataflowBuilder.Abstractions;
using FluentAssertions;

namespace DataflowBuilder.UnitTests;

[TestClass]
public class PipelineExtensionsTests
{
    #region SendAllAsync (IAsyncEnumerable)

    [TestMethod]
    public async Task SendAllAsync_IAsyncEnumerable_Should_Throw_When_Pipeline_Is_Null()
    {
        IPipeline<int> pipeline = null!;
        Func<Task> act = () => pipeline.SendAllAsync(EmptyAsync<int>());
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    [TestMethod]
    public async Task SendAllAsync_IAsyncEnumerable_Should_Throw_When_Source_Is_Null()
    {
        var pipeline = CreateCollectingPipeline<int>([]);
        IAsyncEnumerable<int> source = null!;
        Func<Task> act = () => pipeline.SendAllAsync(source);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("source");
    }

    [TestMethod]
    public async Task SendAllAsync_IAsyncEnumerable_Should_Stream_Without_Completing_When_CompleteAfterSend_Is_False()
    {
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);

        await pipeline.SendAllAsync(ToAsyncEnumerable(1, 2, 3), completeAfterSend: false);

        await pipeline.SendAsync(4);
        await pipeline.CompleteAsync();

        received.Should().Equal(1, 2, 3, 4);
    }

    [TestMethod]
    public async Task SendAllAsync_IAsyncEnumerable_Should_Complete_Pipeline_When_CompleteAfterSend_Is_True()
    {
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);

        await pipeline.SendAllAsync(ToAsyncEnumerable(10, 20, 30), completeAfterSend: true);

        received.Should().Equal(10, 20, 30);
    }

    [TestMethod]
    public async Task SendAllAsync_IAsyncEnumerable_Should_Throw_When_Cancellation_Requested()
    {
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => pipeline.SendAllAsync(ToAsyncEnumerable(1, 2, 3), cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region SendAllAsync (IEnumerable)

    [TestMethod]
    public async Task SendAllAsync_IEnumerable_Should_Throw_When_Pipeline_Is_Null()
    {
        IPipeline<int> pipeline = null!;
        Func<Task> act = () => pipeline.SendAllAsync([1, 2, 3]);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    [TestMethod]
    public async Task SendAllAsync_IEnumerable_Should_Throw_When_Source_Is_Null()
    {
        var pipeline = CreateCollectingPipeline<int>([]);
        IEnumerable<int> source = null!;
        Func<Task> act = () => pipeline.SendAllAsync(source);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("source");
    }

    [TestMethod]
    public async Task SendAllAsync_IEnumerable_Should_Stream_All_Items_And_Complete_When_CompleteAfterSend_Is_True()
    {
        var received = new List<string>();
        var pipeline = CreateCollectingPipeline(received);

        await pipeline.SendAllAsync(["alpha", "beta", "gamma"], completeAfterSend: true);

        received.Should().Equal("alpha", "beta", "gamma");
    }

    [TestMethod]
    public async Task SendAllAsync_IEnumerable_Should_Stream_All_Items_Without_Completing_When_CompleteAfterSend_Is_False()
    {
        var received = new List<string>();
        var pipeline = CreateCollectingPipeline(received);

        await pipeline.SendAllAsync(["first", "second"], completeAfterSend: false);

        await pipeline.SendAsync("third");
        await pipeline.CompleteAsync();

        received.Should().Equal("first", "second", "third");
    }

    [TestMethod]
    public async Task SendAllAsync_IEnumerable_Should_Handle_Empty_Sequence()
    {
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);

        await pipeline.SendAllAsync([], completeAfterSend: true);

        received.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SendAllAsync_IEnumerable_Should_Throw_When_Cancellation_Requested()
    {
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => pipeline.SendAllAsync([1, 2, 3], cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region SendFromChannelAsync

    [TestMethod]
    public async Task SendFromChannelAsync_Should_Throw_When_Pipeline_Is_Null()
    {
        IPipeline<string> pipeline = null!;
        var channel = Channel.CreateUnbounded<string>();
        Func<Task> act = () => pipeline.SendFromChannelAsync(channel.Reader);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    [TestMethod]
    public async Task SendFromChannelAsync_Should_Throw_When_ChannelReader_Is_Null()
    {
        var pipeline = CreateCollectingPipeline<string>([]);
        ChannelReader<string> reader = null!;
        Func<Task> act = () => pipeline.SendFromChannelAsync(reader);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("channelReader");
    }

    [TestMethod]
    public async Task SendFromChannelAsync_Should_Stream_Items_And_Complete_When_CompleteAfterSend_Is_True()
    {
        var channel = Channel.CreateUnbounded<int>();
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);

        await channel.Writer.WriteAsync(100);
        await channel.Writer.WriteAsync(200);
        channel.Writer.Complete();

        await pipeline.SendFromChannelAsync(channel.Reader, completeAfterSend: true);

        received.Should().Equal(100, 200);
    }

    [TestMethod]
    public async Task SendFromChannelAsync_Should_Stream_Items_Without_Completing_When_CompleteAfterSend_Is_False()
    {
        var channel = Channel.CreateUnbounded<int>();
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);

        await channel.Writer.WriteAsync(1);
        await channel.Writer.WriteAsync(2);
        channel.Writer.Complete();

        await pipeline.SendFromChannelAsync(channel.Reader, completeAfterSend: false);

        await pipeline.SendAsync(3);
        await pipeline.CompleteAsync();

        received.Should().Equal(1, 2, 3);
    }

    [TestMethod]
    public async Task SendFromChannelAsync_Should_Handle_Empty_Completed_Channel()
    {
        var channel = Channel.CreateUnbounded<int>();
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);

        channel.Writer.Complete();

        await pipeline.SendFromChannelAsync(channel.Reader, completeAfterSend: true);

        received.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SendFromChannelAsync_Should_Throw_When_Cancellation_Requested()
    {
        var channel = Channel.CreateUnbounded<int>();
        var received = new List<int>();
        var pipeline = CreateCollectingPipeline(received);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => pipeline.SendFromChannelAsync(channel.Reader, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Helpers

    private static IPipeline<T> CreateCollectingPipeline<T>(List<T> destination)
    {
        return DataFlowPipelineBuilder
            .FromSource<T>()
            .ToTarget(item => destination.Add(item))
            .Build();
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        await Task.Yield();
        yield break;
    }

    #endregion
}
