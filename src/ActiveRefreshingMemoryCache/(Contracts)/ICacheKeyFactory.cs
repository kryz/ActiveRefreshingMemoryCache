namespace ActiveRefreshingMemoryCache;

public interface ICacheKeyFactory<TCacheKey, TValue> 
    where TCacheKey : notnull
{
    /// <summary>
    /// Get the cache key for the value.
    /// The cache key must always be the same for the same value.
    /// The cache key must be unique, i.e. different values must have different cache keys.
    /// The cache key must not be null.
    /// </summary>
    TCacheKey GetCacheKey(TValue value);
}
