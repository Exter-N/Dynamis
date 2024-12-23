namespace Dynamis.Interop.Ipfd;

[Flags]
public enum BreakpointFlags : byte
{
    LocalEnable = 1,
    GlobalEnable = 2,

    InstructionExecution = 0,
    DataWrites = 0x10,
    IoReadsAndWrites = 0x20,
    DataReadsAndWrites = 0x30,

    LengthOne = 0,
    LengthTwo = 0x40,
    LengthEight = 0x80,
    LengthFour = 0xC0,
}
