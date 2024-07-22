using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Dynamis.ClientStructs;

[Serializable]
public sealed class DataYaml
{
    public string? Version { get; set; }

    public Dictionary<Address, string?>? Globals { get; set; }

    public Dictionary<Address, string?>? Functions { get; set; }

    public Dictionary<string, Class?>? Classes { get; set; }

    [Serializable]
    public sealed class Class
    {
        public List<Instance?>? Instances { get; set; }

        public List<VTable?>? Vtbls { get; set; }

        public Dictionary<uint, string?>? Vfuncs { get; set; }

        public Dictionary<Address, string?>? Funcs { get; set; }
    }

    [Serializable]
    public sealed class Instance
    {
        public Address Ea { get; set; }

        public string? Name { get; set; }

        public bool Pointer { get; set; } = true;
    }

    [Serializable]
    public sealed class VTable
    {
        public Address Ea { get; set; }

        public string? Base { get; set; }
    }

    public record struct Address(nint Value) : IYamlConvertible
    {
        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            var scalar = parser.Consume<Scalar>();
            var valueStr = scalar.Value;
            Value = valueStr.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)
                ? nint.Parse(valueStr[2..], NumberStyles.HexNumber)
                : nint.Parse(valueStr);
        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            nestedObjectSerializer(Value);
        }

        public static implicit operator nint(Address value)
            => value.Value;

        public static implicit operator Address(nint value)
            => new(value);
    }
}
