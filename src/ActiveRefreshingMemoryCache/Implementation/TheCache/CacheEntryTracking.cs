namespace ActiveRefreshingMemoryCache.Implementation.TheCache;

internal class CacheEntryTracking<TCacheKey>
    where TCacheKey : notnull
{
    internal required TCacheKey CacheKey { get; init; }

    internal required DateTimeOffset RemoveAfter { get; set; }

    internal required DateTimeOffset RefreshAfter { get; set; }
}