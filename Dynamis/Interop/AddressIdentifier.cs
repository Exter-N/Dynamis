using Dynamis.ClientStructs;

namespace Dynamis.Interop;

public class AddressIdentifier(DataYamlContainer dataYamlContainer, ModuleAddressResolver moduleAddressResolver)
{
    public AddressIdentification Identify(nint address, AddressType typeHint = AddressType.All)
    {
        var addressId = dataYamlContainer.IdentifyAddress(address, typeHint);
        if (addressId != AddressIdentification.Default) {
            return addressId;
        }

        if (moduleAddressResolver.Resolve(address) is
            {
            } resolved) {
            return AddressIdentification.Default with
            {
                Name = resolved.SymbolName is not null
                    ? resolved.Displacement != 0
                        ? $"{resolved.ModuleName}!{resolved.SymbolName}+{resolved.Displacement:X}"
                        : $"{resolved.ModuleName}!{resolved.SymbolName}"
                    : $"{resolved.ModuleName}+{resolved.Displacement:X}",
            };
        }

        return AddressIdentification.Default;
    }
}
