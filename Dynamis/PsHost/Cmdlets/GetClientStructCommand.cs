using System.Management.Automation;
using System.Reflection;
using Dynamis.ClientStructs;
using Dynamis.Interop;
using Dynamis.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Get, "ClientStruct")]
[Alias("gs")]
public sealed class GetClientStructCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByAddress")]
    public object? Address { get; set; }

    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByType")]
    public Type? Type { get; set; }

    [Parameter(Position = 1, ParameterSetName = "ByType")]
    public string? Name { get; set; }

    [Parameter]
    public BoxAccess Access { get; set; } = BoxAccess.Mutable;

    protected override void BeginProcessing()
    {
        var factory = CommandRuntime.GetServiceProvider().GetRequiredService<DynamicBoxFactory>();
        WriteObject(factory.BoxStruct(GetAddress(), Access));
    }

    private nint GetAddress()
    {
        if (Type is null) {
            return CastAddress(Address is PSObject psObject ? psObject.BaseObject : Address);
        }

        var getInstance = Type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public);
        if (getInstance is not null) {
            return CastAddress(getInstance.Invoke(null, null));
        }

        if (ClassRegistry.TryGetClientStructsClassName(Type, out var className)) {
            var dataYaml = CommandRuntime.GetServiceProvider().GetRequiredService<DataYamlContainer>();
            if (dataYaml.Data?.Classes?.TryGetValue(className, out var @class) ?? false) {
                foreach (var instance in @class.Instances ?? []) {
                    if (string.IsNullOrEmpty(Name) || string.Equals(Name, instance.Name, StringComparison.Ordinal)) {
                        return dataYaml.Resolve(instance);
                    }
                }
            }
        }

        throw new RuntimeException("Cannot resolve client struct address from given parameters");
    }

    public static nint CastAddress(object? value)
        => IBoxedAddress.TryUnbox(value, out var address) ? address : ConvertEx.ToIntPtr(value);
}
