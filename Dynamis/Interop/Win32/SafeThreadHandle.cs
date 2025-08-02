using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

public sealed partial class SafeThreadHandle : SafeHandle
{
    public override bool IsInvalid
        => handle == 0;

    public SafeThreadHandle(nint hThread, bool ownsHandle = true) : base(0, ownsHandle)
    {
        SetHandle(hThread);
    }

    protected override bool ReleaseHandle()
    {
        var previousHandle = Interlocked.Exchange(ref handle, 0);
        return previousHandle == 0 || HandleApi.CloseHandle(previousHandle);
    }

    public static SafeThreadHandle Open(uint desiredAccess, bool inheritHandle, uint threadId)
    {
        var hThread = OpenThread(desiredAccess, inheritHandle, threadId);
        if (hThread == 0) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return new(hThread);
    }

    public static SafeThreadHandle OpenCurrent(uint desiredAccess, bool inheritHandle)
        => Open(desiredAccess, inheritHandle, ProcessThreadApi.GetCurrentThreadId());

    public uint Suspend()
    {
        var suspendCount = SuspendThread(handle);
        if (suspendCount == uint.MaxValue) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return suspendCount;
    }

    public uint Resume()
    {
        var suspendCount = ResumeThread(handle);
        if (suspendCount == uint.MaxValue) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return suspendCount;
    }

    public void GetContext(ref Context context)
    {
        if (!GetThreadContext(handle, ref context)) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }

    public void SetContext(ref readonly Context context)
    {
        if (!SetThreadContext(handle, in context)) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenThread(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        uint dwThreadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint SuspendThread(nint hThread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint ResumeThread(nint hThread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetThreadContext(nint hThread, ref Context lpContext);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetThreadContext(nint hThread, ref readonly Context lpContext);
}
