using DotnetKit.DataflowBuilder.Abstractions;

namespace DotnetKit.DataflowBuilder.Core;

/// <summary>
/// Provides a typed fluent wrapper around a pipeline builder context with an extension marker type.
/// </summary>
/// <typeparam name="TExtension">The type marker representing the current pipeline stage.</typeparam>
/// <typeparam name="TContext">The underlying pipeline builder context.</typeparam>
public class FluentBuilder<TExtension, TContext> : IFluentBuilder<TExtension, TContext>
{
    /// <summary>
    /// Gets the underlying pipeline builder context.
    /// </summary>
    public TContext Current { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentBuilder{TExtension, TContext}"/> class.
    /// </summary>
    /// <param name="context">The underlying context instance.</param>
    public FluentBuilder(TContext context)
    {
        Current = context;
    }
}

/// <summary>
/// Provides a fluent wrapper around a pipeline builder context.
/// </summary>
/// <typeparam name="TContext">The underlying pipeline builder context.</typeparam>
public class FluentBuilder<TContext> : IFluentBuilder<TContext>
{
    /// <summary>
    /// Gets the underlying pipeline builder context.
    /// </summary>
    public TContext Current { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentBuilder{TContext}"/> class.
    /// </summary>
    /// <param name="context">The underlying context instance.</param>
    public FluentBuilder(TContext context)
    {
        Current = context;
    }
}