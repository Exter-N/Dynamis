using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

public sealed partial class SafeLibraryHandle : SafeHandle
{
    public override bool IsInvalid
        => handle == 0;

    public SafeLibraryHandle(nint hModule, bool ownsHandle = true) : base(0, ownsHandle)
    {
        SetHandle(hModule);
    }

    public static SafeLibraryHandle Load(string libFileName)
    {
        var hModule = LoadLibrary(libFileName);
        if (hModule == 0) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        try {
            return new(hModule);
        } catch {
            FreeLibrary(hModule);
            throw;
        }
    }

    public static SafeLibraryHandle Get(string moduleName)
    {
        var hModule = GetModuleHandle(moduleName);
        if (hModule == 0) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return new(hModule, false);
    }

    public nint GetProcAddress(string procName)
    {
        var address = GetProcAddress(handle, procName);
        if (address == 0) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return address;
    }

    protected override bool ReleaseHandle()
    {
        var previousHandle = Interlocked.Exchange(ref handle, 0);
        return previousHandle == 0 || FreeLibrary(previousHandle);
    }

    [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint LoadLibrary(string lpLibFileName);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint GetModuleHandle(string lpModuleName);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial nint GetProcAddress(nint hModule, string lpProcName);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeLibrary(nint hLibModule);
}
