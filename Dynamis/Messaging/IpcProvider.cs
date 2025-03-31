using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dynamis.Interop;
using Dynamis.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dynamis.Messaging;

public sealed class IpcProvider(
    IDalamudPluginInterface pi,
    ILogger<IpcProvider> logger,
    MessageHub messageHub,
    ImGuiComponents imGuiComponents,
    ObjectInspector objectInspector)
    : IHostedService
{
    private ICallGateProvider<nint, object?>?                           _inspectObject;
    private ICallGateProvider<nint, uint, string, uint, uint, object?>? _inspectRegion;
    private ICallGateProvider<nint, object?>?                           _imGuiDrawPointer;
    private ICallGateProvider<nint, object?>?                           _imGuiDrawPointerTooltipDetails;
    private ICallGateProvider<nint, (string, Type?, uint, uint)>?       _getClass;
    private ICallGateProvider<nint, string?, Type?, (bool, uint)>?      _isInstanceOf;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try {
            _inspectObject = pi.GetIpcProvider<nint, object?>($"Dynamis.{nameof(InspectObject)}.V1");
            _inspectObject.RegisterAction(InspectObject);
        } catch (Exception e) {
            _inspectObject = null;
            logger.LogError(e, $"Error while registering IPC provider for {nameof(InspectObject)}");
        }

        try {
            _inspectRegion =
                pi.GetIpcProvider<nint, uint, string, uint, uint, object?>($"Dynamis.{nameof(InspectRegion)}.V1");
            _inspectRegion.RegisterAction(InspectRegion);
        } catch (Exception e) {
            _inspectRegion = null;
            logger.LogError(e, $"Error while registering IPC provider for {nameof(InspectRegion)}");
        }

        try {
            _imGuiDrawPointer = pi.GetIpcProvider<nint, object?>($"Dynamis.{nameof(ImGuiDrawPointer)}.V1");
            _imGuiDrawPointer.RegisterAction(ImGuiDrawPointer);
        } catch (Exception e) {
            _imGuiDrawPointer = null;
            logger.LogError(e, $"Error while registering IPC provider for {nameof(ImGuiDrawPointer)}");
        }

        try {
            _imGuiDrawPointerTooltipDetails =
                pi.GetIpcProvider<nint, object?>($"Dynamis.{nameof(ImGuiDrawPointerTooltipDetails)}.V1");
            _imGuiDrawPointerTooltipDetails.RegisterAction(ImGuiDrawPointerTooltipDetails);
        } catch (Exception e) {
            _imGuiDrawPointerTooltipDetails = null;
            logger.LogError(e, $"Error while registering IPC provider for {nameof(ImGuiDrawPointerTooltipDetails)}");
        }

        try {
            _getClass = pi.GetIpcProvider<nint, (string, Type?, uint, uint)>($"Dynamis.{nameof(GetClass)}.V1");
            _getClass.RegisterFunc(GetClass);
        } catch (Exception e) {
            _getClass = null;
            logger.LogError(e, $"Error while registering IPC provider for {nameof(GetClass)}");
        }

        try {
            _isInstanceOf = pi.GetIpcProvider<nint, string?, Type?, (bool, uint)>($"Dynamis.{nameof(IsInstanceOf)}.V1");
            _isInstanceOf.RegisterFunc(IsInstanceOf);
        } catch (Exception e) {
            _isInstanceOf = null;
            logger.LogError(e, $"Error while registering IPC provider for {nameof(IsInstanceOf)}");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _isInstanceOf?.UnregisterFunc();
        _isInstanceOf = null;

        _getClass?.UnregisterFunc();
        _getClass = null;

        _imGuiDrawPointerTooltipDetails?.UnregisterAction();
        _imGuiDrawPointerTooltipDetails = null;

        _imGuiDrawPointer?.UnregisterAction();
        _imGuiDrawPointer = null;

        _inspectRegion?.UnregisterAction();
        _inspectRegion = null;

        _inspectObject?.UnregisterAction();
        _inspectObject = null;

        return Task.CompletedTask;
    }

    private void InspectObject(nint address)
        => messageHub.Publish(new InspectObjectMessage(address, null));

    private void InspectRegion(nint address, uint size, string typeName, uint typeTemplateId, uint classKindId)
        => messageHub.Publish(
            new InspectObjectMessage(
                address, PseudoClasses.Generate(typeName, size, (PseudoClasses.Template)typeTemplateId, (ClassKind)classKindId)
            )
        );

    private void ImGuiDrawPointer(nint pointer)
        => imGuiComponents.DrawPointer(pointer, null);

    private void ImGuiDrawPointerTooltipDetails(nint pointer)
        => imGuiComponents.DrawPointerTooltipDetails(pointer, null);

    private (string, Type?, uint, uint) GetClass(nint pointer)
    {
        var (@class, displacement) = objectInspector.DetermineClassAndDisplacement(pointer);
        return (@class.Name, @class.ManagedType ?? (@class.ManagedParents.Length > 0 ? @class.ManagedParents[0] : null),
            @class.EstimatedSize, (uint)displacement);
    }

    private (bool, uint) IsInstanceOf(nint pointer, string? className, Type? type)
    {
        if (className is not null) {
            if (type is not null) {
                throw new ArgumentException(
                    $"Either {nameof(className)} or {type} must be non-null, and the other must be null"
                );
            }

            return IsInstanceOf(pointer, className);
        } else {
            if (type is null) {
                throw new ArgumentException(
                    $"Either {nameof(className)} or {type} must be non-null, and the other must be null"
                );
            }

            return IsInstanceOf(pointer, type);
        }
    }

    private (bool, uint) IsInstanceOf(nint pointer, string className)
    {
        var (@class, displacement) = objectInspector.DetermineClassAndDisplacement(pointer);
        if (string.Equals(@class.Name, className, StringComparison.OrdinalIgnoreCase)) {
            return (true, (uint)displacement);
        }

        foreach (var parent in @class.DataYamlParents) {
            if (string.Equals(parent.Name, className, StringComparison.OrdinalIgnoreCase)) {
                return (true, (uint)displacement);
            }
        }

        return (false, 0);
    }

    private (bool, uint) IsInstanceOf(nint pointer, Type type)
    {
        var (@class, displacement) = objectInspector.DetermineClassAndDisplacement(pointer);
        if (@class.ManagedType is not null && type.IsAssignableFrom(@class.ManagedType)) {
            return (true, (uint)displacement);
        }

        foreach (var parent in @class.ManagedParents) {
            if (type.IsAssignableFrom(parent)) {
                return (true, (uint)displacement);
            }
        }

        return (false, 0);
    }
}
