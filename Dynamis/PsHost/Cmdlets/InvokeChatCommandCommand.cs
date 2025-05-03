using System.Management.Automation;
using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsLifecycle.Invoke, "ChatCommand")]
[Alias("icc")]
public class InvokeChatCommandCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string? Command { get; set; }

    protected override void BeginProcessing()
    {
        var command = Command;
        if (string.IsNullOrEmpty(command)) {
            return;
        }

        var serviceProvider = CommandRuntime.GetServiceProvider();
        var framework = serviceProvider.GetRequiredService<IFramework>();
        var commandManager = serviceProvider.GetRequiredService<ICommandManager>();
        framework.Run(() => commandManager.ProcessCommand(command));
    }
}
