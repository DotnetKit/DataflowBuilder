using System.Threading.Tasks.Dataflow;

namespace DotnetKit.DataflowBuilder.Abstractions;

/// <summary>
/// Base class for custom propagator blocks that combine an internal target block and source block into an encapsulated <see cref="IPropagatorBlock{TInput, TOutput}"/>.
/// </summary>
/// <typeparam name="TInput">The type of items accepted by the target end of the block.</typeparam>
/// <typeparam name="TOutput">The type of items produced by the source end of the block.</typeparam>
public abstract class BasePropagatorBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>
{
    /// <summary>
    /// Gets the inner target block responsible for receiving incoming messages.
    /// </summary>
    public abstract ITargetBlock<TInput> Target { get; }

    /// <summary>
    /// Gets the inner source block responsible for dispatching outgoing messages.
    /// </summary>
    public abstract ISourceBlock<TOutput> Source { get; }

    private IPropagatorBlock<TInput, TOutput>? _pipeline;

    /// <summary>
    /// Gets the encapsulated propagator block combining <see cref="Target"/> and <see cref="Source"/>.
    /// </summary>
    public IPropagatorBlock<TInput, TOutput> Pipeline => _pipeline ??= DataflowBlock.Encapsulate(Target, Source);

    #region DataFlowConnector

    /// <summary>
    /// Consumes a previously reserved message from the source block.
    /// </summary>
    /// <param name="messageHeader">The header of the message being consumed.</param>
    /// <param name="target">The target block consuming the message.</param>
    /// <param name="messageConsumed">Set to <see langword="true"/> if the message was successfully consumed; otherwise, <see langword="false"/>.</param>
    /// <returns>The consumed message value, or default if the message could not be consumed.</returns>
    public virtual TOutput? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
    {
        return Pipeline.ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    /// <summary>
    /// Links this source block to the specified target block with the given link options.
    /// </summary>
    /// <param name="target">The target block to link to.</param>
    /// <param name="linkOptions">Options defining the link behavior, such as completion propagation.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to unlink the source and target blocks.</returns>
    public virtual IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
    {
        return Pipeline.LinkTo(target, linkOptions);
    }

    /// <summary>
    /// Releases a previous message reservation on the source block.
    /// </summary>
    /// <param name="messageHeader">The header of the message reservation being released.</param>
    /// <param name="target">The target block that held the reservation.</param>
    public virtual void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
    {
        Pipeline.ReleaseReservation(messageHeader, target);
    }

    /// <summary>
    /// Reserves a message previously offered by the source block.
    /// </summary>
    /// <param name="messageHeader">The header of the message being reserved.</param>
    /// <param name="target">The target block attempting to reserve the message.</param>
    /// <returns><see langword="true"/> if the message was successfully reserved; otherwise, <see langword="false"/>.</returns>
    public virtual bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
    {
        return Pipeline.ReserveMessage(messageHeader, target);
    }

    /// <summary>
    /// Offers a message to the target block and yields status regarding its consumption or rejection.
    /// </summary>
    /// <param name="messageHeader">The header identifying the offered message.</param>
    /// <param name="messageValue">The message payload being offered.</param>
    /// <param name="source">The source block offering the message, or null.</param>
    /// <param name="consumeToAccept">Whether the target must consume the message to accept it.</param>
    /// <returns>The acceptance status of the offered message.</returns>
    public virtual DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
    {
        return Pipeline.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    /// <summary>
    /// Signals the target block that no more data will be provided, and propagates completion to the source block.
    /// </summary>
    public virtual void Complete()
    {
        Target.Complete();
        Target.Completion.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                ((IDataflowBlock)Source).Fault(t.Exception!);
            }
            else
            {
                Source.Complete();
            }
        }, TaskScheduler.Default);
    }

    /// <summary>
    /// Causes the encapsulated dataflow block to complete in a faulted state with the specified exception.
    /// </summary>
    /// <param name="exception">The exception that caused the fault.</param>
    public virtual void Fault(Exception exception)
    {
        Target.Fault(exception);
        ((IDataflowBlock)Source).Fault(exception);
    }

    /// <summary>
    /// Gets a <see cref="Task"/> that represents the asynchronous operation and completion of the dataflow block.
    /// </summary>
    public virtual Task Completion => Pipeline.Completion;

    #endregion DataFlowConnector
}