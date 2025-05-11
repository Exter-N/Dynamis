using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dynamis.ClientStructs;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.Utility;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop;

public sealed partial class ClassRegistry(
    ILogger<ClassRegistry> logger,
    MemoryHeuristics memoryHeuristics,
    AddressIdentifier addressIdentifier,
    ModuleAddressResolver moduleAddressResolver,
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
                DefiningModule = GetVtblDefiningModule(vtbl, true),
            };

            PopulateFromVtbl(classInfo, vtbl, true);
            PopulateFromClientStructs(classInfo);
            PopulateAggregates(classInfo, restOfPageSize);

            _classCache.Add(className, classInfo);
        }

        return classInfo;
    }

    public static bool TryGetClientStructsClassName(Type type, out string className)
    {
        var typeName = type.FullName;
        if (typeName is null || type.Assembly != typeof(StdString).Assembly) {
            className = string.Empty;
            return false;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>)) {
            var pointedType = type.GetGenericArguments()[0];
            if (!TryGetClientStructsClassName(pointedType, out var pointedTypeName)) {
                pointedTypeName = pointedType.FullName;
            }

            if (pointedTypeName is null) {
                className = string.Empty;
                return false;
            }

            className = pointedTypeName + "*";
            return true;
        }

        className = typeName[25..].Replace(".", "::");
        return true;
    }

    public ClassInfo FromManagedType(Type type)
    {
        var isClientStruct = TryGetClientStructsClassName(type, out var className);
        if (!isClientStruct) {
            className = type.FullName;
            if (className is null) {
                throw new ArgumentException($"Invalid type {type}");
            }
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

            if (isClientStruct) {
                if (dataYamlContainer.Data?.Classes?.TryGetValue(className, out var dataClass) ?? false) {
                    var vtbl = dataClass.Vtbls?[0].Ea;
                    if (vtbl.HasValue) {
                        PopulateFromVtbl(classInfo, dataYamlContainer.GetLiveAddress(vtbl.Value), true);
                    }
                }

                PopulateFromClientStructs(classInfo);
            } else {
                PopulateFromManagedType(classInfo);
            }

            PopulateAggregates(classInfo, (uint)Environment.SystemPageSize);

            _classCache.Add(className, classInfo);
        }

        return classInfo;
    }

    private void PopulateFromVtbl(ClassInfo classInfo, nint vtbl, bool safeReads)
    {
        var dtor = VirtualMemory.GetProtection(vtbl).CanRead() ? Read<nint>(vtbl, safeReads) : 0;
        var sizeFromDtor0 = memoryHeuristics.EstimateSizeAndDisplacementFromDtor(dtor);
        classInfo.SizeFromDtor = sizeFromDtor0.HasValue ? sizeFromDtor0.Value.Size : null;

        if ((dataYamlContainer.Data?.Classes?.TryGetValue(classInfo.Name, out var @class) ?? false)
         && @class.Vtbls is not null) {
            foreach (var vt in @class.Vtbls) {
                var otherVtbl = dataYamlContainer.GetLiveAddress(vt.Ea);
                if (otherVtbl == vtbl) {
                    continue;
                }

                dtor = VirtualMemory.GetProtection(otherVtbl).CanRead() ? Read<nint>(otherVtbl, safeReads) : 0;
                var sizeFromDtor = memoryHeuristics.EstimateSizeAndDisplacementFromDtor(dtor);
                if (sizeFromDtor.HasValue && (!classInfo.SizeFromDtor.HasValue
                                           || classInfo.SizeFromDtor.Value < sizeFromDtor.Value.Size)) {
                    classInfo.SizeFromDtor = sizeFromDtor.Value.Size;
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

                var parentName = currentClass.Vtbls[0].Base;
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
        Type managedType;
        if (classInfo.ManagedType is not null) {
            // Cannot use Marshal.SizeOf as it fails on certain types.
            classInfo.SizeFromManagedType = (uint)classInfo.ManagedType.SizeOf();
            managedType = classInfo.ManagedType;
        } else if (classInfo.ManagedParents.Length > 0) {
            managedType = classInfo.ManagedParents[0];
        } else {
            return;
        }

        classInfo.SetFields(GetFieldsFromManagedType(managedType));
        classInfo.SetProperties(GetPropertiesFromManagedType(managedType));
        classInfo.SetMethods(GetMethodsFromManagedType(managedType));
    }

    private static Type? ResolveClientStructsType(string className)
        => typeof(StdString).Assembly.GetType("FFXIVClientStructs.FFXIV." + className.Replace("::", "."));

    private IEnumerable<FieldInfo> GetFieldsFromManagedType(Type type, uint offset = 0, string prefix = "",
        bool isInherited = false)
    {
        var inherited = type.GetCustomAttributes("InheritsAttribute`1")
                       .Select(attr => attr.GetType().GetGenericArguments()[0].FullName)
                       .OfType<string>()
                       .ToArray();
        foreach (var reflField in
                 type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (reflField.GetCustomAttribute("CExportIgnoreAttribute") is not null) {
                continue;
            }

            if (reflField.GetCustomAttribute("FixedSizeArrayAttribute") is
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
                            Offset = offset + (uint)reflField.OffsetOf(),
                            Size = (uint)reflField.FieldType.SizeOf(),
                            ManagedType = elementType,
                        };
                        if (elementFieldType.HasValue) {
                            field.Type = elementFieldType.Value;
                        } else {
                            field.Type = FieldType.ObjectArray;
                            field.ElementClass = FromManagedType(elementType);
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
                            Offset = offset + (uint)reflField.OffsetOf(),
                            Size =
                                (uint)(reflField.FieldType.IsPointer ? nint.Size : reflField.FieldType.SizeOf()),
                            Type = fieldType.Value,
                            ManagedType = reflField.FieldType,
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
                    FieldInfo? objField = null;
                    IEnumerable<FieldInfo> fields;
                    try {
                        var objOffset = offset + (uint)reflField.OffsetOf();
                        if (!fieldType.HasValue && !inheritanceField) {
                            objField = new FieldInfo
                            {
                                Name = prefix + reflField.Name,
                                Offset = objOffset,
                                Size = (uint)reflField.FieldType.SizeOf(),
                                Type = FieldType.Object,
                                ElementClass = FromManagedType(reflField.FieldType),
                            };
                        }
                        fields = GetFieldsFromManagedType(
                            reflField.FieldType, objOffset, $"{prefix}{reflField.Name}.", inheritanceField
                        );
                    } catch (Exception e) {
                        logger.LogError(
                            e, "Error while analyzing field {Field} of class {Class}", reflField.Name, type
                        );
                        continue;
                    }

                    if (objField is not null) {
                        yield return objField;
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

    private IEnumerable<(string Name, MethodInfo? Getter, MethodInfo? Setter)> GetPropertiesFromManagedType(Type type)
    {
        if (!type.IsValueType) {
            yield break;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (property.GetIndexParameters().Length > 0) {
                continue;
            }

            var getter = property.GetGetMethod() is
            {
            } get
                ? StructPointerThunkGenerator.GeneratePointerThunk(get)
                : null;
            var setter = property.GetSetMethod() is
            {
            } set
                ? StructPointerThunkGenerator.GeneratePointerThunk(set)
                : null;
            yield return (property.Name, getter, setter);
        }
    }

    private IEnumerable<(string Name, MethodInfo Method)> GetMethodsFromManagedType(Type type)
    {
        if (!type.IsValueType) {
            yield break;
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
            if (method.IsSpecialName) {
                continue;
            }

            yield return (method.Name, StructPointerThunkGenerator.GeneratePointerThunk(method));
        }
    }

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
