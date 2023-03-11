namespace ActiveRefreshingMemoryCache.Implementation.RequestHandling;

internal interface ICacheForRequestHandling<TCacheKey, TValue>
    where TCacheKey : notnull
{
    bool TryGetValue(TCacheKey key, out TValue? value);

    void Upsert(TCacheKey key, TValue value);
}
