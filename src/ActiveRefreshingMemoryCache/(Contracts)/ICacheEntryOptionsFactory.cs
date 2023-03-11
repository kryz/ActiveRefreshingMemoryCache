namespace ActiveRefreshingMemoryCache;

/// <summary>
/// Here you can finetune the memory consumption and load on <see cref="IRefreshDataProvider{TCacheKey, TValue}"/> and <see cref="ICacheMissDataProvider{TCacheKey, TValue}"/>.
/// </summary>
public interface ICacheEntryOptionsFactory<TCacheKey, TValue>
    where TCacheKey : notnull
{
    /// <summary>
    /// At what time should the entry be refreshed?
    /// </summary>
    /// <remarks>
    /// Refreshing only happens in intervals, see <see cref="CacheOptions.RefreshJobFrequency"/>, not at the exact time given here.
    /// 
    /// Fine tuning ideas:
    /// - During night time hardly any updates to the values are expected? Return higher values during the night.
    /// - You would like to prevent refreshing many entries at the same time? Distribute the RefreshAfters over a larger TimeSpan than <see cref="CacheOptions.RefreshJobFrequency"/>. E.g. by randomizing them.
    /// </remarks>
    DateTimeOffset GetRefreshAfter(TCacheKey cacheKey, TValue value);

    /// <summary>
    /// How long can the cache entry be inactive (i.e. not accessed) before it will be removed.
    /// </summary>
    /// <remarks>
    /// This gets called very frequently: On every read of an entry (and on inserting an entry to the cache).
    /// 
    /// Fine tuning ideas:
    /// - During night time less reads will happen? Return higher SlidingExpirations, so the cache doesn't loos too many entries.
    /// </remarks>
    DateTimeOffset GetSlidingExpiration(TCacheKey cacheKey, TValue value);

    /// <summary>
    /// Gets or sets the priority for keeping the cache entry in the cache during a memory pressure triggered cleanup. The default is <see cref="CacheItemPriority.Normal"/>.
    /// Inside the priority group, the removal is random.
    /// </summary>
    CacheItemPriority GetPriority(TCacheKey cacheKey, TValue value);

    /// <summary>
    /// Gets or sets the size of the cache entry value. The size corresponds to <see cref="CacheOptions.SizeLimit"/>.
    /// </summary>
    long GetSize(TCacheKey cacheKey, TValue value);
}
