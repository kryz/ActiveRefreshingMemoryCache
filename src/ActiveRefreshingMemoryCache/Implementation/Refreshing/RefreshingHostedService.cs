using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActiveRefreshingMemoryCache.Implementation.Refreshing;

internal class RefreshingHostedService<TCacheKey, TValue> : BackgroundService
    where TCacheKey : notnull
{
    private readonly CacheOptions options;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<RefreshingHostedService<TCacheKey, TValue>> logger;
    private readonly IHostApplicationLifetime hostApplicationLifetime;

    private DateTimeOffset lastRefreshStartedAt;
    private int exceptionCounter = 0;

    internal RefreshingHostedService(
        IOptions<CacheOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<RefreshingHostedService<TCacheKey, TValue>> logger)
    {
        this.options = options.Value;
        this.serviceScopeFactory = serviceScopeFactory;
        this.hostApplicationLifetime = hostApplicationLifetime;
        this.logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            return base.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not start {nameof(RefreshingHostedService<TCacheKey, TValue>)}.");
            return Task.CompletedTask;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await TryExecuteAsync(stoppingToken);
            await Task.Delay(options.RefreshJobFrequency, stoppingToken);
        }
    }

    private async Task TryExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await DoRefreshAsync(stoppingToken);
            exceptionCounter = 0;
        }
        catch (Exception ex)
        {
            ++exceptionCounter;
            logger.LogError(ex, $"Refreshing {nameof(RefreshingHostedService<TCacheKey, TValue>)} failed {exceptionCounter} times.");

            if (options.ShutDownServiceAfterHowManyFailedRefreshes >= 0 && options.ShutDownServiceAfterHowManyFailedRefreshes <= exceptionCounter)
                hostApplicationLifetime.StopApplication();
        }
    }

    private async Task DoRefreshAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheForRefreshing<TCacheKey, TValue>>();
        var dataProvider = scope.ServiceProvider.GetRequiredService<IRefreshDataProvider<TCacheKey, TValue>>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        RemoveUnusedEntries(cache, clock, stoppingToken);
        await RefreshAsync(cache, dataProvider, clock, stoppingToken);
    }

    private void RemoveUnusedEntries(ICacheForRefreshing<TCacheKey, TValue> cache, IClock clock, CancellationToken stoppingToken)
    {
        var keysToRemove = cache.GetCacheEntryTrackings()
            .Where(tracking => tracking.RemoveAfter <= clock.UtcNow)
            .Select(tracking => tracking.CacheKey);
        cache.Remove(keysToRemove);
    }

    private async Task RefreshAsync(
        ICacheForRefreshing<TCacheKey, TValue> cache,
        IRefreshDataProvider<TCacheKey, TValue> dataProvider,
        IClock clock,
        CancellationToken stoppingToken)
    {
        logger.LogDebug("Refresh cash");

        var keysToRefresh = cache.GetCacheEntryTrackings()
            .Where(tracking => tracking.RefreshAfter <= clock.UtcNow)
            .Select(tracking => tracking.CacheKey);

        // TODO Batching?: A lot of entries can have the same RefreshAfter. Especially on the Startup a lot of values are loaded together. Therefore batching would be nice. Configurable in the options.
        // Although: The batching could also be done in dataProvider.LoadOnRefreshAsync()
        // Would it be better in cache.Update to only process batches?
        // The memory consumption of the refresh process would be smaller. -> Batching would be nice!
        var loadedValues = await dataProvider.LoadOnRefreshAsync(keysToRefresh, stoppingToken);
        cache.Update(loadedValues);
    }
}
