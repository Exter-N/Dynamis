using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

public static partial class HandleApi
{
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint hObject);
}
