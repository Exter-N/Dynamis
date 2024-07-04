using Dynamis.Interop;

namespace Dynamis.Messaging;

public record InspectObjectMessage(nint ObjectAddress, ClassInfo? Class);
