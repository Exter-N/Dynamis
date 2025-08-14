using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
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
    public const uint  ApiMajorVersion = 1;
    public const uint  ApiMinorVersion = 6;
    public const ulong ApiFeatureFlags = SmaApiFeatureFlag;

#if WITH_SMA
    private const ulong SmaApiFeatureFlag = 1;
#else
    private const ulong SmaApiFeatureFlag = 0;
#endif

    private ICallGateProvider<uint, uint, ulong, Version, object?>?                    _apiInitialized;
    private ICallGateProvider<object?>?                                                _apiDisposing;
    private ICallGateProvider<(uint, uint, ulong)>?                                    _getApiVersion;
    private ICallGateProvider<nint, object?>?                                          _inspectObjectV1;
    private ICallGateProvider<nint, string?, object?>?                                 _inspectObjectV2;
    private ICallGateProvider<nint, uint, string, uint, uint, object?>?                _inspectRegionV1;
    private ICallGateProvider<nint, uint, string, uint, uint, string?, object?>?       _inspectRegionV2;
    private ICallGateProvider<nint, object?>?                                          _imGuiDrawPointerV1;
    private ICallGateProvider<nint, Func<string?>?, object?>?                          _imGuiDrawPointerV2;
    private ICallGateProvider<nint, Func<string?>?, string?, ulong, Vector2, object?>? _imGuiDrawPointerV3;
    private ICallGateProvider<Action<nint>>?                                           _getImGuiDrawPointerDelegateV1;
    private ICallGateProvider<Action<nint, Func<string?>?>>?                           _getImGuiDrawPointerDelegateV2;
    private ICallGateProvider<Action<nint, Func<string?>?, string?, ulong, Vector2>>?  _getImGuiDrawPointerDelegateV3;
    private ICallGateProvider<nint, object?>?                                          _imGuiDrawPointerTooltipDetails;
    private ICallGateProvider<nint, Func<string?>?, object?>?                          _imGuiOpenPointerContextMenu;
    private ICallGateProvider<nint, (string, Type?, uint, uint)>?                      _getClass;
    private ICallGateProvider<nint, string?, Type?, (bool, uint)>?                     _isInstanceOf;
    private ICallGateProvider<object?>?                                                _preloadDataYaml;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RegisterEvent(out _apiInitialized, "Dynamis.ApiInitialized");
        RegisterEvent(out _apiDisposing,   "Dynamis.ApiDisposing");

        RegisterFunc(
            out _getApiVersion, "Dynamis.GetApiVersion", () => (ApiMajorVersion, ApiMinorVersion, ApiFeatureFlags)
        );

        RegisterAction(out _inspectObjectV1,    "Dynamis.InspectObject.V1",    InspectObjectV1);
        RegisterAction(out _inspectObjectV2,    "Dynamis.InspectObject.V2",    InspectObjectV2);
        RegisterAction(out _inspectRegionV1,    "Dynamis.InspectRegion.V1",    InspectRegionV1);
        RegisterAction(out _inspectRegionV2,    "Dynamis.InspectRegion.V2",    InspectRegionV2);
        RegisterAction(out _imGuiDrawPointerV1, "Dynamis.ImGuiDrawPointer.V1", ImGuiDrawPointerV1);
        RegisterAction(out _imGuiDrawPointerV2, "Dynamis.ImGuiDrawPointer.V2", ImGuiDrawPointerV2);
        RegisterAction(out _imGuiDrawPointerV3, "Dynamis.ImGuiDrawPointer.V3", ImGuiDrawPointerV3);

        RegisterFunc(
            out _getImGuiDrawPointerDelegateV1, $"Dynamis.GetImGuiDrawPointerDelegate.V1",
            () => ImGuiDrawPointerV1
        );
        RegisterFunc(
            out _getImGuiDrawPointerDelegateV2, $"Dynamis.GetImGuiDrawPointerDelegate.V2",
            () => ImGuiDrawPointerV2
        );
        RegisterFunc(
            out _getImGuiDrawPointerDelegateV3, $"Dynamis.GetImGuiDrawPointerDelegate.V3",
            () => ImGuiDrawPointerV3
        );

        RegisterAction(
            out _imGuiDrawPointerTooltipDetails, $"Dynamis.{nameof(ImGuiDrawPointerTooltipDetails)}.V1",
            ImGuiDrawPointerTooltipDetails
        );
        RegisterAction(
            out _imGuiOpenPointerContextMenu, $"Dynamis.{nameof(ImGuiOpenPointerContextMenu)}.V1",
            ImGuiOpenPointerContextMenu
        );

        RegisterFunc(out _getClass,     $"Dynamis.{nameof(GetClass)}.V1",     GetClass);
        RegisterFunc(out _isInstanceOf, $"Dynamis.{nameof(IsInstanceOf)}.V1", IsInstanceOf);

        RegisterAction(out _preloadDataYaml, $"Dynamis.{nameof(PreloadDataYaml)}.V1", PreloadDataYaml);

        try {
            _apiInitialized?.SendMessage(
                ApiMajorVersion, ApiMinorVersion, ApiFeatureFlags,
                typeof(IpcProvider).Assembly.GetName().Version ?? new()
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

        UnregisterAction(ref _imGuiOpenPointerContextMenu);
        UnregisterAction(ref _imGuiDrawPointerTooltipDetails);

        UnregisterFunc(ref _getImGuiDrawPointerDelegateV3);
        UnregisterFunc(ref _getImGuiDrawPointerDelegateV2);
        UnregisterFunc(ref _getImGuiDrawPointerDelegateV1);

        UnregisterAction(ref _imGuiDrawPointerV3);
        UnregisterAction(ref _imGuiDrawPointerV2);
        UnregisterAction(ref _imGuiDrawPointerV1);
        UnregisterAction(ref _inspectRegionV2);
        UnregisterAction(ref _inspectRegionV1);
        UnregisterAction(ref _inspectObjectV2);
        UnregisterAction(ref _inspectObjectV1);

        UnregisterFunc(ref _getApiVersion);

        UnregisterEvent(out _apiDisposing);
        UnregisterEvent(out _apiInitialized);

        return Task.CompletedTask;
    }

    private void InspectObjectV1(nint address)
        => InspectObjectV2(address, null);

    private void InspectObjectV2(nint address, string? name)
        => messageHub.PublishOnFrameworkThread(new InspectObjectMessage(address, null, null, name));

    private void InspectRegionV1(nint address, uint size, string typeName, uint typeTemplateId, uint classKindId)
        => InspectRegionV2(address, size, typeName, typeTemplateId, classKindId, null);

    private void InspectRegionV2(nint address, uint size, string typeName, uint typeTemplateId, uint classKindId,
        string? name)
        => messageHub.PublishOnFrameworkThread(
            new InspectObjectMessage(
                address,
                PseudoClasses.Generate(typeName, size, (PseudoClasses.Template)typeTemplateId, (ClassKind)classKindId),
                null, name
            )
        );

    private void ImGuiDrawPointerV1(nint pointer)
        => imGuiComponents.DrawPointer(pointer, null, null);

    private void ImGuiDrawPointerV2(nint pointer, Func<string?>? name)
        => imGuiComponents.DrawPointer(pointer, null, name);

    private void ImGuiDrawPointerV3(nint pointer, Func<string?>? name, string? customText, ulong flags, Vector2 size)
        => imGuiComponents.DrawPointer(
            pointer, null, name, customText, (ImGuiComponents.DrawPointerFlags)unchecked((uint)flags),
            (ImGuiSelectableFlags)(uint)(flags >> 32), size
        );

    private void ImGuiDrawPointerTooltipDetails(nint pointer)
        => imGuiComponents.DrawPointerTooltipDetails(pointer, null);

    private void ImGuiOpenPointerContextMenu(nint pointer, Func<string?>? name)
        => imGuiComponents.OpenPointerContextMenu(pointer, null, name);

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

    private void RegisterEvent<T1, T2, T3, T4>(out ICallGateProvider<T1, T2, T3, T4, object?>? provider, string name)
    {
        try {
            provider = pi.GetIpcProvider<T1, T2, T3, T4, object?>(name);
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

    private void RegisterAction<T1, T2>(out ICallGateProvider<T1, T2, object?>? provider, string name,
        Action<T1, T2> action)
    {
        try {
            var prov = pi.GetIpcProvider<T1, T2, object?>(name);
            prov.RegisterAction(action);
            provider = prov;
        } catch (Exception e) {
            provider = null;
            logger.LogError(e, "Error while registering IPC provider for {Name}", name);
        }
    }

    private void RegisterAction<T1, T2, T3, T4, T5>(out ICallGateProvider<T1, T2, T3, T4, T5, object?>? provider,
        string name, Action<T1, T2, T3, T4, T5> action)
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

    private void RegisterAction<T1, T2, T3, T4, T5, T6>(
        out ICallGateProvider<T1, T2, T3, T4, T5, T6, object?>? provider, string name,
        Action<T1, T2, T3, T4, T5, T6> action)
    {
        try {
            var prov = pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, object?>(name);
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

    private void RegisterFunc<T1, T2, T3, TRet>(out ICallGateProvider<T1, T2, T3, TRet>? provider, string name,
        Func<T1, T2, T3, TRet> func)
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
