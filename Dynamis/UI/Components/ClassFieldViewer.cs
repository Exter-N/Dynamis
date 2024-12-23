using Dynamis.Interop;
using Dynamis.Utility;
using ImGuiNET;

namespace Dynamis.UI.Components;

public sealed class ClassFieldViewer
{
    private ObjectSnapshot? _vmSnapshot;
    private bool            _vmHexIntegers;

    public ObjectSnapshot? Snapshot
    {
        get => _vmSnapshot;
        set => _vmSnapshot = value;
    }

    public void DrawHeader()
    {
        ImGui.Checkbox("Show Integers as Hex", ref _vmHexIntegers);
    }

    public void Draw(nint baseAddress, bool writable)
    {
        foreach (var field in _vmSnapshot!.Class!.Fields) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.4f);
            DrawField(baseAddress + (nint)field.Offset, string.Empty, field, writable);
        }
    }

    private unsafe bool DrawField(nint address, string prefix, FieldInfo field, bool writable)
    {
        var elementSize = (int)field.ElementSize;

        string CalculateSingleLabel()
            => $"{prefix}{field.Name} @ 0x{field.Offset:X}";

        string CalculateElementLabel(int i)
            => $"{prefix}{field.Name}[{i}] @ 0x{(field.Offset + i * elementSize):X}";

        var count = (int)(field.Size / elementSize);
        switch (field.Type) {
            case FieldType.Byte:
            case FieldType.SByte:
            case FieldType.UInt16:
            case FieldType.Int16:
            case FieldType.UInt32:
            case FieldType.Int32:
            case FieldType.UInt64:
            case FieldType.Int64:
            case FieldType.UIntPtr:
            case FieldType.IntPtr:
            case FieldType.Single:
            case FieldType.Double:
            case FieldType.Pointer:
                var (dataType, format) = field.Type.ToImGui(_vmHexIntegers);
                if (count > 1) {
                    var anyChanged = false;
                    var itemWidth = ImGui.CalcItemWidth();
                    for (var i = 0; i < count; ++i) {
                        ImGui.SetNextItemWidth(itemWidth);
                        anyChanged |= ImGui.InputScalar(
                            CalculateElementLabel(i), dataType, address + i * elementSize, 0, 0, format,
                            writable ? 0 : ImGuiInputTextFlags.ReadOnly
                        );
                    }

                    return anyChanged;
                }

                return ImGui.InputScalar(
                    CalculateSingleLabel(), dataType, address, 0, 0, format,
                    writable ? 0 : ImGuiInputTextFlags.ReadOnly
                );
            case FieldType.Boolean:
                bool boolValue;
                if (count > 1) {
                    var anyChanged = false;
                    for (var i = 0; i < count; ++i) {
                        boolValue = *(byte*)(address + i * elementSize) != 0;
                        if (ImGui.Checkbox(CalculateElementLabel(i), ref boolValue) && writable) {
                            *(byte*)(address + i * elementSize) = boolValue ? (byte)1 : (byte)0;
                            anyChanged = true;
                        }
                    }

                    return anyChanged;
                }
                boolValue = *(byte*)address != 0;
                if (ImGui.Checkbox(CalculateSingleLabel(), ref boolValue) && writable) {
                    *(byte*)address = boolValue ? (byte)1 : (byte)0;
                    return true;
                }

                return false;
            case FieldType.Char:
                if (count > 1) {
                    var anyChanged = false;
                    var itemWidth = ImGui.CalcItemWidth();
                    for (var i = 0; i < count; ++i) {
                        ImGui.SetNextItemWidth(itemWidth);
                        anyChanged |= ImGuiComponents.InputText(
                            CalculateElementLabel(i), new((void*)(address + i * elementSize), 1), false,
                            writable ? 0 : ImGuiInputTextFlags.ReadOnly
                        );
                    }

                    return anyChanged;
                }

                return ImGuiComponents.InputText(
                    CalculateSingleLabel(), new((void*)address, 1), false, writable ? 0 : ImGuiInputTextFlags.ReadOnly
                );
            case FieldType.Half:
                float floatValue;
                if (count > 1) {
                    var anyChanged = false;
                    var itemWidth = ImGui.CalcItemWidth();
                    for (var i = 0; i < count; ++i) {
                        ImGui.SetNextItemWidth(itemWidth);
                        floatValue = (float)*(Half*)(address + i * elementSize);
                        if (ImGui.InputFloat(
                                CalculateElementLabel(i), ref floatValue, 0.0f, 0.0f,
                                FieldType.Single.ToImGui(false).CFormat,
                                writable ? 0 : ImGuiInputTextFlags.ReadOnly
                            )) {
                            *(Half*)(address + i * elementSize) = (Half)floatValue;
                            anyChanged = true;
                        }
                    }

                    return anyChanged;
                }

                floatValue = (float)*(Half*)address;
                if (ImGui.InputFloat(
                        CalculateSingleLabel(), ref floatValue, 0.0f, 0.0f, FieldType.Single.ToImGui(false).CFormat,
                        writable ? 0 : ImGuiInputTextFlags.ReadOnly
                    )) {
                    *(Half*)address = (Half)floatValue;
                    return true;
                }

                return false;
            case FieldType.ByteString:
                return ImGui.InputText(
                    CalculateSingleLabel(), address, field.Size, writable ? 0 : ImGuiInputTextFlags.ReadOnly
                );
            case FieldType.CharString:
                return ImGuiComponents.InputText(
                    CalculateSingleLabel(), new((void*)address, (int)count), true,
                    writable ? 0 : ImGuiInputTextFlags.ReadOnly
                );
            case FieldType.ObjectArray when field.ElementClass is not null:
                if (count > 1) {
                    var anyChanged = false;
                    var itemWidth = ImGui.CalcItemWidth();
                    for (var i = 0; i < count; ++i) {
                        var elementPrefix = $"{prefix}{field.Name}[{i}].";
                        foreach (var subField in field.ElementClass.Fields) {
                            ImGui.SetNextItemWidth(itemWidth);
                            anyChanged |= DrawField(
                                address + i * elementSize + (nint)subField.Offset, elementPrefix, subField, writable
                            );
                        }
                    }

                    return anyChanged;
                }

                var oaAnyChanged = false;
                var oaItemWidth = ImGui.CalcItemWidth();
                var oaPrefix = $"{prefix}{field.Name}.";
                foreach (var subField in field.ElementClass.Fields) {
                    ImGui.SetNextItemWidth(oaItemWidth);
                    oaAnyChanged |= DrawField(address + (nint)subField.Offset, oaPrefix, subField, writable);
                }

                return oaAnyChanged;
            default:
                ImGui.TextUnformatted(
                    $"{CalculateSingleLabel()}: Non-editable field type {field.Type}{(count > 1 ? $"[{count}]" : string.Empty)}"
                );
                return false;
        }
    }
}
