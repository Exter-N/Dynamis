namespace Dynamis.Interop;

public readonly record struct AddressIdentification(
    AddressType Type,
    string ClassName,
    ClassIdentifier? ClassIdentifierHint,
    string? Name)
{
    public static readonly AddressIdentification Default = new(0, string.Empty, null, null);

    public string? GetFullName()
    {
        var nameSuffix = string.IsNullOrEmpty(Name) ? string.Empty : $" - {Name}";
        return Type switch
        {
            0                        => Name,
            AddressType.Instance     => $"{ClassName}{nameSuffix}",
            AddressType.VirtualTable => $"vtbl_{ClassName}{nameSuffix}",
            AddressType.Global       => Name,
            AddressType.Function     => string.IsNullOrEmpty(ClassName) ? Name : $"{ClassName}::{Name}",
            _                        => $"{ClassName}{nameSuffix}",
        };
    }

    public string? Describe()
    {
        var nameSuffix = string.IsNullOrEmpty(Name) ? string.Empty : $" - {Name}";
        return Type switch
        {
            0                        => Name,
            AddressType.Instance     => $"Well-known {ClassName}{nameSuffix}",
            AddressType.VirtualTable => $"Virtual table of {ClassName}{nameSuffix}",
            AddressType.Global       => $"Global variable {Name}",
            AddressType.Function => string.IsNullOrEmpty(ClassName)
                ? $"Function {Name}"
                : $"Function {ClassName}::{Name}",
            _ => $"{Type} of {ClassName}{nameSuffix}",
        };
    }
}
