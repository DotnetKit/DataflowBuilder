namespace DotnetKit.DataflowBuilder.Abstractions;

public interface IPipeline<TSource>
{
    Task CompleteAsync();

    Task SendAsync(TSource dataItem, CancellationToken cancellationToken = default);
}