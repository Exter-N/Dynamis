using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ExceptionRecord
{
    public       uint             ExceptionCode;
    public       uint             ExceptionFlags;
    public       ExceptionRecord* InnerExceptionRecord;
    public       nint             ExceptionAddress;
    public       uint             NumberParameters;
    public fixed ulong            ExceptionInformation[15];
}
