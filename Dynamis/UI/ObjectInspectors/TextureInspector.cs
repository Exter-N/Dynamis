using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Interop;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using TerraFX.Interop.DirectX;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class TextureInspector(TextureArraySlicer textureArraySlicer) : IObjectInspector<Texture>
{
    private void DrawAdditionalDetailsCommon(Texture* pointer, bool live)
    {
        if (pointer->ActualWidth < pointer->AllocatedWidth || pointer->ActualHeight < pointer->AllocatedHeight) {
            ImGui.TextUnformatted($"Size: {pointer->ActualWidth}x{pointer->ActualHeight} (out of {pointer->AllocatedWidth}x{pointer->AllocatedHeight})");
        } else {
            ImGui.TextUnformatted($"Size: {pointer->ActualWidth}x{pointer->ActualHeight}");
        }

        if (pointer->Depth > 1) {
            ImGui.SameLine();
            ImGui.TextUnformatted($"Depth: {pointer->Depth}");
        }

        if (pointer->ArraySize > 1) {
            ImGui.SameLine();
            ImGui.TextUnformatted($"Array: {pointer->ArraySize}");
        }

        ImGui.SameLine();
        ImGui.TextUnformatted($"Mip: {pointer->MipLevel}");
        if (!live || pointer->D3D11ShaderResourceView is null) {
            ImGui.TextUnformatted($"Format: {pointer->TextureFormat}");
            return;
        }

        D3D11_SHADER_RESOURCE_VIEW_DESC description;
        ((ID3D11ShaderResourceView*)pointer->D3D11ShaderResourceView)->GetDesc(&description);
        ImGui.TextUnformatted($"Format: {description.Format} Dimension: {description.ViewDimension}");
    }

    public void DrawAdditionalTooltipDetails(Texture* pointer)
    {
        DrawAdditionalDetailsCommon(pointer, true);
        ImGui.Image(
            new((nint)pointer->D3D11ShaderResourceView),
            new Vector2(pointer->ActualWidth, pointer->ActualHeight).Contain(new(128.0f, 128.0f)),
            Vector2.Zero,
            new Vector2((float)pointer->ActualWidth / pointer->AllocatedWidth, (float)pointer->ActualHeight / pointer->AllocatedHeight)
        );
    }

    public void DrawAdditionalHeaderDetails(Texture* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
        => DrawAdditionalDetailsCommon(pointer, live);

    public void DrawAdditionalTabs(Texture* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
        if (!live) {
            return;
        }

        using var tab = ImRaii.TabItem("Texture Preview");
        if (!tab) {
            return;
        }

        var state = window.GetCustomViewModel<ViewModel>();
        ImGui.Checkbox("Shrink to Fit", ref state.ContainTextureView);
        if (pointer->ArraySize > 1) {
            ImGui.SameLine();
            var slice = (int)state.ArraySliceIndex;
            ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.DragInt(
                    "Array Slice", ref slice, 0.25f, 0, pointer->ArraySize - 1, "%d",
                    ImGuiSliderFlags.AlwaysClamp
                )) {
                state.ArraySliceIndex = (byte)slice;
            }
        }

        if (state.ArraySliceIndex >= pointer->ArraySize) {
            state.ArraySliceIndex = (byte)Math.Max(0, pointer->ArraySize - 1);
        }

        using var _ = ImRaii.Child("###textureView", -Vector2.One);
        var size = new Vector2(pointer->ActualWidth, pointer->ActualHeight);
        if (state.ContainTextureView) {
            size = size.Contain(ImGui.GetContentRegionAvail());
        }

        ImGui.Image(
            textureArraySlicer.GetImGuiHandle(pointer, state.ArraySliceIndex), size,
            Vector2.Zero,
            new Vector2((float)pointer->ActualWidth / pointer->AllocatedWidth, (float)pointer->ActualHeight / pointer->AllocatedHeight)
        );
    }

    private sealed class ViewModel
    {
        public bool ContainTextureView = true;
        public byte ArraySliceIndex    = 0;
    }
}
