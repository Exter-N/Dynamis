#if WITH_SMA
using System.Management.Automation;
using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Get, "IpcFunction")]
[Alias("gipc")]
public class GetIpcFunction : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string? Name { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Type? Type { get; set; }

    protected override void BeginProcessing()
    {
        if (string.IsNullOrEmpty(Name)) {
            return;
        }

        if (!typeof(Delegate).IsAssignableFrom(Type)) {
            throw new PSArgumentException($"{nameof(Type)} must be a delegate type", nameof(Type));
        }

        var invoke = Type.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
        if (invoke is null) {
            throw new PSArgumentException($"{nameof(Type)} must have an Invoke method", nameof(Type));
        }

        var parameters = invoke.GetParameters();

        var pi = CommandRuntime.GetServiceProvider().GetRequiredService<IDalamudPluginInterface>();
        var getIpcSubscriber = pi.GetType()
                                 .GetMethod(
                                      "GetIpcSubscriber",                          parameters.Length + 1,
                                      BindingFlags.Instance | BindingFlags.Public, [typeof(string)]
                                  );
        if (getIpcSubscriber is null) {
            throw new PSInvalidOperationException($"Cannot resolve suitable GetIpcSubscriber overload for {Type}");
        }

        var typeArguments = new Type[parameters.Length + 1];
        for (var i = 0; i < parameters.Length; ++i) {
            typeArguments[i] = parameters[i].ParameterType;
        }

        typeArguments[parameters.Length] = invoke.ReturnType == typeof(void)
            ? typeof(object)
            : invoke.ReturnType;

        var subscriber = (ICallGateSubscriber)getIpcSubscriber.MakeGenericMethod(typeArguments).Invoke(pi, [Name,])!;
        WriteObject(
            Delegate.CreateDelegate(Type, subscriber, invoke.ReturnType == typeof(void) ? "InvokeAction" : "InvokeFunc")
        );
    }
}
#endif
