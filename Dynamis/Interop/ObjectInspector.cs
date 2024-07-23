using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dynamis.ClientStructs;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.UI;
using Dynamis.Utility;
using FFXIVClientStructs.STD;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop;

public sealed class ObjectInspector : IMessageObserver<ConfigurationChangedMessage>
{
    private readonly DataYamlContainer        _dataYamlContainer;
    private readonly MemoryHeuristics         _memoryHeuristics;
    private readonly ILogger<ObjectInspector> _logger;

    private readonly Dictionary<string, ClassInfo> _classCache = new();

    public ObjectInspector(DataYamlContainer dataYamlContainer, MemoryHeuristics memoryHeuristics, ILogger<ObjectInspector> logger)
    {
        _dataYamlContainer = dataYamlContainer;
        _memoryHeuristics = memoryHeuristics;
        _logger = logger;
    }

    public unsafe ClassInfo DetermineClass(nint objectAddress)
    {
        if (!VirtualMemory.GetProtection(objectAddress).CanRead()) {
            return new ClassInfo();
        }

        var restOfPageSize = (uint)(MemoryHeuristics.NextPage(objectAddress) - objectAddress).ToInt32();
        if ((objectAddress & (nint.Size - 1)) != 0) {
            // The object is not aligned on a void* boundary.
            // Return a dummy class that will contain the rest of the page.
            return new ClassInfo
            {
                EstimatedSize = restOfPageSize,
            };
        }

        var vtbl = *(nint*)objectAddress.ToPointer();
        var className = DetermineClassName(objectAddress, vtbl);
        if (_classCache.TryGetValue(className, out var classInfo)) {
            return classInfo;
        }

        classInfo = new ClassInfo
        {
            Name = className,
        };

        PopulateFromVtbl(classInfo, vtbl);
        PopulateFromClientStructs(classInfo);
        PopulateAggregates(classInfo, restOfPageSize);

        _classCache.Add(className, classInfo);
        return classInfo;
    }

    public unsafe (AddressType Type, string Name) IdentifyAddress(nint address)
    {
        if (_dataYamlContainer.Data is not null) {
            if (_dataYamlContainer.ClassesByInstance!.TryGetValue(address, out var className)) {
                return (AddressType.Instance, className);
            }

            if (_dataYamlContainer.ClassesByVtbl!.TryGetValue(address, out className)) {
                return (AddressType.Vtbl, className);
            }

            foreach (var (pointer, clsName) in _dataYamlContainer.ClassesByInstancePointer!) {
                if (*(nint*)pointer.ToPointer() == address) {
                    return (AddressType.Instance, clsName);
                }
            }
        }

        return (AddressType.None, string.Empty);
    }

    public ClassInfo FromClientStructs(Type type)
    {
        var typeName = type.FullName;
        if (typeName is null || !typeName.StartsWith("FFXIVClientStructs.FFXIV.")) {
            throw new ArgumentException($"Invalid type {type}");
        }

        var className = typeName.Substring(25).Replace(".", "::");
        if (_classCache.TryGetValue(className, out var classInfo)) {
            return classInfo;
        }

        classInfo = new ClassInfo
        {
            Name = className,
            ClientStructsType = type,
        };

        if (_dataYamlContainer.Data?.Classes?.TryGetValue(className, out var dataClass) ?? false) {
            var vtbl = dataClass?.Vtbls?[0]?.Ea.Value;
            if (vtbl.HasValue) {
                PopulateFromVtbl(classInfo, vtbl.Value);
            }
        }

        PopulateFromClientStructs(classInfo);
        PopulateAggregates(classInfo, (uint)Environment.SystemPageSize);

        _classCache.Add(className, classInfo);
        return classInfo;
    }

    private unsafe string DetermineClassName(nint objectAddress, nint vtbl)
    {
        if (_dataYamlContainer.Data is not null) {
            if (_dataYamlContainer.ClassesByInstance!.TryGetValue(objectAddress, out var className)
             || _dataYamlContainer.ClassesByVtbl!.TryGetValue(vtbl, out className)) {
                return className;
            }

            foreach (var (pointer, clsName) in _dataYamlContainer.ClassesByInstancePointer!) {
                if (*(nint*)pointer.ToPointer() == objectAddress) {
                    return clsName;
                }
            }
        }

        return $"Cls_{vtbl:X}";
    }

    public void Highlight(ReadOnlySpan<byte> objectBytes, ClassInfo classInfo, Span<byte> byteColors, bool nested = false)
    {
        foreach (var fieldInfo in classInfo.Fields) {
            switch (fieldInfo.Type) {
                case FieldType.Byte:
                case FieldType.SByte:
                case FieldType.UInt16:
                case FieldType.Int16:
                case FieldType.UInt32:
                case FieldType.Int32:
                case FieldType.UInt64:
                case FieldType.Int64:
                    byteColors[(int)fieldInfo.Offset..(int)(fieldInfo.Offset + fieldInfo.Size)].Fill((byte)HexViewerColor.Integer);
                    break;
                case FieldType.Half:
                case FieldType.Single:
                case FieldType.Double:
                    byteColors[(int)fieldInfo.Offset..(int)(fieldInfo.Offset + fieldInfo.Size)].Fill((byte)HexViewerColor.Float);
                    break;
                case FieldType.ByteString:
                    for (var i = 0u; i < fieldInfo.Size; ++i) {
                        byteColors[(int)(fieldInfo.Offset + i)] = (byte)(objectBytes[(int)(fieldInfo.Offset + i)] == 0 ? HexViewerColor.Null : HexViewerColor.Text);
                    }
                    break;
                case FieldType.Char:
                case FieldType.CharString:
                    for (var i = 0u; i < fieldInfo.Size; i += 2) {
                        var color = (byte)(objectBytes[(int)(fieldInfo.Offset + i)] == 0
                                        && objectBytes[(int)(fieldInfo.Offset + i + 1)] == 0
                            ? HexViewerColor.Null
                            : HexViewerColor.Text);
                        byteColors[(int)(fieldInfo.Offset + i)] = color;
                        byteColors[(int)(fieldInfo.Offset + i + 1)] = color;
                    }
                    byteColors[(int)fieldInfo.Offset..(int)(fieldInfo.Offset + fieldInfo.Size)].Fill((byte)HexViewerColor.Text);
                    break;
                case FieldType.Pointer:
                    for (var i = 0u; i < fieldInfo.Size; i += (uint)nint.Size) {
                        var value = MemoryMarshal.Cast<byte, nint>(objectBytes[(int)(fieldInfo.Offset + i)..(int)(fieldInfo.Offset + i + nint.Size)])[0];
                        byte color;
                        if (value == 0) {
                            color = (byte)HexViewerColor.Null;
                        } else {
                            var protect = VirtualMemory.GetProtection(value);
                            if (protect.CanExecute()) {
                                color = (byte)HexViewerColor.CodePointer;
                            } else if (!protect.CanRead()) {
                                color = (byte)HexViewerColor.BadPointer;
                            } else {
                                color = (byte)(DetermineClass(value).Known ? HexViewerColor.ObjectPointer : HexViewerColor.Pointer);
                            }
                        }
                        byteColors[(int)(fieldInfo.Offset + i)..(int)(fieldInfo.Offset + i + nint.Size)].Fill(color);
                    }
                    break;
            }

            if (fieldInfo.ElementClass is not null) {
                for (var elOffset = 0u; elOffset < fieldInfo.Size; elOffset += fieldInfo.ElementClass.EstimatedSize) {
                    Highlight(
                        objectBytes[
                            (int)(fieldInfo.Offset + elOffset)..(int)(fieldInfo.Offset + elOffset
                              + fieldInfo.ElementClass.EstimatedSize)], fieldInfo.ElementClass,
                        byteColors[
                            (int)(fieldInfo.Offset + elOffset)..(int)(fieldInfo.Offset + elOffset
                              + fieldInfo.ElementClass.EstimatedSize)], true
                    );
                }
            }
        }

        if (!nested) {
            for (var i = 0; i + nint.Size - 1 < objectBytes.Length; i += nint.Size) {
                if (MemoryMarshal.Cast<byte, nint>(byteColors[i..(i + nint.Size)])[0] == 0) {
                    var value = MemoryMarshal.Cast<byte, nint>(objectBytes[i..(i + nint.Size)])[0];
                    byte color;
                    if (value == 0) {
                        color = (byte)HexViewerColor.Null;
                    } else {
                        var protect = VirtualMemory.GetProtection(value);
                        if (protect.CanExecute()) {
                            color = (byte)HexViewerColor.CodePointer;
                        } else if (!protect.CanRead()) {
                            color = (byte)HexViewerColor.Default;
                        } else {
                            color = (byte)(DetermineClass(value).Known ? HexViewerColor.ObjectPointer : HexViewerColor.Pointer);
                        }
                    }
                    byteColors[i..(i + nint.Size)].Fill(color);
                }
            }
        }
    }

    private unsafe void PopulateFromVtbl(ClassInfo classInfo, nint vtbl)
    {
        var dtor = VirtualMemory.GetProtection(vtbl).CanRead() ? *(nint*)vtbl.ToPointer() : 0;
        classInfo.SizeFromDtor = _memoryHeuristics.EstimateSizeFromDtor(dtor);

        if ((_dataYamlContainer.Data?.Classes?.TryGetValue(classInfo.Name, out var @class) ?? false)
         && @class?.Vtbls is not null) {
            foreach (var vt in @class.Vtbls) {
                if (vt is null) {
                    continue;
                }

                if (vt.Ea != vtbl) {
                    dtor = *(nint*)vt.Ea.Value.ToPointer();
                    var sizeFromDtor = _memoryHeuristics.EstimateSizeFromDtor(dtor);
                    if (sizeFromDtor.HasValue && (!classInfo.SizeFromDtor.HasValue
                                               || classInfo.SizeFromDtor.Value < sizeFromDtor.Value)) {
                        classInfo.SizeFromDtor = sizeFromDtor;
                    }
                }
            }
        }
    }

    private void PopulateFromClientStructs(ClassInfo classInfo)
    {
        if (_dataYamlContainer.Data is not null) {
            classInfo.DataYamlClass = _dataYamlContainer.Data.Classes?.GetValueOrDefault(classInfo.Name);
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

                currentClass = _dataYamlContainer.Data.Classes!.GetValueOrDefault(parentName);
                if (currentClass is null) {
                    break;
                }

                parents.Add((parentName, currentClass));
            }

            classInfo.DataYamlParents = parents.ToArray();
        }

        classInfo.ClientStructsType ??= ResolveClientStructsType(classInfo.Name);
        if (classInfo.ClientStructsType is not null) {
            // Cannot use Marshal.SizeOf as it fails on certain types.
            classInfo.SizeFromClientStructs = (uint)UnsafeSizeOf(classInfo.ClientStructsType);
            classInfo.Fields = GetFieldsFromClientStructs(classInfo.ClientStructsType).ToArray();
            Array.Sort(classInfo.Fields);
        }

        classInfo.ClientStructsParents = classInfo.DataYamlParents
                                                  .Select(yamlClass => ResolveClientStructsType(yamlClass.Name))
                                                  .OfType<Type>()
                                                  .ToArray();
        if (classInfo.ClientStructsType is null && classInfo.ClientStructsParents.Length > 0) {
            classInfo.Fields = GetFieldsFromClientStructs(classInfo.ClientStructsParents[0]).ToArray();
            Array.Sort(classInfo.Fields);
        }
    }

    private Type? ResolveClientStructsType(string className)
        => typeof(StdString).Assembly.GetType("FFXIVClientStructs.FFXIV." + className.Replace("::", "."));

    private IEnumerable<FieldInfo> GetFieldsFromClientStructs(Type type, uint offset = 0, string prefix = "", bool isInherited = false)
    {
        var inherited = GetCustomAttributes(type, "InheritsAttribute`1")
                       .Select(attr => attr.GetType().GetGenericArguments()[0].FullName)
                       .OfType<string>()
                       .ToArray();
        foreach (var reflField in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            if (GetCustomAttribute(reflField, "CExportIgnoreAttribute") is not null) {
                continue;
            }

            if (GetCustomAttribute(reflField, "FixedSizeArrayAttribute") is {} fsaAttribute) {
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
                        _logger.LogError(e, "Error while analyzing field {Field} of class {Class}", reflField.Name, type);
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
                        _logger.LogError(e, "Error while analyzing field {Field} of class {Class}", reflField.Name, type);
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
                        fields = GetFieldsFromClientStructs(
                            reflField.FieldType, offset + (uint)OffsetOf(reflField),
                            $"{prefix}{reflField.Name}.",
                            inheritanceField
                        );
                    } catch (Exception e) {
                        _logger.LogError(e, "Error while analyzing field {Field} of class {Class}", reflField.Name, type);
                        continue;
                    }

                    foreach (var field in fields) {
                        yield return field;
                    }
                }
            }
        }
    }

    private static bool IsInheritedField(Type fieldType, string fieldName, Type declaringType, string[] inheritedTypeNames)
        => Array.IndexOf(inheritedTypeNames, fieldType.FullName) >= 0 && fieldName == (fieldType.Name == declaringType.Name ? $"{fieldType.Name}Base" : fieldType.Name);

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

    private void PopulateAggregates(ClassInfo classInfo, uint restOfPageSize)
    {
        if (classInfo.SizeFromDtor.HasValue || classInfo.SizeFromClientStructs.HasValue) {
            classInfo.EstimatedSize = Math.Max(classInfo.SizeFromDtor ?? 0, classInfo.SizeFromClientStructs ?? 0);
        } else {
            classInfo.EstimatedSize = restOfPageSize;
        }
    }

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (message.IsPropertyChanged(nameof(Configuration.Configuration.DataYamlPath))) {
            _classCache.Clear();
        }
    }
}
