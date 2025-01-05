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
                Name = resolved.ToString(),
            };
        }

        return AddressIdentification.Default;
    }
}
