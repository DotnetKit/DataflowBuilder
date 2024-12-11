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

public interface IPipeline<TSource1, TSource2> : IPipeline<Tuple<TSource1, TSource2>>
{
    Task Send1Async(TSource1 dataItem, CancellationToken cancellationToken = default);
    Task Send2Async(TSource2 dataItem, CancellationToken cancellationToken = default);
}


