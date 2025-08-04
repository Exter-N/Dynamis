using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ReflFieldInfo = System.Reflection.FieldInfo;

namespace Dynamis.Interop;

// Parts from FFXIVClientStructs
public static class ReflectionExtensions
{
    private static readonly SearchValues<char> GenericTypePunctuation = SearchValues.Create(',', '<', '>');

    public static nint OffsetOf(this ReflFieldInfo field)
    {
        var t = field.ReflectedType;
        if (t is null) {
            throw new ArgumentException($"Given field {field} has no reflected type", nameof(field));
        }

        try {
            return Marshal.OffsetOf(t, field.Name);
        } catch (ArgumentException) {
            if (t.IsExplicitLayout) {
                var fieldOffset = field.GetCustomAttribute<FieldOffsetAttribute>();
                if (fieldOffset is not null) {
                    return fieldOffset.Value;
                }
            } else if (t.IsLayoutSequential) {
                return GetFieldOffsetSequential(field);
            }

            throw;
        }
    }

    public static Attribute? GetCustomAttribute(this MemberInfo member, string attributeName)
        => member.GetCustomAttributes().FirstOrDefault(attribute => attribute.GetType().Name == attributeName);

    public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo member, string attributeName)
        => member.GetCustomAttributes().Where(attribute => attribute.GetType().Name == attributeName);

    public static int SizeOf(this Type type)
        => (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(type).Invoke(null, null)!;

    private static int GetFieldOffsetSequential(ReflFieldInfo info)
    {
        if (info.DeclaringType is not
            {
            } declaring) {
            throw new Exception($"Unable to access declaring type of field {info.Name}");
        }

        var pack = declaring.StructLayoutAttribute?.Pack ?? 0; // Default to 0 if no pack is specified
        var fields = declaring.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var offset = 0;
        foreach (var field in fields) {
            if (pack != 0) {
                var actualPack = Math.Min(pack, field.FieldType.PackSize());
                offset = (offset + actualPack - 1) / actualPack * actualPack;
            }
            if (field == info) {
                return offset;
            }
            offset += field.FieldType.SizeOf();
        }
        throw new Exception($"Field {info} not found");
    }

    public static int PackSize(this Type type)
    {
        if (type.GetCustomAttribute("FixedSizeArrayAttribute") is not null) {
            // FixedSizeArrayAttribute is always packed to 1 as the generated struct gets generated with Pack = 1
            return 1;
        }

        if (!type.IsValueType) {
            return type.SizeOf();
        }

        var pack = type.StructLayoutAttribute?.Pack ?? 8;
        if (pack == 0) {
            pack = 8;
        }

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return fields.Max(t => Math.Min(pack, t.FieldType.PackSize()));
    }

    public static bool TryParseGenericType(this string fullType, out string baseType, out string[] typeArguments)
    {
        var typeArgumentsStart = fullType.IndexOf('<');
        if (typeArgumentsStart < 0) {
            baseType = fullType;
            typeArguments = [];
            return true;
        }

        baseType = fullType[..typeArgumentsStart].TrimEnd();
        if (!fullType.EndsWith('>')) {
            typeArguments = [];
            return false;
        }

        var typeArgumentsList = new List<string>();
        var end = fullType.Length - 1;
        var nesting = 0u;
        var argumentStart = typeArgumentsStart + 1;
        var punctuation = typeArgumentsStart;
        while ((punctuation = fullType.AsSpan(punctuation + 1).IndexOfAny(GenericTypePunctuation) + punctuation + 1)
            != end) {
            switch (fullType[punctuation]) {
                case '<':
                    ++nesting;
                    break;
                case '>':
                    if (nesting == 0) {
                        typeArguments = [];
                        return false;
                    }

                    --nesting;
                    break;
                case ',':
                    if (nesting == 0) {
                        typeArgumentsList.Add(fullType[argumentStart..punctuation].Trim());
                        argumentStart = punctuation + 1;
                    }

                    break;
                default:
                    throw new($"Character in {nameof(GenericTypePunctuation)} not handled by {nameof(TryParseGenericType)}");
            }
        }

        if (nesting != 0) {
            typeArguments = [];
            return false;
        }

        if (end > argumentStart) {
            var lastArgument = fullType[argumentStart..end].Trim();
            if (lastArgument.Length > 0) {
                typeArgumentsList.Add(lastArgument);
            }
        }

        typeArguments = typeArgumentsList.ToArray();
        return true;
    }
}
