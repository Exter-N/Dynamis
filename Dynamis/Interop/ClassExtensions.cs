namespace Dynamis.Interop;

public static class ClassExtensions
{
    public static bool IsObject(this ClassIdentifierKind kind)
        => kind is ClassIdentifierKind.ObjectWithVirtualTable or ClassIdentifierKind.WellKnownObject
            or ClassIdentifierKind.WellKnownObjectByPointer;
}
