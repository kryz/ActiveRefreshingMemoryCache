namespace ActiveRefreshingMemoryCache.Implementation.RequestHandling;

internal class RequestEntry<TCacheKey, TValue>
    where TCacheKey : notnull
{
    internal RequestEntry(TCacheKey key)
    {
        Key = key;
    }

    internal TCacheKey Key { get; }

    internal TValue? Value { get; private set; }

    internal bool IsValueFound { get; private set; }

    internal void FoundValue(TValue value)
    {
        Value = value;
        IsValueFound = true;
    }
}
