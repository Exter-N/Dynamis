using System.Globalization;
using System.Text;
using Dynamis.ClientStructs;

namespace Dynamis.Interop;

public class PointerParser(DataYamlContainer dataYamlContainer, ModuleAddressResolver moduleAddressResolver)
{
    public bool TryParse(ReadOnlySpan<char> value, out nint result)
    {
        if (value.IsEmpty) {
            result = 0;
            return false;
        }

        if (TryParseIntPtr(value, out var parsedHex)) {
            result = parsedHex;
            return true;
        }

        var separator = value.IndexOf('+');
        if (separator >= 0 && TryParseIntPtr(value[(separator + 1)..], out parsedHex)) {
            var baseValue = value[..separator];
            separator = baseValue.IndexOf('!');
            ModuleAddress moduleAddress;
            if (separator >= 0) {
                moduleAddress = new(new(baseValue[..separator]), new(baseValue[(separator + 1)..]), parsedHex, -1);
            } else {
                moduleAddress = new(new(baseValue), null, parsedHex, -1);
            }

            if (moduleAddressResolver.GetAddress(moduleAddress) is
                {
                } address) {
                result = address;
                return true;
            }

            result = 0;
            return false;
        }

        separator = value.IndexOf('!');
        if (separator >= 0) {
            var moduleAddress = new ModuleAddress(new(value[..separator]), new(value[(separator + 1)..]), 0, -1);

            if (moduleAddressResolver.GetAddress(moduleAddress) is
                {
                } address) {
                result = address;
                return true;
            }

            result = 0;
            return false;
        }

        separator = value.IndexOf('_');
        if (separator >= 0 && TryParseIntPtr(value[(separator + 1)..], out parsedHex)) {
            result = dataYamlContainer.GetLiveAddress(new(parsedHex));
            return true;
        }

        result = 0;
        return false;
    }

    public bool TryParse(ReadOnlySpan<byte> value, out nint result)
    {
        if (value.IsEmpty) {
            result = 0;
            return false;
        }

        if (TryParseIntPtr(value, out var parsedHex)) {
            result = parsedHex;
            return true;
        }

        var separator = value.IndexOf((byte)'+');
        if (separator >= 0 && TryParseIntPtr(value[(separator + 1)..], out parsedHex)) {
            var baseValue = value[..separator];
            separator = baseValue.IndexOf((byte)'!');
            ModuleAddress moduleAddress;
            if (separator >= 0) {
                moduleAddress = new(
                    Encoding.UTF8.GetString(baseValue[..separator]),
                    Encoding.UTF8.GetString(baseValue[(separator + 1)..]), parsedHex, -1
                );
            } else {
                moduleAddress = new(Encoding.UTF8.GetString(baseValue), null, parsedHex, -1);
            }

            if (moduleAddressResolver.GetAddress(moduleAddress) is
                {
                } address) {
                result = address;
                return true;
            }

            result = 0;
            return false;
        }

        separator = value.IndexOf((byte)'!');
        if (separator >= 0) {
            var moduleAddress = new ModuleAddress(
                Encoding.UTF8.GetString(value[..separator]),
                Encoding.UTF8.GetString(value[(separator + 1)..]), 0, -1
            );

            if (moduleAddressResolver.GetAddress(moduleAddress) is
                {
                } address) {
                result = address;
                return true;
            }

            result = 0;
            return false;
        }

        separator = value.IndexOf((byte)'_');
        if (separator >= 0 && TryParseIntPtr(value[(separator + 1)..], out parsedHex)) {
            result = dataYamlContainer.GetLiveAddress(new(parsedHex));
            return true;
        }

        result = 0;
        return false;
    }

    private static bool TryParseIntPtr(ReadOnlySpan<char> value, out nint result)
        => nint.TryParse(StripHexPrefix(value), NumberStyles.HexNumber, null, out result);

    private static bool TryParseIntPtr(ReadOnlySpan<byte> value, out nint result)
        => nint.TryParse(StripHexPrefix(value), NumberStyles.HexNumber, null, out result);

    private static ReadOnlySpan<char> StripHexPrefix(ReadOnlySpan<char> value)
        => value.StartsWith("0x") || value.StartsWith("0X") ? value[2..] : value;

    private static ReadOnlySpan<byte> StripHexPrefix(ReadOnlySpan<byte> value)
        => value.StartsWith("0x"u8) || value.StartsWith("0X"u8) ? value[2..] : value;
}
