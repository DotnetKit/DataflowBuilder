using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder.Abstractions;

public interface IPipelineBuilder<TSource> : IPipelineBuilder
{
    ISourceBlock<TSource> SourceBlock { get; }

    IPipeline<TSource> Build();
}

public interface IPipelineBuilder
{
    CancellationToken CancellationToken { get; }

    void AddPropagatorBlock<TInput, TOutput>(IPropagatorBlock<TInput, TOutput> propagatorBlock);

    void AddTargetBlock<TInput>(ITargetBlock<TInput> targetBlock);

    IPropagatorBlock<TInput, TOutput> CreateAsyncTransformBlock<TInput, TOutput>(
        Func<TInput, Task<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism);

    ITargetBlock<TInput> CreateActionBlock<TInput>(
        Action<TInput> targetAction,
        int maxDegreeOfParallelism);
    ITargetBlock<TInput> CreateAscynActionBlock<TInput>(
        Func<TInput, Task> targetAction,
        int maxDegreeOfParallelism);

    IPropagatorBlock<TInput, TOutput> CreateTransformBlock<TInput, TOutput>(
        Func<TInput, TOutput> propagatorFunc,
        int maxDegreeOfParallelism);

    IPropagatorBlock<TInput, TOutput> CreateTransformManyBlock<TInput, TOutput>(
        Func<TInput, IEnumerable<TOutput>> propagatorFunc,
        int maxDegreeOfParallelism);

    IPropagatorBlock<TInput, TOutput> CreateAsyncTransformManyBlock<TInput, TOutput>(
        Func<TInput, Task<IEnumerable<TOutput>>> propagatorFunc,
        int maxDegreeOfParallelism);

    IPropagatorBlock<TInput, TInput[]> CreateBatchBlock<TInput>(int batchSize);
}