using Dynamis.Interop;
using Dynamis.UI.Windows;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class ConstantBufferInspector(ImGuiComponents imGuiComponents) : IObjectInspector<ConstantBuffer>
{
    public void DrawAdditionalTooltipDetails(ConstantBuffer* pointer)
    {
        ImGui.TextUnformatted($"Constant Buffer Size: {pointer->ByteSize} bytes ({pointer->ByteSize >> 4} vectors)");
    }

    public void DrawAdditionalHeaderDetails(ConstantBuffer* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
        DrawAdditionalTooltipDetails(pointer);
        var sourcePtr = pointer->TryGetSourcePointer();
        if (sourcePtr is null) {
            return;
        }

        ImGui.TextUnformatted("Constant Buffer Contents: ");
        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        imGuiComponents.DrawPointer(
            (nint)sourcePtr,
            () => PseudoClasses.Generate(
                "<Constant Buffer Contents>", (uint)pointer->ByteSize, PseudoClasses.Template.SingleArray,
                ClassKind.ConstantBufferContents
            ), null
        );
    }

    public void DrawAdditionalTabs(ConstantBuffer* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
    }
}
