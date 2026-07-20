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

    private readonly delegate* unmanaged<int>                                     _terminateFn;
    private readonly delegate* unmanaged<byte, nint, byte, int>                   _setBreakpointFn;
    private readonly delegate* unmanaged<int>                                     _refreshAllBreakpointsFn;
    private readonly delegate* unmanaged<int>                                     _clearAllBreakpointsFn;
    private readonly delegate* unmanaged<nint, nint, nint, int>                   _memmoveFn;
    private readonly delegate* unmanaged<nint, nint, nint, int>                   _userMemmoveFn;
    private readonly delegate* unmanaged<nint, nint, nint, nint, nint, nint, int> _userInvokeFn;
    private readonly delegate* unmanaged<nint, int>                               _setEventFn;
    private readonly delegate* unmanaged<nint, int>                               _userSetEventFn;
    private readonly delegate* unmanaged<nint, int, int>                          _releaseSemaphoreFn;
    private readonly delegate* unmanaged<nint, int, int>                          _userReleaseSemaphoreFn;
    private readonly delegate* unmanaged<int>                                     _syncFn;
    private readonly delegate* unmanaged<int>                                     _userSyncFn;

    public event EventHandler<BreakpointEventArgs>? Breakpoint;

    public IpfdModule(ResourceProvider resourceProvider, ILogger logger)
    {
        _logger = logger;

        _logger.LogInformation("Loading IPFD module");

        _breakpointHandler = HandleBreakpoint;
        _ipfdLibrary = SafeLibraryHandle.Load(resourceProvider.GetFileResourcePath("dynamis_ipfd.dll"));

        var initializeFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_initialize");
        _terminateFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_terminate");

        var setBreakpointCallbackFn =
            (delegate* unmanaged<nint, int>)_ipfdLibrary.GetProcAddress("ipfd_set_breakpoint_callback");

        _setBreakpointFn =
            (delegate* unmanaged<byte, nint, byte, int>)_ipfdLibrary.GetProcAddress("ipfd_set_breakpoint");
        _refreshAllBreakpointsFn =
            (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_refresh_all_breakpoints");
        _clearAllBreakpointsFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_clear_all_breakpoints");
        _memmoveFn = (delegate* unmanaged<nint, nint, nint, int>)_ipfdLibrary.GetProcAddress("ipfd_memmove");
        _userMemmoveFn = (delegate* unmanaged<nint, nint, nint, int>)_ipfdLibrary.GetProcAddress("ipfd_user_memmove");
        _userInvokeFn =
            (delegate* unmanaged<nint, nint, nint, nint, nint, nint, int>)_ipfdLibrary.GetProcAddress(
                "ipfd_user_invoke"
            );
        _setEventFn = (delegate* unmanaged<nint, int>)_ipfdLibrary.GetProcAddress("ipfd_set_event");
        _userSetEventFn = (delegate* unmanaged<nint, int>)_ipfdLibrary.GetProcAddress("ipfd_user_set_event");
        _releaseSemaphoreFn =
            (delegate* unmanaged<nint, int, int>)_ipfdLibrary.GetProcAddress("ipfd_release_semaphore");
        _userReleaseSemaphoreFn =
            (delegate* unmanaged<nint, int, int>)_ipfdLibrary.GetProcAddress("ipfd_user_release_semaphore");
        _syncFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_sync");
        _userSyncFn = (delegate* unmanaged<int>)_ipfdLibrary.GetProcAddress("ipfd_user_sync");

        Marshal.ThrowExceptionForHR(initializeFn());
        Marshal.ThrowExceptionForHR(setBreakpointCallbackFn(Marshal.GetFunctionPointerForDelegate(_breakpointHandler)));
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

    public void UserMemoryCopy(nint source, nint destination, nint size)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_userMemmoveFn(source, destination, size));
    }

    public void UserInvoke(nint function, nint arg0, nint arg1, nint arg2, nint arg3, nint returnPtr)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_userInvokeFn(function, arg0, arg1, arg2, arg3, returnPtr));
    }

    public void UserInvoke(Action action)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        delegate* unmanaged<nint, void> invokeActionPtr = &InvokeAction;
        Marshal.ThrowExceptionForHR(_userInvokeFn((nint)invokeActionPtr, (nint)GCHandle.Alloc(action), 0, 0, 0, 0));
    }

    public void SetEvent(SafeWaitHandle @event)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_setEventFn(@event.DangerousGetHandle()));
    }

    public void UserSetEvent(SafeWaitHandle @event)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_userSetEventFn(@event.DangerousGetHandle()));
    }

    public void ReleaseSemaphore(SafeWaitHandle semaphore, int releaseCount)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_releaseSemaphoreFn(semaphore.DangerousGetHandle(), releaseCount));
    }

    public void UserReleaseSemaphore(SafeWaitHandle semaphore, int releaseCount)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_userReleaseSemaphoreFn(semaphore.DangerousGetHandle(), releaseCount));
    }

    public void Sync()
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_syncFn());
    }

    public void UserSync()
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        Marshal.ThrowExceptionForHR(_userSyncFn());
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

    [UnmanagedCallersOnly]
    private static void InvokeAction(nint rawHandle)
    {
        var handle = (GCHandle)rawHandle;
        try {
            (handle.Target as Action)?.Invoke();
        } finally {
            handle.Free();
        }
    }
}
