using System.Runtime.InteropServices;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.Utility;
using ImGuiNET;

namespace Dynamis.UI.Components;

public sealed class SnapshotViewer(
    ConfigurationContainer configuration,
    MessageHub messageHub,
    ObjectInspector objectInspector,
    ContextMenu contextMenu,
    ImGuiComponents imGuiComponents)
{
    private ObjectSnapshot? _vmSnapshot;

    public ObjectSnapshot? Snapshot
    {
        get => _vmSnapshot;
        set => _vmSnapshot = value;
    }

    public void Draw()
    {
        if (_vmSnapshot is null) {
            return;
        }

        ImGuiComponents.DrawHexViewer(
            "snapshot", _vmSnapshot.Data, _vmSnapshot.HighlightColors,
            configuration.Configuration.GetHexViewerPalette(), OnSnapshotHover
        );
    }

    private void OnSnapshotHover(int offset, bool printable, bool clicked)
    {
        if (_vmSnapshot is null) {
            return;
        }

        var path = GetValuePath(_vmSnapshot.Class, (uint)offset);
        if (path.Path.Length == 0) {
            var ptrOffset = offset & -nint.Size;
            if (ptrOffset + nint.Size <= _vmSnapshot.Data.Length) {
                var pointer =
                    MemoryMarshal.Cast<byte, nint>(_vmSnapshot.Data.AsSpan(ptrOffset..(ptrOffset + nint.Size)))[0];
                if (VirtualMemory.GetProtection(pointer).CanRead()) {
                    path = new((uint)ptrOffset, (uint)nint.Size, $"Unk_{ptrOffset:X}", FieldType.Pointer, null);
                }
            }
        }

        if (path.Path.Length == 0) {
            return;
        }

        using var _ = ImRaii.Tooltip();
        var valueSpan = _vmSnapshot.Data.AsSpan((int)path.Offset..(int)(path.Offset + path.Size));
        var value = path.Type.Read(valueSpan);
        ImGui.TextUnformatted($"{path.Type.Description()} {path.Path} @ 0x{path.Offset:X}");
        ImGui.Separator();
        ImGui.TextUnformatted(ToString(value, path));

        if (path.Type == FieldType.Pointer && (nint)value != 0) {
            ImGui.Separator();
            imGuiComponents.DrawPointerTooltipDetails((nint)value, null);
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Click for options.");

        if (clicked) {
            contextMenu.Open(
                new FieldContextMenu(messageHub, objectInspector, path, value, _vmSnapshot.Address + (nint)path.Offset)
            );
        }
    }

    private static ValuePath GetValuePath(ClassInfo? @class, uint offset)
    {
        var field = @class?.Fields
                           .LastOrDefault(field => offset >= field.Offset && offset < field.Offset + field.Size);
        if (field is null) {
            return ValuePath.Default;
        }

        var elementSize = field.ElementSize;
        if (field.Type is FieldType.ByteString or FieldType.CharString) {
            elementSize = field.Size;
        }

        var retOffset = field.Offset;
        var path = field.Name;
        if (field.Size > elementSize) {
            var i = (offset - retOffset) / elementSize;
            retOffset += i * elementSize;
            path = $"{path}[{i}]";
        }

        if (field.ElementClass is null) {
            return new(retOffset, elementSize, path, field.Type, field.EnumType);
        }

        var subPath = GetValuePath(field.ElementClass, offset - retOffset);
        if (subPath.Path.Length == 0) {
            return subPath;
        }

        return subPath with
        {
            Offset = retOffset + subPath.Offset,
            Path = $"{path}.{subPath.Path}",
        };
    }

    private static string ToString(object value, ValuePath path)
    {
        if (path.EnumType is not null) {
            var enumName = Enum.GetName(path.EnumType, value);
            if (enumName is not null) {
                return $"{path.EnumType.Name}.{enumName} = {value}";
            }
        }

        if (path.Type == FieldType.Pointer) {
            return $"0x{value:X}";
        }

        return path.Type.IsInteger()
            ? $"{value} (0x{value:X})"
            : $"{value}";
    }

    private readonly record struct ValuePath(uint Offset, uint Size, string Path, FieldType Type, Type? EnumType)
    {
        public static readonly ValuePath Default = new(0, 0, string.Empty, FieldType.Byte, null);
    }

    private sealed class FieldContextMenu(
        MessageHub messageHub,
        ObjectInspector objectInspector,
        ValuePath path,
        object value,
        nint? ea) : IDrawable
    {
        private readonly string?    _enumName = path.EnumType is not null ? Enum.GetName(path.EnumType, value) : null;

        private readonly ClassInfo? _class = path.Type == FieldType.Pointer && (nint)value != 0
            ? objectInspector.DetermineClass((nint)value)
            : null;

        public bool Draw()
        {
            var ret = false;
            if (path.Type == FieldType.Pointer && (nint)value != 0) {
                if (_class is not null && _class.Kind == ClassKind.VirtualTable) {
                    if (ImGui.Selectable("Inspect virtual table")) {
                        messageHub.Publish(new InspectObjectMessage((nint)value, _class));
                        ret = true;
                    }

                    if (ea is
                        {
                        } eoAddress && ImGui.Selectable("Inspect embedded object")) {
                        messageHub.Publish(new InspectObjectMessage(eoAddress, null));
                        ret = true;
                    }
                } else {
                    if (ImGui.Selectable("Inspect object")) {
                        messageHub.Publish(new InspectObjectMessage((nint)value, _class));
                        ret = true;
                    }
                }

                ImGui.Separator();
            }

            if (_enumName is not null && ImGui.Selectable($"Copy {_enumName}")) {
                ImGui.SetClipboardText(_enumName);
                ret = true;
            }

            if (path.Type == FieldType.Pointer) {
                if (ImGui.Selectable($"Copy {value:X}")) {
                    ImGui.SetClipboardText($"{value:X}");
                    ret = true;
                }
            } else if (path.Type.IsInteger() && $"{value:X}" != $"{value}") {
                if (ImGui.Selectable($"Copy {value:X}")) {
                    ImGui.SetClipboardText($"{value:X}");
                    ret = true;
                }
                if (ImGui.Selectable($"Copy {value}")) {
                    ImGui.SetClipboardText($"{value}");
                    ret = true;
                }
            } else {
                if (ImGui.Selectable($"Copy {value}")) {
                    ImGui.SetClipboardText($"{value}");
                    ret = true;
                }
            }

            ImGui.Separator();
            if (ea is
                {
                } address) {
                if (ImGui.Selectable("Copy effective address")) {
                    ImGui.SetClipboardText(address.ToString("X"));
                    ret = true;
                }
            }

            if (ImGui.Selectable("Copy field offset")) {
                ImGui.SetClipboardText(path.Offset.ToString("X"));
                ret = true;
            }

            if (ImGui.Selectable("Copy field path")) {
                ImGui.SetClipboardText(path.Path);
                ret = true;
            }

            return ret;
        }
    }
}
