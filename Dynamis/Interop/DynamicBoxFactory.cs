using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FFXIVClientStructs.Interop;

namespace Dynamis.Interop;

public sealed class DynamicBoxFactory(ObjectInspector objectInspector, ClassRegistry classRegistry)
{
    public DynamicStructBox BoxStruct(nint address, BoxAccess access)
    {
        var (@class, displacement) = objectInspector.DetermineClassAndDisplacement(address);
        return new(address - (nint)displacement, @class, access, this);
    }

    [return: NotNullIfNotNull(nameof(obj))]
    public object? Box(object? obj, BoxAccess access)
    {
        if (obj is null) {
            return null;
        }

        if (IBoxedAddress.TryUnboxStrict(obj, out var address)) {
            return BoxStruct(address, access);
        }

        var type = obj.GetType();
        if (type.IsGenericType) {
            if (type.GetGenericTypeDefinition() == typeof(Memory<>) && !type.GetGenericArguments()[0].IsPrimitive) {
                return typeof(DynamicBoxFactory).GetMethod(
                                                     nameof(BoxMemory),
                                                     BindingFlags.NonPublic | BindingFlags.Instance
                                                 )!.MakeGenericMethod(type.GetGenericArguments())
                                                .Invoke(this, [obj, access,])!;
            }

            if (type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>)
             && !type.GetGenericArguments()[0].IsPrimitive) {
                return typeof(DynamicBoxFactory).GetMethod(
                                                     nameof(BoxReadOnlyMemory),
                                                     BindingFlags.NonPublic | BindingFlags.Instance
                                                 )!.MakeGenericMethod(type.GetGenericArguments())
                                                .Invoke(this, [obj, access,])!;
            }
        }

        return obj;
    }

    private unsafe DynamicSpanBox BoxMemory<T>(Memory<T> memory, BoxAccess access) where T : unmanaged
    {
        var type = typeof(T);
        var pointers = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>);
        var elementClass = classRegistry.FromManagedType(pointers ? type.GetGenericArguments()[0] : type);
        fixed (T* ptr = memory.Span) {
            return new((nint)ptr, memory.Length, elementClass, pointers, access, this);
        }
    }

    private unsafe DynamicSpanBox BoxReadOnlyMemory<T>(ReadOnlyMemory<T> memory, BoxAccess access) where T : unmanaged
    {
        var type = typeof(T);
        var pointers = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>);
        var elementClass = classRegistry.FromManagedType(pointers ? type.GetGenericArguments()[0] : type);
        fixed (T* ptr = memory.Span) {
            return new((nint)ptr, memory.Length, elementClass, pointers, access.Least(BoxAccess.ShallowConstant), this);
        }
    }
}
