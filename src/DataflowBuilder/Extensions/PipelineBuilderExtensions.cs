using DataflowBuilder.Abstractions;
using DataflowBuilder.CustomBlocks;
using System.Threading.Tasks.Dataflow;

namespace DataflowBuilder.Core.Pipeline;

public static class PipelineBuilderExtensions
{
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> Project<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, TOutput> propagatorFunc, int maxDegreeOfParallelism = 1)
    {
        var block = new TransformBlock<TInput, TOutput>(propagatorFunc, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = pipelineBuilder.Current.CancellationToken
        });
        pipelineBuilder.Current.AddPropagator(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProjectAsync<TSource, TInput, TOutput>(
   this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
   Func<TInput, Task<TOutput>> propagatorFunc, int maxDegreeOfParallelism = 1)
    {
        var block = new TransformBlock<TInput, TOutput>(

            async input =>
            {
                try
                {
                    return await propagatorFunc(input);
                }
                catch (Exception ex)
                {
                    if (!pipelineBuilder.Current.SourceBlock.Completion.IsCompleted)
                    {
                        pipelineBuilder.Current.SourceBlock.Fault(ex);
                    }
                    //To prevent pipeline to be block from crashing, pipeline will be completed manually with faulted state
                    throw;
                }
            }

            , new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = maxDegreeOfParallelism,
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = pipelineBuilder.Current.CancellationToken
            });
        pipelineBuilder.Current.AddPropagator(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProjectMany<TSource, TInput, TOutput>(
      this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
      Func<TInput, IEnumerable<TOutput>> propagatorFunc, int maxDegreeOfParallelism = 1)
    {
        var block = new TransformManyBlock<TInput, TOutput>(propagatorFunc, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = pipelineBuilder.Current.CancellationToken
        });
        pipelineBuilder.Current.AddPropagator(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>> Batch<TSource, TInput>(
       this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder, int batchSize = 100)
    {
        var block = new BatchBlock<TInput>(batchSize, new GroupingDataflowBlockOptions()
        {
            BoundedCapacity = batchSize,
            Greedy = true,
            CancellationToken = pipelineBuilder.Current.CancellationToken
        });
        pipelineBuilder.Current.AddPropagator(block);
        return new FluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>> Group<TSource, TGroupKey, TInput>(
    this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder, Func<TInput, TGroupKey> groupingSelector, int maxDegreeOfParallelism = 1)

    {
        var block = new GroupBlock<TGroupKey, TInput>(groupingSelector, maxDegreeOfParallelism, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagator(block);
        return new FluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> ToTarget<TSource, TInput>(
       this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
       Func<TInput, Task> targetAction, int maxDegreeOfParallelism = 1)
    {
        var block = new ActionBlock<TInput>(async input =>
        {
            try
            {
                await targetAction(input);
            }
            catch (Exception ex)
            {
                if (!pipelineBuilder.Current.SourceBlock.Completion.IsCompleted)
                {
                    pipelineBuilder.Current.SourceBlock.Fault(ex);
                }
                //To prevent pipeline to be block from crashing, pipeline will be completed manually with faulted state
                throw;
            }
        }, new ExecutionDataflowBlockOptions()
        {
            BoundedCapacity = maxDegreeOfParallelism,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = pipelineBuilder.Current.CancellationToken
        });
        pipelineBuilder.Current.AddTarget(block);
        return new FluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IPipeline<TSource> Build<TSource, TInput>(
       this IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder)
    {
        return pipelineBuilder.Current.Build();
    }
}