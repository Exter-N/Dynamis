using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.ClientStructs;
using Dynamis.Interop;
using Dynamis.UI.Components;
using Dynamis.UI.ObjectInspectors;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindow : IndexedWindow
{
    private readonly ILogger                         _logger;
    private readonly DataYamlContainer               _dataYamlContainer;
    private readonly ObjectInspector                 _objectInspector;
    private readonly Lazy<ObjectInspectorDispatcher> _objectInspectorDispatcher;

    private readonly SnapshotViewer   _snapshotViewer;
    private readonly SnapshotViewer   _associatedSnapshotViewer;
    private readonly ClassFieldViewer _classFieldViewer;

    private          bool                     _vmShowParents = false;
    private readonly Dictionary<Type, object> _vmCustom      = [];

    private nint            _vmAddress;
    private int             _vmStatus;
    private ObjectSnapshot? _vmSnapshot;

    private bool _vmLive;

    public nint ObjectAddress
        => _vmAddress;

    public ObjectSnapshot? Snapshot
        => _vmSnapshot;

    public ObjectInspectorWindow(ILogger logger, WindowSystem windowSystem, ImGuiComponents imGuiComponents,
        DataYamlContainer dataYamlContainer, ObjectInspector objectInspector,
        SnapshotViewerFactory snapshotViewerFactory, Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher,
        int index) : base($"Dynamis - Object Inspector##{index}", windowSystem, index, 0)
    {
        _logger = logger;
        _dataYamlContainer = dataYamlContainer;
        _objectInspector = objectInspector;
        _objectInspectorDispatcher = objectInspectorDispatcher;

        _snapshotViewer = snapshotViewerFactory.Create();
        _associatedSnapshotViewer = snapshotViewerFactory.Create();
        _classFieldViewer = new();

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(16384, 16384),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public void Inspect(nint address, ClassInfo? @class)
    {
        _vmAddress = address;
        RunInspection(@class);
    }

    public void Inspect(ObjectSnapshot snapshot)
    {
        _vmAddress = snapshot.Address ?? 0;
        _vmSnapshot = snapshot;
        _vmStatus = 1;

        UpdateComponentViewModels();
    }

    public T GetCustomViewModel<T>() where T : class, new()
    {
        if (_vmCustom.TryGetValue(typeof(T), out var customVm) && customVm is T vm) {
            return vm;
        }

        vm = new T();
        _vmCustom.Add(typeof(T), vm);

        return vm;
    }

    private void RunInspection(ClassInfo? @class)
    {
        try {
            _vmSnapshot = _objectInspector.TakeSnapshot(_vmAddress, @class);
            _vmAddress = _vmSnapshot.Address ?? _vmAddress;
            _vmStatus = 1;
        } catch (Exception e) {
            _logger.LogError(e, "Object snapshotting or inspection failed for address 0x{Address:X}", _vmAddress);
            _vmSnapshot = null;
            _vmStatus = 2;
        }

        UpdateComponentViewModels();
    }

    private void UpdateComponentViewModels()
    {
        _snapshotViewer.Snapshot = _vmSnapshot;
        _associatedSnapshotViewer.Snapshot = _vmSnapshot?.AssociatedSnapshot;
        _classFieldViewer.Snapshot = _vmSnapshot;
    }

    public override void Draw()
    {
        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var refreshButtonWidth = ImGuiComponents.NormalizedIconButtonSize(FontAwesomeIcon.Sync).X;
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - itemInnerSpacing - refreshButtonWidth - ImGui.GetFrameHeight());
        if (ImGuiComponents.InputPointer("Object Address", ref _vmAddress, ImGuiInputTextFlags.EnterReturnsTrue, false)) {
            RunInspection(null);
        }

        if (!ImGui.IsItemActive() && _vmSnapshot?.Name is not null) {
            var framePadding = ImGui.GetStyle().FramePadding;
            var rectMin = ImGui.GetItemRectMin() + framePadding;
            rectMin.X += ImGuiComponents.CalcPointerSize(_vmAddress).X + itemInnerSpacing;
            var rectMax = ImGui.GetItemRectMax() - framePadding;
            var rectSize = rectMax - rectMin;
            var textSize = ImGui.CalcTextSize(_vmSnapshot.Name);
            var textStart = new Vector2(rectMin.X + Math.Max(0.0f, rectSize.X - textSize.X), rectMin.Y + (rectSize.Y - textSize.Y) * 0.5f);
            ImGui.GetWindowDrawList().PushClipRect(rectMin, rectMax, false);
            try {
                ImGui.GetWindowDrawList().AddText(textStart, ImGuiUtil.HalfTransparentText(), _vmSnapshot.Name);
            } finally {
                ImGui.GetWindowDrawList().PopClipRect();
            }
        }

        ImGui.SameLine(0.0f, 0.0f);

        using (var combo = ImRaii.Combo(
                   "###wellKnownObjectsCombo", string.Empty,
                   ImGuiComboFlags.NoPreview | ImGuiComboFlags.HeightLarge | ImGuiComboFlags.PopupAlignLeft
               )) {
            if (combo) {
                foreach (var (address, identification) in
                         _dataYamlContainer.GetWellKnownAddresses(AddressType.Instance)) {
                    if (ImGui.Selectable(identification.Describe(), address == _vmAddress)) {
                        _vmAddress = address;
                        RunInspection(null);
                    }
                }
            }
        }

        if (_vmStatus != 0 && (_vmSnapshot is null || _vmSnapshot.Address.HasValue && _vmSnapshot.Live)) {
            ImGui.SameLine(0.0f, itemInnerSpacing);
            if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Sync)) {
                _vmAddress = _vmSnapshot?.Address ?? _vmAddress;
                RunInspection(_vmSnapshot?.Class);
            }

            if (ImGui.IsItemHovered()) {
                using var _ = ImRaii.Tooltip();
                ImGui.TextUnformatted("Refresh the Object");
            }
            ImGui.SameLine(0.0f, itemInnerSpacing);
        } else {
            ImGui.SameLine(0.0f, itemInnerSpacing * 2.0f + refreshButtonWidth);
        }

        ImGui.TextUnformatted("Object Address");

        switch (_vmStatus) {
            case 1:
                DrawSnapshot();
                break;
            case 2:
                using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                    ImGui.TextUnformatted("Error");
                }

                break;
        }
    }

    private void DrawSnapshot()
    {
        if (_vmSnapshot is null) {
            using var _ = ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value);
            ImGui.TextUnformatted("Error");
            return;
        }

        DrawClass(_vmSnapshot.Class);

        var liveSnapshot = _vmSnapshot.Live & _vmSnapshot.Address.HasValue;
        if (liveSnapshot) {
            ImGui.Checkbox("Link to Live Object (where applicable)", ref _vmLive);
        }

        var inspectors = (_vmSnapshot.Class is not null
            ? _objectInspectorDispatcher.Value.GetInspectors(_vmSnapshot.Class)
            : []).ToList();

        foreach (var inspector in inspectors) {
            inspector.DrawAdditionalHeaderDetails(_vmSnapshot, liveSnapshot && _vmLive, this);
        }

        using var tabs = ImRaii.TabBar("###inspectorTabs");
        if (!tabs) {
            return;
        }

        using (var tab = ImRaii.TabItem("Memory Snapshot")) {
            if (tab) {
                using var _ = ImRaii.Child("###memorySnapshot", -Vector2.One);
                _snapshotViewer.Draw();
            }
        }

        if (_vmSnapshot.Class?.Fields.Length > 0) {
            using var tab = ImRaii.TabItem("Class Fields");
            if (tab) {
                _classFieldViewer.DrawHeader();

                using var _ = ImRaii.Child("###classFields", -Vector2.One);
                if (liveSnapshot && _vmLive) {
                    _classFieldViewer.Draw(_vmSnapshot.Address!.Value, true);
                } else {
                    unsafe {
                        fixed (byte* ptr = _vmSnapshot.Data) {
                            _classFieldViewer.Draw((nint)ptr, false);
                        }
                    }
                }
            }
        }

        foreach (var inspector in inspectors) {
            inspector.DrawAdditionalTabs(_vmSnapshot, liveSnapshot && _vmLive, this);
        }
    }

    public void DrawAssociatedSnapshot()
        => _associatedSnapshotViewer.Draw();

    public void DrawAssociatedSnapshot(Range range)
        => _associatedSnapshotViewer.Draw(range);

    private void DrawClass(ClassInfo? @class)
    {
        if (@class is null) {
            return;
        }

        if (@class.DataYamlParents.Length > 0) {
            if (ImGuiComponents.NormalizedIconButton(
                    _vmShowParents ? FontAwesomeIcon.CaretDown : FontAwesomeIcon.CaretRight
                )) {
                _vmShowParents = !_vmShowParents;
            }

            ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        }

        ImGui.TextUnformatted($"Class Name: {@class.Name}");
        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Copy)) {
            ImGui.SetClipboardText(@class.Name);
        }

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            ImGui.TextUnformatted("Copy to clipboard");
        }

        if (@class.DataYamlParents.Length > 0 && _vmShowParents) {
            using var indent = ImRaii.PushIndent(2);
            foreach (var parent in @class.DataYamlParents) {
                ImGui.TextUnformatted($"Parent: {parent.Name}");
                ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
                if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Copy)) {
                    ImGui.SetClipboardText(parent.Name);
                }

                if (ImGui.IsItemHovered()) {
                    using var _ = ImRaii.Tooltip();
                    ImGui.TextUnformatted("Copy to clipboard");
                }

                indent.Push();
            }
        }

        if (@class.ManagedType is not null && @class.ManagedType.FullName != @class.Name) {
            ImGui.TextUnformatted($"Managed type: {@class.ManagedType}");
        }

        ImGui.TextUnformatted($"Estimated Size: {@class.EstimatedSize} (0x{@class.EstimatedSize:X}) bytes");
        var sizeIsFromDtor = @class.SizeFromDtor.HasValue
                          && @class.SizeFromDtor.Value == @class.EstimatedSize;
        var sizeIsFromManaged = @class.SizeFromManagedType.HasValue
                        && @class.SizeFromManagedType.Value == @class.EstimatedSize;
        var sizeIsFromCtx = @class.SizeFromOuterContext.HasValue
                         && @class.SizeFromOuterContext.Value == @class.EstimatedSize;
        if (sizeIsFromCtx) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.ParsedBlue!.Value)) {
                ImGui.TextUnformatted("(from outer context)");
            }
        } else if (sizeIsFromDtor && sizeIsFromManaged) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.ParsedGreen!.Value)) {
                ImGui.TextUnformatted("(from both managed type and dtor)");
            }
        } else if (!sizeIsFromDtor && !sizeIsFromManaged) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                ImGui.TextUnformatted("(no valid source - using rest of page)");
            }
        } else {
            if (sizeIsFromDtor) {
                ImGui.SameLine();
                ImGui.TextUnformatted("(from dtor)");
            }

            if (sizeIsFromManaged) {
                ImGui.SameLine();
                ImGui.TextUnformatted("(from managed type)");
            }
        }

        if (@class.SizeFromManagedType.HasValue
         && @class.SizeFromManagedType.Value != @class.EstimatedSize) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudYellow!.Value)) {
                ImGui.TextUnformatted(
                    $"Size from managed type: {@class.SizeFromManagedType.Value} (0x{@class.SizeFromManagedType.Value:X}) bytes"
                );
            }
        }

        if (@class.SizeFromDtor.HasValue && @class.SizeFromDtor.Value != @class.EstimatedSize) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudYellow!.Value)) {
                ImGui.TextUnformatted(
                    $"Size from dtor: {@class.SizeFromDtor.Value} (0x{@class.SizeFromDtor.Value:X}) bytes"
                );
            }
        }
    }
}
