using Dynamis.ClientStructs;

namespace Dynamis.Interop;

public sealed class ClassInfo
{
    public string Name { get; set; } = string.Empty;

    public ClassKind Kind { get; set; } = ClassKind.Regular;

    public Type? ManagedType { get; set; }

    public Type[] ManagedParents { get; set; } = [];

    public DataYaml.Class? DataYamlClass { get; set; }

    public (string Name, DataYaml.Class Class)[] DataYamlParents { get; set; } = [];

    public uint  EstimatedSize        { get; set; }
    public uint? SizeFromDtor         { get; set; }
    public uint? SizeFromManagedType  { get; set; }
    public uint? SizeFromOuterContext { get; set; }

    public FieldInfo[] Fields { get; set; } = [];

    public bool Known
        => Name.Length > 0 && !Name.StartsWith("Cls_");

    public bool IsClass
        => Known || SizeFromDtor.HasValue;

    /// <remarks> Only applies if <see cref="Kind"/> is <see cref="ClassKind.VirtualTable"/>. </remarks>
    public (uint Size, nuint Displacement)? VtblOwnerSizeAndDisplacementFromDtor { get; set; }

    /// <remarks> Only applies if <see cref="Kind"/> is <see cref="ClassKind.Function"/>. </remarks>
    public FunctionInstruction[] FunctionBody { get; set; } = [];
}
