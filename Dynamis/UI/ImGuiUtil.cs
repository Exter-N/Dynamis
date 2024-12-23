using System.Runtime.CompilerServices;
using ImGuiNET;

namespace Dynamis.UI;

// Parts borrowed from https://github.com/Ottermandias/OtterGui/blob/main/Util.cs
public static class ImGuiUtil
{
    /// <summary> Halves the alpha of the given color. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HalfTransparent(uint baseColor)
        => (baseColor & 0x00FFFFFFu) | ((baseColor & 0xFE000000u) >> 1);

    /// <summary> Gets the current text color, but with halved alpha. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HalfTransparentText()
        => HalfTransparent(ImGui.GetColorU32(ImGuiCol.Text));

    /// <summary>
    /// Blends the given colors in equal proportions.
    /// <paramref name="overlayColor"/> can only be fully saturated black, red, green, blue, cyan, magenta, yellow or white.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HalfBlend(uint baseColor, uint overlayColor)
        => (baseColor & 0xFF000000u) | ((baseColor & 0x00FEFEFEu) >> 1) | (overlayColor & 0x00808080u);

    /// <summary> Gets the current text color, blended in equal proportions with the given fully saturated color. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HalfBlendText(uint overlayColor)
        => HalfBlend(ImGui.GetColorU32(ImGuiCol.Text), overlayColor);
}
