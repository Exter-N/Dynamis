using Dynamis.Utility;

namespace Dynamis.Interop;

public sealed class FieldInfo : IComparable<FieldInfo>
{
    public string Name { get; set; } = string.Empty;

    public uint Offset { get; set; }

    public uint Size { get; set; }

    public FieldType Type { get; set; }

    public ClassInfo? ElementClass { get; set; }

    public Type? EnumType { get; set; }

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
}
