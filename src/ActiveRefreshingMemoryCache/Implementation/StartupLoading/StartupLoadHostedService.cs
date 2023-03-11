using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActiveRefreshingMemoryCache.Implementation.StartupLoading;

internal class StartupLoadHostedService<TCacheKey, TValue> : BackgroundService
    where TCacheKey : notnull
{
    private readonly CacheOptions options;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<StartupLoadHostedService<TCacheKey, TValue>> logger;
    private readonly IHostApplicationLifetime hostApplicationLifetime;

    internal StartupLoadHostedService(
        IOptions<CacheOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<StartupLoadHostedService<TCacheKey, TValue>> logger)
    {
        this.options = options.Value;
        this.serviceScopeFactory = serviceScopeFactory;
        this.hostApplicationLifetime = hostApplicationLifetime;
        this.logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var exceptionCounter = 0;

        do
        {
            try
            {
                await LoadOnStartupWhileBlockingStartupAsync(cancellationToken);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ++exceptionCounter;
                logger.LogError(ex, $"Could not start {nameof(StartupLoadHostedService<TCacheKey, TValue>)} (blocking) {exceptionCounter} times.");

                if (ShouldShutDown(exceptionCounter))
                    hostApplicationLifetime.StopApplication();
            }
        } while (IsAbortingOnExceptionEnabled());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var exceptionCounter = 0;

        do
        {
            try
            {
                await LoadOnStartupWithoutBlockingStartupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                ++exceptionCounter;
                logger.LogError(ex, $"Could not start {nameof(StartupLoadHostedService<TCacheKey, TValue>)} (without blocking) {exceptionCounter} times.");

                if (ShouldShutDown(exceptionCounter))
                    hostApplicationLifetime.StopApplication();
            }
        } while (IsAbortingOnExceptionEnabled());
    }

    private async Task LoadOnStartupWhileBlockingStartupAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheForStartup<TCacheKey, TValue>>();
        var dataProvider = scope.ServiceProvider.GetRequiredService<IStartupDataProvider<TCacheKey, TValue>>();

        var values = await dataProvider.LoadOnStartupWhileBlockingStartupAsync(cancellationToken);
        cache.Insert(values);
    }

    private async Task LoadOnStartupWithoutBlockingStartupAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheForStartup<TCacheKey, TValue>>();
        var dataProvider = scope.ServiceProvider.GetRequiredService<IStartupDataProvider<TCacheKey, TValue>>();

        var values = await dataProvider.LoadOnStartupWithoutBlockingStartupAsync(stoppingToken);
        cache.Insert(values);
    }

    private bool ShouldShutDown(int exceptionCounter)
    {
        return IsAbortingOnExceptionEnabled()
            && exceptionCounter >= options.AbortHostStartupAfterHowManyExceptions;
    }

    private bool IsAbortingOnExceptionEnabled()
    {
        return options.AbortHostStartupAfterHowManyExceptions >= 0;
    }
}
