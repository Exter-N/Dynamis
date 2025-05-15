using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
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
    ClassRegistry classRegistry,
    ModuleAddressResolver moduleAddressResolver,
    ContextMenu contextMenu,
    ImGuiComponents imGuiComponents)
{
    private ObjectSnapshot? _vmSnapshot;

    private bool _vmAnnotated = configuration.Configuration.OpenSnapshotsAnnotated
                             ?? configuration.Configuration.LastSnapshotAnnotated;

    private int _vmOffset;

    public ObjectSnapshot? Snapshot
    {
        get => _vmSnapshot;
        set => _vmSnapshot = value;
    }

    public void DrawHeader()
    {
        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var offset = (ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) * 0.5f;
        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0.0f, offset));
        ImGui.TextUnformatted("Display Mode");

        ImGui.SameLine(0.0f, itemInnerSpacing);
        ImGui.SetCursorPos(ImGui.GetCursorPos() - new Vector2(0.0f, offset));
        if (ImGui.RadioButton("Compact", !_vmAnnotated)) {
            _vmAnnotated = false;
            configuration.Configuration.LastSnapshotAnnotated = false;
            configuration.Save(nameof(configuration.Configuration.LastSnapshotAnnotated));
        }

        ImGui.SameLine(0.0f, itemInnerSpacing);
        ImGui.SetCursorPos(ImGui.GetCursorPos() - new Vector2(0.0f, offset));
        if (ImGui.RadioButton("Annotated", _vmAnnotated)) {
            _vmAnnotated = true;
            configuration.Configuration.LastSnapshotAnnotated = true;
            configuration.Save(nameof(configuration.Configuration.LastSnapshotAnnotated));
        }
    }

    public void Draw()
    {
        if (_vmSnapshot is null) {
            return;
        }

        ImGuiComponents.DrawHexViewer(
            "snapshot", _vmSnapshot.Data, _vmSnapshot.HighlightColors,
            configuration.Configuration.GetHexViewerPalette(), _vmAnnotated ? nint.Size : int.MaxValue, OnSnapshotHover,
            _vmAnnotated ? AnnotateSnapshotRow : null
        );
    }

    public void Draw(Range range)
    {
        if (_vmSnapshot is null) {
            return;
        }

        var offset = _vmOffset;
        try {
            _vmOffset = range.Start.GetOffset(_vmSnapshot.Data.Length);
            ImGuiComponents.DrawHexViewer(
                "snapshot", _vmSnapshot.Data.AsSpan(range),
                _vmSnapshot.HighlightColors is not null
                    ? _vmSnapshot.HighlightColors.AsSpan(range)
                    : ReadOnlySpan<byte>.Empty,
                configuration.Configuration.GetHexViewerPalette(), _vmAnnotated ? nint.Size : int.MaxValue,
                OnSnapshotHover, _vmAnnotated ? AnnotateSnapshotRow : null
            );
        } finally {
            _vmOffset = offset;
        }
    }

    private void OnSnapshotHover(int offset, ImGuiComponents.HexViewerPart part, bool clicked)
    {
        if (_vmSnapshot is null) {
            return;
        }

        offset += _vmOffset;
        var path = GetValuePath(_vmSnapshot.Class, (uint)offset);
        if (path.Path.Length == 0) {
            var ptrOffset = offset & -nint.Size;
            if (ptrOffset + nint.Size <= _vmSnapshot.Data.Length) {
                var pointer =
                    MemoryMarshal.Read<nint>(_vmSnapshot.Data.AsSpan(ptrOffset, nint.Size));
                if (VirtualMemory.GetProtection(pointer).CanRead()) {
                    path = new((uint)ptrOffset, (uint)nint.Size, $"Unk_{ptrOffset:X}", FieldType.Pointer, null, 0, null);
                }
            }
        }

        if (path.Path.Length == 0) {
            return;
        }

        using var _ = ImRaii.Tooltip();
        var value = path.Read(_vmSnapshot.Data);
        ImGui.TextUnformatted($"{path.Type.Description()} {path.Path} @ 0x{path.Offset:X}");
        ImGui.Separator();
        ImGui.TextUnformatted(ToString(value, path));

        if (path.Type == FieldType.Pointer && FieldInfo.GetAddress(value) != 0) {
            ImGui.Separator();
            imGuiComponents.DrawPointerTooltipDetails(
                FieldInfo.GetAddress(value),
                FieldInfo.DetermineClassAndDisplacement(value, objectInspector, classRegistry)?.Class
            );
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Click for options.");

        if (clicked) {
            contextMenu.Open(
                new FieldContextMenu(
                    messageHub, objectInspector, classRegistry, path, value,
                    path.Type == FieldType.Pointer ? moduleAddressResolver.Resolve(FieldInfo.GetAddress(value)) : null,
                    _vmSnapshot.Address + (nint)path.Offset
                )
            );
        }
    }

    private int? AnnotateSnapshotRow(int offset, int length)
    {
        if (length <= 0) {
            return null;
        }

        var fields = _vmSnapshot?.Class?.AllScalars.Where(field
            => offset <= field.Offset && (uint)(offset + length) >= field.Offset + field.Size
        ) ?? [];

        int? clicked = null;
        var palette = configuration.Configuration.GetHexViewerPalette();
        var first = true;
        foreach (var field in fields) {
            var elementCount = field.ElementCount;
            var firstElement = true;
            for (var i = 0u; i < elementCount; ++i) {
                var elementOffset = field.Offset + i * field.ElementSize;
                var annotation = GetValueAnnotation(elementOffset, field.ElementSize, field);
                if (string.IsNullOrEmpty(annotation)) {
                    continue;
                }

                if (firstElement) {
                    if (first) {
                        ImGui.SameLine();
                        first = false;
                    } else {
                        ImGui.TextUnformatted(" - ");
                        ImGui.SameLine(0.0f, 0.0f);
                    }

                    ImGui.TextUnformatted($"{field.Name.AfterLast('.')}: ");
                    firstElement = false;
                } else {
                    ImGui.TextUnformatted(", ");
                    ImGui.SameLine(0.0f, 0.0f);
                }

                ImGui.SameLine(0.0f, 0.0f);
                using (ImRaii.PushColor(
                           ImGuiCol.Text, palette[_vmSnapshot!.HighlightColors?[elementOffset] ?? 0]
                       )) {
                    ImGui.TextUnformatted(annotation);
                }

                if (HandleHover(unchecked((int)elementOffset))) {
                    clicked = unchecked((int)elementOffset);
                }

                ImGui.SameLine(0.0f, 0.0f);
            }
        }

        if (!first || _vmSnapshot?.HighlightColors is null) {
            return clicked;
        }

        var highlight = _vmSnapshot.HighlightColors[offset];
        for (var i = 1; i < length; ++i) {
            if (_vmSnapshot.HighlightColors[offset + i] != highlight) {
                return clicked;
            }
        }

        if (!((HexViewerColor)highlight).IsPointer()) {
            return clicked;
        }

        var ptrAnnotation = GetValueAnnotation(unchecked((uint)offset), unchecked((uint)length), null);
        if (!string.IsNullOrEmpty(ptrAnnotation)) {
            ImGui.SameLine();
            ImGui.TextUnformatted($"Unk_{offset:X}: ");
            ImGui.SameLine(0.0f, 0.0f);
            using (ImRaii.PushColor(ImGuiCol.Text, palette[highlight])) {
                ImGui.TextUnformatted(ptrAnnotation);
            }

            if (HandleHover(offset)) {
                clicked = offset;
            }

            ImGui.SameLine(0.0f, 0.0f);
        }

        return clicked;

        bool HandleHover(int fieldOffset)
        {
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            if (!ImGui.IsMouseHoveringRect(min, max)) {
                return false;
            }

            ImGui.SameLine(0.0f, 0.0f);
            ImGui.SetCursorScreenPos(min);
            var itemClicked = ImGui.InvisibleButton($"###A{fieldOffset:X}", max - min);
            OnSnapshotHover(fieldOffset, ImGuiComponents.HexViewerPart.Annotation, itemClicked);

            return itemClicked;
        }
    }

    private string GetValueAnnotation(uint offset, uint size, FieldInfo? field)
    {
        if (_vmSnapshot is null) {
            return string.Empty;
        }

        var valueSpan = _vmSnapshot.Data.AsSpan(unchecked((int)offset), unchecked((int)size));
        object value;
        FieldType type;
        if (field is not null) {
            value = _vmSnapshot.Class!.GetFieldValue(
                field, _vmSnapshot.Data, (offset - field.Offset) / field.ElementSize
            );
            if (field.ManagedType is not null && field.ManagedType.IsEnum) {
                value = Enum.GetName(field.ManagedType, value) ?? value;
            }

            type = field.Type;
        } else {
            value = MemoryMarshal.Read<nint>(valueSpan);
            type = FieldType.Pointer;
        }

        return GetAnnotation(value, type).ReplaceLineEndings("¶");

        string GetAnnotation(object value, FieldType type)
        {
            if (type is not FieldType.Pointer) {
                return value.ToString() ?? string.Empty;
            }

            var pointer = FieldInfo.GetAddress(value);
            if (pointer == 0) {
                return "nullptr";
            }

            var @class = FieldInfo.DetermineClassAndDisplacement(value, objectInspector, classRegistry)?.Class;
            if (@class is null) {
                return string.Empty;
            }

            if (@class.Known) {
                return @class.Name;
            }

            var builder = new StringBuilder();
            builder.Append($"{@class.EstimatedSize} (0x{@class.EstimatedSize:X}) bytes");
            if (!string.IsNullOrEmpty(@class.DefiningModule)) {
                builder.Append($", defined in {@class.DefiningModule}");
            }

            return builder.ToString();
        }
    }

    private static ValuePath GetValuePath(ClassInfo? @class, uint offset)
    {
        var field = @class?.AllScalars
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
            return new(retOffset, elementSize, path, field.Type, @class, 0, field);
        }

        var subPath = GetValuePath(field.ElementClass, offset - retOffset);
        if (subPath.Path.Length == 0) {
            return subPath;
        }

        return subPath with
        {
            Offset = retOffset + subPath.Offset,
            Path = $"{path}.{subPath.Path}",
            OffsetToClass = retOffset + subPath.OffsetToClass,
        };
    }

    private static string ToString(object value, ValuePath path)
    {
        if (path.Field?.ManagedType is not null && path.Field.ManagedType.IsEnum) {
            var enumName = Enum.GetName(path.Field.ManagedType, value);
            if (enumName is not null) {
                return $"{path.Field.ManagedType.Name}.{enumName} = {value}";
            }
        }

        if (path.Type == FieldType.Pointer) {
            if (value is DynamicMemory memory) {
                return $"0x{memory.Address:X}";
            }

            return $"0x{value:X}";
        }

        return path.Type.IsInteger()
            ? $"{value} (0x{value:X})"
            : $"{value}";
    }

    private readonly record struct ValuePath(
        uint Offset,
        uint Size,
        string Path,
        FieldType Type,
        ClassInfo? Class,
        uint OffsetToClass,
        FieldInfo? Field)
    {
        public static readonly ValuePath Default = new(0, 0, string.Empty, FieldType.Byte, null, 0, null);

        public object Read(ReadOnlySpan<byte> instance)
        {
            if (Class is not null && Field is not null) {
                instance = instance.Slice(unchecked((int)OffsetToClass), unchecked((int)Class.EstimatedSize));
                return Class.GetFieldValue(Field, instance, (Offset - Field.Offset - OffsetToClass) / Field.ElementSize);
            }

            return Type.Read(instance.Slice(unchecked((int)Offset), unchecked((int)Size)));
        }
    }

    private sealed class FieldContextMenu(
        MessageHub messageHub,
        ObjectInspector objectInspector,
        ClassRegistry classRegistry,
        ValuePath path,
        object value,
        ModuleAddress? moduleAddress,
        nint? ea) : IDrawable
    {
        private readonly string? _enumName =
            path.Field?.ManagedType is not null && path.Field.ManagedType.IsEnum
                ? Enum.GetName(path.Field.ManagedType, value)
                : null;

        private readonly (ClassInfo Class, nuint Displacement)? _class =
            path.Type == FieldType.Pointer
                ? FieldInfo.DetermineClassAndDisplacement(value, objectInspector, classRegistry)
                : null;

        public bool Draw()
        {
            var ret = false;
            if (path.Type == FieldType.Pointer && FieldInfo.GetAddress(value) != 0) {
                if (_class is
                    {
                    } @class && @class.Class.Kind == ClassKind.VirtualTable) {
                    if (ImGui.Selectable("Inspect virtual table")) {
                        messageHub.Publish(new InspectObjectMessage(FieldInfo.GetAddress(value), @class.Class));
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
                        messageHub.Publish(
                            new InspectObjectMessage(FieldInfo.GetAddress(value) - (nint)(_class?.Displacement ?? 0), _class?.Class)
                        );
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
                if (ImGui.Selectable($"Copy {FieldInfo.GetAddress(value):X}")) {
                    ImGui.SetClipboardText($"{FieldInfo.GetAddress(value):X}");
                    ret = true;
                }
            } else if (path.Type == FieldType.CStringPointer && value is CStringSnapshot str) {
                if (ImGui.Selectable($"Copy {str.Value}")) {
                    ImGui.SetClipboardText($"{str.Value}");
                    ret = true;
                }

                if (ImGui.Selectable($"Copy {str.Address:X}")) {
                    ImGui.SetClipboardText($"{str.Address:X}");
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

            if (moduleAddress is not null) {
                if (ImGui.Selectable($"Copy {moduleAddress}")) {
                    ImGui.SetClipboardText(moduleAddress.ToString());
                    ret = true;
                }

                if (moduleAddress.OriginalAddress != 0
                 && (!FieldInfo.TryGetAddress(value, out var pointer) || moduleAddress.OriginalAddress != pointer)
                 && ImGui.Selectable($"Copy original address ({moduleAddress.OriginalAddress:X})")) {
                    ImGui.SetClipboardText(moduleAddress.OriginalAddress.ToString("X"));
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
