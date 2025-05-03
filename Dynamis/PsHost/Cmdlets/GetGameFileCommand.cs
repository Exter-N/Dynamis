using System.Management.Automation;
using Dalamud.Plugin.Services;
using Lumina.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Get, "GameFile")]
[Alias("ggf")]
public class GetGameFileCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string? Path { get; set; }

    [Parameter(Position = 1)]
    public Type? AsType { get; set; }

    protected override void BeginProcessing()
    {
        var type = AsType ?? typeof(FileResource);
        if (!typeof(FileResource).IsAssignableFrom(type)) {
            throw new RuntimeException($"Requested type {type} is not a FileResource");
        }

        var dataManager = CommandRuntime.GetServiceProvider().GetRequiredService<IDataManager>();
        var fileResource = typeof(IDataManager).GetMethod(nameof(dataManager.GetFile), 1, [typeof(string),])!
                                               .MakeGenericMethod(type)
                                               .Invoke(dataManager, [Path,]);
        if (fileResource is not null) {
            WriteObject(fileResource);
        }
    }
}
