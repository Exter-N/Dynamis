using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;

namespace Dynamis.UI;

public ref struct ImRaiiTextWrapPos
{
    public bool Disposed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImRaiiTextWrapPos()
        => ImGui.PushTextWrapPos();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImRaiiTextWrapPos(float wrapLocalPosX)
        => ImGui.PushTextWrapPos(wrapLocalPosX);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed) {
            return;
        }

        ImGui.PopTextWrapPos();
        Disposed = true;
    }
}
