using System.Management.Automation;
using System.Reflection;
using Dynamis.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommon.Get, "ClientStruct")]
public sealed class GetClientStructCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public nint Address { get; set; }

    protected override unsafe void BeginProcessing()
    {
        var objectInspector = CommandRuntime.GetServiceProvider().GetRequiredService<ObjectInspector>();
        var (@class, displacement) = objectInspector.DetermineClassAndDisplacement(Address);

        if (@class.ManagedType is
            {
            } type) {
            WriteObject(Pointer.Box((void*)(Address - (nint)displacement), type.MakePointerType()));
        }
    }
}
