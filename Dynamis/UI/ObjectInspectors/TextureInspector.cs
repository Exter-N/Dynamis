using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class TextureInspector : IObjectInspector<Texture>
{
    private void DrawAdditionalDetailsCommon(Texture* pointer)
    {
        ImGui.TextUnformatted($"Width: {pointer->Width} Height: {pointer->Height} Depth: {pointer->Depth} Array: {pointer->ArraySize} Mip: {pointer->MipLevel}");
        ImGui.TextUnformatted($"Pixel Format: {pointer->TextureFormat}");
    }

    public void DrawAdditionalTooltipDetails(Texture* pointer)
    {
        DrawAdditionalDetailsCommon(pointer);
        ImGui.Image(
            (nint)pointer->D3D11ShaderResourceView,
            new Vector2(pointer->Width, pointer->Height).Contain(new(128.0f, 128.0f)),
            Vector2.Zero,
            new Vector2((float)pointer->Width / pointer->Width2, (float)pointer->Height / pointer->Height2)
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
                using var _ = ImRaii.Child("###textureView", -Vector2.One);
                var size = new Vector2(pointer->Width, pointer->Height);
                if (state.ContainTextureView) {
                    size = size.Contain(ImGui.GetContentRegionAvail());
                }

                ImGui.Image(
                    (nint)pointer->D3D11ShaderResourceView, size,
                    Vector2.Zero,
                    new Vector2((float)pointer->Width / pointer->Width2, (float)pointer->Height / pointer->Height2)
                );
            }
        }
    }

    private sealed class State
    {
        public bool ContainTextureView = true;
    }
}
