using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct KdHelp
{
    public       ulong Thread;
    public       uint  ThCallbackStack;
    public       uint  ThCallbackBStore;
    public       uint  NextCallback;
    public       uint  FramePointer;
    public       ulong KiCallUserMode;
    public       ulong KeUserCallbackDispatcher;
    public       ulong SystemRangeStart;
    public       ulong KiUserExceptionDispatcher;
    public       ulong StackBase;
    public       ulong StackLimit;
    public       uint  BuildVersion;
    public       uint  RetpolineStubFunctionTableSize;
    public       ulong RetpolineStubFunctionTable;
    public       uint  RetpolineStubOffset;
    public       uint  RetpolineStubSize;
    public fixed ulong Reserved0[2];
}
