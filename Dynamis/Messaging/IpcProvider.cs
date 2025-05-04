using System.Runtime.CompilerServices;
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
    public const uint ApiMajorVersion = 1;
    public const uint ApiMinorVersion = 3;

    private ICallGateProvider<uint, uint, Version, object?>?            _apiInitialized;
    private ICallGateProvider<object?>?                                 _apiDisposing;
    private ICallGateProvider<(uint, uint)>?                            _getApiVersion;
    private ICallGateProvider<nint, object?>?                           _inspectObject;
    private ICallGateProvider<nint, uint, string, uint, uint, object?>? _inspectRegion;
    private ICallGateProvider<nint, object?>?                           _imGuiDrawPointer;
    private ICallGateProvider<Action<nint>>?                            _getImGuiDrawPointerDelegate;
    private ICallGateProvider<nint, object?>?                           _imGuiDrawPointerTooltipDetails;
    private ICallGateProvider<nint, (string, Type?, uint, uint)>?       _getClass;
    private ICallGateProvider<nint, string?, Type?, (bool, uint)>?      _isInstanceOf;
    private ICallGateProvider<object?>?                                 _preloadDataYaml;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RegisterEvent(out _apiInitialized, "Dynamis.ApiInitialized");
        RegisterEvent(out _apiDisposing,   "Dynamis.ApiDisposing");

        RegisterFunc(out _getApiVersion, "Dynamis.GetApiVersion", () => (ApiMajorVersion, ApiMinorVersion));

        RegisterAction(out _inspectObject,    $"Dynamis.{nameof(InspectObject)}.V1",    InspectObject);
        RegisterAction(out _inspectRegion,    $"Dynamis.{nameof(InspectRegion)}.V1",    InspectRegion);
        RegisterAction(out _imGuiDrawPointer, $"Dynamis.{nameof(ImGuiDrawPointer)}.V1", ImGuiDrawPointer);

        RegisterFunc(
            out _getImGuiDrawPointerDelegate, $"Dynamis.Get{nameof(ImGuiDrawPointer)}Delegate.V1",
            () => ImGuiDrawPointer
        );

        RegisterAction(
            out _imGuiDrawPointerTooltipDetails, $"Dynamis.{nameof(ImGuiDrawPointerTooltipDetails)}.V1",
            ImGuiDrawPointerTooltipDetails
        );

        RegisterFunc(out _getClass,     $"Dynamis.{nameof(GetClass)}.V1",     GetClass);
        RegisterFunc(out _isInstanceOf, $"Dynamis.{nameof(IsInstanceOf)}.V1", IsInstanceOf);

        RegisterAction(out _preloadDataYaml, $"Dynamis.{nameof(PreloadDataYaml)}.V1", PreloadDataYaml);

        try {
            _apiInitialized?.SendMessage(
                ApiMajorVersion, ApiMinorVersion, typeof(IpcProvider).Assembly.GetName().Version ?? new()
            );
        } catch (Exception e) {
            logger.LogError(e, "Error while firing API initialized event");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try {
            _apiDisposing?.SendMessage();
        } catch (Exception e) {
            logger.LogError(e, "Error while firing API disposing event");
        }

        UnregisterAction(ref _preloadDataYaml);

        UnregisterFunc(ref _isInstanceOf);
        UnregisterFunc(ref _getClass);

        UnregisterAction(ref _imGuiDrawPointerTooltipDetails);

        UnregisterFunc(ref _getImGuiDrawPointerDelegate);

        UnregisterAction(ref _imGuiDrawPointer);
        UnregisterAction(ref _inspectRegion);
        UnregisterAction(ref _inspectObject);

        UnregisterFunc(ref _getApiVersion);

        UnregisterEvent(out _apiDisposing);
        UnregisterEvent(out _apiInitialized);

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
        return (@class.Name, @class.BestManagedType, @class.EstimatedSize, (uint)displacement);
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

    private void PreloadDataYaml()
        => messageHub.Publish<DataYamlPreloadMessage>();

    #region Register helpers

    private void RegisterEvent(out ICallGateProvider<object?>? provider, string name)
    {
        try {
            provider = pi.GetIpcProvider<object?>(name);
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterEvent<T1, T2, T3>(out ICallGateProvider<T1, T2, T3, object?>? provider, string name)
    {
        try {
            provider = pi.GetIpcProvider<T1, T2, T3, object?>(name);
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterAction(out ICallGateProvider<object?>? provider, string name, Action action)
    {
        try {
            var prov = pi.GetIpcProvider<object?>(name);
            prov.RegisterAction(action);
            provider = prov;
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterAction<T1>(out ICallGateProvider<T1, object?>? provider, string name, Action<T1> action)
    {
        try {
            var prov = pi.GetIpcProvider<T1, object?>(name);
            prov.RegisterAction(action);
            provider = prov;
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterAction<T1, T2, T3, T4, T5>(out ICallGateProvider<T1, T2, T3, T4, T5, object?>? provider, string name, Action<T1, T2, T3, T4, T5> action)
    {
        try {
            var prov = pi.GetIpcProvider<T1, T2, T3, T4, T5, object?>(name);
            prov.RegisterAction(action);
            provider = prov;
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterFunc<TRet>(out ICallGateProvider<TRet>? provider, string name, Func<TRet> func)
    {
        try {
            var prov = pi.GetIpcProvider<TRet>(name);
            prov.RegisterFunc(func);
            provider = prov;
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterFunc<T1, TRet>(out ICallGateProvider<T1, TRet>? provider, string name, Func<T1, TRet> func)
    {
        try {
            var prov = pi.GetIpcProvider<T1, TRet>(name);
            prov.RegisterFunc(func);
            provider = prov;
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterFunc<T1, T2, T3, TRet>(out ICallGateProvider<T1, T2, T3, TRet>? provider, string name, Func<T1, T2, T3, TRet> func)
    {
        try {
            var prov = pi.GetIpcProvider<T1, T2, T3, TRet>(name);
            prov.RegisterFunc(func);
            provider = prov;
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnregisterEvent<T>(out T? provider) where T : class, ICallGateProvider
    {
        provider = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnregisterAction<T>(ref T? provider) where T : class, ICallGateProvider
    {
        provider?.UnregisterAction();
        provider = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnregisterFunc<T>(ref T? provider) where T : class, ICallGateProvider
    {
        provider?.UnregisterFunc();
        provider = null;
    }

    #endregion
}
