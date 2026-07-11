namespace Dynamis.Interop;

public readonly record struct ClassIdentifier(ClassIdentifierKind Kind, nint Address)
{
    public static ClassIdentifier ObjectWithVirtualTable(nint address)
        => new(ClassIdentifierKind.ObjectWithVirtualTable, address;

    public static ClassIdentifier WellKnownObject(nint address)
        => new(ClassIdentifierKind.WellKnownObject, address;

    public static ClassIdentifier WellKnownObjectByPointer(nint address)
        => new(ClassIdentifierKind.WellKnownObjectByPointer, address;

    public static ClassIdentifier VirtualTable(nint address)
        => new(ClassIdentifierKind.VirtualTable, address;

    public static ClassIdentifier Function(nint address)
        => new(ClassIdentifierKind.Function, address;
}
