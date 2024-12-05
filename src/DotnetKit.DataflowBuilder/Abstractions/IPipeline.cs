namespace DotnetKit.DataflowBuilder.Abstractions;

/// <summary>
///   Represents a pipeline abstraction. 
/// </summary>
/// <typeparam name="TSource"></typeparam> <summary>
/// 
/// </summary>
/// <typeparam name="TSource"></typeparam>
public interface IPipeline<TSource>
{
    Task CompleteAsync();

    Task SendAsync(TSource dataItem, CancellationToken cancellationToken = default);
}