using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Interop;
using Dynamis.Interop.Win32;
using Dynamis.UI.Windows;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed class ContextInspector(ImGuiComponents imGuiComponents, ModuleAddressResolver moduleAddressResolver)
    : IObjectInspector<Context>
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

    public unsafe void DrawAdditionalTabs(Context* pointer, ObjectSnapshot snapshot, bool live,
        ObjectInspectorWindow window)
    {
        if (snapshot.StackTrace is not null && snapshot.StackTrace.Length > 0) {
            using var tab = ImRaii.TabItem("Stack Snapshot");
            if (tab) {
                using var _ = ImRaii.Child("###memorySnapshot", -Vector2.One);
                if (snapshot.AssociatedSnapshot?.Address is not null
                 && (nint)snapshot.StackTrace[0].AddrStack.Offset > snapshot.AssociatedSnapshot.Address.Value) {
                    using var node = ImRaii.TreeNode($"???###-1", ImGuiTreeNodeFlags.DefaultOpen);
                    if (node) {
                        ImGui.TextUnformatted("Stack Pointer: ");
                        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
                        var rsp = snapshot.AssociatedSnapshot.Address.Value;
                        ImGuiComponents.DrawCopyable(rsp == 0 ? "nullptr" : $"0x{rsp:X}", true, () => $"{rsp:X}");

                        window.DrawAssociatedSnapshot(
                            ..(int)(snapshot.StackTrace[0].AddrStack.Offset
                                  - (ulong)snapshot.AssociatedSnapshot.Address.Value)
                        );
                    }
                }

                for (var i = 0; i < snapshot.StackTrace.Length; ++i) {
                    var frame = snapshot.StackTrace[i];
                    var rip = (nint)frame.AddrPC.Offset;
                    using var node = ImRaii.TreeNode(
                        $"{moduleAddressResolver.Resolve(rip)?.ToString() ?? rip.ToString("X")}###{i}",
                        ImGuiTreeNodeFlags.DefaultOpen
                    );
                    if (node) {
                        ImGui.TextUnformatted("Instruction Pointer: ");
                        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
                        ImGuiComponents.DrawCopyable(rip == 0 ? "nullptr" : $"0x{rip:X}", true, () => $"{rip:X}");

                        ImGui.TextUnformatted("Stack Pointer: ");
                        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
                        var rsp = snapshot.StackTrace[i].AddrStack.Offset;
                        ImGuiComponents.DrawCopyable(rsp == 0 ? "nullptr" : $"0x{rsp:X}", true, () => $"{rsp:X}");

                        if (snapshot.AssociatedSnapshot?.Address is not null) {
                            var start = (int)(rsp - (ulong)snapshot.AssociatedSnapshot.Address.Value);
                            if (i + 1 < snapshot.StackTrace.Length) {
                                var end = (int)(snapshot.StackTrace[i + 1].AddrStack.Offset
                                              - (ulong)snapshot.AssociatedSnapshot.Address.Value);
                                window.DrawAssociatedSnapshot(start..end);
                            } else {
                                window.DrawAssociatedSnapshot(start..);
                            }
                        }
                    }
                }
            }
        } else if (snapshot.AssociatedSnapshot is not null) {
            using var tab = ImRaii.TabItem("Stack Snapshot");
            if (tab) {
                using var _ = ImRaii.Child("###memorySnapshot", -Vector2.One);
                window.DrawAssociatedSnapshot();
            }
        }
    }
}
