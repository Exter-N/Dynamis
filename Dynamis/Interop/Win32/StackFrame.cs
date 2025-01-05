using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct StackFrame
{
    public       Address AddrPC;
    public       Address AddrReturn;
    public       Address AddrFrame;
    public       Address AddrStack;
    public       Address AddrBStore;
    public       nint    FuncTableEntry;
    public fixed ulong   Params[4];
    [MarshalAs(UnmanagedType.Bool)]
    public bool Far;
    [MarshalAs(UnmanagedType.Bool)]
    public bool Virtual;

    public fixed ulong  Reserved[3];
    public       KdHelp KdHelp;

    [StructLayout(LayoutKind.Sequential)]
    public struct Address
    {
        public ulong       Offset;
        public ushort      Segment;
        public AddressMode Mode;
    }

    public enum AddressMode : uint
    {
        AddrMode1616 = 0,
        AddrMode1632 = 1,
        AddrModeReal = 2,
        AddrModeFlat = 3,
    }
}
