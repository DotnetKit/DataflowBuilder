using DotnetKit.DataflowBuilder.Abstractions;
using DotnetKit.DataflowBuilder.Core;
using DotnetKit.DataflowBuilder.CustomBlocks;
using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder;

public static class PipelineBuilderExtensions
{
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> Process<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, TOutput> propagatorFunc, int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateTransformBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProcessAsync<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, Task<TOutput>> propagatorFunc, int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateAsyncTransformBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProcessMany<TSource, TInput, TOutput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, IEnumerable<TOutput>> propagatorFunc, int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateTransformManyBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }
    public static IFluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>> ProcessManyAsync<TSource, TInput, TOutput>(
       this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
       Func<TInput, Task<IEnumerable<TOutput>>> propagatorFunc, int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateAsyncTransformManyBlock(propagatorFunc, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TOutput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }


    public static IFluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>> Batch<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder, int batchSize = 100)
    {

        var block = pipelineBuilder.Current.CreateBatchBlock<TInput>(batchSize);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>> Group<TSource, TGroupKey, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder, Func<TInput, TGroupKey> groupingSelector, int maxDegreeOfParallelism = 1)
    {
        var block = new GroupBlock<TGroupKey, TInput>(groupingSelector, maxDegreeOfParallelism, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddPropagatorBlock(block);
        return new FluentBuilder<ISourceBlock<TInput[]>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> ToTargetAsync<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Func<TInput, Task> targetAction, int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateAscynActionBlock(targetAction, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddTargetBlock(block);
        return new FluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }


    public static IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> ToTarget<TSource, TInput>(
        this IFluentBuilder<ISourceBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder,
        Action<TInput> targetAction, int maxDegreeOfParallelism = 1)
    {
        var block = pipelineBuilder.Current.CreateActionBlock(targetAction, maxDegreeOfParallelism);
        pipelineBuilder.Current.AddTargetBlock(block);
        return new FluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>>(pipelineBuilder.Current);
    }

    public static IPipeline<TSource> Build<TSource, TInput>(
        this IFluentBuilder<ITargetBlock<TInput>, IPipelineBuilder<TSource>> pipelineBuilder)
    {
        return pipelineBuilder.Current.Build();
    }
}
