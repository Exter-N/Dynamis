using Dynamis.ClientStructs;

namespace Dynamis.Interop;

public sealed class ClassInfo
{
    public string Name { get; set; } = string.Empty;

    public Type? ClientStructsType { get; set; }

    public DataYaml.Class? DataYamlClass { get; set; }

    public uint  EstimatedSize         { get; set; }
    public uint? SizeFromDtor          { get; set; }
    public uint? SizeFromClientStructs { get; set; }
}
