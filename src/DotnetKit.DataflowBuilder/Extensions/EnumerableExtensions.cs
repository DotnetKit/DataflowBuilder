namespace DotnetKit.DataflowBuilder.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Return batched IEnumerable List of TSource by given batchSize
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="rangeSize"></param>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<TSource>> GroupByRange<TSource>(this IEnumerable<TSource> source, int rangeSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (rangeSize <= 0) throw new ArgumentOutOfRangeException(nameof(rangeSize));

        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                var batch = new List<TSource>(rangeSize);
                do
                {
                    batch.Add(enumerator.Current);
                }
                while (batch.Count < rangeSize && enumerator.MoveNext());
                yield return batch;
            }
        }
    }
}