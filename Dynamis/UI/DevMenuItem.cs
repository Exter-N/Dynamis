using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.UI.Windows;

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

        if (!configuration.Configuration.ShowInDevMenu || !pluginInterface.IsDevMenuOpen)
            return;

        using var bar = ImRaii.MainMenuBar();
        if (!bar)
            return;

        using var menu = ImRaii.Menu("Dynamis"u8);
        if (!menu)
            return;

        toolbox.Draw(static label => ImGui.MenuItem(label), ImGui.Separator, false);

        ImGui.Separator();

        if (ImGui.MenuItem("Toolbox"u8)) {
            messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();
        }

        if (ImGui.MenuItem("Settings"u8)) {
            messageHub.Publish<OpenWindowMessage<SettingsWindow>>();
        }

        ImGui.Separator();
        ImGui.MenuItem($"Version {Assembly.GetExecutingAssembly().GetName().Version}###DynamisVersion", enabled: false);
    }
}
