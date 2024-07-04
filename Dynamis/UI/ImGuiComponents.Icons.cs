using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Dynamis.UI;

// Parts borrowed from https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/UI/UISharedService.cs
partial class ImGuiComponents
{
    public static Vector2 GetNormalizedIconTextButtonSize(FontAwesomeIcon icon, string text, float? width = null,
        bool isInPopup = false)
    {
        var iconData = GetIconData(icon);
        var textSize = ImGui.CalcTextSize(text);
        var padding = ImGui.GetStyle().FramePadding;
        var buttonSizeY = ImGui.GetFrameHeight();
        var iconExtraSpacing = isInPopup ? padding.X * 2 : 0;

        if (width == null || width <= 0) {
            var buttonSizeX = iconData.NormalizedIconScale.X + (padding.X * 3) + iconExtraSpacing + textSize.X;
            return new(buttonSizeX, buttonSizeY);
        } else {
            return new(width.Value, buttonSizeY);
        }
    }

    public static Vector2 NormalizedIconButtonSize(FontAwesomeIcon icon)
    {
        var iconData = GetIconData(icon);
        var padding = ImGui.GetStyle().FramePadding;

        return iconData.NormalizedIconScale with
        {
            X = iconData.NormalizedIconScale.X + padding.X * 2,
            Y = iconData.NormalizedIconScale.Y + padding.Y * 2
        };
    }

    public static bool NormalizedIconButton(FontAwesomeIcon icon)
    {
        bool wasClicked = false;
        var iconData = GetIconData(icon);
        var padding = ImGui.GetStyle().FramePadding;
        var cursor = ImGui.GetCursorPos();
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetWindowPos();
        var scrollPosY = ImGui.GetScrollY();
        var scrollPosX = ImGui.GetScrollX();

        var buttonSize = NormalizedIconButtonSize(icon);

        if (ImGui.Button("###" + icon.ToIconString(), buttonSize)) {
            wasClicked = true;
        }

        drawList.AddText(
            UiBuilder.IconFont, ImGui.GetFontSize() * iconData.IconScaling,
            new(
                pos.X - scrollPosX + cursor.X + iconData.OffsetX + padding.X,
                pos.Y - scrollPosY + cursor.Y + (buttonSize.Y - (iconData.IconSize.Y * iconData.IconScaling)) / 2f
            ),
            ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString()
        );

        return wasClicked;
    }

    public static bool NormalizedIconTextButton(FontAwesomeIcon icon, string text, float? width = null,
        bool isInPopup = false)
    {
        var wasClicked = false;

        var iconData = GetIconData(icon);
        var textSize = ImGui.CalcTextSize(text);
        var padding = ImGui.GetStyle().FramePadding;
        var cursor = ImGui.GetCursorPos();
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetWindowPos();
        var scrollPosY = ImGui.GetScrollY();
        var scrollPosX = ImGui.GetScrollX();

        Vector2 buttonSize = GetNormalizedIconTextButtonSize(icon, text, width, isInPopup);
        var iconExtraSpacing = isInPopup ? padding.X * 2 : 0;

        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.PopupBg), isInPopup)) {
            if (ImGui.Button("###" + icon.ToIconString() + text, buttonSize)) {
                wasClicked = true;
            }
        }

        drawList.AddText(
            UiBuilder.DefaultFont, ImGui.GetFontSize(),
            new(
                pos.X - scrollPosX + cursor.X + iconData.NormalizedIconScale.X + (padding.X * 2) + iconExtraSpacing,
                pos.Y - scrollPosY + cursor.Y + ((buttonSize.Y - textSize.Y) / 2f)
            ),
            ImGui.GetColorU32(ImGuiCol.Text), text
        );

        drawList.AddText(
            UiBuilder.IconFont, ImGui.GetFontSize() * iconData.IconScaling,
            new(
                pos.X - scrollPosX + cursor.X + iconData.OffsetX + padding.X,
                pos.Y - scrollPosY + cursor.Y + (buttonSize.Y - (iconData.IconSize.Y * iconData.IconScaling)) / 2f
            ),
            ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString()
        );

        return wasClicked;
    }

    public static void NormalizedIcon(FontAwesomeIcon icon, uint color)
    {
        var cursorPos = ImGui.GetCursorPos();
        var iconData = GetIconData(icon);
        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var scrollPosX = ImGui.GetScrollX();
        var scrollPosY = ImGui.GetScrollY();
        var frameHeight = ImGui.GetFrameHeight();

        var frameOffsetY = ((frameHeight - iconData.IconSize.Y * iconData.IconScaling) / 2f);

        drawList.AddText(
            UiBuilder.IconFont, UiBuilder.IconFont.FontSize * iconData.IconScaling,
            new(
                windowPos.X - scrollPosX + cursorPos.X + iconData.OffsetX,
                windowPos.Y - scrollPosY + cursorPos.Y + frameOffsetY
            ),
            color, icon.ToIconString()
        );

        ImGui.Dummy(new(iconData.NormalizedIconScale.X, ImGui.GetFrameHeight()));
    }

    private static IconScaleData CalcIconScaleData(FontAwesomeIcon icon)
    {
        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        var iconSize = ImGui.CalcTextSize(icon.ToIconString());
        var iconscaling = (iconSize.X < iconSize.Y ? (iconSize.Y - iconSize.X) / 2f : 0f, iconSize.X > iconSize.Y ? 1f / (iconSize.X / iconSize.Y) : 1f);
        var normalized = iconscaling.Item2 == 1f ?
            new Vector2(iconSize.Y, iconSize.Y)
            : new((iconSize.X * iconscaling.Item2) + (iconscaling.Item1 * 2), (iconSize.X * iconscaling.Item2) + (iconscaling.Item1 * 2));
        return new(iconSize, normalized, iconscaling.Item1, iconscaling.Item2);
    }

    public static IconScaleData GetIconData(FontAwesomeIcon icon)
    {
        if (_iconData.TryGetValue(ImGuiHelpers.GlobalScale, out var iconCache)) {
            if (iconCache.TryGetValue(icon, out var iconData)) {
                return iconData;
            }

            return iconCache[icon] = CalcIconScaleData(icon);
        }

        _iconData.Add(ImGuiHelpers.GlobalScale, new());
        return _iconData[ImGuiHelpers.GlobalScale][icon] = CalcIconScaleData(icon);
    }

    public sealed record IconScaleData(Vector2 IconSize, Vector2 NormalizedIconScale, float OffsetX, float IconScaling);

    private static Dictionary<float, Dictionary<FontAwesomeIcon, IconScaleData>> _iconData = new();
}
