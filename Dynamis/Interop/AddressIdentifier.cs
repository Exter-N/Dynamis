using Dynamis.ClientStructs;
using Dynamis.Messaging;
using Dynamis.Utility;

namespace Dynamis.Interop;

public class AddressIdentifier(DataYamlContainer dataYamlContainer, ModuleAddressResolver moduleAddressResolver)
    : IMessageObserver<ConfigurationChangedMessage>
{
    private readonly ShortLivedCache<(nint, AddressType), AddressIdentification> _cache = new();

    public AddressIdentification Identify(nint address, AddressType typeHint = AddressType.All)
    {
        AddressIdentification id;
        lock (_cache) {
            if (_cache.TryGetValue((address, typeHint), out id)) {
                return id;
            }
        }

        id = DoIdentify(address, typeHint);
        lock (_cache) {
            _cache.TryAdd((address, typeHint), id);
        }

        return id;
    }

    private AddressIdentification DoIdentify(nint address, AddressType typeHint)
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

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (DataYamlContainer.IsDataYamlConfigurationChanged(message)) {
            lock (_cache) {
                _cache.Clear();
            }
        }
    }

    public void Tick()
    {
        lock (_cache) {
            _cache.Tick();
        }
    }
}
