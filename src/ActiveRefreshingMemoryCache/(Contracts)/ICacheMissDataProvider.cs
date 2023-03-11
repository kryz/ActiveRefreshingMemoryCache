namespace ActiveRefreshingMemoryCache;

public interface ICacheMissDataProvider<TCacheKey, TValue>
    where TCacheKey : notnull
{
    /// <summary>
    /// Load values that were not found in the cache.
    /// </summary>
    /// <remarks>
    /// The result doesn't need to be in the order of the cacheKeys. Sorting is done by the Cache.
    /// If a value can not be found in your data source, don't add an entry to the result. I.e. don't add a null, or default.
    /// </remarks>
    Task<IEnumerable<TValue>> LoadOnCacheMissAsync(IEnumerable<TCacheKey> cacheKeys, CancellationToken cancellationToken);
}
