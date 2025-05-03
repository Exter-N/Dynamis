using System.Runtime.InteropServices;
using Dalamud.Plugin;
using Dalamud.Utility;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop.Win32;

public sealed partial class SymbolApi : IMessageObserver<ConfigurationChangedMessage>
{
    private const int MaxSymName            = 2000;
    private const int ImageFileMachineAmd64 = 0x8664;

    private readonly IDalamudPluginInterface _pi;
    private readonly ILogger<SymbolApi>      _logger;
    private readonly ConfigurationContainer  _configuration;

    private bool _initialized;
    private bool _forceInitialized;

    public SymbolApi(IDalamudPluginInterface pi, ILogger<SymbolApi> logger, ConfigurationContainer configuration)
    {
        _pi = pi;
        _logger = logger;
        _configuration = configuration;
        switch (configuration.Configuration.SymbolHandlerMode) {
            case SymbolHandlerMode.ForceInitialize:
                InitSymbolHandler(pi, logger);
                _initialized = true;
                _forceInitialized = true;
                break;
            case SymbolHandlerMode.Default:
                // On Windows, Dalamud will have initialized this at boot.
                _initialized = !Util.IsWine();
                break;
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

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (!message.IsPropertyChanged(nameof(_configuration.Configuration.SymbolHandlerMode))) {
            return;
        }

        switch (_configuration.Configuration.SymbolHandlerMode) {
            case SymbolHandlerMode.ForceInitialize:
                if (!_forceInitialized) {
                    InitSymbolHandler(_pi, _logger);
                    _initialized = true;
                    _forceInitialized = true;
                }
                break;
            case SymbolHandlerMode.Default:
                _initialized |= !Util.IsWine();
                break;
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

    [LibraryImport(
        "dbghelp.dll", EntryPoint = "StackWalk64", StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool StackWalk64(uint MachineType, nint hProcess, nint hThread,
        StackFrame* StackFrame, Context* ContextRecord, nint ReadMemoryRoutine, nint FunctionTableAccessRoutine,
        nint GetModuleBaseRoutine, nint TranslateAddress);

    [return: MarshalAs(UnmanagedType.Bool)]
    public delegate bool ReadProcessMemoryRoutine(nint hProcess, ulong qwBaseAddress, nint lpBuffer, uint nSize,
        out uint lpNumberOfBytesRead);

    public unsafe (string Name, SymbolInfo Info, nint Displacement)? SymFromAddr(nint hProcess, nint Address)
    {
        if (!_initialized) {
            return null;
        }

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

    public unsafe StackFrame[] StackWalk(Context context, ReadProcessMemoryRoutine? readProcessMemory)
    {
        if (!_initialized) {
            return [];
        }

        var currentProcess = ProcessThreadApi.GetCurrentProcess();
        var currentThread = ProcessThreadApi.GetCurrentThread();
        var readMemoryRoutine = readProcessMemory is not null
            ? Marshal.GetFunctionPointerForDelegate(readProcessMemory)
            : 0;
        var dbghelp = SafeLibraryHandle.Get("dbghelp.dll");
        var functionTableAccessRoutine = dbghelp.GetProcAddress("SymFunctionTableAccess64");
        var getModuleBaseRoutine = dbghelp.GetProcAddress("SymGetModuleBase64");
        var trace = new List<StackFrame>();
        var writableContext = stackalloc Context[1];
        *writableContext = context;
        var stackFrame = stackalloc StackFrame[1];
        stackFrame->AddrPC.Offset = context.Rip;
        stackFrame->AddrPC.Mode = StackFrame.AddressMode.AddrModeFlat;
        stackFrame->AddrStack.Offset = context.Rsp;
        stackFrame->AddrStack.Mode = StackFrame.AddressMode.AddrModeFlat;
        stackFrame->AddrFrame.Offset = context.Rbp;
        stackFrame->AddrFrame.Mode = StackFrame.AddressMode.AddrModeFlat;
        while (true) {
            lock (this) {
                if (!StackWalk64(
                        ImageFileMachineAmd64, currentProcess,             currentThread, stackFrame, writableContext,
                        readMemoryRoutine,     functionTableAccessRoutine, getModuleBaseRoutine, 0
                    )) {
                    break;
                }
            }

            trace.Add(*stackFrame);
        }

        GC.KeepAlive(readProcessMemory);

        return trace.ToArray();
    }
}
