using System.Runtime.CompilerServices;

namespace Dynamis.Utility;

public static unsafe class MemoryExtensions
{
    public static T* GetPointer<T>(this Span<T> span, int index = 0) where T : unmanaged
        => (T*)Unsafe.AsPointer(ref span[index]);

    public static T* GetPointer<T>(this ReadOnlySpan<T> span, int index = 0) where T : unmanaged
    {
        fixed (T* ptr = &span[index]) {
            return ptr;
        }
    }

    public static T* GetPointer<T>(this Memory<T> memory, int index = 0) where T : unmanaged
        => memory.Span.GetPointer(index);

    public static T* GetPointer<T>(this ReadOnlyMemory<T> memory, int index = 0) where T : unmanaged
        => memory.Span.GetPointer(index);

    public static nint GetAddress<T>(this Span<T> span, int index = 0) where T : unmanaged
        => (nint)Unsafe.AsPointer(ref span[index]);

    public static nint GetAddress<T>(this ReadOnlySpan<T> span, int index = 0) where T : unmanaged
    {
        fixed (T* ptr = &span[index]) {
            return (nint)ptr;
        }
    }

    public static nint GetAddress<T>(this Memory<T> memory, int index = 0) where T : unmanaged
        => memory.Span.GetAddress(index);

    public static nint GetAddress<T>(this ReadOnlyMemory<T> memory, int index = 0) where T : unmanaged
        => memory.Span.GetAddress(index);
}
