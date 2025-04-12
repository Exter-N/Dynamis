using System.Runtime.InteropServices;
using System.Text;
using Dynamis.Interop;
using FFXIVClientStructs.Interop;
using ImGuiNET;

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
            "System.Boolean" => FieldType.Boolean,
            "System.Byte"    => isString ? FieldType.ByteString : FieldType.Byte,
            "System.SByte"   => FieldType.SByte,
            "System.UInt16"  => FieldType.UInt16,
            "System.Int16"   => FieldType.Int16,
            "System.UInt32"  => FieldType.UInt32,
            "System.Int32"   => FieldType.Int32,
            "System.UInt64"  => FieldType.UInt64,
            "System.Int64"   => FieldType.Int64,
            "System.UIntPtr" => FieldType.UIntPtr,
            "System.IntPtr"  => FieldType.IntPtr,
            "System.Char"    => isString ? FieldType.CharString : FieldType.Char,
            "System.Half"    => FieldType.Half,
            "System.Single"  => FieldType.Single,
            "System.Double"  => FieldType.Double,
            _                => null,
        };

    public static Type ToType(this FieldType t)
        => t switch
        {
            FieldType.Boolean    => typeof(bool),
            FieldType.Byte       => typeof(byte),
            FieldType.SByte      => typeof(sbyte),
            FieldType.UInt16     => typeof(ushort),
            FieldType.Int16      => typeof(short),
            FieldType.UInt32     => typeof(uint),
            FieldType.Int32      => typeof(int),
            FieldType.UInt64     => typeof(ulong),
            FieldType.Int64      => typeof(long),
            FieldType.UIntPtr    => typeof(nuint),
            FieldType.IntPtr     => typeof(nint),
            FieldType.Char       => typeof(char),
            FieldType.Half       => typeof(Half),
            FieldType.Single     => typeof(float),
            FieldType.Double     => typeof(double),
            FieldType.Pointer    => typeof(nint),
            FieldType.ByteString => typeof(byte),
            FieldType.CharString => typeof(char),
            _                    => throw new ArgumentException($"Unrecognized FieldType {t}"),
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

    public static string Description(this FieldType t)
        => t switch
        {
            FieldType.Boolean    => "bool",
            FieldType.Byte       => "byte",
            FieldType.SByte      => "sbyte",
            FieldType.UInt16     => "ushort",
            FieldType.Int16      => "short",
            FieldType.UInt32     => "uint",
            FieldType.Int32      => "int",
            FieldType.UInt64     => "ulong",
            FieldType.Int64      => "long",
            FieldType.UIntPtr    => "nuint",
            FieldType.IntPtr     => "nint",
            FieldType.Char       => "char",
            FieldType.Half       => "half float",
            FieldType.Single     => "float",
            FieldType.Double     => "double",
            FieldType.Pointer    => "void*",
            FieldType.ByteString => "byte string",
            FieldType.CharString => "string",
            _                    => throw new ArgumentException($"Unrecognized FieldType {t}"),
        };

    public static int Size(this FieldType t)
        => t switch
        {
            FieldType.Boolean    => 1,
            FieldType.Byte       => 1,
            FieldType.SByte      => 1,
            FieldType.UInt16     => 2,
            FieldType.Int16      => 2,
            FieldType.UInt32     => 4,
            FieldType.Int32      => 4,
            FieldType.UInt64     => 8,
            FieldType.Int64      => 8,
            FieldType.UIntPtr    => nuint.Size,
            FieldType.IntPtr     => nint.Size,
            FieldType.Char       => 2,
            FieldType.Half       => 2,
            FieldType.Single     => 4,
            FieldType.Double     => 8,
            FieldType.Pointer    => nint.Size,
            FieldType.ByteString => 1,
            FieldType.CharString => 2,
            _                    => throw new ArgumentException($"Unrecognized FieldType {t}"),
        };

    public static object Read(this FieldType t, ReadOnlySpan<byte> bytes)
        => t switch
        {
            FieldType.Boolean    => bytes[0] != 0,
            FieldType.Byte       => bytes[0],
            FieldType.SByte      => MemoryMarshal.Cast<byte, sbyte>(bytes)[0],
            FieldType.UInt16     => MemoryMarshal.Cast<byte, ushort>(bytes)[0],
            FieldType.Int16      => MemoryMarshal.Cast<byte, short>(bytes)[0],
            FieldType.UInt32     => MemoryMarshal.Cast<byte, uint>(bytes)[0],
            FieldType.Int32      => MemoryMarshal.Cast<byte, int>(bytes)[0],
            FieldType.UInt64     => MemoryMarshal.Cast<byte, ulong>(bytes)[0],
            FieldType.Int64      => MemoryMarshal.Cast<byte, long>(bytes)[0],
            FieldType.UIntPtr    => MemoryMarshal.Cast<byte, nuint>(bytes)[0],
            FieldType.IntPtr     => MemoryMarshal.Cast<byte, nint>(bytes)[0],
            FieldType.Char       => MemoryMarshal.Cast<byte, char>(bytes)[0],
            FieldType.Half       => MemoryMarshal.Cast<byte, Half>(bytes)[0],
            FieldType.Single     => MemoryMarshal.Cast<byte, float>(bytes)[0],
            FieldType.Double     => MemoryMarshal.Cast<byte, double>(bytes)[0],
            FieldType.Pointer    => MemoryMarshal.Cast<byte, nint>(bytes)[0],
            FieldType.ByteString => Encoding.UTF8.GetString(bytes),
            FieldType.CharString => new string(MemoryMarshal.Cast<byte, char>(bytes)),
            _                    => throw new ArgumentException($"Unrecognized FieldType {t}"),
        };

    public static (ImGuiDataType DataType, string CFormat) ToImGui(this FieldType type, bool hexIntegers)
        => type switch
        {
            FieldType.Byte    => (ImGuiDataType.U8, hexIntegers ? "%02X" : "%u"),
            FieldType.SByte   => (ImGuiDataType.S8, hexIntegers ? "%02X" : "%d"),
            FieldType.UInt16  => (ImGuiDataType.U16, hexIntegers ? "%04X" : "%u"),
            FieldType.Int16   => (ImGuiDataType.S16, hexIntegers ? "%04X" : "%d"),
            FieldType.UInt32  => (ImGuiDataType.U32, hexIntegers ? "%08X" : "%u"),
            FieldType.Int32   => (ImGuiDataType.S32, hexIntegers ? "%08X" : "%d"),
            FieldType.UInt64  => (ImGuiDataType.U64, hexIntegers ? "%016llX" : "%llu"),
            FieldType.Int64   => (ImGuiDataType.S64, hexIntegers ? "%016llX" : "%lld"),
            FieldType.UIntPtr => UIntPtrToImGui(hexIntegers),
            FieldType.IntPtr  => IntPtrToImGui(hexIntegers),
            FieldType.Single  => (ImGuiDataType.Float, "%.6f"),
            FieldType.Double  => (ImGuiDataType.Double, "%.6f"),
            FieldType.Pointer => UIntPtrToImGui(true),
            _                 => throw new ArgumentException($"Unsupported FieldType {type}", nameof(type)),
        };

    private static (ImGuiDataType DataType, string CFormat) UIntPtrToImGui(bool hex)
        => nint.Size switch
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
