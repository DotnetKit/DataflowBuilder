using System.Threading.Tasks.Dataflow;

namespace DataflowBuilder.Core.Pipeline
{
    public interface IPipelineBuilder<TSource> : IPipelineBuilder
    {
        ISourceBlock<TSource> SourceBlock { get; }

        IPipeline<TSource> Build();
    }

    public interface IPipelineBuilder
    {
        CancellationToken CancellationToken { get; }

        void AddPropagator<TInput, TOutput>(IPropagatorBlock<TInput, TOutput> propagatorBlock);

        void AddTarget<TInput>(ITargetBlock<TInput> targetBlock);
    }
}