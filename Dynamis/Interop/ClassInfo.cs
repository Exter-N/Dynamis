using Dynamis.ClientStructs;

namespace Dynamis.Interop;

public sealed class ClassInfo
{
    public string Name { get; set; } = string.Empty;

    public Type? ClientStructsType { get; set; }

    public Type[] ClientStructsParents { get; set; } = [];

    public DataYaml.Class? DataYamlClass { get; set; }

    public (string Name, DataYaml.Class Class)[] DataYamlParents { get; set; } = [];

    public uint  EstimatedSize         { get; set; }
    public uint? SizeFromDtor          { get; set; }
    public uint? SizeFromClientStructs { get; set; }
    public uint? SizeFromOuterContext  { get; set; }

    public FieldInfo[] Fields { get; set; } = [];

    public bool Known
        => Name.Length > 0 && !Name.StartsWith("Cls_");

    public bool IsClass
        => Known || SizeFromDtor.HasValue;
}
