using System.Buffers;
using System.Runtime.InteropServices;

namespace Dynamis.Utility;

public sealed class HGlobalBuffer<T> : MemoryManager<T> where T : unmanaged
{
    private readonly int  _length;
    private readonly bool _clearOnFree;
    private          nint _address;

    public override Memory<T> Memory
        => CreateMemory(_length);

    public unsafe HGlobalBuffer(int length, bool clear, bool clearOnFree)
    {
        _length = length;
        _clearOnFree = clearOnFree;
        _address = Marshal.AllocHGlobal(length * sizeof(T));
        GC.AddMemoryPressure(length * sizeof(T));
        if (clear) {
            new Span<T>((void*)_address, _length).Clear();
        }
    }

    protected override unsafe void Dispose(bool disposing)
    {
        var address = Interlocked.Exchange(ref _address, 0);
        if (address == 0) {
            return;
        }

        if (_clearOnFree) {
            new Span<T>((void*)address, _length).Clear();
        }

        GC.RemoveMemoryPressure(_length * sizeof(T));
        Marshal.FreeHGlobal(address);
    }

    public override unsafe Span<T> GetSpan()
    {
        var pointer = (void*)_address;
        ObjectDisposedException.ThrowIf(pointer is null, this);
        return new(pointer, _length);
    }

    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        var pointer = (T*)_address;
        ObjectDisposedException.ThrowIf(pointer is null, this);
        return new(pointer + elementIndex, default, this);
    }

    public override void Unpin()
    {
    }
}
