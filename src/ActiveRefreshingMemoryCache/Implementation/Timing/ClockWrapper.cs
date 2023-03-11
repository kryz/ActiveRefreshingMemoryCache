using Microsoft.Extensions.Internal;

namespace ActiveRefreshingMemoryCache.Implementation.Timing;

internal class ClockWrapper : IClock, ISystemClock
{
    internal static ISystemClock GetMemoryCacheSystemClock(IClock clock)
    {
        // Both, the DefaultClock and ClockWrapper implement both interfaces (IClock & SystemClock).
        // If an IClock implementation from outside this library is given, I need to wrappe it.
        var systemClock = clock as ISystemClock;
        if (systemClock is not null)
            return systemClock;
        return new ClockWrapper(clock);
    }

    private readonly IClock wrappedClock;

    internal ClockWrapper(IClock clock)
    {
        wrappedClock = clock;
    }

    public DateTimeOffset UtcNow => wrappedClock.UtcNow;
}
