namespace DotnetKit.DataflowBuilder.Extensions;

/// <summary>
/// Provides extension methods for partitioning and chunking enumerable sequences.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Partitions an enumerable sequence into consecutive chunks of a specified size.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
    /// <param name="source">The sequence of elements to chunk.</param>
    /// <param name="rangeSize">The maximum number of elements per partitioned batch. Must be greater than zero.</param>
    /// <returns>A sequence of batches, where each batch contains at most <paramref name="rangeSize"/> items.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rangeSize"/> is less than or equal to zero.</exception>
    /// <example>
    /// <code>
    /// var numbers = new[] { 1, 2, 3, 4, 5 };
    /// var chunks = numbers.GroupByRange(2); // [[1, 2], [3, 4], [5]]
    /// </code>
    /// </example>
    public static IEnumerable<IEnumerable<TSource>> GroupByRange<TSource>(this IEnumerable<TSource> source, int rangeSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (rangeSize <= 0) throw new ArgumentOutOfRangeException(nameof(rangeSize), "Range size must be greater than zero.");

        using var enumerator = source.GetEnumerator();
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