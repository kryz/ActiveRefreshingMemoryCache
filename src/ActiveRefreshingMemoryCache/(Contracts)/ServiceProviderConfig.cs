using ActiveRefreshingMemoryCache.Implementation.Refreshing;
using ActiveRefreshingMemoryCache.Implementation.RequestHandling;
using ActiveRefreshingMemoryCache.Implementation.StartupLoading;
using ActiveRefreshingMemoryCache.Implementation.TheCache;
using ActiveRefreshingMemoryCache.Implementation.Timing;
using Microsoft.Extensions.DependencyInjection;

namespace ActiveRefreshingMemoryCache;

/* More TODOs
 * 
 * Known Bugs
 *  - Nochmals durchlesen, was passiert wenn die CacheEntryTrackings nicht konsitent sind mit dem Cache (passiert immer wenn Einträge wegen SizeLimit removed werden. Kann aber auch durch race conditions passieren.)
 *  -- Nach dem Removen von CachEntries wegen erreichen des SizeLimits, bleiben aktuell die CacheEntryTracking erhalten. Dies führt dazu, dass diese beim Refresh wieder in den Cache geladen werden.
 *  -- Lösung 1: Auf die MemoryCacheEntryOptions.PostEvictionCallbackRegistration registrieren. Darin jeweils die passenden CacheEntryTrackings entfernen. (Damit könnte ich auch im TheCacheEntry.Remove() nur noch aus dem Cache löschen. Und CacheEntryTrackings über den PostEvictionCallbackRegistration Handler löschen lassen.)
 *  -- Lösung 2: Vor dem Refresh eines Entries schauen, ob er auch wirklich noch im Cache vorhanden ist. Falls nicht, den entsprechenden CacheEntryTracking löschen.
 * 
 * - TESTEN
 * -- In einem Testprojekt mal einbinden
 * -- Unittesten
 * 
 * Readability
 * - Nochmals durchlesen. Die Readability ist wahrscheinlich noch verbesserungsfähig.
 * 
 * Monitoring
 * - Logging ist noch praktisch gar nicht vorhanden.
 * 
 * Fetures verbessern
 * - Siehe Todos im Code
 * - TCacheEntryOptionsFactory.GetPriority() 
 * -- Ich könnte mitgeben, wann der Value zuletzt gelesen wurde. Dann könnte man lange nicht mehr gelesenen Entries eine tiefere Priorität geben. Somit würden diese bei knappem Cache eher removed.
 * -- Alternative: 
 *      Auf TheCacheInstance gibt es ein DateTimeOffset CreatedAt & einen ulong readCounter. Dieser wird mit Interlocked.Increment bei jedem Lesezugriff erhöht.
 *      Auf den CacheEntryTrackings gibt es ein DateTimeOffset CreatedAt & einen ulong readCounter. Dieser wird bei jedem Lesezugriff auf den CacheEntry erhöht.
 *      GetPriority() bekommt jeweils diese 4 Werte.
 *      Dadurch kann man abschätzen, ob ein Eintrag im Vergleich zu den anderen Einträgen häufig oder selten gelesen wird.
 *      Das könnte ich sogar als Referenzimplementierung in der Library lösen. Das scheint allgemeingültig zu sein. Jedoch überschreibbar im GetPriority(). 
 *      Je nach UseCase ist eine statische Priorisierung anhand des Value sinnvoller.
 *      
 * Possible improvements
 * - Überall nochmals prüfen ob class vs. record (vs. struct)
 */

public static class ServiceProviderConfig
{
    /// <summary>
    /// Add ActiveRefreshingMemoryCache to DI, including the dependencies that must be implemented by the user of the library.
    /// </summary>
    /// <remarks>
    /// Remarks about <see cref="ServiceLifetime"/>:
    /// - <see cref="TCacheEntryOptionsFactory{TCacheKey, TValue}"/> and <see cref="ICacheKeyFactory{TCacheKey, TValue}"/> are always registered as singeltons.
    /// - The DataProviders are registered with the given <see cref="ServiceLifetime"/>
    /// 
    /// Remarks about implementing multiple interfaces in one class
    /// - The three DataProvider interfaces can be implemented by the same class.
    /// - The two Factory interfaces can be implemented by the same class.
    /// - The DataProvider interfaces and Factory interfaces can only be implemented in the same class, if <see cref="ServiceLifetime.Singleton"/> is used.
    /// </remarks>
    public static IServiceCollection AddActiveRefreshingMemoryCache<
        TCacheKey,
        TValue,
        TCacheEntryOptionsFactory, 
        TCacheKeyFactory, 
        TCacheMissDataProvider, 
        TRefreshDataProvider, 
        TStartupDataProvider>
        (this IServiceCollection services, ServiceLifetime serviceLifetime, Action<CacheOptions> cacheOptions)
        where TCacheKey : notnull
        where TCacheEntryOptionsFactory : class, ICacheEntryOptionsFactory<TCacheKey, TValue>
        where TCacheKeyFactory : class, ICacheKeyFactory<TCacheKey, TValue>
        where TCacheMissDataProvider : class, ICacheMissDataProvider<TCacheKey, TValue>
        where TRefreshDataProvider : class, IRefreshDataProvider<TCacheKey, TValue>
        where TStartupDataProvider : class, IStartupDataProvider<TCacheKey, TValue>
    {
        services
            .Configure(cacheOptions)

            // TheCacheInstance has a dependency, so this must be singleton
            .AddSingleton<TCacheEntryOptionsFactory>()
            .AddSingleton<ICacheEntryOptionsFactory<TCacheKey, TValue>, TCacheEntryOptionsFactory>(provider => provider.GetRequiredService<TCacheEntryOptionsFactory>())

            // TheCacheInstance hase a dependency, so this must be singleton
            .AddSingleton<TCacheKeyFactory>()
            .AddSingleton<ICacheKeyFactory<TCacheKey, TValue>, TCacheKeyFactory>(provider => provider.GetRequiredService<TCacheKeyFactory>())

            .Add<TCacheMissDataProvider>(serviceLifetime)
            .Add<ICacheMissDataProvider<TCacheKey, TValue>, TCacheMissDataProvider>(serviceLifetime, provider => provider.GetRequiredService<TCacheMissDataProvider>())

            .Add<TRefreshDataProvider>(serviceLifetime)
            .Add<IRefreshDataProvider<TCacheKey, TValue>, TRefreshDataProvider>(serviceLifetime, provider => provider.GetRequiredService<TRefreshDataProvider>())

            .Add<TStartupDataProvider>(serviceLifetime)
            .Add<IStartupDataProvider<TCacheKey, TValue>, TStartupDataProvider>(serviceLifetime, provider => provider.GetRequiredService<TStartupDataProvider>());

        services.AddActiveRefreshingMemoryCache<TCacheKey, TValue>(serviceLifetime);

        return services;
    }

    /// <summary>
    /// This method only adds the internals of the library. Using this meethod, you are responsible to register all dependencies by yourself.
    /// </summary>
    public static IServiceCollection AddActiveRefreshingMemoryCache<TCacheKey, TValue>(this IServiceCollection services, ServiceLifetime serviceLifetime)
        where TCacheKey : notnull
    {
        services.Add<ICache<TCacheKey, TValue>, CacheFacade<TCacheKey, TValue>>(serviceLifetime);

        services.AddHostedService<StartupLoadHostedService<TCacheKey, TValue>>();
        services.AddHostedService<RefreshingHostedService<TCacheKey, TValue>>();

        services.AddSingleton<TheCacheInstance<TCacheKey, TValue>>();
        services.AddSingleton<ICacheForStartup<TCacheKey, TValue>>(provider => provider.GetRequiredService<TheCacheInstance<TCacheKey, TValue>>());
        services.AddSingleton<ICacheForRefreshing<TCacheKey, TValue>>(x => x.GetRequiredService<TheCacheInstance<TCacheKey, TValue>>());
        services.AddSingleton<ICacheForRequestHandling<TCacheKey, TValue>>(x => x.GetRequiredService<TheCacheInstance<TCacheKey, TValue>>());
        
        services.AddSingleton<IClock, DefaultClock>();

        return services;
    }

    private static IServiceCollection Add<TService>(this IServiceCollection services, ServiceLifetime serviceLifetime)
    where TService : class
    {
        return serviceLifetime switch
        {
            ServiceLifetime.Transient => services.AddTransient<TService>(),
            ServiceLifetime.Scoped => services.AddScoped<TService>(),
            ServiceLifetime.Singleton => services.AddSingleton<TService>(),
            _ => throw new ArgumentException($"Unkown ServiceLifeTime {serviceLifetime}."),
        };
    }

    private static IServiceCollection Add<TService, TImplementation>(this IServiceCollection services, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService
    {
        return serviceLifetime switch
        {
            ServiceLifetime.Transient => services.AddTransient<TService, TImplementation>(),
            ServiceLifetime.Scoped => services.AddScoped<TService, TImplementation>(),
            ServiceLifetime.Singleton => services.AddSingleton<TService, TImplementation>(),
            _ => throw new ArgumentException($"Unkown ServiceLifeTime {serviceLifetime}."),
        };
    }

    private static IServiceCollection Add<TService, TImplementation>(this IServiceCollection services, ServiceLifetime serviceLifetime, Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        return serviceLifetime switch
        {
            ServiceLifetime.Transient => services.AddTransient<TService, TImplementation>(implementationFactory),
            ServiceLifetime.Scoped => services.AddScoped<TService, TImplementation>(implementationFactory),
            ServiceLifetime.Singleton => services.AddSingleton<TService, TImplementation>(implementationFactory),
            _ => throw new ArgumentException($"Unkown ServiceLifeTime {serviceLifetime}."),
        };
    }
}
