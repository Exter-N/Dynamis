using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Interop;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using ImGuiNET;
using SharpDX.Direct3D11;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class TextureInspector(TextureArraySlicer textureArraySlicer) : IObjectInspector<Texture>
{
    private void DrawAdditionalDetailsCommon(Texture* pointer)
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
        if (pointer->D3D11ShaderResourceView is null) {
            ImGui.TextUnformatted($"Format: {pointer->TextureFormat}");
            return;
        }

        var description = ((ShaderResourceView)(nint)pointer->D3D11ShaderResourceView).Description;
        ImGui.TextUnformatted($"Format: {description.Format} Dimension: {description.Dimension}");
    }

    public void DrawAdditionalTooltipDetails(Texture* pointer)
    {
        DrawAdditionalDetailsCommon(pointer);
        ImGui.Image(
            (nint)pointer->D3D11ShaderResourceView,
            new Vector2(pointer->ActualWidth, pointer->ActualHeight).Contain(new(128.0f, 128.0f)),
            Vector2.Zero,
            new Vector2((float)pointer->ActualWidth / pointer->AllocatedWidth, (float)pointer->ActualHeight / pointer->AllocatedHeight)
        );
    }

    public void DrawAdditionalHeaderDetails(Texture* pointer, ObjectInspectorWindow window)
    {
        DrawAdditionalDetailsCommon(pointer);
    }

    public void DrawAdditionalTabs(Texture* pointer, ObjectInspectorWindow window)
    {
        using (var tab = ImRaii.TabItem("Texture View")) {
            if (tab) {
                var state = window.GetCustomState<State>();
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
        }
    }

    private sealed class State
    {
        public bool ContainTextureView = true;
        public byte ArraySliceIndex    = 0;
    }
}
