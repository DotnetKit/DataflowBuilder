
using DataflowBuilder.Abstractions;

namespace DataflowBuilder.Core;

public class FluentBuilder<TExtension, TContext> : IFluentBuilder<TExtension, TContext>
{
    public TContext Current { get; }

    public FluentBuilder(TContext context)
    {
        Current = context;
    }
}

public class FluentBuilder<TContext> : IFluentBuilder<TContext>
{
    public TContext Current { get; }

    public FluentBuilder(TContext context)
    {
        Current = context;
    }
}