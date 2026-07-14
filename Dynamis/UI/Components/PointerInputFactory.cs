using Dynamis.Interop;

namespace Dynamis.UI.Components;

public sealed class PointerInputFactory(PointerParser pointerParser, ImGuiComponents imGuiComponents)
{
    public PointerInput Create(string label)
        => new(pointerParser, imGuiComponents, label);
}
