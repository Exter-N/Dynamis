using Dynamis.Interop;
using Dynamis.UI.Windows;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed class VirtualTableInspector : IDynamicObjectInspector
{
    public bool CanInspect(ClassInfo @class)
        => @class.Kind == ClassKind.VirtualTable;

    public void DrawAdditionalTooltipDetails(nint pointer, ClassInfo @class)
    {
        if (@class.VtblOwnerSizeFromDtor is
            {
            } size) {
            ImGui.TextUnformatted($"Class Size: {size} (0x{size:X}) bytes");
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
