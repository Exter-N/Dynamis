using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

public static partial class ProcessThreadApi
{
    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("kernel32.dll")]
    public static partial void GetCurrentThreadStackLimits(out nint lowLimit, out nint highLimit);

    [LibraryImport("kernel32.dll")]
    public static partial nint GetCurrentProcess();

    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool EnumProcessModules(nint hProcess, nint* lphModule, uint cb, out uint lpcbNeeded);

    public static unsafe nint[] EnumProcessModules(nint hProcess)
    {
        var modules = new nint[128];
        for (;;) {
            var cb = (uint)(modules.Length * sizeof(nint));
            uint cbNeeded;
            fixed (nint* lphModule = modules) {
                if (!EnumProcessModules(hProcess, lphModule, cb, out cbNeeded)) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            if (cb < cbNeeded) {
                Array.Resize(ref modules, (int)(cbNeeded / sizeof(nint)));
                return modules;
            }

            modules = new nint[modules.Length << 1];
        }
    }

    [LibraryImport(
        "kernel32.dll", EntryPoint = "GetModuleFileNameW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    private static unsafe partial uint GetModuleFileName(nint hModule, char* lpFilename, uint nSize);

    public static unsafe string GetModuleFileName(nint hModule)
    {
        var buffer = new char[260];
        for (;;) {
            uint length;
            fixed (char* lpFilename = buffer) {
                length = GetModuleFileName(hModule, lpFilename, (uint)buffer.Length);
            }

            if (length < buffer.Length) {
                return new(buffer, 0, (int)length);
            }

            buffer = new char[buffer.Length << 1];
        }
    }
}
