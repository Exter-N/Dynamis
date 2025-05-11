using Dynamis.Utility;

namespace Dynamis.Interop;

public sealed class FieldInfo : IComparable<FieldInfo>
{
    public string Name { get; set; } = string.Empty;

    public uint Offset { get; set; }

    public uint Size { get; set; }

    public FieldType Type { get; set; }

    public ClassInfo? ElementClass { get; set; }

    public Type? ManagedType { get; set; }

    public uint ElementSize
    {
        get
        {
            if (ElementClass is not null) {
                return ElementClass.EstimatedSize;
            }

            return (uint)Type.Size();
        }
    }

    public uint ElementCount
        => Size / ElementSize;

    public override string ToString()
        => Name;

    public int CompareTo(FieldInfo? other)
    {
        if (other is null) {
            return 1;
        }

        if (Offset < other.Offset) {
            return -1;
        }

        if (Offset > other.Offset) {
            return 1;
        }

        if (Size > other.Size) {
            return -1;
        }

        if (Size < other.Size) {
            return 1;
        }

        return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public static nint GetAddress(object? value)
        => TryGetAddress(value, out var address)
            ? address
            : throw new ArgumentException("Unsupported pointer");

    public static bool TryGetAddress(object? value, out nint address)
    {
        switch (value)
        {
            case null:
                address = 0;
                return true;
            case nint raw:
                address = raw;
                return true;
            case DynamicMemory memory:
                address = memory.Address;
                return true;
            default:
                address = 0;
                return false;
        }
    }

    public static (ClassInfo Class, nuint Displacement)? DetermineClassAndDisplacement(object? value, ObjectInspector objectInspector, ClassRegistry classRegistry)
    {
        switch (value)
        {
            case nint raw:
                if (raw == 0) {
                    return null;
                }

                return objectInspector.DetermineClassAndDisplacement(raw);
            case DynamicMemory memory:
                return (PseudoClasses.GenerateArray(classRegistry.FromManagedType(memory.ElementType), memory.Length),
                    0);
            default:
                return null;
        }
    }
}
