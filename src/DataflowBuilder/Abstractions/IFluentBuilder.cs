namespace DotnetKit.DataflowBuilder.Abstractions;

public interface IFluentBuilder<out TContext>
{
    TContext Current { get; }
}

public interface IFluentBuilder<in TExtension, out TContext>
{
    TContext Current { get; }
}