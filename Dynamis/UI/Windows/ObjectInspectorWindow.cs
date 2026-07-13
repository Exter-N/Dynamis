using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Interop;
using Dynamis.UI.Components;
using Dynamis.UI.ObjectInspectors;
using Dynamis.Utility;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindow : IndexedWindow
{
    private readonly ILogger                         _logger;
    private readonly ObjectInspector                 _objectInspector;
    private readonly Lazy<ObjectInspectorDispatcher> _objectInspectorDispatcher;

    private readonly ShortLivedSingleCache<KeyValuePair<nint, AddressIdentification>[]> _wellKnownAddresses;

    private readonly SnapshotViewer   _snapshotViewer;
    private readonly SnapshotViewer   _associatedSnapshotViewer;
    private readonly ClassFieldViewer _classFieldViewer;

    private          bool                     _vmShowParents = false;
    private readonly Dictionary<Type, object> _vmCustom      = [];

    private readonly PointerInput _addressInput;

    private int             _vmStatus;
    private ObjectSnapshot? _vmSnapshot;

    private bool _vmLive;

    public nint ObjectAddress
        => _addressInput.GetValue();

    public ObjectSnapshot? Snapshot
        => _vmSnapshot;

    public ObjectInspectorWindow(ILogger logger, WindowSystem windowSystem, ImGuiComponents imGuiComponents,
        PointerInputFactory pointerInputFactory, ObjectInspector objectInspector,
        ShortLivedSingleCache<KeyValuePair<nint, AddressIdentification>[]> wellKnownAddresses,
        SnapshotViewerFactory snapshotViewerFactory, Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher,
        int index) : base($"Dynamis - Object Inspector##{index}", windowSystem, index, 0)
    {
        _logger = logger;
        _objectInspector = objectInspector;
        _objectInspectorDispatcher = objectInspectorDispatcher;

        _wellKnownAddresses = wellKnownAddresses;

        _snapshotViewer = snapshotViewerFactory.Create();
        _associatedSnapshotViewer = snapshotViewerFactory.Create();
        _classFieldViewer = new();

        _addressInput = pointerInputFactory.Create("###objectAddress");

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(16384, 16384),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public void Inspect(nint address, ClassInfo? @class, ClassIdentifier? classIdHint, string? name)
    {
        _addressInput.SetValue(address);
        RunInspection(@class, classIdHint, name);
    }

    public void Inspect(ObjectSnapshot snapshot)
    {
        _addressInput.SetValue(snapshot.Address ?? 0);
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

    private void RunInspection(ClassInfo? @class, ClassIdentifier? classIdHint, string? name)
    {
        try {
            _vmSnapshot = _objectInspector.TakeSnapshot(_addressInput.GetValue(), @class, classIdHint, name);
            _addressInput.SetValue(_vmSnapshot.Address);
            _vmStatus = 1;
        } catch (Exception e) {
            _logger.LogError(
                e, "Object snapshotting or inspection failed for address 0x{Address:X}", _addressInput.GetValue()
            );
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
        _addressInput.SubText = _vmSnapshot?.Name;
        if (_addressInput.Draw(ImGuiInputTextFlags.EnterReturnsTrue)) {
            RunInspection(null, null, null);
        }

        ImGui.SameLine(0.0f, 0.0f);

        using (var combo = ImRaii.Combo(
                   "###wellKnownObjectsCombo", string.Empty,
                   ImGuiComboFlags.NoPreview | ImGuiComboFlags.HeightLarge | ImGuiComboFlags.PopupAlignLeft
               )) {
            if (combo) {
                foreach (var (address, identification) in _wellKnownAddresses.GetOrCreateValue()) {
                    if (ImGui.Selectable(identification.Describe(), address == _addressInput.GetValue())) {
                        _addressInput.SetValue(address);
                        RunInspection(null, identification.ClassIdentifierHint, identification.Describe());
                    }
                }
            }
        }

        if (_vmStatus != 0 && (_vmSnapshot is null || _vmSnapshot.Address.HasValue && _vmSnapshot.Live)) {
            ImGui.SameLine(0.0f, itemInnerSpacing);
            if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Sync)) {
                _addressInput.SetValue(_vmSnapshot?.Address);
                RunInspection(_vmSnapshot?.Class, null, _vmSnapshot?.Name);
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
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground)) {
                    ImGui.TextUnformatted("Error");
                }

                break;
        }
    }

    private void DrawSnapshot()
    {
        if (_vmSnapshot is null) {
            using var _ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground);
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
                _snapshotViewer.DrawHeader();
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

    public void DrawAssociatedSnapshotHeader()
        => _associatedSnapshotViewer.DrawHeader();

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

        ImGui.AlignTextToFramePadding();
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
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted($"Parent: {parent.Name}");
                ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
                if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Copy)) {
                    ImGui.SetClipboardText(parent.Name);
                }

                if (ImGui.IsItemHovered()) {
                    using var _ = ImRaii.Tooltip();
                    ImGui.TextUnformatted("Copy to clipboard");
                }

                indent.Indent();
            }
        }

        if (@class.ManagedType is not null && @class.ManagedType.FullName != @class.Name) {
            ImGui.TextUnformatted($"Managed type: {@class.ManagedType}");
        }

        var size = @class.EstimatedSize;
        if (size is 0 && _vmSnapshot is not null) {
            size = unchecked((uint)_vmSnapshot.Data.Length);
        }

        ImGui.TextUnformatted($"Estimated Size: {size} (0x{size:X}) bytes");
        var sizeIsFromDtor = @class.SizeFromDtor.HasValue
                          && @class.SizeFromDtor.Value == size;
        var sizeIsFromManaged = @class.SizeFromManagedType.HasValue
                        && @class.SizeFromManagedType.Value == size;
        var sizeIsFromCtx = @class.SizeFromContext.HasValue
                         && @class.SizeFromContext.Value == size;
        if (sizeIsFromCtx) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.InfoForeground)) {
                ImGui.TextUnformatted("(from context)");
            }
        } else if (sizeIsFromDtor && sizeIsFromManaged) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.SuccessForeground)) {
                ImGui.TextUnformatted("(from both managed type and dtor)");
            }
        } else if (!sizeIsFromDtor && !sizeIsFromManaged) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground)) {
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

        if (@class.Truncated) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground)) {
                ImGui.TextUnformatted("This object is actually larger, but is currently truncated."u8);
            }
        }

        if (@class.SizeFromManagedType.HasValue
         && @class.SizeFromManagedType.Value != @class.EstimatedSize) {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.WarningForeground)) {
                ImGui.TextUnformatted(
                    $"Size from managed type: {@class.SizeFromManagedType.Value} (0x{@class.SizeFromManagedType.Value:X}) bytes"
                );
            }
        }

        if (@class.SizeFromDtor.HasValue && @class.SizeFromDtor.Value != @class.EstimatedSize) {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.WarningForeground)) {
                ImGui.TextUnformatted(
                    $"Size from dtor: {@class.SizeFromDtor.Value} (0x{@class.SizeFromDtor.Value:X}) bytes"
                );
            }
        }
    }
}
