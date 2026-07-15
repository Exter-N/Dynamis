using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dynamis.ClientStructs;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.UI;
using Dynamis.Utility;

namespace Dynamis.Interop;

public sealed class ObjectInspector(
    DataYamlContainer dataYamlContainer,
    MemoryHeuristics memoryHeuristics,
    AddressIdentifier addressIdentifier,
    ModuleAddressResolver moduleAddressResolver,
    SymbolApi symbolApi,
    ClassRegistry classRegistry,
    Ipfd.Ipfd ipfd)
    : IMessageObserver<ConfigurationChangedMessage>
{
    private readonly ShortLivedCache<nint, nint?> _instancePointers = new();

    private unsafe T Read<T>(nint address, bool safe) where T : unmanaged
        => safe ? ipfd.Read<T>(address) : *(T*)address;

    public ObjectSnapshot TakeSnapshot(nint objectAddress, ClassInfo? @class = null,
        ClassIdentifier? classIdHint = null, string? name = null, bool safeReads = true)
    {
        var snapshot = TakeMinimalSnapshot(objectAddress, @class, classIdHint, safeReads);
        snapshot.Name = name;
        CompleteSnapshot(snapshot, safeReads);
        return snapshot;
    }

    public unsafe ObjectSnapshot TakeMinimalSnapshot(nint objectAddress, ClassInfo? @class = null,
        ClassIdentifier? classIdHint = null, bool safeReads = true)
    {
        nuint displacement = 0;
        if (@class is null) {
            var classAndDisplacement = DetermineClassAndDisplacement(objectAddress, null, classIdHint, safeReads);
            @class = classAndDisplacement.Class;
            displacement = classAndDisplacement.Displacement;
            objectAddress -= (nint)displacement;
        }

        var size = @class.EstimatedSize;
        if (size is 0 && VirtualMemory.GetProtection(objectAddress).CanRead()) {
            size = unchecked((uint)(MemoryHeuristics.NextPage(objectAddress) - objectAddress));
        }

        var data = new byte[size];
        if (safeReads) {
            ipfd.Copy<byte>(objectAddress, data.Length, data);
        } else {
            new ReadOnlySpan<byte>((void*)objectAddress, data.Length).CopyTo(data);
        }

        return new(data)
        {
            Address = objectAddress,
            Displacement = displacement,
            Class = @class,
        };
    }

    public void CompleteSnapshot(ObjectSnapshot snapshot, bool safeReads = true)
    {
        var colors = new byte[snapshot.Data.Length];
        Highlight(snapshot.Data, snapshot.Class, colors, safeReads);
        if (snapshot.Address is
            {
            } objectAddress) {
            snapshot.Name ??= addressIdentifier.Identify(objectAddress).Describe();
        }

        snapshot.HighlightColors = colors;
    }

    private static uint EstimateStackSize(nint stackPointer)
    {
        ProcessThreadApi.GetCurrentThreadStackLimits(out var stackLowLimit, out var stackHighLimit);
        return stackPointer >= stackLowLimit && stackPointer <= stackHighLimit
            ? (uint)(stackHighLimit - stackPointer).ToInt32()
            : (uint)(MemoryHeuristics.NextPage(stackPointer) - stackPointer).ToInt32();
    }

    public unsafe (uint ThreadId, ObjectSnapshot Context) TakeThreadStateSnapshot(ref readonly Context contextRecord)
    {
        var threadId = ProcessThreadApi.GetCurrentThreadId();
        var contextPointer = (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in contextRecord));
        var context = TakeMinimalSnapshot(contextPointer, classRegistry.FromManagedType(typeof(Context)), null, false);
        context.Name = $"Context of thread {threadId}";
        context.Live = false;

        var stackPointer = unchecked((nint)contextRecord.Rsp & -8);
        var stackDisplacement = unchecked((nuint)((nint)contextRecord.Rsp - stackPointer));
        var stack = TakeMinimalSnapshot(
            stackPointer,
            PseudoClasses.Generate(
                "<Thread Stack>",
                EstimateStackSize(stackPointer),
                PseudoClasses.Template.None,
                ClassKind.ThreadStack
            ),
            null,
            false
        );
        stack.Name = $"Stack of thread {threadId}";
        stack.Displacement += stackDisplacement;
        stack.Live = false;

        context.AssociatedSnapshot = stack;

        var reader = new SnapshotReader(null);
        reader.Mount(context);
        context.StackTrace = symbolApi.StackWalk(in contextRecord, reader);

        return (threadId, context);
    }

    public (ClassInfo Class, nuint Displacement) DetermineClassAndDisplacement(nint objectAddress,
        nint? vtblHint = null, ClassIdentifier? classIdHint = null, bool safeReads = true, bool allowProbing = false)
    {
        var protection = VirtualMemory.GetProtection(objectAddress);
        if (!protection.CanRead()) {
            return (new(), 0);
        }

        if (protection.CanExecute()) {
            var moduleAddress = moduleAddressResolver.Resolve(objectAddress);
            var displacement = moduleAddress?.SymbolName != null
                ? moduleAddress.Displacement
                : 0;
            return (classRegistry.GetFunctionClass(objectAddress - displacement, safeReads), (uint)displacement);
        }

        if ((objectAddress & (nint.Size - 1)) != 0) {
            // The object is not aligned on a void* boundary.
            // Return a dummy class that will contain the rest of the page.
            return (new(), 0);
        }

        var vtbl = vtblHint ?? Read<nint>(objectAddress, safeReads);
        var vtblProtection = vtbl is 0 ? default : VirtualMemory.GetProtection(vtbl);
        if ((vtbl & (nint.Size - 1)) == 0 && vtblProtection.CanExecute()
                                          && memoryHeuristics.EstimateSizeAndDisplacementFromDtor(vtbl) is
                                             {
                                             } ownerSize) {
            // objectAddress is actually a vtbl and vtbl is actually a dtor
            return (classRegistry.GetVirtualTableClass(objectAddress, ownerSize, safeReads), 0);
        }

        var classId = DetermineClassId(objectAddress, vtbl, classIdHint);
        if (classId.Kind is ClassIdentifierKind.ManagedType) {
            return (classRegistry.FromManagedType(classId.Type!), 0);
        }

        if (classId.Kind is ClassIdentifierKind.WellKnownObject or ClassIdentifierKind.WellKnownObjectByPointer) {
            return (classRegistry.GetClass(classId, vtbl, allowProbing ? objectAddress : null), 0);
        }

        if (vtbl is 0) {
            return (new(), 0);
        }

        if (vtblProtection.CanRead()) {
            var dtor = Read<nint>(vtbl, safeReads);
            var displacement = memoryHeuristics.EstimateDisplacementFromVfunc(dtor);
            if (displacement != 0) {
                var actual = DetermineClassAndDisplacement(
                    objectAddress - (nint)displacement, null, null, safeReads, allowProbing
                );
                return (actual.Class, actual.Displacement + displacement);
            }
        }

        return (classRegistry.GetClass(classId, vtbl, allowProbing ? objectAddress : null), 0);
    }

    private unsafe ClassIdentifier DetermineClassId(nint objectAddress, nint vtbl, ClassIdentifier? hint)
    {
        if (hint is
            {
                Kind: not ClassIdentifierKind.ManagedType,
            } hintValue) {
            return hintValue;
        }

        if (dataYamlContainer.Data is not null) {
            if (objectAddress != 0 && dataYamlContainer.ClassesByInstance!.ContainsKey(objectAddress)) {
                return ClassIdentifier.WellKnownObject(objectAddress);
            }

            if (vtbl != 0 && dataYamlContainer.ClassesByVtbl!.ContainsKey(vtbl)) {
                return ClassIdentifier.ObjectWithVirtualTable(vtbl);
            }

            if (objectAddress != 0) {
                bool foundPointer;
                lock (_instancePointers) {
                    foundPointer = _instancePointers.TryGetValue(objectAddress, out var pointer);
                    if (foundPointer && pointer.HasValue) {
                        return ClassIdentifier.WellKnownObjectByPointer(pointer.Value);
                    }
                }

                if (!foundPointer) {
                    foreach (var pointer in dataYamlContainer.ClassesByInstancePointer!.Keys) {
                        if (VirtualMemory.GetProtection(pointer).CanRead() && *(nint*)pointer == objectAddress) {
                            lock (_instancePointers) {
                                _instancePointers.TryAdd(objectAddress, pointer);
                            }

                            return ClassIdentifier.WellKnownObjectByPointer(pointer);
                        }
                    }

                    lock (_instancePointers) {
                        _instancePointers.TryAdd(objectAddress, null);
                    }
                }
            }
        }

        return hint ?? ClassIdentifier.ObjectWithVirtualTable(vtbl);
    }

    private void Highlight(ReadOnlySpan<byte> objectBytes, ClassInfo? classInfo, Span<byte> byteColors, bool safeReads = true)
    {
        if (classInfo is not null) {
            HighlightInstance(objectBytes, classInfo, byteColors, safeReads);
        }

        HighlightPointers(objectBytes, byteColors, safeReads, classInfo is not null && classInfo.EstimatedSize > 0);
    }

    private void HighlightInstance(ReadOnlySpan<byte> objectBytes, ClassInfo classInfo, Span<byte> byteColors, bool safeReads)
    {
        foreach (var fieldInfo in classInfo.Fields) {
            switch (fieldInfo.Type) {
                case FieldType.Boolean:
                case FieldType.Byte:
                case FieldType.SByte:
                case FieldType.UInt16:
                case FieldType.Int16:
                case FieldType.UInt32:
                case FieldType.Int32:
                case FieldType.UInt64:
                case FieldType.Int64:
                case FieldType.IntPtr:
                case FieldType.UIntPtr:
                    byteColors[(int)fieldInfo.Offset..(int)(fieldInfo.Offset + fieldInfo.Size)]
                       .Fill((byte)HexViewerColor.Integer);
                    break;
                case FieldType.Half:
                case FieldType.Single:
                case FieldType.Double:
                    byteColors[(int)fieldInfo.Offset..(int)(fieldInfo.Offset + fieldInfo.Size)]
                       .Fill((byte)HexViewerColor.Float);
                    break;
                case FieldType.ByteString:
                    for (var i = 0u; i < fieldInfo.Size; ++i) {
                        byteColors[(int)(fieldInfo.Offset + i)] = (byte)(objectBytes[(int)(fieldInfo.Offset + i)] == 0
                            ? HexViewerColor.Null
                            : HexViewerColor.Text);
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

                    break;
                case FieldType.Pointer:
                    for (var i = 0u; i < fieldInfo.Size; i += (uint)nint.Size) {
                        var value = MemoryMarshal.Read<nint>(
                            objectBytes[(int)(fieldInfo.Offset + i)..(int)(fieldInfo.Offset + i + nint.Size)]
                        );
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
                                color = (byte)GetClassColor(
                                    DetermineClassAndDisplacement(
                                            value, null, null, safeReads, classInfo.EstimatedSize > 0
                                        )
                                       .Class
                                );
                            }
                        }

                        byteColors[(int)(fieldInfo.Offset + i)..(int)(fieldInfo.Offset + i + nint.Size)].Fill(color);
                    }

                    break;
                case FieldType.CStringPointer:
                    for (var i = 0u; i < fieldInfo.Size; i += (uint)nint.Size) {
                        var value = MemoryMarshal.Read<nint>(
                            objectBytes[(int)(fieldInfo.Offset + i)..(int)(fieldInfo.Offset + i + nint.Size)]
                        );
                        byte color;
                        if (value == 0) {
                            color = (byte)HexViewerColor.Null;
                        } else {
                            var protect = VirtualMemory.GetProtection(value);
                            if (protect.CanRead()) {
                                color = (byte)HexViewerColor.Text;
                            } else {
                                color = (byte)HexViewerColor.BadPointer;
                            }
                        }

                        byteColors[(int)(fieldInfo.Offset + i)..(int)(fieldInfo.Offset + i + nint.Size)].Fill(color);
                    }

                    break;
            }

            if (fieldInfo.ElementClass is not null) {
                for (var elOffset = 0u; elOffset < fieldInfo.Size; elOffset += fieldInfo.ElementClass.EstimatedSize) {
                    HighlightInstance(
                        objectBytes[
                            (int)(fieldInfo.Offset + elOffset)..(int)(fieldInfo.Offset + elOffset
                              + fieldInfo.ElementClass.EstimatedSize)], fieldInfo.ElementClass,
                        byteColors[
                            (int)(fieldInfo.Offset + elOffset)..(int)(fieldInfo.Offset + elOffset
                              + fieldInfo.ElementClass.EstimatedSize)], safeReads
                    );
                }
            }
        }
    }

    private void HighlightPointers(ReadOnlySpan<byte> objectBytes, Span<byte> byteColors, bool safeReads,
        bool allowProbing)
    {
        for (var i = 0; i + nint.Size - 1 < objectBytes.Length; i += nint.Size) {
            if (MemoryMarshal.Read<nint>(byteColors[i..(i + nint.Size)]) != 0) {
                continue;
            }

            var value = MemoryMarshal.Read<nint>(objectBytes[i..(i + nint.Size)]);
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
                    color = (byte)GetClassColor(
                        DetermineClassAndDisplacement(value, null, null, safeReads, allowProbing).Class
                    );
                }
            }

            byteColors[i..(i + nint.Size)].Fill(color);
        }
    }

    private static HexViewerColor GetClassColor(ClassInfo @class)
        => @class.Kind switch
        {
            ClassKind.Function     => HexViewerColor.CodePointer,
            ClassKind.VirtualTable => HexViewerColor.VirtualTablePointer,
            _ => @class.IsClass
                ? HexViewerColor.ObjectPointer
                : string.IsNullOrEmpty(@class.DefiningModule)
                    ? HexViewerColor.Pointer
                    : HexViewerColor.LibraryObjectPointer,
        };

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (DataYamlContainer.IsDataYamlConfigurationChanged(message)) {
            lock (_instancePointers) {
                _instancePointers.Clear();
            }
        }
    }

    public void Tick()
    {
        lock (_instancePointers) {
            _instancePointers.Tick();
        }
    }
}
