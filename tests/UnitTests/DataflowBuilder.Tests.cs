using System.Collections.Concurrent;
using System.Threading.Channels;
using DotnetKit.DataflowBuilder;
using DotnetKit.DataflowBuilder.Extensions;
using FluentAssertions;

namespace DataflowBuilder.UnitTests;

[TestClass]
public class DataflowBuilderTests
{
    [TestMethod]
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
        int[] input = [0, 1, 2, 4, 5];
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
        int[] input = [1, 2, 4, 5, 0];
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

    [TestMethod]
    public async Task Should_Handle_Concurrent_Inputs_In_GroupBlock_ThreadSafely()
    {
        var receivedBatches = new ConcurrentBag<int[]>();
        const int itemsPerKey = 20;
        const int keyCount = 5;

        var pipeline = DataFlowPipelineBuilder
            .FromSource<int>(sourceBufferSize: 100)
            .Group(key => key % keyCount, maxDegreeOfParallelism: 4)
            .ToTargetAsync(batch =>
            {
                receivedBatches.Add(batch);
                return Task.CompletedTask;
            }, maxDegreeOfParallelism: 4)
            .Build();

        var sendTasks = Enumerable.Range(0, keyCount * itemsPerKey)
            .Select(i => pipeline.SendAsync(i))
            .ToArray();

        await Task.WhenAll(sendTasks);
        await pipeline.CompleteAsync();

        var totalItems = receivedBatches.Sum(b => b.Length);
        totalItems.Should().Be(keyCount * itemsPerKey);
    }

    [TestMethod]
    public async Task Should_Flush_Incomplete_Groups_On_Pipeline_Completion()
    {
        var receivedBatches = new List<string[]>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<(string Key, string Value)>()
            .Group(item => item.Key, boundedCapacity: 5)
            .Process(batch => batch.Select(x => x.Value).ToArray())
            .ToTargetAsync(batch =>
            {
                receivedBatches.Add(batch);
                return Task.CompletedTask;
            })
            .Build();

        // 3 items for "Alpha" (does not reach capacity of 5)
        await pipeline.SendAsync(("Alpha", "A1"));
        await pipeline.SendAsync(("Alpha", "A2"));
        await pipeline.SendAsync(("Alpha", "A3"));

        // 2 items for "Beta" (does not reach capacity of 5)
        await pipeline.SendAsync(("Beta", "B1"));
        await pipeline.SendAsync(("Beta", "B2"));

        // 6 items for "Gamma" (reaches 5, leaving 1 remaining)
        for (var i = 1; i <= 6; i++)
        {
            await pipeline.SendAsync(("Gamma", $"G{i}"));
        }

        await pipeline.CompleteAsync();

        var totalItems = receivedBatches.Sum(b => b.Length);
        totalItems.Should().Be(11);

        receivedBatches.Should().Contain(b => b.SequenceEqual(new[] { "G1", "G2", "G3", "G4", "G5" }));
        receivedBatches.Should().Contain(b => b.SequenceEqual(new[] { "A1", "A2", "A3" }));
        receivedBatches.Should().Contain(b => b.SequenceEqual(new[] { "B1", "B2" }));
        receivedBatches.Should().Contain(b => b.SequenceEqual(new[] { "G6" }));
    }

    [TestMethod]
    public async Task Should_Support_Configurable_BoundedCapacity_In_Group_Extension()
    {
        var receivedBatches = new List<int[]>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<int>()
            .Group(key => key % 2, boundedCapacity: 3, maxDegreeOfParallelism: 1)
            .ToTargetAsync(batch =>
            {
                receivedBatches.Add(batch);
                return Task.CompletedTask;
            })
            .Build();

        // Send 3 evens and 3 odds
        await pipeline.SendAsync(2);
        await pipeline.SendAsync(4);
        await pipeline.SendAsync(6);

        await pipeline.SendAsync(1);
        await pipeline.SendAsync(3);
        await pipeline.SendAsync(5);

        await pipeline.CompleteAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().Equal(2, 4, 6);
        receivedBatches[1].Should().Equal(1, 3, 5);
    }

    [TestMethod]
    public async Task Should_Stream_From_IAsyncEnumerable_Using_SendAllAsync()
    {
        var received = new List<int>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<int>()
            .ToTargetAsync(item =>
            {
                received.Add(item);
                return Task.CompletedTask;
            })
            .Build();

        static async IAsyncEnumerable<int> GenerateNumbersAsync()
        {
            for (var i = 1; i <= 5; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }

        await pipeline.SendAllAsync(GenerateNumbersAsync(), completeAfterSend: true);

        received.Should().Equal(1, 2, 3, 4, 5);
    }

    [TestMethod]
    public async Task Should_Stream_From_Channel_Using_SendFromChannelAsync()
    {
        var channel = Channel.CreateUnbounded<string>();
        var received = new List<string>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<string>()
            .ToTargetAsync(item =>
            {
                received.Add(item);
                return Task.CompletedTask;
            })
            .Build();

        await channel.Writer.WriteAsync("A");
        await channel.Writer.WriteAsync("B");
        await channel.Writer.WriteAsync("C");
        channel.Writer.Complete();

        await pipeline.SendFromChannelAsync(channel.Reader, completeAfterSend: true);

        received.Should().Equal("A", "B", "C");
    }

    [TestMethod]
    public async Task Should_Forward_Pipeline_Output_To_Channel_Using_ToChannel()
    {
        var channel = Channel.CreateUnbounded<int>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<int>()
            .Process(x => x * 10)
            .ToChannel(channel.Writer)
            .Build();

        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);
        await pipeline.SendAsync(3);
        await pipeline.CompleteAsync();

        var results = new List<int>();
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            results.Add(item);
        }

        results.Should().Equal(10, 20, 30);
    }

    [TestMethod]
    public async Task Should_Filter_Items_Synchronously_And_Asynchronously()
    {
        var results = new List<int>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<int>()
            .Filter(x => x % 2 == 0) // only evens
            .FilterAsync(async x =>
            {
                await Task.Yield();
                return x > 4; // only > 4
            })
            .ToTargetAsync(item =>
            {
                results.Add(item);
                return Task.CompletedTask;
            })
            .Build();

        foreach (var i in Enumerable.Range(1, 10))
        {
            await pipeline.SendAsync(i);
        }
        await pipeline.CompleteAsync();

        results.Should().Equal(6, 8, 10);
    }

    [TestMethod]
    public async Task Should_Batch_Items_By_Time_When_Timeout_Expires()
    {
        var receivedBatches = new List<string[]>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<string>()
            .Batch(batchSize: 10, timeout: TimeSpan.FromMilliseconds(80))
            .ToTargetAsync(batch =>
            {
                receivedBatches.Add(batch);
                return Task.CompletedTask;
            })
            .Build();

        // Send 3 items (well below batchSize of 10)
        await pipeline.SendAsync("one");
        await pipeline.SendAsync("two");
        await pipeline.SendAsync("three");

        // Wait for timeout to trigger emission
        await Task.Delay(200);

        receivedBatches.Should().HaveCount(1);
        receivedBatches[0].Should().Equal("one", "two", "three");

        await pipeline.CompleteAsync();
    }

    [TestMethod]
    public async Task Should_Batch_Items_By_Size_When_Capacity_Reached_Before_Timeout()
    {
        var receivedBatches = new List<int[]>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<int>()
            .Batch(batchSize: 3, timeout: TimeSpan.FromSeconds(5))
            .ToTargetAsync(batch =>
            {
                receivedBatches.Add(batch);
                return Task.CompletedTask;
            })
            .Build();

        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);
        await pipeline.SendAsync(3);

        // Immediate complete
        await pipeline.CompleteAsync();

        receivedBatches.Should().HaveCount(1);
        receivedBatches[0].Should().Equal(1, 2, 3);
    }

    [TestMethod]
    public async Task Should_Retry_Failed_Processing_With_Configured_RetryCount()
    {
        var attempts = 0;
        var results = new List<int>();

        var pipeline = DataFlowPipelineBuilder
            .FromSource<int>()
            .ProcessAsync(async item =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("Transient glitch");
                }
                await Task.Yield();
                return item * 2;
            }, maxDegreeOfParallelism: 1, retryCount: 3, retryDelay: TimeSpan.FromMilliseconds(10))
            .ToTargetAsync(item =>
            {
                results.Add(item);
                return Task.CompletedTask;
            })
            .Build();

        await pipeline.SendAsync(42);
        await pipeline.CompleteAsync();

        attempts.Should().Be(3);
        results.Should().Equal(84);
    }
}