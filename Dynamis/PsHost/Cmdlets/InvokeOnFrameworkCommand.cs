#if WITH_SMA
using System.Management.Automation;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsLifecycle.Invoke, "OnFramework")]
[Alias("ifx")]
public class InvokeOnFrameworkCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public ScriptBlock? ScriptBlock { get; set; }

    protected override void ProcessRecord()
    {
        var block = ScriptBlock;
        if (block is null) {
            return;
        }

        var context = CommandRuntime.GetHostContext();
        if (!context.TryGetRunspacePool(out var runspacePool)) {
            throw new PSInvalidOperationException("Could not retrieve runspace pool");
        }

        var framework = context.ServiceProvider.GetRequiredService<IFramework>();
        if (framework.IsInFrameworkUpdateThread) {
            throw new PSNotSupportedException("Cannot call Invoke-OnFramework from within the framework thread itself");
        }

        var invocation = framework.RunOnFrameworkThread(() =>
            {
                using var _ = BootHelper.Use(runspacePool);
                return block.Invoke();
            }
        );

        foreach (var item in invocation.Result) {
            WriteObject(item);
        }
    }
}
#endif
