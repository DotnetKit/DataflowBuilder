using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder.Abstractions;

/// <summary>
/// PropagatorBlock base implementation used to create any block as both target and source.
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TOutput"></typeparam>
public abstract class BasePropagatorBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>
{
    public abstract ITargetBlock<TInput> Target { get; }
    public abstract ISourceBlock<TOutput> Source { get; }
    public IPropagatorBlock<TInput, TOutput> Pipeline => DataflowBlock.Encapsulate(Target, Source);

    #region DataFlowConnector

    public virtual TOutput? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
    {
        return Pipeline.ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public virtual IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
    {
        return Pipeline.LinkTo(target, linkOptions);
    }

    public virtual void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
    {
        Pipeline.ReleaseReservation(messageHeader, target);
    }

    public virtual bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
    {
        return Pipeline.ReserveMessage(messageHeader, target);
    }

    public virtual DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
    {
        return Pipeline.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public virtual void Complete()
    {
        Target.Complete();
        Target.Completion.ContinueWith(c =>
        {
            Source.Complete();
        });
    }

    public virtual void Fault(Exception exception)
    {
        Pipeline.Fault(exception);
    }

    public virtual Task Completion => Pipeline.Completion;

    #endregion DataFlowConnector
}