using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using ImGuiNET;

namespace Dynamis.UI;

// Parts borrowed from https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/UI/UISharedService.cs
public sealed class ImGuiComponents
{
    private readonly MessageHub        _messageHub;
    private readonly FileDialogManager _fileDialogManager;

    public ImGuiComponents(MessageHub messageHub, FileDialogManager fileDialogManager)
    {
        _messageHub = messageHub;
        _fileDialogManager = fileDialogManager;
    }

    public void AddTitleBarButtons(Window window)
    {
        if (window is not HomeWindow) {
            window.TitleBarButtons.Add(
                new()
                {
                    Icon = FontAwesomeIcon.Home,
                    Click = _ => _messageHub.Publish<OpenWindowMessage<HomeWindow>>(),
                    IconOffset = new(1, 0),
                    ShowTooltip = () =>
                    {
                        using var _ = ImRaii.Tooltip();
                        ImGui.Text("Open Dynamis Home");
                    }
                }
            );
        }

        if (window is not SettingsWindow) {
            window.TitleBarButtons.Add(
                new()
                {
                    Icon = FontAwesomeIcon.Cog,
                    Click = _ => _messageHub.Publish<OpenWindowMessage<SettingsWindow>>(),
                    IconOffset = new(2, 1),
                    ShowTooltip = () =>
                    {
                        using var _ = ImRaii.Tooltip();
                        ImGui.Text("Open Dynamis Settings");
                    }
                }
            );
        }
    }

    public void InputFile(string label, string filters, string path, Action<string> setPath)
    {
        using var id = ImRaii.PushId(label);

        var innerSpacing = ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - NormalizedIconButtonSize(FontAwesomeIcon.Folder).X - innerSpacing);
        if (ImGui.InputText(
                "###path", ref path, 260, ImGuiInputTextFlags.EnterReturnsTrue
            )) {
            setPath(path);
        }

        ImGui.SameLine(0.0f, innerSpacing);
        if (NormalizedIconButton(FontAwesomeIcon.Folder)) {
            _fileDialogManager.OpenFileDialog(
                "Pick " + label, filters, (success, newPath) =>
                {
                    if (!success) {
                        return;
                    }

                    setPath(newPath);
                }
            );
        }

        ImGui.SameLine(0.0f, innerSpacing);
        ImGui.TextUnformatted(label);
    }

    public static bool ComboEnum<T>(string label, ref T value, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, Enum
    {
        using var combo = ImRaii.Combo(label, value.ToString(), flags);
        if (!combo) {
            return false;
        }

        var boxedValue = (object)value;
        var changed = false;
        foreach (var v in Enum.GetValues<T>()) {
            var selected = v.Equals(boxedValue);
            if (ImGui.Selectable(v.ToString(), selected)) {
                value = v;
                changed = true;
            }

            if (selected) {
                ImGui.SetItemDefaultFocus();
            }
        }

        return changed;
    }

    public static unsafe bool InputPointer(string label, ref nint value, ImGuiInputTextFlags flags = 0)
    {
        using var id = ImRaii.PushId(label);

        bool changed;
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            fixed (nint* pValue = &value) {
                changed = ImGui.InputScalar(
                    "###pointer", nint.Size switch
                    {
                        4 => ImGuiDataType.U32,
                        8 => ImGuiDataType.U64,
                        _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
                    },
                    new(pValue),
                    0,
                    0,
                    nint.Size switch
                    {
                        4 => "%08X",
                        8 => "%016llX",
                        _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
                    },
                    ImGuiInputTextFlags.CharsHexadecimal | flags
                );
            }
        }

        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.TextUnformatted(label);

        return changed;
    }

    #region Icon and Icon Buttons
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
    #endregion
}
