using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Runspaces;

namespace Dynamis.PsHost;

public record HostContext(IServiceProvider ServiceProvider)
{
    public WeakReference<RunspacePool>? RunspacePool { get; set; }

    public bool TryGetRunspacePool([MaybeNullWhen(false)] out RunspacePool runspacePool)
    {
        if (RunspacePool is
            {
            } pool) {
            return pool.TryGetTarget(out runspacePool);
        }

        runspacePool = null;
        return false;
    }
}
