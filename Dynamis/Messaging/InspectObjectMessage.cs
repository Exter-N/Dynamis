using Dynamis.Interop;

namespace Dynamis.Messaging;

public record InspectObjectMessage(nint ObjectAddress, ClassInfo? Class, ObjectSnapshot? Snapshot = null)
{
    public InspectObjectMessage(ObjectSnapshot snapshot) : this(snapshot.Address ?? 0, snapshot.Class, snapshot)
    {
    }
}
