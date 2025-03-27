using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Dynamis.ClientStructs;

public record struct Address(nint Value) : IYamlConvertible
{
    public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
    {
        Value = DoParse(parser.Consume<Scalar>().Value);
    }

    public static Address Parse(string value)
        => new(DoParse(value));

    private static nint DoParse(string value)
        => value.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)
            ? nint.Parse(value[2..], NumberStyles.HexNumber)
            : nint.Parse(value);

    public static bool TryParse(YamlNode? node, out Address address)
    {
        var value = (node as YamlScalarNode)?.Value;
        if (value is not null) {
            return TryParse(value, out address);
        }

        address = default;
        return false;
    }

    public static bool TryParse(string value, out Address address)
    {
        if (TryParse(value, out nint result)) {
            address = new(result);
            return true;
        }

        address = default;
        return false;
    }

    private static bool TryParse(string value, out nint result)
        => value.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)
            ? nint.TryParse(value[2..], NumberStyles.HexNumber, null, out result)
            : nint.TryParse(value,      out result);

    public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
    {
        nestedObjectSerializer(Value);
    }

    public override string ToString()
        => $"0x{Value:X}";

    public static explicit operator nint(Address value)
        => value.Value;

    public static explicit operator Address(nint value)
        => new(value);

    public static Address operator +(Address lhs, nint rhs)
        => new(lhs.Value + rhs);

    public static Address operator +(nint lhs, Address rhs)
        => new(lhs + rhs.Value);
}
