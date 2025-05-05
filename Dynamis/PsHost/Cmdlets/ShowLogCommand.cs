#if WITH_SMA
using System.Management.Automation;
using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Show, "Log")]
[Alias("shlog", "shl")]
public class ShowLogCommand : Cmdlet
{
    protected override void BeginProcessing()
    {
        var messageHub = CommandRuntime.GetServiceProvider().GetRequiredService<MessageHub>();
        messageHub.PublishOnFrameworkThread<OpenDalamudConsoleMessage>();
    }
}
#endif
