namespace DotnetKit.DataflowBuilder.Abstractions;

/// <summary>
/// Represents a fluent builder abstraction wrapping an underlying builder context.
/// </summary>
/// <typeparam name="TContext">The underlying context type wrapped by the builder.</typeparam>
public interface IFluentBuilder<out TContext>
{
    /// <summary>
    /// Gets the current underlying builder context.
    /// </summary>
    TContext Current { get; }
}

/// <summary>
/// Represents a fluent builder abstraction carrying a type marker for extension method chaining.
/// </summary>
/// <typeparam name="TExtension">The marker type representing the current step, block, or payload type in the pipeline.</typeparam>
/// <typeparam name="TContext">The underlying pipeline builder context type.</typeparam>
public interface IFluentBuilder<in TExtension, out TContext>
{
    /// <summary>
    /// Gets the current underlying builder context.
    /// </summary>
    TContext Current { get; }
}