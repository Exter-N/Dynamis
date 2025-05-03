using System.Dynamic;
using System.Reflection;
using Dynamis.Interop.Win32;
using Dynamis.Utility;
using FFXIVClientStructs.Interop;

namespace Dynamis.Interop;

public sealed class DynamicSpanBox(
    nint address,
    int length,
    ClassInfo elementClass,
    bool pointers,
    BoxAccess access,
    DynamicBoxFactory factory) : DynamicObject, IBoxedAddress, IDynamicBox
{
    public nint Address
        => address;

    public int Length
        => length;

    public ClassInfo ElementClass
        => elementClass;

    private uint ElementSize
        => pointers ? (uint)nint.Size : ElementClass.EstimatedSize;

    private object Memory
        => typeof(DynamicSpanBox).GetMethod(
                                      access >= BoxAccess.Mutable ? nameof(WrapMemory) : nameof(WrapReadOnlyMemory),
                                      BindingFlags.NonPublic | BindingFlags.Static
                                  )!
                                 .MakeGenericMethod(
                                      pointers
                                          ? typeof(Pointer<>).MakeGenericType(elementClass.BestManagedType!)
                                          : elementClass.BestManagedType!
                                  )
                                 .Invoke(null, [address, length,])!;

    public unsafe object? this[int index]
    {
        get
        {
            var elementAddress = GetElementAddress(index);
            if (pointers) {
                var element = VirtualMemory.GetProtection(elementAddress).CanRead() ? *(nint*)elementAddress : 0;
                return element != 0 ? factory.BoxStruct(element, access.Deep()) : null;
            } else {
                return new DynamicStructBox(elementAddress, elementClass, access, factory);
            }
        }
        set
        {
            if (access < BoxAccess.Mutable) {
                throw new NotSupportedException("This box is read-only.");
            }

            if (!pointers) {
                throw new NotSupportedException("Elements cannot be replaced in value span boxes.");
            }

            var elementAddress = GetElementAddress(index);
            var element = IBoxedAddress.TryUnbox(value, out var addr) ? addr : ConvertEx.ToIntPtr(addr);
            *(nint*)elementAddress = element;
        }
    }

    private nint GetElementAddress(int index)
    {
        if (index < 0 || index >= length) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return address + index * (nint)ElementSize;
    }

    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        if (indexes.Length != 1) {
            result = null;
            return false;
        }

        result = this[Convert.ToInt32(indexes[0])];
        return true;
    }

    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
    {
        if (access < BoxAccess.Mutable || !pointers) {
            return false;
        }

        if (indexes.Length != 1) {
            return false;
        }

        this[Convert.ToInt32(indexes[0])] = value;
        return true;
    }

    object IDynamicBox.Unbox()
        => Memory;

    private static unsafe Memory<T> WrapMemory<T>(nint address, int length) where T : unmanaged
        => new BorrowedUnmanagedMemory<T>((T*)address, length).Memory;

    private static unsafe ReadOnlyMemory<T> WrapReadOnlyMemory<T>(nint address, int length) where T : unmanaged
        => new BorrowedUnmanagedMemory<T>((T*)address, length).Memory;
}
