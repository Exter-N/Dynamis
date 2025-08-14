using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Interop;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.UI.ObjectInspectors;
using Dynamis.UI.Windows;

namespace Dynamis.UI;

public sealed partial class ImGuiComponents(
    MessageHub messageHub,
    FileDialogManager fileDialogManager,
    ModuleAddressResolver moduleAddressResolver,
    AddressIdentifier addressIdentifier,
    ObjectInspector objectInspector,
    Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher,
    ContextMenu contextMenu)
{
    public void AddTitleBarButtons(Window window)
    {
        if (window is not ToolboxWindow) {
            window.TitleBarButtons.Add(
                new()
                {
                    Icon = FontAwesomeIcon.Home,
                    Click = _ => messageHub.Publish<OpenWindowMessage<ToolboxWindow>>(),
                    IconOffset = new(1, 0),
                    ShowTooltip = () =>
                    {
                        using var _ = ImRaii.Tooltip();
                        ImGui.Text("Toolbox");
                    }
                }
            );
        }

        if (window is not SettingsWindow) {
            window.TitleBarButtons.Add(
                new()
                {
                    Icon = FontAwesomeIcon.Cog,
                    Click = _ => messageHub.Publish<OpenWindowMessage<SettingsWindow>>(),
                    IconOffset = new(2, 1),
                    ShowTooltip = () =>
                    {
                        using var _ = ImRaii.Tooltip();
                        ImGui.Text("Settings");
                    }
                }
            );
        }
    }

    public static void DrawCopyable(string text, bool mono, Func<string>? copyText = null)
    {
        bool clicked;
        using (ImRaii.PushFont(UiBuilder.MonoFont, mono)) {
            clicked = ImGui.Selectable(text);
        }

        if (clicked) {
            ImGui.SetClipboardText(copyText?.Invoke() ?? text);
        }

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            using (ImRaii.PushFont(UiBuilder.MonoFont, mono)) {
                ImGui.TextUnformatted(copyText?.Invoke() ?? text);
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Click to copy to clipboard.");
        }
    }

    public void DrawPointer(nint pointer, Func<ClassInfo?>? @class, Func<string?>? name, string? customText = null,
        DrawPointerFlags flags = DrawPointerFlags.None,
        ImGuiSelectableFlags selectableFlags = ImGuiSelectableFlags.None, Vector2 size = default)
    {
        {
            using var font = ImRaii.PushFont(
                UiBuilder.MonoFont, customText is null ? pointer != 0 : flags.HasFlag(DrawPointerFlags.MonoFont)
            );
            using var style = ImRaii.PushStyle(
                ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f,
                customText is null ? pointer == 0 : flags.HasFlag(DrawPointerFlags.Semitransparent)
            );
            style.Push(
                ImGuiStyleVar.SelectableTextAlign, new Vector2(1.0f, 0.5f),
                size != default || flags.HasFlag(DrawPointerFlags.RightAligned)
            );
            if (ImGui.Selectable(
                    customText ?? (pointer == 0 ? "nullptr" : $"0x{pointer:X}"),
                    flags.HasFlag(DrawPointerFlags.Selected), selectableFlags, size
                )) {
                OpenPointerContextMenu(pointer, @class, name);
            }
        }

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            ImGui.TextUnformatted("Address: ");
            ImGui.SameLine(0, 0);
            using (ImRaii.PushFont(UiBuilder.MonoFont)) {
                ImGui.TextUnformatted(pointer.ToString("X"));
            }

            if (pointer != 0) {
                ImGui.Separator();
                DrawPointerTooltipDetails(pointer, @class?.Invoke());
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Click for options.");
        }
    }

    [Flags]
    public enum DrawPointerFlags : uint
    {
        None = 0,

        /// <summary>
        /// Draws the ImGui selectable as selected.
        /// </summary>
        Selected = 1,

        /// <summary>
        /// Draws the supplied custom text in a monospace font.
        /// Applied to the default text if the pointer is not null.
        /// </summary>
        MonoFont = 2,

        /// <summary>
        /// Draws the supplied custom text with halved opacity.
        /// Applied to the default text if the pointer is null.
        /// </summary>
        Semitransparent = 4,

        /// <summary>
        /// Aligns the text to the right horizontally and centers it vertically.
        /// Always applied when passed an explicit size.
        /// </summary>
        RightAligned = 8,
    }

    public void DrawPointerTooltipDetails(nint pointer, ClassInfo? @class)
    {
        var protect = VirtualMemory.GetProtection(pointer);
        if (protect.CanExecute()) {
            ImGui.TextUnformatted("Function pointer");
        }

        var wellKnown = addressIdentifier.Identify(pointer);
        var wellKnownStr = wellKnown.Describe();
        if (!string.IsNullOrEmpty(wellKnownStr)) {
            ImGui.TextUnformatted(wellKnownStr);
        }

        nuint displacement = 0;
        if (@class is null) {
            (@class, displacement) = objectInspector.DetermineClassAndDisplacement(pointer, null, null, false);
        }

        if (@class.Known && @class.Name != wellKnown.ClassName) {
            ImGui.TextUnformatted($"Class Name: {@class.Name}");
        }

        if (displacement > 0) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudYellow!.Value)) {
                ImGui.TextUnformatted($"Displacement: {displacement} (0x{displacement:X}) bytes");
            }
        }

        ImGui.TextUnformatted($"Estimated Size: {@class.EstimatedSize} (0x{@class.EstimatedSize:X}) bytes");

        if (!string.IsNullOrEmpty(@class.DefiningModule)) {
            ImGui.TextUnformatted($"Defined in Module: {@class.DefiningModule}");
        }

        foreach (var inspector in objectInspectorDispatcher.Value.GetInspectors(@class)) {
            inspector.DrawAdditionalTooltipDetails(pointer - (nint)displacement, @class);
        }
    }

    public void OpenPointerContextMenu(nint pointer, Func<ClassInfo?>? @class, Func<string?>? name)
        => contextMenu.Open(
            new PointerContextMenu(messageHub, pointer, moduleAddressResolver.Resolve(pointer), @class, name)
        );

    private sealed class PointerContextMenu(
        MessageHub messageHub,
        nint pointer,
        ModuleAddress? moduleAddress,
        Func<ClassInfo?>? @class,
        Func<string?>? name) : IDrawable
    {
        public bool Draw()
        {
            var ret = false;
            if (pointer != 0 && ImGui.Selectable("Inspect object")) {
                messageHub.Publish(new InspectObjectMessage(pointer, @class?.Invoke(), null, name?.Invoke()));
                ret = true;
            }

            if (ImGui.Selectable($"Copy address ({pointer:X})")) {
                ImGui.SetClipboardText(pointer.ToString("X"));
                ret = true;
            }

            if (moduleAddress is not null) {
                if (ImGui.Selectable($"Copy {moduleAddress}")) {
                    ImGui.SetClipboardText(moduleAddress.ToString());
                    ret = true;
                }

                if (moduleAddress.OriginalAddress != 0 && moduleAddress.OriginalAddress != pointer
                                                       && ImGui.Selectable(
                                                              $"Copy original address ({moduleAddress.OriginalAddress:X})"
                                                          )) {
                    ImGui.SetClipboardText(moduleAddress.OriginalAddress.ToString("X"));
                    ret = true;
                }
            }

            return ret;
        }
    }
}
