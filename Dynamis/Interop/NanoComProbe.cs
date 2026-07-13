using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Dynamis.Interop;

public sealed class NanoComProbe
{
    private static readonly Lazy<InterfaceInfo> IUnknownInfo = new(BuildInterfaceTree);

    public static bool CanPopulate(ClassInfo classInfo)
    {
        var module = classInfo.DefiningModule;
        return string.Equals(module, "d3d11.dll", StringComparison.OrdinalIgnoreCase)
            || string.Equals(module, "dxgi.dll",  StringComparison.OrdinalIgnoreCase);
    }

    public unsafe void Populate(ClassInfo classInfo, nint obj)
    {
        var module = classInfo.DefiningModule;
        if (string.Equals(module, "d3d11.dll", StringComparison.OrdinalIgnoreCase) || string.Equals(
                module, "dxgi.dll", StringComparison.OrdinalIgnoreCase
            )) {
            classInfo.ManagedType = FindBestInterface((IUnknown*)obj).Interface;
        }
    }

    private static unsafe InterfaceInfo FindBestInterface(IUnknown* obj)
    {
        var iUnknown = IUnknownInfo.Value;
        return iUnknown.FindBestInterface(obj) ?? iUnknown;
    }

    private static InterfaceInfo BuildInterfaceTree()
    {
        var children = new Dictionary<Type, HashSet<Type>>();
        var entries = new Dictionary<Type, InterfaceInfo>();
        foreach (var t in typeof(IUnknown).Assembly.ExportedTypes) {
            if (!t.IsValueType) {
                continue;
            }

            if (!t.Name.StartsWith("ID3D11", StringComparison.OrdinalIgnoreCase)
             && !t.Name.StartsWith("IDXGI",  StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            AddType(children, entries, t);
        }

        LinkChildren(children, entries);

        var root = entries[typeof(IUnknown)];
        WeightAndSort(root);

        return root;
    }

    private static void AddType(Dictionary<Type, HashSet<Type>> children, Dictionary<Type, InterfaceInfo> entries,
        Type t)
    {
        var parent = GetParent(t);
        if (parent is not null) {
            entries.Add(t, new(t));

            if (!children.TryGetValue(parent, out var siblings)) {
                siblings = [];
                children.Add(parent, siblings);
            }

            siblings.Add(t);
        }
    }

    private static Type? GetParent(Type t)
    {
        var iface = t.GetNestedType("Interface");
        if (iface is null) {
            return null;
        }

        var interfaces = iface.GetDirectInterfaces();
        foreach (var @interface in interfaces) {
            if (@interface.Name is "Interface") {
                return @interface.DeclaringType;
            }
        }

        return null;
    }

    private static void LinkChildren(Dictionary<Type, HashSet<Type>> children, Dictionary<Type, InterfaceInfo> entries)
    {
        foreach (var (parent, childSet) in children) {
            if (!entries.TryGetValue(parent, out var parentEntry)) {
                parentEntry = new(parent);
                entries.Add(parent, parentEntry);
            }

            foreach (var child in childSet) {
                parentEntry.ChildInterfaces.Add(entries[child]);
            }
        }
    }

    private static void WeightAndSort(InterfaceInfo iface)
    {
        iface.Weight = 0;
        foreach (var child in iface.ChildInterfaces) {
            WeightAndSort(child);
            iface.Weight += child.Weight;
        }

        iface.Weight = Math.Max(iface.Weight, 1);

        iface.ChildInterfaces.Sort((x, y) => y.Weight - x.Weight);
    }

    private sealed class InterfaceInfo(Type @interface)
    {
        public readonly string Name      = @interface.Name;
        public readonly Guid   Iid       = @interface.GUID;
        public readonly Type   Interface = @interface;

        public readonly List<InterfaceInfo> ChildInterfaces = [];

        public int Weight = 0;

        public unsafe HRESULT QueryInterface(IUnknown* obj, IUnknown** outObj)
        {
            fixed (Guid* refIid = &Iid) {
                return obj->QueryInterface(refIid, (void**)outObj);
            }
        }

        public unsafe InterfaceInfo? FindBestInterface(IUnknown* obj)
        {
            if (!IsInstance(obj)) {
                return null;
            }

            foreach (var child in ChildInterfaces) {
                var best = child.FindBestInterface(obj);
                if (best is not null) {
                    return best;
                }
            }

            return this;
        }

        public unsafe bool IsInstance(IUnknown* obj)
        {
            IUnknown* obj2;
            if (QueryInterface(obj, &obj2).FAILED) {
                return false;
            }

            obj2->Release();

            return obj2 == obj;
        }
    }
}
