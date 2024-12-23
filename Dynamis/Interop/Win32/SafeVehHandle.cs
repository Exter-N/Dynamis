using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

public sealed partial class SafeVehHandle : SafeHandle
{
    public override bool IsInvalid
        => handle == 0;

    public SafeVehHandle(nint hVeh, bool ownsHandle = true) : base(0, ownsHandle)
    {
        SetHandle(hVeh);
    }

    public static SafeVehHandle AddHandler(bool first, nint handler)
    {
        var hVeh = AddVectoredExceptionHandler(first ? 1UL : 0UL, handler);
        if (hVeh == 0) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        try {
            return new(hVeh);
        } catch {
            RemoveVectoredExceptionHandler(hVeh);
            throw;
        }
    }

    protected override bool ReleaseHandle()
    {
        var previousHandle = Interlocked.Exchange(ref handle, 0);
        return previousHandle == 0 || RemoveVectoredExceptionHandler(previousHandle) != 0;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint AddVectoredExceptionHandler(ulong First, nint Handler);

    [LibraryImport("kernel32.dll")]
    private static partial ulong RemoveVectoredExceptionHandler(nint Handle);

    public unsafe delegate long VectoredExceptionHandler(ExceptionPointers* ExceptionInfo);

    public const long ExceptionContinueExecution = 0xFFFFFFFFL;
    public const long ExceptionContinueSearch    = 0L;
}
