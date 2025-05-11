using System.Reflection;
using Dynamis.Utility;

namespace Dynamis.Interop;

public readonly record struct DynamicMemory(nint Address, int Length, Type ElementType, bool Writable)
{
    public object ToMemory()
        => typeof(DynamicMemory).GetMethod("ToMemory", BindingFlags.Static | BindingFlags.NonPublic)!
                                .MakeGenericMethod(ElementType)
                                .Invoke(null, [this,])!;

    public object ToReadOnlyMemory()
        => typeof(DynamicMemory).GetMethod("ToReadOnlyMemory", BindingFlags.Static | BindingFlags.NonPublic)!
                                .MakeGenericMethod(ElementType)
                                .Invoke(null, [this,])!;

    public static bool TryFrom(object? value, out DynamicMemory memory)
    {
        if (value is null) {
            memory = default;
            return false;
        }

        var type = value.GetType();
        if (!type.IsGenericType) {
            memory = default;
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        if (definition == typeof(Memory<>)) {
            memory = (DynamicMemory)typeof(DynamicMemory)
                                   .GetMethod("FromMemory", BindingFlags.Static | BindingFlags.NonPublic)!
                                   .MakeGenericMethod(type.GetGenericArguments()[0])
                                   .Invoke(null, [value,])!;
            return true;
        }

        if (definition == typeof(ReadOnlyMemory<>)) {
            memory = (DynamicMemory)typeof(DynamicMemory)
                                   .GetMethod("FromReadOnlyMemory", BindingFlags.Static | BindingFlags.NonPublic)!
                                   .MakeGenericMethod(type.GetGenericArguments()[0])
                                   .Invoke(null, [value,])!;
            return true;
        }

        memory = default;
        return false;
    }

    private static unsafe DynamicMemory FromMemory<T>(Memory<T> memory) where T : unmanaged
        => new(memory.GetAddress(), memory.Length, typeof(T), true);

    private static unsafe DynamicMemory FromReadOnlyMemory<T>(ReadOnlyMemory<T> memory) where T : unmanaged
        => new(memory.GetAddress(), memory.Length, typeof(T), false);

    private static unsafe Memory<T> ToMemory<T>(DynamicMemory memory) where T : unmanaged
        => new BorrowedUnmanagedMemory<T>((T*)memory.Address, memory.Length).Memory;

    private static ReadOnlyMemory<T> ToReadOnlyMemory<T>(DynamicMemory memory) where T : unmanaged
        => ToMemory<T>(memory);
}
