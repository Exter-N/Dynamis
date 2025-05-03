using System.Globalization;
using System.Reflection;
using Dynamis.Interop;

namespace Dynamis.Utility;

public static class ConvertEx
{
    public static object? DynamicChangeType(object? value, Type type)
    {
        if (value is null) {
            if (!type.IsValueType || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                return null;
            }

            throw new ArgumentNullException(nameof(value));
        }

        if (type.IsInstanceOfType(value)) {
            return value;
        }

        if (GetCast(value.GetType(), type, true) is
            {
            } cast) {
            return cast.Invoke(null, [value,]);
        }

        var typeCode = Type.GetTypeCode(type);
        if (typeCode is not TypeCode.Empty and not TypeCode.Object) {
            return Convert.ChangeType(value, typeCode);
        }

        if (type == typeof(Half)) {
            return (Half)Convert.ToSingle(value);
        }

        if (type == typeof(nint)) {
            return ToIntPtr(value);
        }

        if (type == typeof(nuint)) {
            return ToUIntPtr(value);
        }

        if (value is IConvertible convertible) {
            return convertible.ToType(type, CultureInfo.CurrentCulture);
        }

        throw new InvalidCastException();
    }

    public static MethodInfo? GetCast(Type sourceType, Type targetType, bool @explicit)
    {
        if (((@explicit
                 ? targetType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, [sourceType,])
                 : null)
          ?? targetType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [sourceType,])) is
            {
            } cast) {
            return cast;
        }

        foreach (var method in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if ((@explicit ? method.Name is "op_Implicit" or "op_Explicit" : method.Name is "op_Implicit")
             && method.ReturnType == targetType) {
                return method;
            }
        }

        return null;
    }

    public static nuint ToUIntPtr(object? value)
        => IBoxedAddress.TryUnbox(value, out var address)
            ? unchecked((nuint)address)
            : nuint.Size switch
            {
                4 => Convert.ToUInt32(value),
                8 => (nuint)Convert.ToUInt64(value),
                _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
            };

    public static nint ToIntPtr(object? value)
        => IBoxedAddress.TryUnbox(value, out var address)
            ? address
            : nint.Size switch
            {
                4 => Convert.ToInt32(value),
                8 => (nint)Convert.ToInt64(value),
                _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
            };
}
