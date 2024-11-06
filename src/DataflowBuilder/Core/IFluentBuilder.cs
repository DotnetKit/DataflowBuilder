namespace DataflowBuilder.Core.Pipeline
{
    public interface IFluentBuilder<out TContext>
    {
        TContext Current { get; }
    }

    public interface IFluentBuilder<in TExtension, out TContext>
    {
        TContext Current { get; }
    }
}