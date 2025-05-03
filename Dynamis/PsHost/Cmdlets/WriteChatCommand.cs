using System.Management.Automation;
using Dalamud.Plugin.Services;
using Dynamis.UI.PsHost.Output;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommunications.Write, "Chat")]
[Alias("wrc")]
public sealed class WriteChatCommand : Cmdlet
{
    private IChatGui? _chatGui;

    [Parameter(Position = 0, ValueFromPipeline = true)]
    public object? Object { get; set; }

    protected override void BeginProcessing()
    {
        _chatGui = CommandRuntime.GetServiceProvider().GetRequiredService<IChatGui>();
    }

    protected override void ProcessRecord()
    {
        if (_chatGui is null) {
            throw new RuntimeException("Chat GUI not initialized");
        }

        var message = Object?.ToString();
        if (!string.IsNullOrEmpty(message)) {
            _chatGui.Print(AnsiHelper.AnsiCodesToSeString(message));
        }
    }
}
