using System.Runtime.CompilerServices;
using Dynamis.ClientStructs;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using FFXIVClientStructs.STD;

namespace Dynamis.Interop;

public sealed class ObjectInspector : IMessageObserver<ConfigurationChangedMessage>
{
    private readonly DataYamlContainer _dataYamlContainer;
    private readonly MemoryHeuristics  _memoryHeuristics;

    private readonly Dictionary<string, ClassInfo> _classCache = new();

    public ObjectInspector(DataYamlContainer dataYamlContainer, MemoryHeuristics memoryHeuristics)
    {
        _dataYamlContainer = dataYamlContainer;
        _memoryHeuristics = memoryHeuristics;
    }

    public unsafe ClassInfo DetermineClass(nint objectAddress)
    {
        if (!VirtualMemory.CanRead(objectAddress)) {
            return new ClassInfo();
        }

        var restOfPageSize = (uint)(MemoryHeuristics.NextPage(objectAddress) - objectAddress).ToInt32();
        if ((objectAddress & (nint.Size - 1)) != 0) {
            // The object is not aligned on a void* boundary.
            // Return a dummy class that will contain the rest of the page.
            return new ClassInfo
            {
                EstimatedSize = restOfPageSize,
            };
        }

        var vtbl = *(nint*)objectAddress.ToPointer();
        var className = DetermineClassName(objectAddress, vtbl);
        if (_classCache.TryGetValue(className, out var classInfo)) {
            return classInfo;
        }

        classInfo = new ClassInfo
        {
            Name = className,
        };

        PopulateFromVtbl(classInfo, vtbl);
        PopulateFromClientStructs(classInfo);
        PopulateAggregates(classInfo, restOfPageSize);

        _classCache.Add(className, classInfo);
        return classInfo;
    }

    private unsafe string DetermineClassName(nint objectAddress, nint vtbl)
    {
        if (_dataYamlContainer.Data is not null) {
            if (_dataYamlContainer.ClassesByInstance!.TryGetValue(objectAddress, out var className)
             || _dataYamlContainer.ClassesByVtbl!.TryGetValue(vtbl, out className)) {
                return className;
            }

            foreach (var (pointer, clsName) in _dataYamlContainer.ClassesByInstancePointer!) {
                if (*(nint*)pointer.ToPointer() == objectAddress) {
                    return clsName;
                }
            }
        }

        return $"Cls_{vtbl:X}";
    }

    private unsafe void PopulateFromVtbl(ClassInfo classInfo, nint vtbl)
    {
        var dtor = VirtualMemory.CanRead(vtbl) ? *(nint*)vtbl.ToPointer() : 0;
        classInfo.SizeFromDtor = _memoryHeuristics.EstimateSizeFromDtor(dtor);

        if ((_dataYamlContainer.Data?.Classes?.TryGetValue(classInfo.Name, out var @class) ?? false)
         && @class?.Vtbls is not null) {
            foreach (var vt in @class.Vtbls) {
                if (vt is null) {
                    continue;
                }

                if (vt.Ea != vtbl) {
                    dtor = *(nint*)vt.Ea.Value.ToPointer();
                    var sizeFromDtor = _memoryHeuristics.EstimateSizeFromDtor(dtor);
                    if (sizeFromDtor.HasValue && (!classInfo.SizeFromDtor.HasValue
                                               || classInfo.SizeFromDtor.Value < sizeFromDtor.Value)) {
                        classInfo.SizeFromDtor = sizeFromDtor;
                    }
                }
            }
        }
    }

    private void PopulateFromClientStructs(ClassInfo classInfo)
    {
        if (_dataYamlContainer.Data is not null) {
            classInfo.DataYamlClass = _dataYamlContainer.Data.Classes?.GetValueOrDefault(classInfo.Name);
        }

        var csType =
            typeof(StdString).Assembly.GetType("FFXIVClientStructs.FFXIV." + classInfo.Name.Replace("::", "."));
        classInfo.ClientStructsType = csType;
        if (csType is not null) {
            // Cannot use Marshal.SizeOf as it fails on certain types.
            classInfo.SizeFromClientStructs = (uint)(int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!
                                                                       .MakeGenericMethod(csType)
                                                                       .Invoke(null, null)!;
        }
    }

    private void PopulateAggregates(ClassInfo classInfo, uint restOfPageSize)
    {
        if (classInfo.SizeFromDtor.HasValue || classInfo.SizeFromClientStructs.HasValue) {
            classInfo.EstimatedSize = Math.Max(classInfo.SizeFromDtor ?? 0, classInfo.SizeFromClientStructs ?? 0);
        } else {
            classInfo.EstimatedSize = restOfPageSize;
        }
    }

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (message.IsPropertyChanged(nameof(Configuration.Configuration.DataYamlPath))) {
            _classCache.Clear();
        }
    }
}
