using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Explicit, Size = 0x2D0)]
public unsafe struct XmmSaveArea
{
    [FieldOffset(0x0)]  public ushort ControlWord;
    [FieldOffset(0x2)]  public ushort StatusWord;
    [FieldOffset(0x4)]  public byte   TagWord;
    [FieldOffset(0x6)]  public ushort ErrorOpcode;
    [FieldOffset(0x8)]  public uint   ErrorOffset;
    [FieldOffset(0xC)]  public ushort ErrorSelector;
    [FieldOffset(0x10)] public uint   DataOffset;
    [FieldOffset(0x14)] public ushort DataSelector;
    [FieldOffset(0x18)] public uint   MxCsr;
    [FieldOffset(0x1C)] public uint   MxCsrMask;

    [FieldOffset(0x20)] public fixed byte RawFloatRegister[8 << 4];
    [FieldOffset(0xA0)] public fixed byte RawVectorRegister[16 << 4];

    public Span<UInt128> FloatRegister
    {
        get
        {
            fixed (byte* ptr = RawFloatRegister) {
                return new(ptr, 8);
            }
        }
    }

    public Span<UInt128> VectorRegister
    {
        get
        {
            fixed (byte* ptr = RawVectorRegister) {
                return new(ptr, 16);
            }
        }
    }
}
