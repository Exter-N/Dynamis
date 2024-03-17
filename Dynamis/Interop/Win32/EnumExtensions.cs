using Dalamud.Memory;

namespace Dynamis.Interop.Win32;

public static class EnumExtensions
{
    public static bool CanRead(this MemoryProtection protection)
        => 0 != (protection & (MemoryProtection.ReadOnly | MemoryProtection.ReadWrite | MemoryProtection.WriteCopy
                             | MemoryProtection.ExecuteRead | MemoryProtection.ExecuteReadWrite
                             | MemoryProtection.ExecuteWriteCopy));

    public static bool CanWrite(this MemoryProtection protection)
        => 0 != (protection & (MemoryProtection.ReadWrite | MemoryProtection.WriteCopy
                                                          | MemoryProtection.ExecuteReadWrite
                                                          | MemoryProtection.ExecuteWriteCopy));

    public static bool CanExecute(this MemoryProtection protection)
        => 0 != (protection & (MemoryProtection.Execute | MemoryProtection.ExecuteRead
                                                        | MemoryProtection.ExecuteReadWrite
                                                        | MemoryProtection.ExecuteWriteCopy));
}
