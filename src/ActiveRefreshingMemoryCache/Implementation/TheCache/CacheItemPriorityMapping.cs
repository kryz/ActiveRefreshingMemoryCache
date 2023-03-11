namespace ActiveRefreshingMemoryCache.Implementation.TheCache;

internal static class CacheItemPriorityMapping
{
    internal static Microsoft.Extensions.Caching.Memory.CacheItemPriority MapPriority(CacheItemPriority priority)
    {
        return priority switch
        {
            CacheItemPriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
            CacheItemPriority.Normal => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
            CacheItemPriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
            CacheItemPriority.NeverRemove => Microsoft.Extensions.Caching.Memory.CacheItemPriority.NeverRemove,
            _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
        };
    }
}
