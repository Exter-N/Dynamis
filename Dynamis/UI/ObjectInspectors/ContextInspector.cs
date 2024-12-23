using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Interop;
using Dynamis.Interop.Win32;
using Dynamis.UI.Windows;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed class ContextInspector(ImGuiComponents imGuiComponents) : IObjectInspector<Context>
{
    public unsafe void DrawAdditionalTooltipDetails(Context* pointer)
    {
    }

    public unsafe void DrawAdditionalHeaderDetails(Context* pointer, ObjectSnapshot snapshot, bool live,
        ObjectInspectorWindow window)
    {
        ImGui.TextUnformatted("Instruction Pointer: ");
        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        imGuiComponents.DrawPointer((nint)pointer->Rip, null);

        ImGui.TextUnformatted("This Argument: ");
        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        imGuiComponents.DrawPointer((nint)pointer->Rcx, null);

        ImGui.TextUnformatted("Stack Pointer: ");
        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        var rsp = (nint)pointer->Rsp;
        ImGuiComponents.DrawCopyable(rsp == 0 ? "nullptr" : $"0x{rsp:X}", true, () => $"{rsp:X}");
    }

    public unsafe void DrawAdditionalTabs(Context* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
        using (var tab = ImRaii.TabItem("Stack Snapshot")) {
            if (tab) {
                using var _ = ImRaii.Child("###memorySnapshot", -Vector2.One);
                window.DrawAssociatedSnapshot();
            }
        }
    }
}
