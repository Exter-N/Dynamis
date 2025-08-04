using Dynamis.Interop;
using Dynamis.UI.Windows;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class GameObjectInspector(ImGuiComponents imGuiComponents) : IObjectInspector<GameObject>
{
    public void DrawAdditionalTooltipDetails(GameObject* pointer)
    {
        ImGui.TextUnformatted($"Name: {pointer->NameString}");
    }

    public void DrawAdditionalHeaderDetails(GameObject* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
        DrawAdditionalTooltipDetails(pointer);
        if (!live) {
            return;
        }

        var drawObject = pointer->GetDrawObject();
        if (drawObject is not null) {
            ImGui.TextUnformatted("Draw Object: ");
            ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
            var name = pointer->NameString;
            imGuiComponents.DrawPointer((nint)drawObject, null, () => $"Draw object of {name}");
        }
    }

    public void DrawAdditionalTabs(GameObject* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
    }
}
