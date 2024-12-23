using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Explicit, Size = 0x4D0)]
public unsafe struct Context
{
    [FieldOffset(0x0)]  public ulong P1Home;
    [FieldOffset(0x8)]  public ulong P2Home;
    [FieldOffset(0x10)] public ulong P3Home;
    [FieldOffset(0x18)] public ulong P4Home;
    [FieldOffset(0x20)] public ulong P5Home;
    [FieldOffset(0x28)] public ulong P6Home;

    [FieldOffset(0x30)] public uint   ContextFlags;
    [FieldOffset(0x34)] public uint   MxCsr;
    [FieldOffset(0x38)] public ushort SegCs;
    [FieldOffset(0x3A)] public ushort SegDs;
    [FieldOffset(0x3C)] public ushort SegEs;
    [FieldOffset(0x3E)] public ushort SegFs;
    [FieldOffset(0x40)] public ushort SegGs;
    [FieldOffset(0x42)] public ushort SegSs;
    [FieldOffset(0x44)] public uint   EFlags;

    [FieldOffset(0x48)] public ulong Dr0;
    [FieldOffset(0x50)] public ulong Dr1;
    [FieldOffset(0x58)] public ulong Dr2;
    [FieldOffset(0x60)] public ulong Dr3;
    [FieldOffset(0x68)] public ulong Dr6;
    [FieldOffset(0x70)] public ulong Dr7;

    [FieldOffset(0x78)] public ulong Rax;
    [FieldOffset(0x80)] public ulong Rcx;
    [FieldOffset(0x88)] public ulong Rdx;
    [FieldOffset(0x90)] public ulong Rbx;
    [FieldOffset(0x98)] public ulong Rsp;
    [FieldOffset(0xA0)] public ulong Rbp;
    [FieldOffset(0xA8)] public ulong Rsi;
    [FieldOffset(0xB0)] public ulong Rdi;
    [FieldOffset(0xB8)] public ulong R8;
    [FieldOffset(0xC0)] public ulong R9;
    [FieldOffset(0xC8)] public ulong R10;
    [FieldOffset(0xD0)] public ulong R11;
    [FieldOffset(0xD8)] public ulong R12;
    [FieldOffset(0xE0)] public ulong R13;
    [FieldOffset(0xE8)] public ulong R14;
    [FieldOffset(0xF0)] public ulong R15;

    [FieldOffset(0xF8)] public ulong Rip;

    [FieldOffset(0x100)] public XmmSaveArea FloatSave;

    [FieldOffset(0x300)] public fixed byte RawVectorRegister[26 << 4];

    public Span<UInt128> VectorRegister
    {
        get
        {
            fixed (byte* ptr = RawVectorRegister) {
                return new(ptr, 26);
            }
        }
    }

    [FieldOffset(0x4A0)] public ulong VectorControl;
    [FieldOffset(0x4A8)] public ulong DebugControl;
    [FieldOffset(0x4B0)] public ulong LastBranchToRip;
    [FieldOffset(0x4B8)] public ulong LastBranchFromRip;
    [FieldOffset(0x4C0)] public ulong LastExceptionToRip;
    [FieldOffset(0x4C8)] public ulong LastExceptionFromRip;
}
