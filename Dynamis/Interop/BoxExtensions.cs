using System.Reflection;
using MemoryExtensions = Dynamis.Utility.MemoryExtensions;

namespace Dynamis.Interop;

public static class BoxExtensions
{
    public static BoxAccess Least(this BoxAccess access, BoxAccess other)
        => access < other ? access : other;

    public static BoxAccess Deep(this BoxAccess access)
        => access is BoxAccess.ShallowConstant ? BoxAccess.Mutable : access;

    public static BoxAccess Deep(this BoxAccess access, nint containerAddress, uint containerSize, object? address)
    {
        if (access is not BoxAccess.ShallowConstant) {
            return access;
        }

        return IsWithin(containerAddress, containerSize, address) ? access : access.Deep();
    }

    public static bool IsWithin(nint containerAddress, uint containerSize, nint address)
        => address >= containerAddress && address < containerAddress + containerSize;

    public static bool IsWithin(nint containerAddress, uint containerSize, object? address)
    {
        if (IBoxedAddress.TryUnbox(address, out var addr)) {
            return IsWithin(containerAddress, containerSize, addr);
        }

        var type = address.GetType();
        if (type.IsGenericType) {
            if (type.GetGenericTypeDefinition() == typeof(Memory<>) && !type.GetGenericArguments()[0].IsPrimitive) {
                addr = (nint)typeof(MemoryExtensions).GetMethod(
                                                          nameof(MemoryExtensions.GetAddress), 1,
                                                          BindingFlags.Public | BindingFlags.Static,
                                                          [
                                                              typeof(Memory<>).MakeGenericType(
                                                                  Type.MakeGenericMethodParameter(0)
                                                              ),
                                                          ]
                                                      )!.MakeGenericMethod(type.GetGenericArguments())
                                                     .Invoke(null, [address,])!;
                return IsWithin(containerAddress, containerSize, addr);
            }

            if (type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>)
             && !type.GetGenericArguments()[0].IsPrimitive) {
                addr = (nint)typeof(MemoryExtensions).GetMethod(
                                                          nameof(MemoryExtensions.GetAddress), 1,
                                                          BindingFlags.Public | BindingFlags.Static,
                                                          [
                                                              typeof(ReadOnlyMemory<>).MakeGenericType(
                                                                  Type.MakeGenericMethodParameter(0)
                                                              ),
                                                          ]
                                                      )!.MakeGenericMethod(type.GetGenericArguments())
                                                     .Invoke(null, [address,])!;
                return IsWithin(containerAddress, containerSize, addr);
            }
        }

        return false;
    }
}
