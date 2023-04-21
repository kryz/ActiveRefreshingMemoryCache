using Microsoft.Extensions.Logging;

namespace ActiveRefreshingMemoryCache.Implementation.RequestHandling;

internal class RequestHandler<TCacheKey, TValue>
    where TCacheKey : notnull
{
    private readonly List<RequestEntry<TCacheKey, TValue>> requestEntries;
    private readonly Dictionary<TCacheKey, RequestEntry<TCacheKey, TValue>> requestEntriesMissedInCache;
    private readonly ICacheForRequestHandling<TCacheKey, TValue> cache;
    private readonly ICacheMissDataProvider<TCacheKey, TValue> dataProvider;
    private readonly ICacheKeyFactory<TCacheKey, TValue> cacheKeyFactory;
    private readonly ILogger logger;
    private readonly CancellationToken cancellationToken;

    public RequestHandler(
        IEnumerable<TCacheKey> requestedKeys,
        ICacheForRequestHandling<TCacheKey, TValue> cache,
        ICacheMissDataProvider<TCacheKey, TValue> dataProvider,
        ICacheKeyFactory<TCacheKey, TValue> cacheKeyFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        requestEntries = requestedKeys
            .Select(key => new RequestEntry<TCacheKey, TValue>(key))
            .ToList();

        requestEntriesMissedInCache = new();

        this.cache = cache;
        this.dataProvider = dataProvider;
        this.cacheKeyFactory = cacheKeyFactory;
        this.logger = logger;
        this.cancellationToken = cancellationToken;
    }

    internal async Task<IEnumerable<TValue>> GetAsync()
    {
        SearchCache();
        await LoadEntriesMissedInCacheAsync();

        return requestEntries
            .Where(entry => entry.IsValueFound)
            .Select(entry => entry.Value!)
            .ToList();
    }

    private void SearchCache()
    {
        foreach (var requestEntry in requestEntries)
        {
            if (cache.TryGetValue(requestEntry.Key, out TValue? value))
                requestEntry.FoundValue(value!);
            else
                requestEntriesMissedInCache[requestEntry.Key] = requestEntry;
        }

        var missed = requestEntriesMissedInCache.Count;
        var found = requestEntries.Count - missed;
        logger.LogDebug("Found in cache: {found}. Missed in cache: {missed}.", found, missed);
    }

    private async Task LoadEntriesMissedInCacheAsync()
    {
        if (!requestEntriesMissedInCache.Any())
            return;

        var loadedValues = await dataProvider.LoadOnCacheMissAsync(requestEntriesMissedInCache.Keys, cancellationToken);

        foreach (var loadedValue in loadedValues)
        {
            var cacheKey = cacheKeyFactory.GetCacheKey(loadedValue);
            if (!requestEntriesMissedInCache.TryGetValue(cacheKey, out var loadedRequestEntry))
                continue;

            loadedRequestEntry.FoundValue(loadedValue);
            cache.Upsert(loadedRequestEntry.Key, loadedRequestEntry.Value!);
        }
    }
}
