using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.UI.ObjectInspectors;
using Dynamis.Utility;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindow : Window
{
    private readonly ILogger<ObjectInspectorWindowFactory> _logger;
    private readonly WindowSystem                          _windowSystem;
    private readonly ImGuiComponents                       _imGuiComponents;
    private readonly ObjectInspector                       _objectInspector;
    private readonly ConfigurationContainer                _configuration;
    private readonly MessageHub                            _messageHub;
    private readonly Lazy<ObjectInspectorDispatcher>       _objectInspectorDispatcher;

    private readonly Dictionary<Type, object> _vmCustomState = [];

    private nint       _vmAddress = 0;
    private int        _vmStatus  = 0;
    private ClassInfo? _vmClass;
    private byte[]     _vmSnapshot = [];
    private byte[]     _vmColors   = [];

    public ObjectInspectorWindow(ILogger<ObjectInspectorWindowFactory> logger, WindowSystem windowSystem,
        ImGuiComponents imGuiComponents, ObjectInspector objectInspector, ConfigurationContainer configuration,
        MessageHub messageHub, Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher, int index) : base(
        $"Dynamis - Object Inspector##{index}", 0
    )
    {
        _logger = logger;
        _windowSystem = windowSystem;
        _imGuiComponents = imGuiComponents;
        _objectInspector = objectInspector;
        _configuration = configuration;
        _messageHub = messageHub;
        _objectInspectorDispatcher = objectInspectorDispatcher;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(float.MaxValue, float.MaxValue),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public void Inspect(nint address, ClassInfo? @class)
    {
        _vmAddress = address;
        RunInspection(@class);
    }

    public T GetCustomState<T>() where T : class, new()
    {
        if (_vmCustomState.TryGetValue(typeof(T), out var customState) && customState is T state) {
            return state;
        }

        state = new T();
        _vmCustomState.Add(typeof(T), state);

        return state;
    }

    private void RunInspection(ClassInfo? @class)
    {
        _vmClass = @class;
        try {
            _vmClass ??= _objectInspector.DetermineClass(_vmAddress);
            _vmSnapshot = new byte[_vmClass.EstimatedSize];
            Marshal.Copy(_vmAddress, _vmSnapshot, 0, _vmSnapshot.Length);
            _vmColors = new byte[_vmClass.EstimatedSize];
            _objectInspector.Highlight(_vmSnapshot, _vmClass, _vmColors);
            _vmStatus = 1;
        } catch (Exception e) {
            _logger.LogError(e, "Object inspection failed for address 0x{Address:X}", _vmAddress);
            _vmClass = null;
            _vmSnapshot = [];
            _vmColors = [];
            _vmStatus = 2;
        }
    }

    public override void Draw()
    {
        if (ImGuiComponents.InputPointer("Object Address", ref _vmAddress, ImGuiInputTextFlags.EnterReturnsTrue)) {
            RunInspection(null);
        }

        switch (_vmStatus) {
            case 1:
                DrawSnapshot();
                break;
            case 2:
                ImGui.TextUnformatted("Error");
                break;
        }
    }

    private void DrawSnapshot()
    {
        ImGui.TextUnformatted($"Class Name: {_vmClass!.Name}");
        using (var indent = ImRaii.PushIndent()) {
            foreach (var parent in _vmClass.DataYamlParents) {
                ImGui.TextUnformatted($"Parent: {parent.Name}");
                indent.Push();
            }
        }
        ImGui.TextUnformatted($"In ClientStructs: {_vmClass.ClientStructsType != null}");
        ImGui.TextUnformatted($"Estimated Size: {_vmClass.EstimatedSize} (0x{_vmClass.EstimatedSize:X}) bytes");
        var sizeIsFromDtor = _vmClass.SizeFromDtor.HasValue
                          && _vmClass.SizeFromDtor.Value == _vmClass.EstimatedSize;
        var sizeIsFromCs = _vmClass.SizeFromClientStructs.HasValue
                        && _vmClass.SizeFromClientStructs.Value == _vmClass.EstimatedSize;
        var sizeIsFromCtx = _vmClass.SizeFromOuterContext.HasValue
                         && _vmClass.SizeFromOuterContext.Value == _vmClass.EstimatedSize;
        if (sizeIsFromCtx) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.ParsedBlue!.Value)) {
                ImGui.TextUnformatted("(from outer context)");
            }
        } else if (sizeIsFromDtor && sizeIsFromCs) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.ParsedGreen!.Value)) {
                ImGui.TextUnformatted("(from both ClientStructs and dtor)");
            }
        } else if (!sizeIsFromDtor && !sizeIsFromCs) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                ImGui.TextUnformatted("(no valid source - using rest of page)");
            }
        } else {
            if (sizeIsFromDtor) {
                ImGui.SameLine();
                ImGui.TextUnformatted("(from dtor)");
            }

            if (sizeIsFromCs) {
                ImGui.SameLine();
                ImGui.TextUnformatted("(from ClientStructs)");
            }
        }

        if (_vmClass.SizeFromClientStructs.HasValue
         && _vmClass.SizeFromClientStructs.Value != _vmClass.EstimatedSize) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudYellow!.Value)) {
                ImGui.TextUnformatted(
                    $"Size from ClientStructs: {_vmClass.SizeFromClientStructs.Value} (0x{_vmClass.SizeFromClientStructs.Value:X})"
                );
            }
        }

        if (_vmClass.SizeFromDtor.HasValue && _vmClass.SizeFromDtor.Value != _vmClass.EstimatedSize) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudYellow!.Value)) {
                ImGui.TextUnformatted(
                    $"Size from dtor: {_vmClass.SizeFromDtor.Value} (0x{_vmClass.SizeFromDtor.Value:X})"
                );
            }
        }

        var inspectors = _objectInspectorDispatcher.Value.GetInspectors(_vmClass).ToList();

        foreach (var inspector in inspectors) {
            inspector.DrawAdditionalHeaderDetails(_vmAddress, this);
        }

        using var tabs = ImRaii.TabBar("###inspectorTabs");
        if (!tabs) {
            return;
        }

        using (var tab = ImRaii.TabItem("Memory View")) {
            if (tab) {
                using var _ = ImRaii.Child("###memoryView", -Vector2.One);
                ImGuiComponents.DrawHexViewer(
                    _vmSnapshot, _vmColors, _configuration.Configuration.GetHexViewerPalette(), OnSnapshotHover
                );
            }
        }

        foreach (var inspector in inspectors) {
            inspector.DrawAdditionalTabs(_vmAddress, this);
        }
    }

    private void OnSnapshotHover(int offset, bool printable)
    {
        var clicked = ImGui.IsItemClicked();
        var rightClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);

        var path = GetValuePath(_vmClass, (uint)offset);
        if (path.Path.Length == 0) {
            var ptrOffset = offset & -nint.Size;
            var pointer = MemoryMarshal.Cast<byte, nint>(_vmSnapshot[ptrOffset..(ptrOffset + nint.Size)])[0];
            if (VirtualMemory.GetProtection(pointer).CanRead()) {
                path = ((uint)ptrOffset, (uint)nint.Size, $"Unk_{ptrOffset:X}", FieldType.Pointer, null);
            }
        }

        if (path.Path.Length == 0) {
            return;
        }

        using var _ = ImRaii.Tooltip();
        var valueSpan = _vmSnapshot[(int)path.Offset..(int)(path.Offset + path.Size)];
        var value = path.Type.Read(valueSpan);
        ImGui.TextUnformatted($"{path.Type.Description()} {path.Path} @ 0x{path.Offset:X}");
        ImGui.TextUnformatted(
            path.EnumType is not null
                ? $"{path.EnumType.Name}.{Enum.GetName(path.EnumType, value)} = {value}"
                : path.Type is FieldType.IntPtr or FieldType.UIntPtr or FieldType.Pointer
                    ? $"0x{value:X}"
                    : $"{value}"
        );
        if (path.Type == FieldType.Pointer && (nint)value != 0) {
            _imGuiComponents.DrawPointerTooltipDetails((nint)value, null);
            ImGui.TextUnformatted("Click to inspect.");

            if (clicked) {
                _messageHub.Publish(new InspectObjectMessage((nint)value, null));
            }
        }

        ImGui.TextUnformatted("Right-click to copy to clipboard.");

        if (rightClicked) {
            ImGui.SetClipboardText(
                path.EnumType is not null
                    ? Enum.GetName(path.EnumType, value)
                    : path.Type is FieldType.IntPtr or FieldType.UIntPtr or FieldType.Pointer
                        ? $"{value:X}"
                        : $"{value}"
            );
        }
    }

    private (uint Offset, uint Size, string Path, FieldType Type, Type? EnumType) GetValuePath(ClassInfo? @class, uint offset)
    {
        if (@class is null) {
            return (0, 0, string.Empty, FieldType.Byte, null);
        }

        var field = @class.Fields
                          .LastOrDefault(field => offset >= field.Offset && offset < field.Offset + field.Size);
        if (field is null) {
            return (0, 0, string.Empty, FieldType.Byte, null);
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
            return (retOffset, elementSize, path, field.Type, field.EnumType);
        }

        var subPath = GetValuePath(field.ElementClass, offset - retOffset);
        if (subPath.Path.Length == 0) {
            return subPath;
        }

        return (retOffset + subPath.Offset, subPath.Size, $"{path}.{subPath.Path}", subPath.Type, subPath.EnumType);
    }

    public override void OnClose()
    {
        _windowSystem.RemoveWindow(this);
    }
}
