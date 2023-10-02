namespace Kontur.TestAnalytics.Reporter.Client.Impl;

internal static class AsyncEnumerableExtensions
{
#pragma warning disable CS1998
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
    {
        foreach (var item in enumerable)
            yield return item;
    }
#pragma warning restore CS1998

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
            yield return currentBatch;
    }
}