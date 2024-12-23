using System.Runtime.InteropServices;
using Dynamis.Interop.Win32;
using Dynamis.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Dynamis.Interop.Ipfd;

/// <summary> In-Process Faux Debugger. </summary>
public sealed unsafe partial class IpfdModule : IDisposable
{
    private readonly ILogger _logger;

    private readonly SafeVehHandle.VectoredExceptionHandler _breakpointHandler;
    private readonly SafeLibraryHandle                      _ipfdLibrary;

    private readonly delegate* unmanaged<int>                   _terminateFn;
    private readonly delegate* unmanaged<byte, nint, byte, int> _setBreakpointFn;
    private readonly delegate* unmanaged<int>                   _refreshAllBreakpointsFn;
    private readonly delegate* unmanaged<int>                   _clearAllBreakpointsFn;
    private readonly delegate* unmanaged<nint, nint, nint, int> _memmoveFn;
    private readonly delegate* unmanaged<nint, int>             _setEventFn;
    private readonly delegate* unmanaged<int>                   _syncFn;

    public event EventHandler<BreakpointEventArgs>? Breakpoint;

    public IpfdModule(ResourceProvider resourceProvider, ILogger logger)
    {
        _logger = logger;

        _logger.LogInformation("Loading IPFD module");

        _breakpointHandler = HandleBreakpoint;
        _ipfdLibrary = SafeLibraryHandle.Load(resourceProvider.GetFileResourcePath("dynamis_ipfd.dll"));

        var initializeFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_initialize");
        _terminateFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_terminate");

        var setBreakpointHandlerFn =
            (delegate* unmanaged<nint, int>)_ipfdLibrary.GetProcAddress("ipfd_set_breakpoint_callback");

        _setBreakpointFn =
            (delegate* unmanaged<byte, nint, byte, int>)_ipfdLibrary.GetProcAddress("ipfd_set_breakpoint");
        _refreshAllBreakpointsFn =
            (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_refresh_all_breakpoints");
        _clearAllBreakpointsFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_clear_all_breakpoints");
        _memmoveFn = (delegate* unmanaged<nint, nint, nint, int>)_ipfdLibrary.GetProcAddress("ipfd_memmove");
        _setEventFn = (delegate* unmanaged<nint, int>)_ipfdLibrary.GetProcAddress("ipfd_set_event");
        _syncFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_sync");

        Marshal.ThrowExceptionForHR(initializeFn());
        Marshal.ThrowExceptionForHR(setBreakpointHandlerFn(Marshal.GetFunctionPointerForDelegate(_breakpointHandler)));
        _logger.LogInformation("Loaded IPFD module");
    }

    ~IpfdModule()
        => Dispose(false);

    public void SetBreakpoint(byte index, nint address, BreakpointFlags flags)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_setBreakpointFn(index, address, (byte)flags));
    }

    public void RefreshAllBreakpoints()
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_refreshAllBreakpointsFn());
    }

    public void ClearAllBreakpoints()
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_clearAllBreakpointsFn());
    }

    public void MemoryCopy(nint source, nint destination, nint size)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_memmoveFn(source, destination, size));
    }

    public void SetEvent(SafeWaitHandle @event)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_setEventFn(@event.DangerousGetHandle()));
    }

    public void Sync()
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_syncFn());
    }

    private static byte WhichBreakpoints(Context* context)
    {
        var status = context->Dr6;
        var control = context->Dr7;
        byte which = 0;
        if ((status & 1) != 0 && (control & 0x3) != 0) {
            which |= 1;
        }

        if ((status & 2) != 0 && (control & 0xC) != 0) {
            which |= 2;
        }

        if ((status & 4) != 0 && (control & 0x30) != 0) {
            which |= 4;
        }

        if ((status & 8) != 0 && (control & 0xC0) != 0) {
            which |= 8;
        }

        return which;
    }

    private long HandleBreakpoint(ExceptionPointers* exception)
    {
        var which = WhichBreakpoints(exception->ContextRecord);
        var args = new BreakpointEventArgs(which, exception);

        try {
            Breakpoint?.Invoke(this, args);
        } catch (Exception e) {
            _logger.LogError(
                e, "Failed to process breakpoint(s) {Which} at address {Address:X}", args.Which, args.Address
            );
        }

        return SafeVehHandle.ExceptionContinueExecution;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        _logger.LogInformation("Unloading IPFD module");

        Marshal.ThrowExceptionForHR(_terminateFn());
        _ipfdLibrary.Dispose();

        _logger.LogInformation("Unloaded IPFD module");
    }
}
