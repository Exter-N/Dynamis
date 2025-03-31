using System.Runtime.InteropServices;
using Dynamis.Interop.Win32;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop;

public sealed class ModuleAddressResolver(SymbolApi symbolApi, ILogger<ModuleAddressResolver> logger)
{
    private readonly List<ModuleInfo> _moduleCache        = [];
    private          long             _moduleCacheBuiltAt = 0;

    private const long ModuleCacheMaxAge = 5000L;

    public ModuleAddress? Resolve(nint address)
    {
        if (!VirtualMemory.TryQuery(address, out var basicInfo) || basicInfo.Type != MemoryType.Image) {
            return null;
        }

        UpdateModuleCacheIfStale();
        var i = _moduleCache.BinarySearch(new(null!, address, 0)); // Only BaseAddress is used in the BinarySearch
        if (i < 0) {
            i = ~i - 1;
        }

        if (i < 0) {
            return null;
        }

        var module = _moduleCache[i];
        var originalAddress = module.OriginalBaseAddress != 0
            ? address + module.OriginalBaseAddress - module.BaseAddress
            : 0;

        try {
            if (symbolApi.SymFromAddr(ProcessThreadApi.GetCurrentProcess(), address) is
                {
                } symbol) {
                return new(module.ModuleName.Value, symbol.Name, symbol.Displacement, originalAddress);
            }
        } catch (Exception e) {
            logger.LogError(
                e, "Failed to resolve symbol information for address {Module}+{Displacement}",
                module.ModuleName.Value, address - module.BaseAddress
            );
        }

        return new(module.ModuleName.Value, null, address - module.BaseAddress, originalAddress);
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
            select new ModuleInfo(
                new(() => Path.GetFileName(ProcessThreadApi.GetModuleFileName(module))), module,
                GetOriginalBaseAddress(module)
            )
        );
        _moduleCacheBuiltAt = Environment.TickCount64;
    }

    public static unsafe nint GetOriginalBaseAddress(nint baseAddress)
    {
        if (!VirtualMemory.GetProtection(baseAddress).CanRead()) {
            return 0;
        }

        var dosHeader = (ImageDosHeader*)baseAddress;
        var ntHeaders = (ImageNtHeaders*)(baseAddress + dosHeader->LfaNew);
        return ntHeaders->SizeOfOptionalHeader >= 0x20 && ntHeaders->OptionalHeaderMagic == 0x020B
            ? ntHeaders->OriginalBaseAddress
            : 0;
    }

    private readonly record struct ModuleInfo(Lazy<string> ModuleName, nint BaseAddress, nint OriginalBaseAddress)
        : IComparable<ModuleInfo>
    {
        public int CompareTo(ModuleInfo other)
            => BaseAddress.CompareTo(other.BaseAddress);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x40)]
    private struct ImageDosHeader
    {
        [FieldOffset(0x3C)] public uint LfaNew;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ImageNtHeaders
    {
        [FieldOffset(0x14)] public ushort SizeOfOptionalHeader;
        [FieldOffset(0x18)] public ushort OptionalHeaderMagic;
        [FieldOffset(0x30)] public nint   OriginalBaseAddress;
    }
}
