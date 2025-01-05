using Dynamis.Interop.Win32;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop;

public sealed class ModuleAddressResolver(SymbolApi symbolApi, ILogger<ModuleAddressResolver> logger)
{
    private static readonly IComparer<(Lazy<string>, IntPtr BaseAddress)> ModuleCacheComparer =
        Comparer<(Lazy<string>, IntPtr BaseAddress)>.Create((lhs, rhs) => lhs.BaseAddress.CompareTo(rhs.BaseAddress));

    private readonly List<(Lazy<string> ModuleName, nint BaseAddress)> _moduleCache        = [];
    private          long                                              _moduleCacheBuiltAt = 0;

    private const long ModuleCacheMaxAge = 5000L;

    public (string ModuleName, string? SymbolName, nint Displacement)? Resolve(nint address)
    {
        if (!VirtualMemory.TryQuery(address, out var basicInfo) || basicInfo.Type != MemoryType.Image) {
            return null;
        }

        UpdateModuleCacheIfStale();
        var i = _moduleCache.BinarySearch(
            (null!, address), // ModuleName is unused in the BinarySearch
            ModuleCacheComparer
        );
        if (i < 0) {
            i = ~i - 1;
        }

        if (i < 0) {
            return null;
        }

        try {
            if (symbolApi.SymFromAddr(ProcessThreadApi.GetCurrentProcess(), address) is
                {
                } symbol) {
                return (_moduleCache[i].ModuleName.Value, symbol.Name, symbol.Displacement);
            }
        } catch (Exception e) {
            logger.LogError(
                e, "Failed to resolve symbol information for address {Module}+{Displacement}",
                _moduleCache[i].ModuleName.Value, address - _moduleCache[i].BaseAddress
            );
        }

        return (_moduleCache[i].ModuleName.Value, null, address - _moduleCache[i].BaseAddress);
    }

    private void UpdateModuleCacheIfStale()
    {
        if (unchecked(Environment.TickCount64 - _moduleCacheBuiltAt) <= ModuleCacheMaxAge) {
            return;
        }

        _moduleCache.Clear();
        var modules = ProcessThreadApi.EnumProcessModules(ProcessThreadApi.GetCurrentProcess());
        Array.Sort(modules);
        _moduleCache.AddRange(
            from module in modules
            select (new Lazy<string>(() => Path.GetFileName(ProcessThreadApi.GetModuleFileName(module))), module)
        );
        _moduleCacheBuiltAt = Environment.TickCount64;
    }
}
