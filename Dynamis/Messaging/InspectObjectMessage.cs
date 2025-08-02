using Dynamis.Interop;

namespace Dynamis.Messaging;

public record InspectObjectMessage(
    nint ObjectAddress,
    ClassInfo? Class,
    ClassIdentifier? ClassIdentifierHint,
    string? Name,
    ObjectSnapshot? Snapshot = null)
{
    public InspectObjectMessage(ObjectSnapshot snapshot) : this(
        snapshot.Address ?? 0, snapshot.Class, null, snapshot.Name, snapshot
    )
    {
    }
}
