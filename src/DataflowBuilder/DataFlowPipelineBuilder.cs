using DataflowBuilder.Abstractions;
using System.Threading.Tasks.Dataflow;

namespace DataflowBuilder.Core.Pipeline
{
    public static class DataFlowPipelineBuilder
    {
        public static IFluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>> FromSource<TSource>(CancellationToken cancellationToken = default)
        {
            var initialBlock = new BufferBlock<TSource>(new DataflowBlockOptions()
            {
                BoundedCapacity = 1,
                CancellationToken = cancellationToken
            });

            return new FluentBuilder<ISourceBlock<TSource>, IPipelineBuilder<TSource>>(new DataFlowPipelineBuilder<TSource>(initialBlock, cancellationToken));
        }
    }

    public partial class DataFlowPipelineBuilder<TSource> : IPipelineBuilder<TSource>
    {
        public CancellationToken CancellationToken { get; }
        private readonly List<IDataflowBlock> _blocks = new();
        public ISourceBlock<TSource> SourceBlock { get; }

        public DataFlowPipelineBuilder(IDataflowBlock initialBlock, CancellationToken cancellationToken)
        {
            SourceBlock = initialBlock as ISourceBlock<TSource> ?? null!;
            if (SourceBlock == null)
            {
                throw new InvalidCastException("initialBlock instance cloud not be casted to ISourceBlock<TSource>");
            }
            _blocks.Add(initialBlock);
            CancellationToken = cancellationToken;
        }

        public void AddPropagator<TInput, TOutput>(IPropagatorBlock<TInput, TOutput> propagatorBlock)
        {
            if (_blocks.Last() is ISourceBlock<TInput> sourceBlock)
            {
                sourceBlock.LinkTo(propagatorBlock, new DataflowLinkOptions()
                {
                    PropagateCompletion = true
                });
            }
            else
            {
                throw new System.Exception("Cannot link to a non-source block");
            }
            _blocks.Add(propagatorBlock);
        }

        public void AddTarget<TInput>(ITargetBlock<TInput> targetBlock)
        {
            if (_blocks.Last() is ISourceBlock<TInput> sourceBlock)
            {
                sourceBlock.LinkTo(targetBlock, new DataflowLinkOptions()
                {
                    PropagateCompletion = true
                });
            }
            else
            {
                throw new System.Exception("Cannot link to a non-source block");
            }
            _blocks.Add(targetBlock);
        }

        public IPipeline<TSource> Build()
        {
            return new DataflowPipeline<TSource>(_blocks.First() as ITargetBlock<TSource> ?? null!, _blocks.Last());
        }
    }
}