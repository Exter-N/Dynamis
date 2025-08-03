using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
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

    public uint  EstimatedSize       { get; set; }
    public uint? SizeFromDtor        { get; set; }
    public uint? SizeFromManagedType { get; set; }
    public uint? SizeFromContext     { get; set; }

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

    public object GetFieldValues(FieldInfo field, ReadOnlySpan<byte> instance)
    {
        var elementCount = field.ElementCount;
        if (elementCount == 0) {
            return Array.Empty<object>();
        }

        if (elementCount == 1) {
            return GetFieldValue(field, instance, 0);
        }

        return field.Type.ReadAll(instance.Slice(unchecked((int)field.Offset), unchecked((int)field.Size)));
    }

    public object GetFieldValue(FieldInfo field, ReadOnlySpan<byte> instance, uint elementIndex)
    {
        var elementOffset = elementIndex * field.ElementSize;
        if (elementOffset + field.ElementSize > field.Size) {
            throw new ArgumentOutOfRangeException(nameof(elementIndex));
        }

        var element = instance.Slice(unchecked((int)(field.Offset + elementOffset)), unchecked((int)field.ElementSize));
        if (elementIndex > 0 || field.Size != field.ElementSize) {
            return field.Type.Read(element);
        }

        return TryGetExtendedFieldValue(field, instance, element, out var value)
            ? value
            : field.Type.Read(element);
    }

    private bool TryGetExtendedFieldValue(FieldInfo field, ReadOnlySpan<byte> instance, ReadOnlySpan<byte> span,
        [MaybeNullWhen(false)] out object value)
    {
        if (!FieldsByName.TryGetValue(field.Name, out var fieldCheck) || !ReferenceEquals(fieldCheck, field)) {
            value = null;
            return false;
        }

        if (field.Type is not FieldType.Pointer) {
            value = null;
            return false;
        }

        var pointer = MemoryMarshal.Read<nint>(span);
        if (TryGetSuffixedProperty(field.Name, "Span", out var spanProperty)) {
            if (spanProperty.Getter is not null) {
                var csPointer = DynamicStructBox.WrapCsPointer(BestManagedType!, instance.GetAddress());
                if (DynamicMemory.TryFrom(spanProperty.Getter.Invoke(null, [csPointer,]), out var memory)) {
                    if (memory.Address == pointer) {
                        value = memory;
                        return true;
                    }
                }
            }
        }

        value = null;
        return false;
    }

    private bool TryGetSuffixedProperty(string name, string suffix, out (MethodInfo? Getter, MethodInfo? Setter) property)
    {
        if (PropertiesByName.TryGetValue(name + suffix, out property)) {
            return true;
        }

        foreach (var singular in name.Singularize()) {
            if (PropertiesByName.TryGetValue(singular + suffix, out property)) {
                return true;
            }
        }

        return false;
    }

    public void SetFields(IEnumerable<FieldInfo> fields)
    {
        var fieldList = fields.ToList();
        fieldList.Sort();
        Fields = [..fieldList.Where(field => !field.Name.Contains('.') && !field.Name.Contains('[')),];
        FieldsByName = Fields.ToFrozenDictionary(field => field.Name, field => field);
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
