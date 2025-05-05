#if WITH_SMA
using System.Management.Automation;

namespace Dynamis.PsHost;

internal static class CmdletExtensions
{
    public static HostContext GetHostContext(this ICommandRuntime runtime)
        => runtime.Host?.PrivateData?.BaseObject as HostContext ??
           throw new InvalidOperationException("Command runtime has no host context.");

    public static IServiceProvider GetServiceProvider(this ICommandRuntime runtime)
        => runtime.GetHostContext().ServiceProvider;
}
#endif
