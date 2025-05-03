using InteropGenerator.Runtime;

namespace Dynamis.Interop;

public sealed record CStringSnapshot(nint Address, string Value)
{
    public override string ToString()
        => Value;

    public static unsafe CStringSnapshot FromAddress(nint address)
        => new(address, new CStringPointer((byte*)address).ToString());
}
