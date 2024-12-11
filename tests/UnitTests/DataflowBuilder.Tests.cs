using System.Collections.Concurrent;
using DotnetKit.DataflowBuilder;
using DotnetKit.DataflowBuilder.Extensions;
using FluentAssertions;

namespace DataflowBuilder.UnitTests;

[TestClass]
public class DataflowBuilderTests
{
    public async Task Should_Build_Pipeline_With_Source_And_Target()
    {
        const string expected = "Hello World";
        var target = string.Empty;
        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .ToTargetAsync(item =>
            {
                target = item;
                return Task.CompletedTask;
            }).Build();

        await pipeline.SendAsync(expected);
        await pipeline.CompleteAsync();

        target.Should().Be(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Batching()
    {

        var target = new List<string>();
        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .Batch(2)
            .ToTargetAsync(items =>
            {
                target.Add(string.Concat(items));
                return Task.CompletedTask;
            })

            .Build();

        await pipeline.SendAsync("Hello");
        await pipeline.SendAsync("World");

        await pipeline.SendAsync("Ping");
        await pipeline.SendAsync("Pong");

        await pipeline.CompleteAsync();

        target.First().Should().Be("HelloWorld");
        target[1].Should().Be("PingPong");
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Processing_Block()
    {
        var expected = "HelloWorld".Split();
        var target = string.Empty.Split();

        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .Process(item => item.Split())
            .ToTargetAsync(items =>
            {
                target = items;
                return Task.CompletedTask;
            })

            .Build();

        await pipeline.SendAsync("HelloWorld");

        await pipeline.CompleteAsync();

        target.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Batching_And_Processing_Blocks()
    {
        var expected = "HelloWorld";
        var result = string.Empty;

        var pipeline = DataFlowPipelineBuilder.FromSource<string>()
            .Batch(2)
            .Process(items => string.Concat(items))
            .ToTargetAsync(item =>
            {
                result = item;
                return Task.CompletedTask;
            })

            .Build();

        await pipeline.SendAsync("Hello");
        await pipeline.SendAsync("World");

        await pipeline.CompleteAsync();

        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Processing_Many_Block()
    {
        var expected = "HelloWorld";
        var result = string.Empty;

        var pipeline = DataFlowPipelineBuilder.FromSource<char[]>()
            .ProcessMany(items => items)
            .ToTargetAsync(item =>
            {
                result = result + item;
                return Task.CompletedTask;
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
            .ProcessMany(items => items)
            .ToTargetAsync(item =>
            {
                result = result + item;
                _ = result / item;
                return Task.CompletedTask;
            }).Build();

        await pipeline.SendAsync(input);

        Func<Task> func = pipeline.CompleteAsync;
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
            .ProcessMany(items => items)
            .ToTargetAsync(item =>
            {
                result = result + item;
                _ = result / item;
                return Task.CompletedTask;
            }).Build();

        await pipeline.SendAsync(input);

        Func<Task> func = pipeline.CompleteAsync;
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
            .ProcessMany(items => items.GroupBy(p => p).Select(g => g.ToList()))
            .ProcessMany(items => items.GroupByRange(3))
            .ToTargetAsync(items =>
            {
                result = result + string.Join("", items) + "-";
                return Task.CompletedTask;
            })
            .Build();

        await pipeline.SendAsync("fffaasss10abcd264daa".ToArray());

        await pipeline.CompleteAsync();

        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Target_Parallelized_Tasks()
    {
        //simulate a slow target with 100ms delay per item
        var expected = "1111-2222-3333-4444-5555-6666-7777-8888-9999";
        var result = new ConcurrentQueue<string>();

        var concurrentTasks = 0;
        var maxConcurrentTasks = 0;

        var pipeline = DataFlowPipelineBuilder
            .FromSource<string>()
            .Process(item => item)
            .ToTargetAsync(async item =>
            {

                try
                {
                    var current = Interlocked.Increment(ref concurrentTasks);
                    if (current >= maxConcurrentTasks)
                    {
                        maxConcurrentTasks = current;
                    }
                    result.Enqueue(item);
                    await Task.Delay(100);

                }
                finally
                {
                    Interlocked.Decrement(ref concurrentTasks);
                }

            }, maxDegreeOfParallelism: 2)
            .Build();

        foreach (var segment in expected.Split("-"))
        {
            await pipeline.SendAsync(segment);
        }
        await pipeline.CompleteAsync();

        result.ToList().Count.Should().Be(9);
        expected.Split("-").Should().BeEquivalentTo(result.ToList());

        maxConcurrentTasks.Should().Be(2);
        concurrentTasks.Should().Be(0);
    }

    [TestMethod]
    public async Task Should_Build_Pipeline_With_Project_Parallelized_Tasks()
    {
        //simulate a slow projection with 100ms delay per item
        var expected = "1111-2222-3333-4444-5555-6666-7777-8888-9999";
        var result = new ConcurrentQueue<string>();

        var concurrentTasks = 0;
        var maxConcurrentTasks = 0;

        var pipeline = DataFlowPipelineBuilder
            .FromSource<string>()
            .ProcessAsync(async item =>
            {

                try
                {
                    var current = Interlocked.Increment(ref concurrentTasks);
                    if (current >= maxConcurrentTasks)
                    {
                        maxConcurrentTasks = current;
                    }
                    await Task.Delay(100);
                    return item;
                }
                finally
                {
                    Interlocked.Decrement(ref concurrentTasks);
                }
            }, maxDegreeOfParallelism: 2)
            .ToTargetAsync(async item =>
            {
                await Task.Delay(100);
                result.Enqueue(item);
            })
            .Build();

        foreach (var segment in expected.Split("-"))
        {
            await pipeline.SendAsync(segment);
        }
        await pipeline.CompleteAsync();

        result.ToList().Count.Should().Be(9);
        expected.Split("-").Should().BeEquivalentTo(result.ToList());

        maxConcurrentTasks.Should().Be(2);
        concurrentTasks.Should().Be(0);
    }
}