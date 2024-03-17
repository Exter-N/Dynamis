using System.Runtime.InteropServices;
using Dalamud.Memory;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Sequential)]
public struct MemoryBasicInformation
{
    public nint             BaseAddress;
    public nint             AllocationBase;
    public MemoryProtection AllocationProtect;
    public ushort           PartitionId;
    public nint             RegionSize;
    public MemoryState      State;
    public MemoryProtection Protect;
    public MemoryType       Type;
}
