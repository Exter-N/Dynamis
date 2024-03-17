using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

public static unsafe partial class VirtualMemory
{
    public static bool CanRead(nint address)
        => TryQuery(address, out var info) && info.State.HasFlag(MemoryState.Commit) && info.Protect.CanRead();

    public static MemoryBasicInformation Query(nint address)
    {
        if (!TryQuery(address, out var info)) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return info;
    }

    public static bool TryQuery(nint address, out MemoryBasicInformation info)
    {
        fixed (MemoryBasicInformation* pInfo = &info) {
            return 0 != VirtualQuery(address, pInfo, sizeof(MemoryBasicInformation));
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint VirtualQuery(nint lpAddress, MemoryBasicInformation* lpBuffer, int dwLength);
}
