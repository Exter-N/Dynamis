using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dynamis.ClientStructs;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.Utility;
using FFXIVClientStructs.STD;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop;

public sealed partial class ClassRegistry(
    ILogger<ClassRegistry> logger,
    MemoryHeuristics memoryHeuristics,
    AddressIdentifier addressIdentifier,
    Ipfd.Ipfd ipfd,
    DataYamlContainer dataYamlContainer)
    : IMessageObserver<ConfigurationChangedMessage>
{
    private readonly Dictionary<string, ClassInfo> _classCache = new();

    private unsafe T Read<T>(nint address, bool safe) where T : unmanaged
        => safe ? ipfd.Read<T>(address) : *(T*)address;

    public ClassInfo GetClass(string className, nint vtbl, uint restOfPageSize)
    {
        ClassInfo? classInfo;
        lock (_classCache) {
            if (_classCache.TryGetValue(className, out classInfo)) {
                return classInfo;
            }

            classInfo = new ClassInfo
            {
                Name = className,
            };

            PopulateFromVtbl(classInfo, vtbl, true);
            PopulateFromClientStructs(classInfo);
            PopulateAggregates(classInfo, restOfPageSize);

            _classCache.Add(className, classInfo);
        }

        return classInfo;
    }

    public ClassInfo FromClientStructs(Type type)
    {
        var typeName = type.FullName;
        if (typeName is null || type.Assembly != typeof(StdString).Assembly
                             || !typeName.StartsWith("FFXIVClientStructs.FFXIV.")) {
            throw new ArgumentException($"Invalid ClientStructs type {type}");
        }

        var className = typeName[25..].Replace(".", "::");
        ClassInfo? classInfo;
        lock (_classCache) {
            if (_classCache.TryGetValue(className, out classInfo)) {
                return classInfo;
            }

            classInfo = new ClassInfo
            {
                Name = className,
                ManagedType = type,
            };

            if (dataYamlContainer.Data?.Classes?.TryGetValue(className, out var dataClass) ?? false) {
                var vtbl = dataClass?.Vtbls?[0]?.Ea.Value;
                if (vtbl.HasValue) {
                    PopulateFromVtbl(classInfo, vtbl.Value, true);
                }
            }

            PopulateFromClientStructs(classInfo);
            PopulateAggregates(classInfo, (uint)Environment.SystemPageSize);

            _classCache.Add(className, classInfo);
        }

        return classInfo;
    }

    public ClassInfo FromManagedType(Type type)
    {
        var className = type.FullName;
        if (className is null) {
            throw new ArgumentException($"Invalid type {type}");
        }

        ClassInfo? classInfo;
        lock (_classCache) {
            if (_classCache.TryGetValue(className, out classInfo)) {
                return classInfo;
            }

            classInfo = new ClassInfo
            {
                Name = className,
                ManagedType = type,
            };

            PopulateFromManagedType(classInfo);
            PopulateAggregates(classInfo, (uint)Environment.SystemPageSize);

            _classCache.Add(className, classInfo);
        }

        return classInfo;
    }

    private void PopulateFromVtbl(ClassInfo classInfo, nint vtbl, bool safeReads)
    {
        var dtor = VirtualMemory.GetProtection(vtbl).CanRead() ? Read<nint>(vtbl, safeReads) : 0;
        classInfo.SizeFromDtor = memoryHeuristics.EstimateSizeFromDtor(dtor);

        if ((dataYamlContainer.Data?.Classes?.TryGetValue(classInfo.Name, out var @class) ?? false)
         && @class?.Vtbls is not null) {
            foreach (var vt in @class.Vtbls) {
                if (vt.Ea == vtbl) {
                    continue;
                }

                dtor = Read<nint>(vt.Ea.Value, safeReads);
                var sizeFromDtor = memoryHeuristics.EstimateSizeFromDtor(dtor);
                if (sizeFromDtor.HasValue && (!classInfo.SizeFromDtor.HasValue
                                           || classInfo.SizeFromDtor.Value < sizeFromDtor.Value)) {
                    classInfo.SizeFromDtor = sizeFromDtor;
                }
            }
        }
    }

    private void PopulateFromClientStructs(ClassInfo classInfo)
    {
        if (dataYamlContainer.Data is not null) {
            classInfo.DataYamlClass = dataYamlContainer.Data.Classes?.GetValueOrDefault(classInfo.Name);
            var parents = new List<(string, DataYaml.Class)>();
            var currentClass = classInfo.DataYamlClass;
            for (;;) {
                if (currentClass?.Vtbls is null || currentClass.Vtbls.Count == 0) {
                    break;
                }

                var parentName = currentClass.Vtbls[0]?.Base;
                if (parentName is null) {
                    break;
                }

                currentClass = dataYamlContainer.Data.Classes!.GetValueOrDefault(parentName);
                if (currentClass is null) {
                    break;
                }

                parents.Add((parentName, currentClass));
            }

            classInfo.DataYamlParents = parents.ToArray();
        }

        classInfo.ManagedType ??= ResolveClientStructsType(classInfo.Name);
        classInfo.ManagedParents = classInfo.DataYamlParents
                                            .Select(yamlClass => ResolveClientStructsType(yamlClass.Name))
                                            .OfType<Type>()
                                            .ToArray();

        PopulateFromManagedType(classInfo);
    }

    private void PopulateFromManagedType(ClassInfo classInfo)
    {
        if (classInfo.ManagedType is not null) {
            // Cannot use Marshal.SizeOf as it fails on certain types.
            classInfo.SizeFromManagedType = (uint)UnsafeSizeOf(classInfo.ManagedType);
            classInfo.Fields = GetFieldsFromManagedType(classInfo.ManagedType).ToArray();
            Array.Sort(classInfo.Fields);
        } else if (classInfo.ManagedParents.Length > 0) {
            classInfo.Fields = GetFieldsFromManagedType(classInfo.ManagedParents[0]).ToArray();
            Array.Sort(classInfo.Fields);
        }
    }

    private static Type? ResolveClientStructsType(string className)
        => typeof(StdString).Assembly.GetType("FFXIVClientStructs.FFXIV." + className.Replace("::", "."));

    private IEnumerable<FieldInfo> GetFieldsFromManagedType(Type type, uint offset = 0, string prefix = "",
        bool isInherited = false)
    {
        var inherited = GetCustomAttributes(type, "InheritsAttribute`1")
                       .Select(attr => attr.GetType().GetGenericArguments()[0].FullName)
                       .OfType<string>()
                       .ToArray();
        foreach (var reflField in
                 type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (GetCustomAttribute(reflField, "CExportIgnoreAttribute") is not null) {
                continue;
            }

            if (GetCustomAttribute(reflField, "FixedSizeArrayAttribute") is
                {
                } fsaAttribute) {
                var typeName = reflField.FieldType.Name;
                if (typeName.StartsWith("FixedSizeArray") && reflField.FieldType.IsGenericType) {
                    var elementType = reflField.FieldType.GetGenericArguments()[0];
                    var isString = (bool)((dynamic)fsaAttribute).IsString;
                    var elementFieldType = elementType.ToFieldType(isString);
                    FieldInfo field;
                    try {
                        field = new FieldInfo
                        {
                            Name = prefix + reflField.Name,
                            Offset = offset + (uint)OffsetOf(reflField),
                            Size = (uint)UnsafeSizeOf(reflField.FieldType),
                        };
                        if (elementFieldType.HasValue) {
                            field.Type = elementFieldType.Value;
                            field.EnumType = elementType.IsEnum ? elementType : null;
                        } else {
                            field.Type = FieldType.ObjectArray;
                            field.ElementClass = FromClientStructs(elementType);
                        }
                    } catch (Exception e) {
                        logger.LogError(
                            e, "Error while analyzing field {Field} of class {Class}", reflField.Name, type
                        );
                        continue;
                    }

                    yield return field;

                    continue;
                }
            }

            var fieldType = reflField.FieldType.ToFieldType();
            if (fieldType.HasValue) {
                if (!isInherited) {
                    FieldInfo field;
                    try {
                        field = new FieldInfo
                        {
                            Name = prefix + reflField.Name,
                            Offset = offset + (uint)OffsetOf(reflField),
                            Size =
                                (uint)(reflField.FieldType.IsPointer ? nint.Size : UnsafeSizeOf(reflField.FieldType)),
                            Type = fieldType.Value,
                            EnumType = reflField.FieldType.IsEnum ? reflField.FieldType : null,
                        };
                    } catch (Exception e) {
                        logger.LogError(
                            e, "Error while analyzing field {Field} of class {Class}", reflField.Name, type
                        );
                        continue;
                    }

                    yield return field;
                }

                continue;
            }

            if (reflField.FieldType.IsValueType) {
                var inheritanceField = IsInheritedField(reflField.FieldType, reflField.Name, type, inherited);
                if (!isInherited || inheritanceField) {
                    IEnumerable<FieldInfo> fields;
                    try {
                        fields = GetFieldsFromManagedType(
                            reflField.FieldType, offset + (uint)OffsetOf(reflField),
                            $"{prefix}{reflField.Name}.",
                            inheritanceField
                        );
                    } catch (Exception e) {
                        logger.LogError(
                            e, "Error while analyzing field {Field} of class {Class}", reflField.Name, type
                        );
                        continue;
                    }

                    foreach (var field in fields) {
                        yield return field;
                    }
                }
            }
        }
    }

    private static bool IsInheritedField(Type fieldType, string fieldName, Type declaringType,
        string[] inheritedTypeNames)
        => Array.IndexOf(inheritedTypeNames, fieldType.FullName) >= 0 && fieldName
         == (fieldType.Name == declaringType.Name ? $"{fieldType.Name}Base" : fieldType.Name);

    private static int UnsafeSizeOf(Type type)
        => (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(type).Invoke(null, null)!;

    private static nint OffsetOf(System.Reflection.FieldInfo field)
    {
        try {
            return Marshal.OffsetOf(field.ReflectedType!, field.Name);
        } catch (ArgumentException) {
            if (field.ReflectedType is not null && field.ReflectedType.IsExplicitLayout) {
                var fieldOffset = field.GetCustomAttribute<FieldOffsetAttribute>();
                if (fieldOffset is not null) {
                    return fieldOffset.Value;
                }
            }

            throw;
        }
    }

    private static Attribute? GetCustomAttribute(MemberInfo member, string attributeName)
        => member.GetCustomAttributes().FirstOrDefault(attribute => attribute.GetType().Name == attributeName);

    private static IEnumerable<Attribute> GetCustomAttributes(MemberInfo member, string attributeName)
        => member.GetCustomAttributes().Where(attribute => attribute.GetType().Name == attributeName);

    private static void PopulateAggregates(ClassInfo classInfo, uint restOfPageSize)
    {
        if (classInfo.SizeFromDtor.HasValue || classInfo.SizeFromManagedType.HasValue) {
            classInfo.EstimatedSize = Math.Max(classInfo.SizeFromDtor ?? 0, classInfo.SizeFromManagedType ?? 0);
        } else {
            classInfo.EstimatedSize = restOfPageSize;
        }
    }

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (message.IsPropertyChanged(nameof(Configuration.Configuration.DataYamlPath))) {
            lock (_classCache) {
                _classCache.Clear();
            }
        }
    }
}
