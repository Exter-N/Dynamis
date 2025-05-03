using System.Buffers;

namespace Dynamis.Interop;

public sealed unsafe class BorrowedUnmanagedMemory<T> : MemoryManager<T> where T : unmanaged
{
    private readonly T*   _pointer;
    private readonly int  _length;
    private          bool _disposed;

    public override Memory<T> Memory
        => CreateMemory(_length);

    public BorrowedUnmanagedMemory(T* pointer, int length)
    {
        _pointer = pointer;
        _length = length;
    }

    private BorrowedUnmanagedMemory(ReadOnlySpan<T> span)
    {
        fixed (T* ptr = span) {
            _pointer = ptr;
        }

        _length = span.Length;
    }

    public BorrowedUnmanagedMemory(Span<T> span)
    {
        fixed (T* ptr = span) {
            _pointer = ptr;
        }

        _length = span.Length;
    }

    public static ReadOnlyMemory<T> ToMemory(ReadOnlySpan<T> span)
        => new BorrowedUnmanagedMemory<T>(span).Memory;

    public static Memory<T> ToMemory(Span<T> span)
        => new BorrowedUnmanagedMemory<T>(span).Memory;

    protected override void Dispose(bool disposing)
    {
        _disposed = true;
    }

    public override Span<T> GetSpan()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new(_pointer, _length);
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if (elementIndex < 0 || elementIndex >= _length) {
            throw new ArgumentOutOfRangeException(nameof(elementIndex));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);
        return new(_pointer + elementIndex);
    }

    public override void Unpin()
    {
    }
}
