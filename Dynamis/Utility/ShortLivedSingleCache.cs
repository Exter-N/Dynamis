using System.Diagnostics.CodeAnalysis;

namespace Dynamis.Utility;

public abstract class ShortLivedSingleCache : IEquatable<ShortLivedSingleCache>
{
    private static int _nextId;

    private readonly int _id = Interlocked.Increment(ref _nextId);

    bool IEquatable<ShortLivedSingleCache>.Equals(ShortLivedSingleCache? other)
        => ReferenceEquals(this, other);

    public override int GetHashCode()
        => HashCode.Combine(_id);

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);
}

public sealed class ShortLivedSingleCache<T>(
    ShortLivedCache<ShortLivedSingleCache, ShortLivedSingleCache> container,
    Func<T>? baseFactory)
    : ShortLivedSingleCache, IDisposable
{
    private bool _initialized;
    private T?   _value;

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        lock (container) {
            // Refresh our entry.
            container.TryGetValue(this, out _);
        }

        value = _value;
        return _initialized;
    }

    public void SetValue(T value)
    {
        _value = value;
        if (_initialized) {
            return;
        }

        _initialized = true;
        lock (container) {
            container.TryAdd(this, this);
        }
    }

    public bool TrySetValue(T value)
    {
        if (_initialized) {
            return false;
        }

        _value = value;
        _initialized = true;
        lock (container) {
            container.TryAdd(this, this);
        }

        return true;
    }

    public T GetOrCreateValue(Func<T>? factory = null)
    {
        if (_initialized) {
            lock (container) {
                // Refresh our entry.
                container.TryGetValue(this, out _);
            }
        } else {
            _value = (factory ?? baseFactory)!();
            _initialized = true;
            lock (container) {
                container.TryAdd(this, this);
            }
        }

        return _value!;
    }

    void IDisposable.Dispose()
    {
        (_value as IDisposable)?.Dispose();
        _initialized = false;
        _value = default;
    }
}
