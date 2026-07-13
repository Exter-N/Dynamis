using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Dynamis.Utility;

namespace Dynamis.UI;

public sealed class DevMenuItem(
    IDalamudPluginInterface pluginInterface,
    ConfigurationContainer configuration,
    Toolbox toolbox,
    MessageHub messageHub)
{
    public void Draw()
    {
        // Must be kept in sync with ToolboxWindow.Draw.

        if (!configuration.Configuration.ShowInDevMenu || !pluginInterface.IsDevMenuOpen) {
            return;
        }

        using var bar = ImRaii.MainMenuBar();
        if (!bar) {
            return;
        }

        using var menu = ImRaii.Menu("Dynamis"u8);
        if (!menu) {
            return;
        }

        toolbox.Draw(static label => ImGui.MenuItem(label), ImGui.Separator, false);

        ImGui.Separator();

        if (ImGui.MenuItem("Toolbox"u8)) {
            messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();
        }

        if (ImGui.MenuItem("Settings"u8)) {
            messageHub.Publish<OpenWindowMessage<SettingsWindow>>();
        }

        if (ImGui.MenuItem("Change Log"u8)) {
            messageHub.Publish<OpenWindowMessage<ChangeLogWindow>>();
        }

        if (configuration.Configuration.ReadChangeLogVersion < ChangeLogWindow.ChangeLogVersion) {
            var drawList = ImGui.GetWindowDrawList();
            var style = ImGui.GetStyle();
            var min = ImGui.GetItemRectMin() + ImGui.CalcTextSize("Change Log"u8) with
            {
                Y = 0.0f,
            };
            var max = ImGui.GetItemRectMax();
            drawList.PushClipRect(min, max);
            try {
                drawList.AddText(
                    min + new Vector2(
                        style.ItemSpacing.X * 0.5f + style.ItemInnerSpacing.X,
                        (max.Y - min.Y - ImGui.CalcTextSize("(NEW!)"u8).Y) * 0.5f
                    ), ImGuiColors.SuccessForeground.ToUInt32(), "(NEW!)"u8
                );
            } finally {
                drawList.PopClipRect();
            }
        }

        ImGui.Separator();
        ImGui.MenuItem($"Version {Assembly.GetExecutingAssembly().GetName().Version}###DynamisVersion", enabled: false);
    }
}
