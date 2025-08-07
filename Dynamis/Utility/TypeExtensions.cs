using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dynamis.Interop;
using FFXIVClientStructs.Interop;
using InteropGenerator.Runtime;

namespace Dynamis.Utility;

internal static class TypeExtensions
{
    public static FieldType? ToFieldType(this Type t, bool isString = false)
    {
        if (t.IsPointer || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Pointer<>)) {
            return FieldType.Pointer;
        }

        if (t.IsEnum) {
            t = Enum.GetUnderlyingType(t);
        }

        return t.FullName.ToFieldType(isString);
    }

    public static FieldType? ToFieldType(this string? typeName, bool isString = false)
        => typeName switch
        {
            "System.Boolean"                          => FieldType.Boolean,
            "System.Byte"                             => isString ? FieldType.ByteString : FieldType.Byte,
            "System.SByte"                            => FieldType.SByte,
            "System.UInt16"                           => FieldType.UInt16,
            "System.Int16"                            => FieldType.Int16,
            "System.UInt32"                           => FieldType.UInt32,
            "System.Int32"                            => FieldType.Int32,
            "System.UInt64"                           => FieldType.UInt64,
            "System.Int64"                            => FieldType.Int64,
            "System.UIntPtr"                          => FieldType.UIntPtr,
            "System.IntPtr"                           => FieldType.IntPtr,
            "System.Char"                             => isString ? FieldType.CharString : FieldType.Char,
            "System.Half"                             => FieldType.Half,
            "System.Single"                           => FieldType.Single,
            "System.Double"                           => FieldType.Double,
            "InteropGenerator.Runtime.CStringPointer" => FieldType.CStringPointer,
            _                                         => null,
        };

    public static Type ToType(this FieldType t)
        => t switch
        {
            FieldType.Boolean        => typeof(bool),
            FieldType.Byte           => typeof(byte),
            FieldType.SByte          => typeof(sbyte),
            FieldType.UInt16         => typeof(ushort),
            FieldType.Int16          => typeof(short),
            FieldType.UInt32         => typeof(uint),
            FieldType.Int32          => typeof(int),
            FieldType.UInt64         => typeof(ulong),
            FieldType.Int64          => typeof(long),
            FieldType.UIntPtr        => typeof(nuint),
            FieldType.IntPtr         => typeof(nint),
            FieldType.Char           => typeof(char),
            FieldType.Half           => typeof(Half),
            FieldType.Single         => typeof(float),
            FieldType.Double         => typeof(double),
            FieldType.Pointer        => typeof(nint),
            FieldType.ByteString     => typeof(byte),
            FieldType.CharString     => typeof(char),
            FieldType.CStringPointer => typeof(CStringPointer),
            _                        => throw new ArgumentException($"Unrecognized FieldType {t}"),
        };

    public static bool IsInteger(this FieldType t)
        => t switch
        {
            FieldType.Byte    => true,
            FieldType.SByte   => true,
            FieldType.UInt16  => true,
            FieldType.Int16   => true,
            FieldType.UInt32  => true,
            FieldType.Int32   => true,
            FieldType.UInt64  => true,
            FieldType.Int64   => true,
            FieldType.UIntPtr => true,
            FieldType.IntPtr  => true,
            _                 => false,
        };

    public static bool IsScalar(this FieldType t)
        => t switch
        {
            FieldType.Object      => false,
            FieldType.ObjectArray => false,
            _                     => Enum.IsDefined(t),
        };

    public static string Description(this FieldType t)
        => t switch
        {
            FieldType.Boolean        => "bool",
            FieldType.Byte           => "byte",
            FieldType.SByte          => "sbyte",
            FieldType.UInt16         => "ushort",
            FieldType.Int16          => "short",
            FieldType.UInt32         => "uint",
            FieldType.Int32          => "int",
            FieldType.UInt64         => "ulong",
            FieldType.Int64          => "long",
            FieldType.UIntPtr        => "nuint",
            FieldType.IntPtr         => "nint",
            FieldType.Char           => "char",
            FieldType.Half           => "half float",
            FieldType.Single         => "float",
            FieldType.Double         => "double",
            FieldType.Pointer        => "void*",
            FieldType.ByteString     => "byte string",
            FieldType.CharString     => "string",
            FieldType.CStringPointer => "byte string*",
            _                        => throw new ArgumentException($"Unrecognized FieldType {t}"),
        };

    public static int Size(this FieldType t)
        => t switch
        {
            FieldType.Boolean        => 1,
            FieldType.Byte           => 1,
            FieldType.SByte          => 1,
            FieldType.UInt16         => 2,
            FieldType.Int16          => 2,
            FieldType.UInt32         => 4,
            FieldType.Int32          => 4,
            FieldType.UInt64         => 8,
            FieldType.Int64          => 8,
            FieldType.UIntPtr        => nuint.Size,
            FieldType.IntPtr         => nint.Size,
            FieldType.Char           => 2,
            FieldType.Half           => 2,
            FieldType.Single         => 4,
            FieldType.Double         => 8,
            FieldType.Pointer        => nint.Size,
            FieldType.ByteString     => 1,
            FieldType.CharString     => 2,
            FieldType.CStringPointer => nint.Size,
            _                        => throw new ArgumentException($"Unrecognized FieldType {t}"),
        };

    public static Array ReadAll(this FieldType t, ReadOnlySpan<byte> bytes)
        => t switch
        {
            FieldType.Boolean    => Array.ConvertAll(bytes.ToArray(), b => b != 0),
            FieldType.Byte       => bytes.ToArray(),
            FieldType.SByte      => MemoryMarshal.Cast<byte, sbyte>(bytes).ToArray(),
            FieldType.UInt16     => MemoryMarshal.Cast<byte, ushort>(bytes).ToArray(),
            FieldType.Int16      => MemoryMarshal.Cast<byte, short>(bytes).ToArray(),
            FieldType.UInt32     => MemoryMarshal.Cast<byte, uint>(bytes).ToArray(),
            FieldType.Int32      => MemoryMarshal.Cast<byte, int>(bytes).ToArray(),
            FieldType.UInt64     => MemoryMarshal.Cast<byte, ulong>(bytes).ToArray(),
            FieldType.Int64      => MemoryMarshal.Cast<byte, long>(bytes).ToArray(),
            FieldType.UIntPtr    => MemoryMarshal.Cast<byte, nuint>(bytes).ToArray(),
            FieldType.IntPtr     => MemoryMarshal.Cast<byte, nint>(bytes).ToArray(),
            FieldType.Char       => MemoryMarshal.Cast<byte, char>(bytes).ToArray(),
            FieldType.Half       => MemoryMarshal.Cast<byte, Half>(bytes).ToArray(),
            FieldType.Single     => MemoryMarshal.Cast<byte, float>(bytes).ToArray(),
            FieldType.Double     => MemoryMarshal.Cast<byte, double>(bytes).ToArray(),
            FieldType.Pointer    => MemoryMarshal.Cast<byte, nint>(bytes).ToArray(),
            FieldType.ByteString => new[] { Encoding.UTF8.GetString(bytes.BeforeNull()) },
            FieldType.CharString => new string[] { new(MemoryMarshal.Cast<byte, char>(bytes).BeforeNull()) },
            FieldType.CStringPointer => Array.ConvertAll(
                MemoryMarshal.Cast<byte, nint>(bytes).ToArray(), CStringSnapshot.FromAddress
            ),
            _ => throw new ArgumentException($"Unsupported FieldType {t}"),
        };

    public static object Read(this FieldType t, ReadOnlySpan<byte> bytes)
        => t switch
        {
            FieldType.Boolean        => bytes[0] != 0,
            FieldType.Byte           => bytes[0],
            FieldType.SByte          => MemoryMarshal.Read<sbyte>(bytes),
            FieldType.UInt16         => MemoryMarshal.Read<ushort>(bytes),
            FieldType.Int16          => MemoryMarshal.Read<short>(bytes),
            FieldType.UInt32         => MemoryMarshal.Read<uint>(bytes),
            FieldType.Int32          => MemoryMarshal.Read<int>(bytes),
            FieldType.UInt64         => MemoryMarshal.Read<ulong>(bytes),
            FieldType.Int64          => MemoryMarshal.Read<long>(bytes),
            FieldType.UIntPtr        => MemoryMarshal.Read<nuint>(bytes),
            FieldType.IntPtr         => MemoryMarshal.Read<nint>(bytes),
            FieldType.Char           => MemoryMarshal.Read<char>(bytes),
            FieldType.Half           => MemoryMarshal.Read<Half>(bytes),
            FieldType.Single         => MemoryMarshal.Read<float>(bytes),
            FieldType.Double         => MemoryMarshal.Read<double>(bytes),
            FieldType.Pointer        => MemoryMarshal.Read<nint>(bytes),
            FieldType.ByteString     => Encoding.UTF8.GetString(bytes.BeforeNull()),
            FieldType.CharString     => new string(MemoryMarshal.Cast<byte, char>(bytes).BeforeNull()),
            FieldType.CStringPointer => CStringSnapshot.FromAddress(MemoryMarshal.Read<nint>(bytes)),
            _                        => throw new ArgumentException($"Unsupported FieldType {t}"),
        };

    public static bool TryWrite(this FieldType t, Span<byte> bytes, object? value)
    {
        switch (t) {
            case FieldType.Boolean:
                bytes[0] = Convert.ToBoolean(value) ? (byte)1 : (byte)0;
                return true;
            case FieldType.Byte:
                bytes[0] = Convert.ToByte(value);
                return true;
            case FieldType.SByte:
                MemoryMarshal.Write(bytes, Convert.ToSByte(value));
                return true;
            case FieldType.UInt16:
                MemoryMarshal.Write(bytes, Convert.ToUInt16(value));
                return true;
            case FieldType.Int16:
                MemoryMarshal.Write(bytes, Convert.ToInt16(value));
                return true;
            case FieldType.UInt32:
                MemoryMarshal.Write(bytes, Convert.ToUInt32(value));
                return true;
            case FieldType.Int32:
                MemoryMarshal.Write(bytes, Convert.ToInt32(value));
                return true;
            case FieldType.UInt64:
                MemoryMarshal.Write(bytes, Convert.ToUInt64(value));
                return true;
            case FieldType.Int64:
                MemoryMarshal.Write(bytes, Convert.ToInt64(value));
                return true;
            case FieldType.UIntPtr:
                MemoryMarshal.Write(bytes, ConvertEx.ToUIntPtr(value));
                return true;
            case FieldType.IntPtr:
                MemoryMarshal.Write(bytes, ConvertEx.ToIntPtr(value));
                return true;
            case FieldType.Char:
                MemoryMarshal.Write(bytes, Convert.ToChar(value));
                return true;
            case FieldType.Half:
                MemoryMarshal.Write(bytes, (Half)Convert.ChangeType(value, typeof(Half))!);
                return true;
            case FieldType.Single:
                MemoryMarshal.Write(bytes, Convert.ToSingle(value));
                return true;
            case FieldType.Double:
                MemoryMarshal.Write(bytes, Convert.ToDouble(value));
                return true;
            case FieldType.Pointer:
                MemoryMarshal.Write(bytes, ConvertEx.ToIntPtr(value));
                return true;
            case FieldType.ByteString:
                (Convert.ToString(value) ?? string.Empty).WriteNullTerminated(bytes);
                return true;
            case FieldType.CharString:
                (Convert.ToString(value) ?? string.Empty).WriteNullTerminated(MemoryMarshal.Cast<byte, char>(bytes));
                return true;
            default:
                return false;
        }
    }

    public static (ImGuiDataType DataType, string CFormat) ToImGui(this FieldType type, bool hexIntegers)
        => type switch
        {
            FieldType.Byte           => (ImGuiDataType.U8, hexIntegers ? "%02X" : "%u"),
            FieldType.SByte          => (ImGuiDataType.S8, hexIntegers ? "%02X" : "%d"),
            FieldType.UInt16         => (ImGuiDataType.U16, hexIntegers ? "%04X" : "%u"),
            FieldType.Int16          => (ImGuiDataType.S16, hexIntegers ? "%04X" : "%d"),
            FieldType.UInt32         => (ImGuiDataType.U32, hexIntegers ? "%08X" : "%u"),
            FieldType.Int32          => (ImGuiDataType.S32, hexIntegers ? "%08X" : "%d"),
            FieldType.UInt64         => (ImGuiDataType.U64, hexIntegers ? "%016llX" : "%llu"),
            FieldType.Int64          => (ImGuiDataType.S64, hexIntegers ? "%016llX" : "%lld"),
            FieldType.UIntPtr        => UIntPtrToImGui(hexIntegers),
            FieldType.IntPtr         => IntPtrToImGui(hexIntegers),
            FieldType.Single         => (ImGuiDataType.Float, "%.6f"),
            FieldType.Double         => (ImGuiDataType.Double, "%.6f"),
            FieldType.Pointer        => UIntPtrToImGui(true),
            FieldType.CStringPointer => UIntPtrToImGui(true),
            _                        => throw new ArgumentException($"Unsupported FieldType {type}", nameof(type)),
        };

    private static (ImGuiDataType DataType, string CFormat) UIntPtrToImGui(bool hex)
        => nuint.Size switch
        {
            4 => (ImGuiDataType.U32, hex ? "%08X" : "%u"),
            8 => (ImGuiDataType.U64, hex ? "%016llX" : "%llu"),
            _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
        };

    private static (ImGuiDataType DataType, string CFormat) IntPtrToImGui(bool hex)
        => nint.Size switch
        {
            4 => (ImGuiDataType.S32, hex ? "%08X" : "%d"),
            8 => (ImGuiDataType.S64, hex ? "%016llX" : "%lld"),
            _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
        };
}
