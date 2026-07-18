namespace Dynamis.Utility;

public ref struct SpanList<T>(Span<T> span)
{
    private readonly Span<T> _span = span;
    private          int     _count;

    public int Count
        => _count;

    public ref T this[int index]
        => ref AsSpan()[index];

    public Span<T>.Enumerator GetEnumerator()
        => AsSpan().GetEnumerator();

    public int Add(T item)
    {
        if (_count == _span.Length) {
            throw new InvalidOperationException("This list is full");
        }

        var index = _count++;
        _span[index] = item;
        return index;
    }

    public void Clear()
        => _count = 0;

    public bool Contains(T item)
        => AsSpan().Contains(item);

    public Span<T> AsSpan()
        => _span[.._count];

    public void CopyTo(T[] array, int arrayIndex)
        => AsSpan().CopyTo(array.AsSpan(arrayIndex));

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

    public int IndexOf(T item)
        => AsSpan().IndexOf(item);

    public void Insert(int index, T item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _count);
        if (_count == _span.Length) {
            throw new InvalidOperationException("This list is full");
        }

        _span[index.._count].CopyTo(_span[(index + 1)..(_count - 1)]);
        ++_count;
        _span[index] = item;
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);

        _span[(index + 1).._count].CopyTo(_span[index..(_count - 1)]);
        --_count;
    }

    public int RemoveLast()
    {
        if (_count is 0)
            throw new InvalidOperationException("This list is empty");

        return --_count;
    }

    public static Slot AddSlot(ref SpanList<T> list, T item)
    {
        var index = list.Add(item);
        return new(ref list._span[index], ref list._count);
    }

    public readonly ref struct Slot
    {
        private readonly ref T   _location;
        private readonly ref int _count;
        private readonly     int _initialCount;

        public ref T Value
            => ref _location;

        public Slot(ref T location, ref int count)
        {
            _location = ref location;
            _count = ref count;
            _initialCount = count;
        }

        public void Dispose()
        {
            if (_count != _initialCount)
                throw new InvalidOperationException("Imbalance between the count on slot reservation and on disposal");

            --_count;
        }
    }
}
