namespace ActiveRefreshingMemoryCache;

public interface ICache<TCacheKey, TValue>
    where TCacheKey : notnull
{
    /// <summary>
    /// Search the cacheKey in the cache. If it is missing, it is loaded from <see cref="ICacheMissDataProvider{TCacheKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// If the key is neither in the cache, nor can be loaded from <see cref="ICacheMissDataProvider{TCacheKey, TValue}"/>, null is returned.
    /// </remarks>
    Task<TValue?> GetAsync(TCacheKey cacheKey, CancellationToken cancellationToken);

    /// <summary>
    /// Search the cacheKeys in the cache. The keys not found are loaded from <see cref="ICacheMissDataProvider{TCacheKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// The returned values are in the same order as the requested cacheKeys.
    /// CacheKeys that are neither in the cache, nor could be loaded from <see cref="ICacheMissDataProvider{TCacheKey, TValue}"/>, will be missing tough.
    /// </remarks>
    Task<IEnumerable<TValue>> GetAsync(IEnumerable<TCacheKey> cacheKeys, CancellationToken cancellationToken);
}
