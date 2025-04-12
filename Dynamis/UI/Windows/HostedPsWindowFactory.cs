using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dynamis.Messaging;
using Dynamis.PsHost;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public class HostedPsWindowFactory(
    ILogger<HostedPsWindowFactory> logger,
    WindowSystem windowSystem,
    BootHelper bootHelper,
    IDalamudPluginInterface pi,
    IServiceProvider serviceProvider,
    ImGuiComponents imGuiComponents)
    : WindowFactory<HostedPsWindow>(windowSystem), IMessageObserver<CommandMessage>
{
    protected override HostedPsWindow DoCreateWindow()
        => new(logger, WindowSystem, bootHelper, pi, serviceProvider, imGuiComponents, GetFreeIndex());

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand("powershell", "pwrsh", "pwsh", "ps", "shell", "sh")) {
            return;
        }

        message.SetHandled();
        CreateWindow();
    }
}
