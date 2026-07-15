namespace Dynamis.Interop;

public readonly record struct ClassIdentifier(ClassIdentifierKind Kind, nint Address, Type? Type, string? Name)
{
    public static ClassIdentifier ObjectWithVirtualTable(nint address)
        => new(ClassIdentifierKind.ObjectWithVirtualTable, address, null, null);

    public static ClassIdentifier WellKnownObject(nint address)
        => new(ClassIdentifierKind.WellKnownObject, address, null, null);

    public static ClassIdentifier WellKnownObjectByPointer(nint address)
        => new(ClassIdentifierKind.WellKnownObjectByPointer, address, null, null);

    public static ClassIdentifier VirtualTable(nint address)
        => new(ClassIdentifierKind.VirtualTable, address, null, null);

    public static ClassIdentifier Function(nint address)
        => new(ClassIdentifierKind.Function, address, null, null);

    public static ClassIdentifier ManagedType(Type type)
        => new(ClassIdentifierKind.ManagedType, 0, type, null);
}
