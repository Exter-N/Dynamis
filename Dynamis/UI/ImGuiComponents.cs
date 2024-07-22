using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Interop;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.UI.ObjectInspectors;
using Dynamis.UI.Windows;
using ImGuiNET;

namespace Dynamis.UI;

public sealed partial class ImGuiComponents(MessageHub messageHub, FileDialogManager fileDialogManager, ObjectInspector objectInspector, Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher)
{
    public void AddTitleBarButtons(Window window)
    {
        if (window is not HomeWindow) {
            window.TitleBarButtons.Add(
                new()
                {
                    Icon = FontAwesomeIcon.Home,
                    Click = _ => messageHub.Publish<OpenWindowMessage<HomeWindow>>(),
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
                    Click = _ => messageHub.Publish<OpenWindowMessage<SettingsWindow>>(),
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

    public static void DrawCopyable(string text, bool mono)
    {
        using (ImRaii.PushFont(UiBuilder.MonoFont, mono)) {
            ImGui.Selectable(text);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            ImGui.SetClipboardText(text);
        }

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            using (ImRaii.PushFont(UiBuilder.MonoFont, mono)) {
                ImGui.TextUnformatted(text);
            }
            ImGui.TextUnformatted("Right-click to copy to clipboard.");
        }
    }

    public void DrawPointer(nint pointer, Func<ClassInfo?>? @class)
    {
        using (ImRaii.PushFont(UiBuilder.MonoFont, pointer != 0)) {
            if (ImGui.Selectable(pointer == 0 ? "nullptr" : $"0x{pointer:X}") && pointer != 0) {
                messageHub.Publish(new InspectObjectMessage(pointer, @class?.Invoke()));
            }
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            ImGui.SetClipboardText(pointer.ToString("X"));
        }

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            ImGui.TextUnformatted("Address: ");
            ImGui.SameLine(0, 0);
            using (ImRaii.PushFont(UiBuilder.MonoFont)) {
                ImGui.TextUnformatted(pointer.ToString("X"));
            }

            if (pointer != 0) {
                DrawPointerTooltipDetails(pointer, @class?.Invoke());
                ImGui.TextUnformatted("Click to inspect.");
            }

            ImGui.TextUnformatted("Right-click to copy address to clipboard.");
        }
    }

    public void DrawPointerTooltipDetails(nint pointer, ClassInfo? @class)
    {
        var protect = VirtualMemory.GetProtection(pointer);
        if (protect.CanExecute()) {
            ImGui.TextUnformatted("Function pointer");
        }

        var wellKnown = objectInspector.IdentifyAddress(pointer);
        switch (wellKnown.Type) {
            case AddressType.Vtbl:
                ImGui.TextUnformatted($"Virtual table of class {wellKnown.Name}");
                break;
            case AddressType.Instance:
                ImGui.TextUnformatted($"Well-known instance of class {wellKnown.Name}");
                break;
        }

        @class ??= objectInspector.DetermineClass(pointer);
        if (@class.Known) {
            ImGui.TextUnformatted($"Class Name: {@class.Name}");
        }

        ImGui.TextUnformatted($"Estimated Size: {@class.EstimatedSize} (0x{@class.EstimatedSize:X}) bytes");

        foreach (var inspector in objectInspectorDispatcher.Value.GetInspectors(@class)) {
            inspector.DrawAdditionalTooltipDetails(pointer);
        }
    }
}
