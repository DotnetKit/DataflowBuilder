namespace DotnetKit.DataflowBuilder.Abstractions;

/// <summary>
///  Represents a fluent builder abstraction.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public interface IFluentBuilder<out TContext>
{
    TContext Current { get; }
}

public interface IFluentBuilder<in TExtension, out TContext>
{
    TContext Current { get; }
}