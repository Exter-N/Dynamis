using System.Runtime.InteropServices;
using Dynamis.ClientStructs;
using Dynamis.Interop.Win32;
using Dynamis.UI;
using Microsoft.Extensions.Logging;

namespace Dynamis.Interop;

public sealed class ObjectInspector(
    DataYamlContainer dataYamlContainer,
    MemoryHeuristics memoryHeuristics,
    AddressIdentifier addressIdentifier,
    ModuleAddressResolver moduleAddressResolver,
    SymbolApi symbolApi,
    ClassRegistry classRegistry,
    Ipfd.Ipfd ipfd)
{
    private unsafe T Read<T>(nint address, bool safe) where T : unmanaged
        => safe ? ipfd.Read<T>(address) : *(T*)address;

    public ObjectSnapshot TakeSnapshot(nint objectAddress, ClassInfo? @class = null, string? name = null,
        bool safeReads = true)
    {
        var snapshot = TakeMinimalSnapshot(objectAddress, @class, safeReads);
        snapshot.Name = name;
        CompleteSnapshot(snapshot, safeReads);
        return snapshot;
    }

    public unsafe ObjectSnapshot TakeMinimalSnapshot(nint objectAddress, ClassInfo? @class = null, bool safeReads = true)
    {
        nuint displacement = 0;
        if (@class is null) {
            var classAndDisplacement = DetermineClassAndDisplacement(objectAddress, safeReads);
            @class = classAndDisplacement.Class;
            displacement = classAndDisplacement.Displacement;
            objectAddress -= (nint)displacement;
        }
        var data = new byte[@class.EstimatedSize];
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

    public unsafe (uint ThreadId, ObjectSnapshot Context) TakeThreadStateSnapshot(ExceptionPointers* exceptionInfo)
    {
        var threadId = ProcessThreadApi.GetCurrentThreadId();
        var context = TakeMinimalSnapshot(
            (nint)exceptionInfo->ContextRecord, classRegistry.FromManagedType(typeof(Context)), false
        );
        context.Name = $"Context of thread {threadId}";
        context.Live = false;

        var stackPointer = unchecked((nint)exceptionInfo->ContextRecord->Rsp & -8);
        var stackDisplacement = unchecked((nuint)((nint)exceptionInfo->ContextRecord->Rsp - stackPointer));
        ProcessThreadApi.GetCurrentThreadStackLimits(out var stackLowLimit, out var stackHighLimit);
        var stack = TakeMinimalSnapshot(
            stackPointer,
            PseudoClasses.Generate(
                "<Thread Stack>",
                stackPointer >= stackLowLimit && stackPointer <= stackHighLimit
                    ? (uint)(stackHighLimit - stackPointer).ToInt32()
                    : (uint)(MemoryHeuristics.NextPage(stackPointer) - stackPointer).ToInt32(),
                PseudoClasses.Template.None,
                ClassKind.ThreadStack
            ),
            false
        );
        stack.Name = $"Stack of thread {threadId}";
        stack.Displacement += stackDisplacement;
        stack.Live = false;

        context.AssociatedSnapshot = stack;

        var reader = new SnapshotReader(null);
        reader.Mount(context);
        context.StackTrace = symbolApi.StackWalk(*exceptionInfo->ContextRecord, reader);

        return (threadId, context);
    }

    public (ClassInfo Class, nuint Displacement) DetermineClassAndDisplacement(nint objectAddress, bool safeReads = true)
    {
        var protection = VirtualMemory.GetProtection(objectAddress);
        if (!protection.CanRead()) {
            return (new ClassInfo(), 0);
        }

        if (protection.CanExecute()) {
            var moduleAddress = moduleAddressResolver.Resolve(objectAddress);
            var displacement = moduleAddress?.SymbolName != null
                ? moduleAddress.Displacement
                : 0;
            return (classRegistry.GetFunctionClass(objectAddress - displacement, safeReads), (uint)displacement);
        }

        var restOfPageSize = (uint)(MemoryHeuristics.NextPage(objectAddress) - objectAddress).ToInt32();
        if ((objectAddress & (nint.Size - 1)) != 0) {
            // The object is not aligned on a void* boundary.
            // Return a dummy class that will contain the rest of the page.
            return (new ClassInfo
            {
                EstimatedSize = restOfPageSize,
            }, 0);
        }

        var vtbl = Read<nint>(objectAddress, safeReads);
        var vtblProtection = VirtualMemory.GetProtection(vtbl);
        if ((vtbl & (nint.Size - 1)) == 0 && vtblProtection.CanExecute()
                                          && memoryHeuristics.EstimateSizeAndDisplacementFromDtor(vtbl) is
                                             {
                                             } ownerSize) {
            // objectAddress is actually a vtbl and vtbl is actually a dtor
            return (classRegistry.GetVirtualTableClass(
                DetermineClassName(0, objectAddress).ClassName, objectAddress, ownerSize, safeReads
            ), 0);
        }

        if (vtblProtection.CanRead()) {
            var dtor = Read<nint>(vtbl, safeReads);
            var displacement = memoryHeuristics.EstimateDisplacementFromVfunc(dtor);
            if (displacement != 0) {
                var actual = DetermineClassAndDisplacement(objectAddress - (nint)displacement, safeReads);
                return (actual.Class, actual.Displacement + displacement);
            }
        }

        return (classRegistry.GetClass(DetermineClassName(objectAddress, vtbl).ClassName, vtbl, restOfPageSize), 0);
    }

    private unsafe DataYamlContainer.InstanceName DetermineClassName(nint objectAddress, nint vtbl)
    {
        if (dataYamlContainer.Data is not null) {
            if (objectAddress != 0 && dataYamlContainer.ClassesByInstance!.TryGetValue(objectAddress, out var name)) {
                return name;
            }

            if (vtbl != 0 && dataYamlContainer.ClassesByVtbl!.TryGetValue(vtbl, out var className)) {
                return new(className, null);
            }

            if (objectAddress != 0) {
                foreach (var (pointer, name2) in dataYamlContainer.ClassesByInstancePointer!) {
                    if (*(nint*)pointer == objectAddress) {
                        return name2;
                    }
                }
            }
        }

        return new($"Cls_{vtbl:X}", null);
    }

    private void Highlight(ReadOnlySpan<byte> objectBytes, ClassInfo? classInfo, Span<byte> byteColors, bool safeReads = true)
    {
        if (classInfo is not null) {
            HighlightInstance(objectBytes, classInfo, byteColors, safeReads);
        }

        HighlightPointers(objectBytes, byteColors, safeReads);
    }

    private void HighlightInstance(ReadOnlySpan<byte> objectBytes, ClassInfo classInfo, Span<byte> byteColors, bool safeReads)
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
                        var value = MemoryMarshal.Cast<byte, nint>(
                            objectBytes[(int)(fieldInfo.Offset + i)..(int)(fieldInfo.Offset + i + nint.Size)]
                        )[0];
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
                                color = (byte)GetClassColor(DetermineClassAndDisplacement(value, safeReads).Class);
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

    private void HighlightPointers(ReadOnlySpan<byte> objectBytes, Span<byte> byteColors, bool safeReads)
    {
        for (var i = 0; i + nint.Size - 1 < objectBytes.Length; i += nint.Size) {
            if (MemoryMarshal.Cast<byte, nint>(byteColors[i..(i + nint.Size)])[0] != 0) {
                continue;
            }

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
                    color = (byte)GetClassColor(DetermineClassAndDisplacement(value, safeReads).Class);
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
}
