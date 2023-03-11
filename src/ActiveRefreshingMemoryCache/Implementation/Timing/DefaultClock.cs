using Microsoft.Extensions.Internal;

namespace ActiveRefreshingMemoryCache.Implementation.Timing;

internal class DefaultClock : IClock, ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
