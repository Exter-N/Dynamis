using System.Reflection;
using System.Text;
using Dynamis.Interop;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using TerraFX.Interop.Windows;

namespace Dynamis.UI.ObjectInspectors;

public sealed partial class DirectXInspector : IDynamicObjectInspector
{
    private readonly Dictionary<Type, IDrawDescWrapper?> _wrappers = [];

    public bool CanInspect(ClassInfo @class)
    {
        var type = @class.ManagedType;
        if (type is null) {
            return false;
        }

        if (!typeof(IUnknown.Interface).IsAssignableFrom(type)) {
            return false;
        }

        if (!_wrappers.TryGetValue(type, out var wrapper)) {
            wrapper = CreateDrawDescWrapper(type);
            _wrappers.Add(type, wrapper);
        }

        return wrapper is not null;
    }

    public void DrawAdditionalTooltipDetails(nint pointer, ClassInfo @class)
    {
        if (@class.ManagedType is null || !_wrappers.TryGetValue(@class.ManagedType, out var wrapper)) {
            return;
        }

        wrapper!.DrawDesc(pointer);
    }

    public void DrawAdditionalHeaderDetails(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
        if (live && snapshot is
            {
                Live: true,
                Address: not null,
                Class: not null,
            }) {
            DrawAdditionalTooltipDetails(snapshot.Address.Value, snapshot.Class);
        }
    }

    public void DrawAdditionalTabs(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
    }

    private IDrawDescWrapper? CreateDrawDescWrapper(Type type)
    {
        var vtblType = type.GetNestedType("Vtbl`1")?.MakeGenericType(type);
        if (vtblType is null) {
            return null;
        }

        var bestLevel = -1;
        var bestVfIndex = -1;
        Type? bestDescType = null;
        MethodInfo? bestDrawMethod = null;

        foreach (var method in type.GetMethods()) {
            int level;
            var name = method.Name;
            if (string.Equals(name, "GetDesc", StringComparison.Ordinal)) {
                level = 0;
            } else if (name.StartsWith("GetDesc", StringComparison.Ordinal)
                    && int.TryParse(name.AsSpan(7), out level)) {
                // This block intentionally left blank.
            } else {
                continue;
            }

            if (level <= bestLevel) {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length is not 1 || !parameters[0].ParameterType.IsPointer) {
                continue;
            }

            var descType = parameters[0].ParameterType.GetElementType();
            if (descType is null) {
                continue;
            }

            var vtblField = vtblType.GetField(method.Name);
            if (vtblField is null) {
                continue;
            }

            var vfIndex = unchecked((int)(vtblField.OffsetOf() / nint.Size));

            var drawMethod = GetType()
               .GetMethod("DrawDesc", BindingFlags.NonPublic | BindingFlags.Instance, [descType.MakeByRefType(),]);
            if (drawMethod is null) {
                continue;
            }

            bestLevel = level;
            bestVfIndex = vfIndex;
            bestDescType = descType;
            bestDrawMethod = drawMethod;
        }

        if (bestLevel < 0 || bestVfIndex < 0 || bestDescType is null || bestDrawMethod is null) {
            return null;
        }

        var drawDelegate = Delegate.CreateDelegate(
            typeof(DrawDescDelegate<>).MakeGenericType(bestDescType), this, bestDrawMethod
        );

        return (IDrawDescWrapper)Activator.CreateInstance(
            typeof(DrawDescWrapper<>).MakeGenericType(bestDescType), bestVfIndex, drawDelegate
        )!;
    }

    private static void AppendList(StringBuilder sb, ReadOnlySpan<uint> list)
    {
        if (list.Length > 0) {
            sb.Append($"0x{list[0]:X}");
            for (var i = 1; i < list.Length; ++i) {
                sb.Append($", 0x{list[i]:X}");
            }
        } else {
            sb.Append("(empty)");
        }
    }

    private static void AppendList<T>(StringBuilder sb, ReadOnlySpan<T> list) where T : Enum
    {
        if (list.Length > 0) {
            sb.Append(list[0].ToShortString());
            for (var i = 1; i < list.Length; ++i) {
                sb.Append(", ");
                sb.Append(list[i].ToShortString());
            }
        } else {
            sb.Append("(empty)");
        }
    }

    private interface IDrawDescWrapper
    {
        void DrawDesc(nint pointer);
    }

    private sealed class DrawDescWrapper<TDesc>(int vfIndex, DrawDescDelegate<TDesc> drawDesc) : IDrawDescWrapper
        where TDesc : unmanaged
    {
        public unsafe void DrawDesc(nint pointer)
        {
            TDesc desc;
            var vtbl = *(void***)pointer;
            ((delegate* unmanaged[MemberFunction]<nint, TDesc*, void>)vtbl[vfIndex])(pointer, &desc);
            drawDesc(in desc);
        }
    }

    private delegate void DrawDescDelegate<TDesc>(in TDesc desc);
}
