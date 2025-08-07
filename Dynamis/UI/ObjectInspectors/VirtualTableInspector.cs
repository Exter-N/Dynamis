using Dalamud.Bindings.ImGui;
using Dynamis.Interop;
using Dynamis.UI.Windows;

namespace Dynamis.UI.ObjectInspectors;

public sealed class VirtualTableInspector : IDynamicObjectInspector
{
    public bool CanInspect(ClassInfo @class)
        => @class.Kind == ClassKind.VirtualTable;

    public void DrawAdditionalTooltipDetails(nint pointer, ClassInfo @class)
    {
        if (@class.VtblOwnerSizeAndDisplacementFromDtor is
            {
            } size) {
            ImGui.TextUnformatted($"Class Size: {size.Size} (0x{size.Size:X}) bytes");

            if (size.Displacement > 0) {
                ImGui.TextUnformatted($"Displacement: {size.Displacement} (0x{size.Displacement:X}) bytes");
            }
        }
    }

    public void DrawAdditionalHeaderDetails(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
        if (snapshot is
            {
                Address:
                {
                } pointer,
                Class: not null,
            }) {
            DrawAdditionalTooltipDetails(pointer, snapshot.Class);
        }
    }

    public void DrawAdditionalTabs(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
    }
}
