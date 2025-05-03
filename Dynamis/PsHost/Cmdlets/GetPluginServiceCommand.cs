using System.Management.Automation;
using Dalamud.IoC;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Get, "PluginService")]
[Alias("gsv")]
public sealed class GetPluginServiceCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    [ValidateNotNull]
    public Type? Type { get; set; }

    protected override void BeginProcessing()
    {
        if (Type is null) {
            throw new RuntimeException("No service type specified");
        }

        var service = GetService(CommandRuntime.GetServiceProvider(), Type);
        if (service is null) {
            throw new RuntimeException($"No service of type {Type} found");
        }

        WriteObject(service);
    }

    private static object? GetService(IServiceProvider serviceProvider, Type type)
    {
        if (serviceProvider.GetService(type) is
            {
            } service) {
            return service;
        }

        var pi = serviceProvider.GetRequiredService<IDalamudPluginInterface>();
        var wrapper =
            (IDalamudServiceWrapper)Activator.CreateInstance(typeof(DalamudServiceWrapper<>).MakeGenericType(type))!;
        pi.Inject(wrapper);
        return wrapper.Service;
    }

    private interface IDalamudServiceWrapper
    {
        object? Service { get; }
    }

    private sealed class DalamudServiceWrapper<T> : IDalamudServiceWrapper
    {
        [PluginService] public T Service { get; private set; } = default!;

        object? IDalamudServiceWrapper.Service
            => Service;
    }
}
