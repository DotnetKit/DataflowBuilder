namespace DataflowBuilder.Core.Pipeline
{
    public interface IPipeline<TSource>
    {
        Task CompleteAsync();

        Task SendAsync(TSource dataItem, CancellationToken cancellationToken = default);
    }
}