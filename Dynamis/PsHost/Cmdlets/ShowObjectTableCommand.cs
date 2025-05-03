using System.Management.Automation;
using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Show, "ObjectTable")]
[Alias("shot")]
public class ShowObjectTableCommand : Cmdlet
{
    protected override void BeginProcessing()
    {
        var messageHub = CommandRuntime.GetServiceProvider().GetRequiredService<MessageHub>();
        messageHub.PublishOnFrameworkThread<OpenWindowMessage<ObjectTableWindow>>();
    }
}
