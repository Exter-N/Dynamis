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
    ImGuiComponents imGuiComponents)
    : IHostedService
{
    private ICallGateProvider<nint, object?>?                           _inspectObject;
    private ICallGateProvider<nint, uint, string, uint, uint, object?>? _inspectRegion;
    private ICallGateProvider<nint, object?>?                           _imGuiDrawPointer;
    private ICallGateProvider<nint, object?>?                           _imGuiDrawPointerTooltipDetails;

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

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
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
}
