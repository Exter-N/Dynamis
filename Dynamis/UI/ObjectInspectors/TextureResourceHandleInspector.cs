using Dynamis.UI.Windows;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class TextureResourceHandleInspector(TextureInspector textureInspector) : IObjectInspector<TextureResourceHandle>
{
    public void DrawAdditionalTooltipDetails(TextureResourceHandle* pointer)
        => textureInspector.DrawAdditionalTooltipDetails(pointer->Texture);

    public void DrawAdditionalHeaderDetails(TextureResourceHandle* pointer, ObjectInspectorWindow window)
        => textureInspector.DrawAdditionalHeaderDetails(pointer->Texture, window);

    public void DrawAdditionalTabs(TextureResourceHandle* pointer, ObjectInspectorWindow window)
        => textureInspector.DrawAdditionalTabs(pointer->Texture, window);
}
