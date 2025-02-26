using DotnetKit.DataflowBuilder;
using FluentAssertions;

namespace DataflowBuilder.UnitTests;

[TestClass]
public class DataflowBuilderMultiSourceTests
{
    [TestMethod]
    public async Task Should_Build_Pipeline_With_Two_Sources()
    {
        const string expected = "Hello-World";
        var target = string.Empty;
        var pipeline = DataFlowPipelineBuilder.FromSources<string, string>()
            .ToTargetAsync(item =>
            {
                target = $"{item.Item1}-{item.Item2}";
                return Task.CompletedTask;
            }).Build();

        await pipeline.Send1Async("Hello");
        await pipeline.Send2Async("World");
        await pipeline.CompleteAsync();

        target.Should().Be(expected);
    }
    [TestMethod]
    public async Task Should_Build_Pipeline_With_Thow_Sources_When_One_Source_Delayed()
    {
        const string expected = "4-8";
        var target = string.Empty;
        var pipeline = DataFlowPipelineBuilder.FromSources<int, int>()
            .ToTargetAsync(item =>
            {
                target = $"{item.Item1}-{item.Item2}";
                return Task.CompletedTask;
            }).Build();
        for (var i = 0; i < 5; i++)
        {
            await Task.WhenAll(
            Task.Delay(1000).ContinueWith(t => pipeline.Send1Async(i)),
            pipeline.Send2Async(i * 2));
        }

        await pipeline.CompleteAsync();

        target.Should().Be(expected);
    }
    [TestMethod]
    public async Task Should_Build_Pipeline_With_Two_Sources_Delayed()
    {
        string[] expected = ["4-8", "3-6", "2-4", "1-2", "0-0"];
        var target = new List<string>();

        var pipeline = DataFlowPipelineBuilder.FromSources<int, int>()
            .ToTargetAsync(item =>
            {
                target.Add($"{item.Item1}-{item.Item2}");
                return Task.CompletedTask;
            }).Build();
        for (var i = 0; i < 5; i++)
        {
            await Task.WhenAll(
            Task.Delay(1000).ContinueWith(t => pipeline.Send1Async(i)),
            Task.Delay(500).ContinueWith(t => pipeline.Send2Async(i * 2)));
        }

        await pipeline.CompleteAsync();

        target.Should().BeEquivalentTo(expected);
    }
    [TestMethod]
    public async Task Should_Build_Pipeline_With_Two_Sources_And_Processing_Block()
    {
        var expected = new List<string>() { $"HelloWorld-10", $"PingPong-8" };
        var result = new List<string>();

        var pipeline = DataFlowPipelineBuilder.FromSources<string, int>()
            .Process(items => $"{items.Item1}-{items.Item2}")
            .ToTargetAsync(item =>
            {
                result.Add(item);
                return Task.CompletedTask;
            })

            .Build();

        //Order of sending is important for sources 
        await pipeline.Send1Async("HelloWorld");
        await pipeline.Send1Async("PingPong");

        await pipeline.Send2Async("HelloWorld".Length);
        await pipeline.Send2Async("PingPong".Length);

        await pipeline.CompleteAsync();

        result.Should().BeEquivalentTo(expected);
    }
}
