using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FFXIVClientStructs.Interop;
using InteropGenerator.Runtime;

namespace Dynamis.Interop;

public interface IBoxedAddress
{
    nint Address { get; }

    public static unsafe bool TryUnbox([NotNullWhen(false)] object? value, out nint address)
    {
        switch (value)
        {
            case IBoxedAddress box:
                address = box.Address;
                return true;
            case Pointer ptr:
                address = (nint)Pointer.Unbox(ptr);
                return true;
            case CStringPointer str:
                address = (nint)str.Value;
                return true;
            case null:
                address = 0;
                return true;
            case nint num:
                address = num;
                return true;
            case nuint uNum:
                address = unchecked((nint)uNum);
                return true;
            default:
                var type = value.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>)) {
                    address = (nint)typeof(IBoxedAddress)
                                   .GetMethod(nameof(Unbox), BindingFlags.NonPublic | BindingFlags.Static)!
                                   .MakeGenericMethod(type.GetGenericArguments())
                                   .Invoke(null, [value,])!;
                    return true;
                }

                address = 0;
                return false;
        }
    }

    public static bool TryUnboxStrict([NotNullWhen(true)] object? value, out nint address)
    {
        switch (value) {
            case null:
            case nint:
            case nuint:
                TryUnbox(value, out address);
                return false;
            default:
                return TryUnbox(value, out address);
        }
    }

    private static unsafe nint Unbox<T>(Pointer<T> value) where T : unmanaged
        => (nint)value.Value;
}
