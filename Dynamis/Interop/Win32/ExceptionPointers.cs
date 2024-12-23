using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ExceptionPointers
{
    public ExceptionRecord* ExceptionRecord;
    public Context*         ContextRecord;
}
