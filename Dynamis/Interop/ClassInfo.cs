using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using Dynamis.ClientStructs;
using Dynamis.Utility;

namespace Dynamis.Interop;

public sealed class ClassInfo
{
    public string Name { get; set; } = string.Empty;

    public string DefiningModule { get; set; } = string.Empty;

    public ClassKind Kind { get; set; } = ClassKind.Regular;

    public Type? ManagedType { get; set; }

    public Type[] ManagedParents { get; set; } = [];

    public Type? BestManagedType
        => ManagedType ?? (ManagedParents.Length > 0 ? ManagedParents[0] : null);

    public DataYaml.Class? DataYamlClass { get; set; }

    public (string Name, DataYaml.Class Class)[] DataYamlParents { get; set; } = [];

    public uint  EstimatedSize        { get; set; }
    public uint? SizeFromDtor         { get; set; }
    public uint? SizeFromManagedType  { get; set; }
    public uint? SizeFromOuterContext { get; set; }

    public ImmutableArray<FieldInfo> AllScalars { get; private set; } = [];
    public ImmutableArray<FieldInfo> Fields     { get; private set; } = [];

    public FrozenDictionary<string, FieldInfo> FieldsByName { get; private set; } =
        FrozenDictionary<string, FieldInfo>.Empty;

    public FrozenDictionary<string, (MethodInfo? Getter, MethodInfo? Setter)> PropertiesByName { get; private set; } =
        FrozenDictionary<string, (MethodInfo? Getter, MethodInfo? Setter)>.Empty;

    public FrozenDictionary<string, ImmutableArray<MethodInfo>> MethodsByName { get; private set; } =
        FrozenDictionary<string, ImmutableArray<MethodInfo>>.Empty;

    public bool Known
        => Name.Length > 0 && !Name.StartsWith("Cls_");

    public bool IsClass
        => Known || SizeFromDtor.HasValue;

    /// <remarks> Only applies if <see cref="Kind"/> is <see cref="ClassKind.VirtualTable"/>. </remarks>
    public (uint Size, nuint Displacement)? VtblOwnerSizeAndDisplacementFromDtor { get; set; }

    /// <remarks> Only applies if <see cref="Kind"/> is <see cref="ClassKind.Function"/>. </remarks>
    public FunctionInstruction[] FunctionBody { get; set; } = [];

    public override string ToString()
        => Name;

    public void SetFields(IEnumerable<FieldInfo> fields)
    {
        var fieldList = fields.ToList();
        fieldList.Sort();
        Fields = [..fieldList.Where(field => !field.Name.Contains('.') && !field.Name.Contains('[')),];
        FieldsByName = Fields.ToFrozenDictionary(field => field.Name, field => field);
        AllScalars = [..fieldList.Where(field => field.Type.IsScalar()),];
    }

    public void SetProperties(IEnumerable<(string Name, MethodInfo? Getter, MethodInfo? Setter)> properties)
    {
        PropertiesByName = properties.ToFrozenDictionary(
            property => property.Name, property => (property.Getter, property.Setter)
        );
    }

    public void SetMethods(IEnumerable<(string Name, MethodInfo Method)> methods)
    {
        MethodsByName = methods.GroupBy(method => method.Name)
                               .ToFrozenDictionary(
                                    group => group.Key,
                                    group => group.Select(method => method.Method).ToImmutableArray()
                                );
    }
}
