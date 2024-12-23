using Dynamis.Interop.Win32;

namespace Dynamis.Interop.Ipfd;

public sealed unsafe class BreakpointEventArgs(byte which, ExceptionPointers* exceptionInfo) : EventArgs
{
    public readonly byte               Which         = which;
    public readonly ExceptionPointers* ExceptionInfo = exceptionInfo;

    public nint Address
        => ExceptionInfo->ExceptionRecord->ExceptionAddress;

    public Context* Context
        => ExceptionInfo->ContextRecord;
}
