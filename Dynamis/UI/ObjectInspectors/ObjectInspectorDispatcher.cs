using System.Runtime.CompilerServices;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Interop;
using Dynamis.UI.Windows;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.ObjectInspectors;

public sealed class ObjectInspectorDispatcher
{
    private readonly Dictionary<Type, IDynamicObjectInspector> _typedInspectors   = new();
    private readonly List<IDynamicObjectInspector>             _dynamicInspectors = [];

    public ObjectInspectorDispatcher(ILogger<ObjectInspectorDispatcher> logger, IEnumerable<IObjectInspector> inspectors)
    {
        foreach (var inspector in inspectors) {
            foreach (var @interface in inspector.GetType().GetInterfaces()) {
                if (@interface.IsConstructedGenericType && @interface.GetGenericTypeDefinition() == typeof(IObjectInspector<>)) {
                    var type = @interface.GetGenericArguments()[0];
                    _typedInspectors.Add(type, (IDynamicObjectInspector)Activator.CreateInstance(typeof(TypedInspectorProxy<>).MakeGenericType(type), logger, inspector)!);
                }
            }

            if (inspector is IDynamicObjectInspector dynamicInspector) {
                _dynamicInspectors.Add(dynamicInspector);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDynamicObjectInspector? GetTypedInspector(Type type)
        => _typedInspectors.GetValueOrDefault(type);

    public IEnumerable<IDynamicObjectInspector> GetInspectors(ClassInfo @class)
    {
        IDynamicObjectInspector? proxy;
        for (var i = @class.ManagedParents.Length; i-- > 0;) {
            proxy = GetTypedInspector(@class.ManagedParents[i]);
            if (proxy is not null) {
                yield return proxy;
            }
        }

        if (@class.ManagedType is Type type) {
            proxy = GetTypedInspector(type);
            if (proxy is not null) {
                yield return proxy;
            }
        }

        foreach (var inspector in _dynamicInspectors) {
            if (inspector.CanInspect(@class)) {
                yield return inspector;
            }
        }
    }

    private sealed unsafe class TypedInspectorProxy<T>(ILogger logger, IObjectInspector<T> inspector) : IDynamicObjectInspector where T : unmanaged
    {
        public bool CanInspect(ClassInfo @class)
        {
            if (@class.ManagedType is Type type && type.IsAssignableFrom(typeof(T))) {
                return true;
            }

            foreach (var parent in @class.ManagedParents) {
                if (parent.IsAssignableFrom(typeof(T))) {
                    return true;
                }
            }

            return false;
        }

        public void DrawAdditionalTooltipDetails(nint pointer, ClassInfo @class)
        {
            try {
                inspector.DrawAdditionalTooltipDetails((T*)pointer);
            } catch (Exception e) {
                logger.LogError(
                    e,
                    $"Exception caught in {{InspectorType}}.{nameof(IObjectInspector<T>.DrawAdditionalTooltipDetails)}",
                    inspector.GetType()
                );
                using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                    ImGui.TextUnformatted($"Error in {typeof(T).Name}");
                }
            }
        }

        public void DrawAdditionalHeaderDetails(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
        {
            try {
                if (live && snapshot is
                    {
                        Live: true,
                        Address: not null,
                    }) {
                    inspector.DrawAdditionalHeaderDetails((T*)snapshot.Address, snapshot, true, window);
                } else {
                    fixed (byte* pointer = snapshot.Data) {
                        inspector.DrawAdditionalHeaderDetails((T*)pointer, snapshot, false, window);
                    }
                }
            } catch (Exception e) {
                logger.LogError(
                    e,
                    $"Exception caught in {{InspectorType}}.{nameof(IObjectInspector<T>.DrawAdditionalHeaderDetails)}",
                    inspector.GetType()
                );
                using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                    ImGui.TextUnformatted($"Error in {typeof(T).Name}");
                }
            }
        }

        public void DrawAdditionalTabs(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
        {
            try {
                if (live && snapshot is
                    {
                        Live: true,
                        Address: not null,
                    }) {
                    inspector.DrawAdditionalTabs((T*)snapshot.Address, snapshot, true, window);
                } else {
                    fixed (byte* pointer = snapshot.Data) {
                        inspector.DrawAdditionalTabs((T*)pointer, snapshot, false, window);
                    }
                }
            } catch (Exception e) {
                logger.LogError(
                    e,
                    $"Exception caught in {{InspectorType}}.{nameof(IObjectInspector<T>.DrawAdditionalTabs)}",
                    inspector.GetType()
                );
                using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                    using var _ = ImRaii.TabItem($"Error in {typeof(T).Name}");
                    // This block intentionally left "blank".
                }
            }
        }
    }
}
