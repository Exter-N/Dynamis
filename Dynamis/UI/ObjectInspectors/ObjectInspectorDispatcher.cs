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
    private readonly Dictionary<Type, Proxy>            _proxies = new();

    public ObjectInspectorDispatcher(ILogger<ObjectInspectorDispatcher> logger, IEnumerable<IObjectInspector> inspectors)
    {
        foreach (var inspector in inspectors) {
            foreach (var @interface in inspector.GetType().GetInterfaces()) {
                if (@interface.IsConstructedGenericType && @interface.GetGenericTypeDefinition() == typeof(IObjectInspector<>)) {
                    var type = @interface.GetGenericArguments()[0];
                    _proxies.Add(type, (Proxy)Activator.CreateInstance(typeof(Proxy<>).MakeGenericType(type), logger, inspector)!);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Proxy? GetInspector(Type type)
        => _proxies.GetValueOrDefault(type);

    public IEnumerable<Proxy> GetInspectors(ClassInfo @class)
    {
        Proxy? proxy;
        for (var i = @class.ClientStructsParents.Length; i-- > 0;) {
            proxy = GetInspector(@class.ClientStructsParents[i]);
            if (proxy is not null) {
                yield return proxy;
            }
        }

        if (@class.ClientStructsType is null) {
            yield break;
        }

        proxy = GetInspector(@class.ClientStructsType);
        if (proxy is not null) {
            yield return proxy;
        }
    }

    public abstract class Proxy
    {
        public abstract void DrawAdditionalTooltipDetails(nint pointer);

        public abstract void DrawAdditionalHeaderDetails(nint pointer, ObjectInspectorWindow window);

        public abstract void DrawAdditionalTabs(nint pointer, ObjectInspectorWindow window);
    }

    private sealed unsafe class Proxy<T>(ILogger<ObjectInspectorDispatcher> logger, IObjectInspector<T> inspector) : Proxy where T : unmanaged
    {
        public override void DrawAdditionalTooltipDetails(nint pointer)
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

        public override void DrawAdditionalHeaderDetails(nint pointer, ObjectInspectorWindow window)
        {
            try {
                inspector.DrawAdditionalHeaderDetails((T*)pointer, window);
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

        public override void DrawAdditionalTabs(nint pointer, ObjectInspectorWindow window)
        {
            try {
                inspector.DrawAdditionalTabs((T*)pointer, window);
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
