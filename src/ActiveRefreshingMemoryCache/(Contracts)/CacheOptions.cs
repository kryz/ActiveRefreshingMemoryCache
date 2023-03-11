namespace ActiveRefreshingMemoryCache;

public class CacheOptions
{
    /// <summary>
    /// Should the Host Startup be aborted, when an exception occures during initialization of the cache?
    /// 
    /// Loading data can fail with an exception. Here you configure the number of retries after which the service will be shutdown.
    /// -1 to not shut down the service.
    /// </summary>
    public int AbortHostStartupAfterHowManyExceptions { get; set; } = -1; // TODO: Brauchts das?

    /// <summary>
    /// Refreshing can fail with an exception. Here you configure the number of consecutive exceptions after which the service will be shutdown.
    /// -1 to never shut down the service.
    /// </summary>
    public int ShutDownServiceAfterHowManyFailedRefreshes { get; set; } = -1; // TODO: Brauchts das?

    /// <summary>
    /// The Time waited after a refresh run finished until the next refresh is started.
    /// </summary>
    /// <remarks>
    /// During a refresh job, entries with expired <see cref="ICacheEntryOptionsFactory{TCacheKey, TValue}.GetRefreshAfter(TCacheKey, TValue)"/> are refreshed.
    /// And entries with expired <see cref="ICacheEntryOptionsFactory{TCacheKey, TValue}.GetSlidingExpiration(TCacheKey, TValue)"/> are removed from the cache.
    /// This value determines, how often the underlying data source (e.g. database) gets a bulk-request for the stale entries.
    /// </remarks>
    public TimeSpan RefreshJobFrequency { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The maximal size of the cache. The size of each cache entry is specified in <see cref="ICacheEntryOptionsFactory{TCacheKey, TValue}.GetSize(TCacheKey, TValue)"/>.
    /// </summary>
    public long SizeLimit { get; set; }

    /// <summary>
    /// The amount to compact the cache by when the maximum size is exceeded.
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.05;

    /// <summary>
    /// The minimum length of time between successive checks whether the maximal size is reached.
    /// </summary>
    public TimeSpan MaximumSizeScanFrequency { get; set; } = TimeSpan.FromMinutes(1);
}
