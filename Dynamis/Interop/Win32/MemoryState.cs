namespace Dynamis.Interop.Win32;

[Flags]
public enum MemoryState : uint
{
    Commit  = 0x1000,
    Reserve = 0x2000,
    Free    = 0x10000,
}
