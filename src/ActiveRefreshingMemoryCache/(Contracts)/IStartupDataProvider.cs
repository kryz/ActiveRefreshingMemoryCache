namespace ActiveRefreshingMemoryCache;

public interface IStartupDataProvider<TCacheKey, TValue>
    where TCacheKey : notnull
{
    /// <summary>
    /// Load values that should be added to the cache on starting up the service.
    /// </summary>
    /// <remarks>
    /// Keep this method as short as possible, as it will delay the startup of the service.
    /// 
    /// Background: This Method will be called in a HostedService in its StartAsync Method.
    /// From https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio#startasync:
    /// StartAsync should be limited to short running tasks because hosted services are run sequentially, and no further services are started until StartAsync runs to completion.
    /// </remarks>
    Task<IEnumerable<TValue>> LoadOnStartupWhileBlockingStartupAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Load values that should be added to the cache on starting up the service.
    /// </summary>
    /// <remarks>
    /// This method does not delay the startup of the service. 
    /// Caveat: It is possible that service startup finishes and the service starts to process its workload, before this method completes.
    /// This would not lead to any inconsistency though. But more cache misses could occure, and therefore loading the values while querying the cache.
    /// </remarks>
    Task<IEnumerable<TValue>> LoadOnStartupWithoutBlockingStartupAsync(CancellationToken cancellationToken);
}
