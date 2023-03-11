using ActiveRefreshingMemoryCache.Implementation.TheCache;
using Microsoft.Extensions.Logging;

namespace ActiveRefreshingMemoryCache.Implementation.RequestHandling;

internal class CacheFacade<TCacheKey, TValue> : ICache<TCacheKey, TValue>
    where TCacheKey : notnull
{
    private readonly TheCacheInstance<TCacheKey, TValue> theCacheInstance;
    private readonly ICacheMissDataProvider<TCacheKey, TValue> dataProvider;
    private readonly ICacheKeyFactory<TCacheKey, TValue> cacheKeyFactory;
    private readonly ILogger<CacheFacade<TCacheKey, TValue>> logger;

    public CacheFacade(
        TheCacheInstance<TCacheKey, TValue> theCacheInstance,
        ICacheMissDataProvider<TCacheKey, TValue> dataProvider,
        ICacheKeyFactory<TCacheKey, TValue> cacheKeyFactory,
        ILogger<CacheFacade<TCacheKey, TValue>> logger)
    {
        this.theCacheInstance = theCacheInstance;
        this.dataProvider = dataProvider;
        this.cacheKeyFactory = cacheKeyFactory;
        this.logger = logger;
    }

    public async Task<TValue?> GetAsync(TCacheKey cacheKey, CancellationToken cancellationToken)
    {
        var result = await GetAsync(new List<TCacheKey> { cacheKey }, cancellationToken);
        return result.FirstOrDefault();
    }

    public async Task<IEnumerable<TValue>> GetAsync(IEnumerable<TCacheKey> cacheKeys, CancellationToken cancellationToken)
    {
        var handler = new RequestHandler<TCacheKey, TValue>(
            cacheKeys,
            theCacheInstance,
            dataProvider,
            cacheKeyFactory,
            logger,
            cancellationToken);

        return await handler.GetAsync();
    }
}
