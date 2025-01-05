using System.Runtime.InteropServices;
using Dalamud.Plugin;
using Dalamud.Utility;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop.Win32;

public sealed partial class SymbolApi
{
    private const int MaxSymName = 2000;

    public SymbolApi(IDalamudPluginInterface pi, ILogger<SymbolApi> logger)
    {
        if (Util.IsWine()) {
            InitSymbolHandler(pi, logger);
        }
    }

    /// <remarks> This mimics <see cref="Dalamud.EntryPoint.InitSymbolHandler"/>. </remarks>
    private static void InitSymbolHandler(IDalamudPluginInterface pi, ILogger logger)
    {
        try {
            var assetDirectory = pi.DalamudAssetDirectory.FullName;
            if (string.IsNullOrEmpty(assetDirectory)) {
                return;
            }

            var symbolPath = Path.Combine(assetDirectory, "UIRes", "pdb");
            var searchPath = $".;{symbolPath}";

            SymCleanup(ProcessThreadApi.GetCurrentProcess());

            if (!SymInitialize(ProcessThreadApi.GetCurrentProcess(), searchPath, true)) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        } catch (Exception ex) {
            logger.LogError(ex, "SymbolHandler Initialize Failed.");
        }
    }

    [LibraryImport(
        "dbghelp.dll", EntryPoint = "SymInitializeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SymInitialize(IntPtr hProcess, string userSearchPath,
        [MarshalAs(UnmanagedType.Bool)] bool fInvadeProcess);

    [LibraryImport("dbghelp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SymCleanup(IntPtr hProcess);

    [LibraryImport(
        "dbghelp.dll", EntryPoint = "SymFromAddrW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool SymFromAddr(nint hProcess, ulong Address, out ulong Displacement,
        SymbolInfo* Symbol);

    public unsafe (string Name, SymbolInfo Info, nint Displacement)? SymFromAddr(nint hProcess, nint Address)
    {
        bool success;
        ulong displacement;
        var buffer = stackalloc byte[sizeof(SymbolInfo) + (MaxSymName - 1) * sizeof(char)];
        var symInfo = (SymbolInfo*)buffer;
        symInfo->SizeOfStruct = (uint)sizeof(SymbolInfo);
        symInfo->MaxNameLen = MaxSymName;
        lock (this) {
            success = SymFromAddr(hProcess, unchecked((ulong)Address), out displacement, symInfo);
        }

        if (!success) {
            var hr = Marshal.GetHRForLastWin32Error();
            if (hr != unchecked((int)0x80070000u)) {
                Marshal.ThrowExceptionForHR(hr);
            }

            return null;
        }

        return (new(symInfo->Name, 0, (int)symInfo->NameLen), *symInfo, unchecked((nint)displacement));
    }
}
