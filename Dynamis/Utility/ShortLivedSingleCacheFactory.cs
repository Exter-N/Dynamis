namespace Dynamis.Utility;

public sealed class ShortLivedSingleCacheFactory
{
    private readonly ShortLivedCache<ShortLivedSingleCache, ShortLivedSingleCache> _cache = new();

    public ShortLivedSingleCache<T> Create<T>(Func<T>? factory = null)
        => new(_cache, factory);

    public void Tick()
    {
        lock (_cache) {
            _cache.Tick();
        }
    }
}
