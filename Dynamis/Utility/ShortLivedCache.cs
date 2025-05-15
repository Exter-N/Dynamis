using System.Diagnostics.CodeAnalysis;

namespace Dynamis.Utility;

public sealed class ShortLivedCache<TKey, TValue> : IDisposable where TKey : IEquatable<TKey>
{
    private const uint InitialTimeToLive = 2;

    private readonly Dictionary<TKey, Entry> _entries     = [];
    private readonly HashSet<TKey>           _expiredKeys = [];

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_entries.TryGetValue(key, out var entry)) {
            entry.Refresh();
            value = entry.Value;
            return true;
        }

        value = default;
        return false;
    }

    public void Add(TKey key, TValue value)
        => _entries.Add(key, new(value));

    public bool TryAdd(TKey key, TValue value)
        => _entries.TryAdd(key, new(value));

    public void Clear()
    {
        foreach (var slice in _entries.Values) {
            slice.Dispose();
        }

        _entries.Clear();
    }

    public void Tick()
    {
        try {
            foreach (var (key, entry) in _entries) {
                if (!entry.Tick()) {
                    _expiredKeys.Add(key);
                }
            }

            foreach (var key in _expiredKeys) {
                _entries.Remove(key);
            }
        } finally {
            _expiredKeys.Clear();
        }
    }

    public void Dispose()
        => Clear();

    private sealed class Entry(TValue value) : IDisposable
    {
        public readonly TValue Value = value;

        private uint _timeToLive = InitialTimeToLive;

        public void Refresh()
        {
            _timeToLive = InitialTimeToLive;
        }

        public bool Tick()
        {
            if (unchecked(_timeToLive--) > 0) {
                return true;
            }

            (Value as IDisposable)?.Dispose();
            return false;
        }

        public void Dispose()
        {
            (Value as IDisposable)?.Dispose();
        }
    }
}
