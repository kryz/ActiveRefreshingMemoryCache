namespace ActiveRefreshingMemoryCache;

public interface IRefreshDataProvider<TCacheKey, TValue>
    where TCacheKey : notnull
{
    /// <summary>
    /// Loads values when they should be refreshed in the cache.
    /// </summary>
    /// <remarks>
    /// The result doesn't need to be in the order of the cacheKeys.
    /// If a value can not be found in your data source, don't add an entry to the result. I.e. don't add a null, or default.
    /// </remarks>
    Task<IEnumerable<TValue>> LoadOnRefreshAsync(IEnumerable<TCacheKey> cacheKeys, CancellationToken cancellationToken);
}
