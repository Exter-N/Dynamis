using Dynamis.Interop.Win32;
using InteropGenerator.Runtime;

namespace Dynamis.Interop;

public sealed record CStringSnapshot(nint Address, string Value)
{
    public override string ToString()
        => Value;

    public static unsafe CStringSnapshot FromAddress(nint address)
        => new(
            address, address is 0 || !VirtualMemory.GetProtection(address).CanRead()
                ? string.Empty
                : new CStringPointer((byte*)address).ToString()
        );
}
