#if WITH_SMA
using System.Management.Automation;
using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Show, "Object")]
[Alias("shobj", "sho")]
public class ShowObjectCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public object? Address { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public string? Name { get; set; }

    protected override void BeginProcessing()
    {
        var message = new InspectObjectMessage(
            GetClientStructCommand.CastAddress(Address is PSObject psObject ? psObject.BaseObject : Address), null,
            null, Name
        );
        var messageHub = CommandRuntime.GetServiceProvider().GetRequiredService<MessageHub>();
        messageHub.PublishOnFrameworkThread(message);
    }
}
#endif
