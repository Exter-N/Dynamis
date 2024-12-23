namespace Dynamis.Interop;

[Flags]
public enum AddressType : byte
{
    Instance = 1,
    VirtualTable = 2,
    Function = 4,
    Global = 8,
    All = Instance | VirtualTable | Function | Global,
}
