using Dynamis.Interop;
using Dynamis.UI.Windows;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class TextureResourceHandleInspector(TextureInspector textureInspector) : IObjectInspector<TextureResourceHandle>
{
    public void DrawAdditionalTooltipDetails(TextureResourceHandle* pointer)
        => textureInspector.DrawAdditionalTooltipDetails(pointer->Texture);

    public void DrawAdditionalHeaderDetails(TextureResourceHandle* pointer, ObjectSnapshot snapshot, bool live,
        ObjectInspectorWindow window)
    {
        if (live) {
            textureInspector.DrawAdditionalHeaderDetails(pointer->Texture, snapshot, live, window);
        }
    }

    public void DrawAdditionalTabs(TextureResourceHandle* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
        => textureInspector.DrawAdditionalTabs(pointer->Texture, snapshot, live, window);
}
