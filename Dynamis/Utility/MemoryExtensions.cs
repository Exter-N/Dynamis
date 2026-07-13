using System.Runtime.CompilerServices;
using System.Text;

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

    public static string ToHexString(this ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var b in span) {
            sb.Append($"{b:X2} ");
        }

        return sb.ToString(0, sb.Length - 1);
    }
}
