namespace DotnetKit.DataflowBuilder.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// return batched IEnumerable List of TSource by given batchSize
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="rangeSize"></param>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<TSource>> GroupByRange<TSource>(this IEnumerable<TSource> source, int rangeSize)
    {
        if (source.Count() <= rangeSize)
        {
            yield return source.ToList();
        }
        else
        {
            var buffer = new List<TSource>();

            foreach (var item in source)
            {
                buffer.Add(item);

                if (buffer.Count >= rangeSize)
                {
                    yield return buffer.ToList();
                    buffer.Clear();
                }
            }
            yield return buffer.ToList();
        }
    }
}