namespace ActiveRefreshingMemoryCache;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
