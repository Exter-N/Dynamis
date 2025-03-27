using Dynamis.Interop.Win32;

namespace Dynamis.Interop;

public sealed partial class ClassRegistry
{
    public unsafe ClassInfo GetVirtualTableClass(string className, nint vtbl, (uint, nuint) ownerSizeAndDisplacement, bool safeReads)
    {
        var vtblClassName = $"<Virtual Table> {className}";
        ClassInfo? classInfo;
        lock (_classCache) {
            if (_classCache.TryGetValue(vtblClassName, out classInfo)) {
                return classInfo;
            }

            var fields = new List<FieldInfo>();

            var knownVfuncCount = 1u;
            if (dataYamlContainer.Data?.Classes?.TryGetValue(className, out var @class) ?? false) {
                if (@class.Vtbls is not null && @class.Vfuncs is not null
                                             && dataYamlContainer.GetLiveAddress(@class.Vtbls[0].Ea) == vtbl) {
                    foreach (var (index, name) in @class.Vfuncs) {
                        knownVfuncCount = Math.Max(knownVfuncCount, index + 1);
                        fields.Add(
                            new FieldInfo()
                            {
                                Name = name,
                                Type = FieldType.Pointer,
                                Offset = (uint)(index * sizeof(nint)),
                                Size = (uint)sizeof(nint),
                            }
                        );
                    }
                }
            }

            classInfo = new ClassInfo
            {
                Name = vtblClassName,
                DefiningModule = GetVtblDefiningModule(vtbl, safeReads),
                Kind = ClassKind.VirtualTable,
                EstimatedSize = (uint)(knownVfuncCount * sizeof(nint) + EstimateVtblRestSize(
                    vtbl + (nint)knownVfuncCount * sizeof(nint), safeReads
                )),
                VtblOwnerSizeAndDisplacementFromDtor = ownerSizeAndDisplacement,
                Fields = fields.ToArray(),
            };

            classInfo.SizeFromOuterContext = classInfo.EstimatedSize;

            _classCache.Add(vtblClassName, classInfo);
        }

        return classInfo;
    }

    private unsafe nint EstimateVtblRestSize(nint vtbl, bool safeReads)
    {
        var restOfPage = stackalloc byte[Environment.SystemPageSize];
        var vtblSize = 0;
        do {
            var nextPage = MemoryHeuristics.NextPage(vtbl);
            var restOfPageSize = (nextPage - vtbl).ToInt32();
            if (safeReads) {
                ipfd.Copy<byte>(vtbl, (nint)restOfPage, restOfPageSize);
            } else {
                new ReadOnlySpan<byte>((void*)vtbl, restOfPageSize).CopyTo(new(restOfPage, restOfPageSize));
            }

            for (var i = 0; i < restOfPageSize; i += sizeof(nint)) {
                var func = *(nint*)(restOfPage + i);
                if (func == 0) {
                    continue;
                }

                if (!VirtualMemory.GetProtection(func).CanExecute()) {
                    return vtblSize + i;
                }

                if (memoryHeuristics.EstimateSizeAndDisplacementFromDtor(func).HasValue) {
                    return vtblSize + i;
                }
            }

            vtblSize += restOfPageSize;
            vtbl = nextPage;
        } while (VirtualMemory.GetProtection(vtbl).CanRead());

        return vtblSize;
    }

    private string GetVtblDefiningModule(nint vtbl, bool safeReads)
    {
        if (!VirtualMemory.GetProtection(vtbl).CanRead()) {
            return string.Empty;
        }

        var vf0 = Read<nint>(vtbl, safeReads);
        var vf0Protection = VirtualMemory.GetProtection(vf0);
        if (!vf0Protection.CanRead() || !vf0Protection.CanExecute()) {
            return string.Empty;
        }

        var vtblModuleAddress = moduleAddressResolver.Resolve(vtbl);
        if (string.IsNullOrEmpty(vtblModuleAddress?.ModuleName)) {
            return string.Empty;
        }

        var vf0ModuleAddress = moduleAddressResolver.Resolve(vf0);

        return string.Equals(
            vtblModuleAddress.ModuleName, vf0ModuleAddress?.ModuleName, StringComparison.OrdinalIgnoreCase
        )
            ? vtblModuleAddress.ModuleName
            : string.Empty;
    }
}
