using ActiveRefreshingMemoryCache.Implementation.Refreshing;
using ActiveRefreshingMemoryCache.Implementation.RequestHandling;
using ActiveRefreshingMemoryCache.Implementation.StartupLoading;
using ActiveRefreshingMemoryCache.Implementation.Timing;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace ActiveRefreshingMemoryCache.Implementation.TheCache;

internal class TheCacheInstance<TCacheKey, TValue> :
    ICacheForStartup<TCacheKey, TValue>,
    ICacheForRefreshing<TCacheKey, TValue>,
    ICacheForRequestHandling<TCacheKey, TValue>,
    IDisposable
    where TCacheKey : notnull
{
    private readonly MemoryCache cache;
    private readonly ICacheEntryOptionsFactory<TCacheKey, TValue> cacheEntryOptionsFactory;
    private readonly ICacheKeyFactory<TCacheKey, TValue> cacheKeyFactory;

    // This will not always be in sync with the actual cache. But since MemoryCache doesn't expose a possibility to access all entries, I need a workaround.
    // It is possible, that entries are removed from cache, but not from cacheEntryTrackings. E.g. when the cache is compacted.
    // This could paritaly be solved by adding a hook to the MemoryCacheEntryOptions.PostEvictionCallbackRegistration when adding an entries to the cache & updating entries.
    // I'm quite sure I couldn't handle all raceconditions though!
    // Like this I may have some stale entries in cacheEntryTrackings.
    private readonly ConcurrentDictionary<TCacheKey, CacheEntryTracking<TCacheKey>> cacheEntryTrackings;

    internal TheCacheInstance(
        CacheOptions options,
        ICacheEntryOptionsFactory<TCacheKey, TValue> cacheEntryOptionsFactory,
        ICacheKeyFactory<TCacheKey, TValue> cacheKeyFactory,
        IClock clock)
    {
        this.cacheEntryOptionsFactory = cacheEntryOptionsFactory;
        this.cacheKeyFactory = cacheKeyFactory;

        var memoryCacheOptions = new MemoryCacheOptions
        {
            SizeLimit = options.SizeLimit,
            CompactionPercentage = options.CompactionPercentage,
            ExpirationScanFrequency = options.MaximumSizeScanFrequency,
            Clock = ClockWrapper.GetMemoryCacheSystemClock(clock)
        };

        cache = new MemoryCache(memoryCacheOptions);
        cacheEntryTrackings = new ConcurrentDictionary<TCacheKey, CacheEntryTracking<TCacheKey>>();
    }

    void ICacheForStartup<TCacheKey, TValue>.Insert(IEnumerable<TValue> valuesToInsert)
    {
        DoUpsert(valuesToInsert, isRequestHandling: false);
    }

    ICollection<CacheEntryTracking<TCacheKey>> ICacheForRefreshing<TCacheKey, TValue>.GetCacheEntryTrackings()
    {
        return cacheEntryTrackings.Values;
    }

    void ICacheForRefreshing<TCacheKey, TValue>.Remove(IEnumerable<TCacheKey> keysToRemove)
    {
        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
            cacheEntryTrackings.TryRemove(key, out CacheEntryTracking<TCacheKey>? _);
        }
    }

    void ICacheForRefreshing<TCacheKey, TValue>.Update(IEnumerable<TValue> valuesToUpdate)
    {
        DoUpsert(valuesToUpdate, isRequestHandling: false);
    }

    bool ICacheForRequestHandling<TCacheKey, TValue>.TryGetValue(TCacheKey key, out TValue? value)
    {
        var found = cache.TryGetValue(key, out value);

        if (found)
            UpdateCacheEntryTracking(key, value!, isRequestHandling: true);

        return found;
    }

    void ICacheForRequestHandling<TCacheKey, TValue>.Upsert(TCacheKey key, TValue value)
    {
        DoUpsert(key, value, isRequestHandling: true);
    }

    private void DoUpsert(IEnumerable<TValue> valuesToUpsert, bool isRequestHandling)
    {
        foreach (var valueToUpsert in valuesToUpsert)
        {
            var key = cacheKeyFactory.GetCacheKey(valueToUpsert);
            DoUpsert(key, valueToUpsert, isRequestHandling);
        }
    }

    private void DoUpsert(TCacheKey key, TValue value, bool isRequestHandling)
    {
        var memoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriorityMapping.MapPriority(cacheEntryOptionsFactory.GetPriority(key, value)),
            Size = cacheEntryOptionsFactory.GetSize(key, value)
        };

        cache.Set(key, value, memoryCacheEntryOptions);
        UpdateCacheEntryTracking(key, value, isRequestHandling: isRequestHandling);
    }

    // TODO: Ist die Methode gut? isFromRequestHandling scheint mir ein Code Smell zu sein. Ein GetOrInit(key, value) würde das Problem besser lösen.
    private void UpdateCacheEntryTracking(TCacheKey key, TValue value, bool isRequestHandling)
    {
        if (cacheEntryTrackings.TryGetValue(key, out CacheEntryTracking<TCacheKey>? cacheEntryTracking))
        {
            if (isRequestHandling)
                cacheEntryTracking.RemoveAfter = cacheEntryOptionsFactory.GetSlidingExpiration(key, value);

            if (!isRequestHandling)
                cacheEntryTracking.RefreshAfter = cacheEntryOptionsFactory.GetRefreshAfter(key, value);
        }
        else
        {
            cacheEntryTrackings[key] = new CacheEntryTracking<TCacheKey>
            {
                CacheKey = key,
                RemoveAfter = cacheEntryOptionsFactory.GetSlidingExpiration(key, value),
                RefreshAfter = cacheEntryOptionsFactory.GetRefreshAfter(key, value)
            };
        }
    }

    public void Dispose()
    {
        cache.Dispose();
    }
}
