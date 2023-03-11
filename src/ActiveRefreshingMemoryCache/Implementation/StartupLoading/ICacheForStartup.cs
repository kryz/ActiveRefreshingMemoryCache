namespace ActiveRefreshingMemoryCache.Implementation.StartupLoading;

internal interface ICacheForStartup<TCacheKey, TValue>
    where TCacheKey : notnull
{
    void Insert(IEnumerable<TValue> valuesToInsert);
}
