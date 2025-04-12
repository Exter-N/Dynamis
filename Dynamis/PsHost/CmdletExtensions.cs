using System.Management.Automation;

namespace Dynamis.PsHost;

internal static class CmdletExtensions
{
    public static IServiceProvider GetServiceProvider(this ICommandRuntime runtime)
        => runtime.Host?.PrivateData?.BaseObject as IServiceProvider ??
           throw new InvalidOperationException("Command runtime has no service provider");
}
