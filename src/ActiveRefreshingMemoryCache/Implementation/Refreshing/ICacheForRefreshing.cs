using ActiveRefreshingMemoryCache.Implementation.TheCache;

namespace ActiveRefreshingMemoryCache.Implementation.Refreshing;

internal interface ICacheForRefreshing<TCacheKey, TValue>
    where TCacheKey : notnull
{
    ICollection<CacheEntryTracking<TCacheKey>> GetCacheEntryTrackings();

    void Remove(IEnumerable<TCacheKey> keysToRemove);

    void Update(IEnumerable<TValue> valuesToUpdate);
}
