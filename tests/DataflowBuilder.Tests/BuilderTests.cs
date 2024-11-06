using DataflowBuilder.Core.Pipeline;
using FluentAssertions;

namespace DataflowBuilder.Tests;

[TestClass]
public class BuilderTests
{
    [TestMethod]
    public async Task Should_Build_Pipeline_With_Source_And_Target()
    {
        const string expected = "Hello World";
        var target = string.Empty;
        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .ToTarget(a =>
            {
                target = a;
                return Task.FromResult(a);
            }).Build();

        await pipeline.SendAsync(expected);
        await pipeline.CompleteAsync();

        target.Should().Be(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Batching()
    {
        const string expected = "HelloWorld";
        var target = string.Empty;
        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .Batch(2)
            .ToTarget(a =>
            {
                target = string.Concat(a);
                return Task.FromResult(a);
            })

            .Build();

        await pipeline.SendAsync("Hello");
        await pipeline.SendAsync("World");

        await pipeline.CompleteAsync();

        target.Should().Be(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Projection()
    {
        var expected = "HelloWorld".Split();
        var target = string.Empty.Split();

        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .Project(a => a.Split())
            .ToTarget(a =>
            {
                target = a;
                return Task.FromResult(a);
            })

            .Build();

        await pipeline.SendAsync("HelloWorld");

        await pipeline.CompleteAsync();

        target.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Batching_And_Projection()
    {
        var expected = "HelloWorld";
        var result = string.Empty;

        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .Batch(2)
            .Project(a => string.Concat(a))
            .ToTarget(a =>
            {
                result = a;
                return Task.FromResult(a);
            })

            .Build();

        await pipeline.SendAsync("Hello");
        await pipeline.SendAsync("World");

        await pipeline.CompleteAsync();

        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_ProjectMany()
    {
        var expected = "HelloWorld";
        var result = string.Empty;

        var pipeline = DataFlowPipelineBuilder.FromSource<char[]>()
            .ProjectMany(p => p)
            .ToTarget(a =>
            {
                result = result + a;
                return Task.FromResult(a);
            }).Build();

        await pipeline.SendAsync("HelloWorld".ToArray());

        await pipeline.CompleteAsync();

        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task Should_Throw_And_Stop_Pipeline_When_First_Exception_Raised()
    {
        var input = new int[] { 0, 1, 2, 4, 5 };
        int expected = -1;
        int result = -1;
        var pipeline = DataFlowPipelineBuilder.FromSource<int[]>()
            .ProjectMany(a => a)
            .ToTarget(a =>
            {
                result = result + a;
                _ = result / a;
                return Task.FromResult(a);
            }).Build();

        await pipeline.SendAsync(input);

        Func<Task> func = async () => await pipeline.CompleteAsync();
        await func.Should().ThrowAsync<DivideByZeroException>();
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task Should_Throw_And_Stop_Pipeline_When_Last_Exception_Raised()
    {
        var input = new int[] { 1, 2, 4, 5, 0 };
        int expected = input.Sum();
        int result = 0;
        var pipeline = DataFlowPipelineBuilder.FromSource<int[]>()
            .ProjectMany(a => a)
            .ToTarget(a =>
            {
                result = result + a;
                _ = result / a;
                return Task.FromResult(a);
            }).Build();

        await pipeline.SendAsync(input);

        Func<Task> func = async () => await pipeline.CompleteAsync();
        await func.Should().ThrowAsync<DivideByZeroException>();
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Advanced_Group_Operations_Block()
    {
        //group caracters by same value with max 3 caracters per group
        var expected = "fff-aaa-aa-sss-1-0-b-c-dd-2-6-4-";
        var result = string.Empty;

        var pipeline = DataFlowPipelineBuilder
            .FromSource<char[]>()
            .ProjectMany(a => a.GroupBy(p => p).Select(g => g.ToList()))
            .ProjectMany(a => a.GroupByRange(3))
            .ToTarget(a =>
            {
                result = result + string.Join("", a) + "-";
                return Task.FromResult(a);
            })
            .Build();

        await pipeline.SendAsync("fffaasss10abcd264daa".ToArray());

        await pipeline.CompleteAsync();

        result.Should().Be(expected);
    }
}