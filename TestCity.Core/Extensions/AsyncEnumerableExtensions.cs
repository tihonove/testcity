namespace TestCity.Core.Extensions;

public static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<List<T>> Batches<T>(this IAsyncEnumerable<T> items, int batchSize)
    {
        var currentBatch = new List<T>();
        await foreach (var item in items)
        {
            currentBatch.Add(item);
            if (currentBatch.Count >= batchSize)
            {
                yield return currentBatch;
                currentBatch = new List<T>();
            }
        }

        if (currentBatch.Count > 0)
        {
            yield return currentBatch;
        }
    }
}
