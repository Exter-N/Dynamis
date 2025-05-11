using System.Dynamic;
using System.Reflection;
using System.Text;
using Dynamis.Utility;
using FFXIVClientStructs.Interop;
using InteropGenerator.Runtime;

namespace Dynamis.Interop;

public sealed class DynamicStructBox(nint address, ClassInfo @class, BoxAccess access, DynamicBoxFactory factory)
    : DynamicObject, IBoxedAddress, IDynamicBox
{
    public nint Address
        => address;

    public ClassInfo Class
        => @class;

    private object CsPointer
        => WrapCsPointer(@class.BestManagedType!, address);

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        foreach (var field in @class.FieldsByName.Keys) {
            yield return field;
        }

        foreach (var (property, impl) in @class.PropertiesByName) {
            if (access >= BoxAccess.Mutable || impl.Getter is not null) {
                yield return property;
            }
        }

        if (access >= BoxAccess.Mutable) {
            foreach (var method in @class.MethodsByName.Keys) {
                yield return method;
            }
        }
    }

    public override unsafe bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (@class.FieldsByName.TryGetValue(binder.Name, binder.IgnoreCase, out var field)) {
            switch (field.Type) {
                case FieldType.Object:
                    result = new DynamicStructBox(
                        address + (nint)field.Offset,
                        field.ElementClass
                     ?? throw new InvalidOperationException($"Field {field.Name} doesn't have a class"),
                        access, factory
                    );
                    return true;
                case FieldType.ObjectArray:
                    result = new DynamicSpanBox(
                        address + (nint)field.Offset, (int)field.ElementCount, field.ElementClass
                     ?? throw new InvalidOperationException($"Field {field.Name} doesn't have a class"), false,
                        access, factory
                    );
                    return true;
                default:
                    result = @class.GetFieldValues(field, new((void*)address, unchecked((int)@class.EstimatedSize)));
                    if (field.Type == FieldType.Pointer && result is nint ptr) {
                        result = factory.BoxStruct(
                            ptr, ptr >= address && ptr < address + @class.EstimatedSize ? access : access.Deep()
                        );
                    } else if (field.Type == FieldType.Pointer && result is nint[] ptrs) {
                        result = Array.ConvertAll(
                            ptrs,
                            item => factory.BoxStruct(
                                item, item >= address && item < address + @class.EstimatedSize ? access : access.Deep()
                            )
                        );
                    } else if (result is Array array) {
                        var boxArray = Array.CreateInstance(
                            DynamicBoxFactory.GetBoxType(array.GetType().GetElementType()!), array.Length
                        );
                        for (var i = 0; i < array.Length; ++i) {
                            var item = array.GetValue(i);
                            boxArray.SetValue(factory.Box(item, access.Deep(address, @class.EstimatedSize, item)), i);
                        }

                        result = boxArray;
                    } else {
                        result = factory.Box(result, access.Deep(address, @class.EstimatedSize, result));
                    }

                    return true;
            }
        }

        if (@class.PropertiesByName.TryGetValue(binder.Name, binder.IgnoreCase, out var property)) {
            if (property.Getter is null) {
                result = null;
                return false;
            }

            result = property.Getter.Invoke(null, [CsPointer,]);
            result = factory.Box(result, access.Deep(address, @class.EstimatedSize, result));
            return true;
        }

        result = null;
        return false;
    }

    public override unsafe bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (access < BoxAccess.Mutable) {
            return false;
        }

        if (@class.FieldsByName.TryGetValue(binder.Name, binder.IgnoreCase, out var field)) {
            if (!field.Type.IsScalar()) {
                return false;
            }

            if (field.Type == FieldType.Pointer && IBoxedAddress.TryUnbox(value, out var boxedAddress)) {
                value = boxedAddress;
            } else if (value is IDynamicBox dynamicBox) {
                value = dynamicBox.Unbox();
            }

            var bytes = new Span<byte>((void*)(address + (nint)field.Offset), (int)field.Size);
            return field.Type.TryWrite(bytes, value);
        }

        if (@class.PropertiesByName.TryGetValue(binder.Name, binder.IgnoreCase, out var property)) {
            if (property.Setter is null) {
                return false;
            }

            if (value is IDynamicBox dynamicBox) {
                value = dynamicBox.Unbox();
            }

            Invoke(property.Setter, [CsPointer, value,]);
            return true;
        }

        return false;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        if (access < BoxAccess.Mutable) {
            result = null;
            return false;
        }

        if (@class.MethodsByName.TryGetValue(binder.Name, binder.IgnoreCase, out var methods)) {
            var argCount = 1 + (args?.Length ?? 0);
            foreach (var method in methods) {
                if (method.GetParameters().Length != argCount) {
                    continue;
                }

                result = Invoke(
                    method,
                    [
                        CsPointer,
                        ..Array.ConvertAll(args ?? [], arg => arg is IDynamicBox dynamicBox ? dynamicBox.Unbox() : arg),
                    ]
                );
                result = factory.Box(result, access.Deep(address, @class.EstimatedSize, result));
                return true;
            }
        }

        result = null;
        return false;
    }

    #region Reads/writes by offset

    private unsafe T Read<T>(int offset) where T : unmanaged
    {
        if (offset < 0 || offset + sizeof(T) > @class.EstimatedSize) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        return *(T*)(address + offset);
    }

    private unsafe void Write<T>(int offset, T value) where T : unmanaged
    {
        if (offset < 0 || offset + sizeof(T) > @class.EstimatedSize) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        *(T*)(address + offset) = value;
    }

    public bool ReadBoolean(int offset)
        => Read<byte>(offset) != 0;

    public void WriteBoolean(int offset, bool value)
        => Write(offset, value ? (byte)1 : (byte)0);

    public byte ReadByte(int offset)
        => Read<byte>(offset);

    public void WriteByte(int offset, byte value)
        => Write(offset, value);

    public sbyte ReadSByte(int offset)
        => Read<sbyte>(offset);

    public void WriteSByte(int offset, sbyte value)
        => Write(offset, value);

    public ushort ReadUInt16(int offset)
        => Read<ushort>(offset);

    public void WriteUInt16(int offset, ushort value)
        => Write(offset, value);

    public short ReadInt16(int offset)
        => Read<short>(offset);

    public void WriteInt16(int offset, short value)
        => Write(offset, value);

    public uint ReadUInt32(int offset)
        => Read<uint>(offset);

    public void WriteUInt32(int offset, uint value)
        => Write(offset, value);

    public int ReadInt32(int offset)
        => Read<int>(offset);

    public void WriteInt32(int offset, int value)
        => Write(offset, value);

    public ulong ReadUInt64(int offset)
        => Read<ulong>(offset);

    public void WriteUInt64(int offset, ulong value)
        => Write(offset, value);

    public long ReadInt64(int offset)
        => Read<long>(offset);

    public void WriteInt64(int offset, long value)
        => Write(offset, value);

    public nuint ReadUIntPtr(int offset)
        => Read<nuint>(offset);

    public void WriteUIntPtr(int offset, nuint value)
        => Write(offset, value);

    public nint ReadIntPtr(int offset)
        => Read<nint>(offset);

    public void WriteIntPtr(int offset, nint value)
        => Write(offset, value);

    public char ReadChar(int offset)
        => Read<char>(offset);

    public void WriteChar(int offset, char value)
        => Write(offset, value);

    public Half ReadHalf(int offset)
        => Read<Half>(offset);

    public void WriteHalf(int offset, Half value)
        => Write(offset, value);

    public float ReadSingle(int offset)
        => Read<float>(offset);

    public void WriteSingle(int offset, float value)
        => Write(offset, value);

    public double ReadDouble(int offset)
        => Read<double>(offset);

    public void WriteDouble(int offset, double value)
        => Write(offset, value);

    public DynamicStructBox ReadPointer(int offset)
        => factory.BoxStruct(Read<nint>(offset), access.Deep());

    public void WritePointer(int offset, object? value)
    {
        if (!IBoxedAddress.TryUnbox(value, out var pointer)) {
            throw new ArgumentException("Value is not a valid boxed pointer", nameof(value));
        }

        Write(offset, pointer);
    }

    private unsafe Span<T> GetSpan<T>(int offset, int maxLength) where T : unmanaged
    {
        if (maxLength < 0 || maxLength * sizeof(T) > @class.EstimatedSize) {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        if (offset < 0 || offset + maxLength * sizeof(T) > @class.EstimatedSize) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        return new((void*)(address + offset), maxLength);
    }

    private ReadOnlySpan<T> GetStringSpan<T>(int offset, int maxLength) where T : unmanaged, IEquatable<T>
    {
        var span = (ReadOnlySpan<T>)GetSpan<T>(offset, maxLength);
        var end = span.IndexOf(default(T));
        if (end >= 0) {
            span = span[..end];
        }

        return span;
    }

    public string ReadByteString(int offset, int maxLength)
        => Encoding.UTF8.GetString(GetStringSpan<byte>(offset, maxLength));

    public int WriteByteString(int offset, int maxLength, string value, bool forceNullTerminated = true)
    {
        var span = GetSpan<byte>(offset, maxLength);
        if (forceNullTerminated) {
            return value.WriteNullTerminated(span);
        }

        var byteCount = Encoding.UTF8.GetBytes(value, span);
        if (byteCount < span.Length) {
            span[byteCount] = 0;
        }

        return byteCount;
    }

    public string ReadCharString(int offset, int maxLength)
        => new(GetStringSpan<char>(offset, maxLength));

    public int WriteCharString(int offset, int maxLength, string value, bool forceNullTerminated = true)
    {
        var span = GetSpan<char>(offset, maxLength);
        if (forceNullTerminated) {
            return value.WriteNullTerminated(span);
        }

        var valueSpan = value.AsSpan();
        if (valueSpan.Length > span.Length) {
            valueSpan = valueSpan[..span.Length];
        }

        valueSpan.CopyTo(span);
        if (valueSpan.Length < span.Length) {
            span[valueSpan.Length] = '\0';
        }

        return valueSpan.Length;
    }

    public DynamicStructBox ReadObject(int offset)
    {
        if (offset < 0 || offset + nint.Size > @class.EstimatedSize) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        var obj = factory.BoxStruct(address + offset, access);
        if (offset + obj.Class.EstimatedSize > @class.EstimatedSize) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        return obj;
    }

    public string ReadCStringPointer(int offset)
        => Read<CStringPointer>(offset);

    #endregion

    object IDynamicBox.Unbox()
        => CsPointer;

    public static object WrapCsPointer(Type pointedType, nint address)
        => typeof(DynamicStructBox).GetMethod(nameof(DoWrapCsPointer), BindingFlags.NonPublic | BindingFlags.Static)!
                                   .MakeGenericMethod(pointedType)
                                   .Invoke(null, [address,])!;

    private static unsafe Pointer<T> DoWrapCsPointer<T>(nint address) where T : unmanaged
        => (T*)address;

    private static object? Invoke(MethodBase method, object?[] args)
    {
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; ++i) {
            args[i] = ConvertEx.DynamicChangeType(args[i], parameters[i].ParameterType);
        }

        return method.Invoke(null, args);
    }
}
