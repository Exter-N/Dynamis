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
    private readonly ClassRegistry                   _classRegistry;
    private readonly Lazy<ObjectInspectorDispatcher> _objectInspectorDispatcher;

    private readonly ShortLivedSingleCache<KeyValuePair<nint, AddressIdentification>[]> _wellKnownAddresses;

    private readonly SnapshotViewer   _snapshotViewer;
    private readonly SnapshotViewer   _associatedSnapshotViewer;
    private readonly ClassFieldViewer _classFieldViewer;

    private          bool                     _vmShowParents = false;
    private readonly Dictionary<Type, object> _vmCustom      = [];

    private readonly PointerInput _addressInput;

    private int              _vmStatus;
    private ObjectSnapshot?  _vmSnapshot;
    private ClassInfo?       _vmInitialClass;
    private ClassInfo?       _vmInitialActualClass;
    private ClassIdentifier? _vmInitialClassIdHint;
    private bool             _vmReanalyzed  = false;
    private int              _vmReanalyze   = 1;
    private string           _vmNewTypeName = string.Empty;
    private ClassInfo?       _vmNewClass    = null;

    private bool _vmLive;

    public nint ObjectAddress
        => _addressInput.GetValue();

    public ObjectSnapshot? Snapshot
        => _vmSnapshot;

    public ObjectInspectorWindow(ILogger logger, WindowSystem windowSystem, ImGuiComponents imGuiComponents,
        PointerInputFactory pointerInputFactory, ObjectInspector objectInspector, ClassRegistry classRegistry,
        ShortLivedSingleCache<KeyValuePair<nint, AddressIdentification>[]> wellKnownAddresses,
        SnapshotViewerFactory snapshotViewerFactory, Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher,
        int index) : base($"Dynamis - Object Inspector##{index}", windowSystem, index, 0)
    {
        _logger = logger;
        _objectInspector = objectInspector;
        _classRegistry = classRegistry;
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
        _vmInitialClass = snapshot.Class;
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

    private void RunInspection(ClassInfo? @class, ClassIdentifier? classIdHint, string? name,
        bool setInitialClass = true)
    {
        if (setInitialClass) {
            _vmInitialClass = @class;
            _vmInitialClassIdHint = classIdHint;
        }

        _vmReanalyzed = _vmInitialClass != @class || _vmInitialClassIdHint != classIdHint;

        try {
            _vmSnapshot = _objectInspector.TakeSnapshot(_addressInput.GetValue(), @class, classIdHint, name);
            if (setInitialClass) {
                _vmInitialActualClass = _vmSnapshot.Class;
            }

            _addressInput.SetValue(_vmSnapshot.Address);
            _vmStatus = 1;
        } catch (Exception e) {
            _logger.LogError(
                e, "Object snapshotting or inspection failed for address 0x{Address:X}", _addressInput.GetValue()
            );
            _vmSnapshot = null;
            if (setInitialClass) {
                _vmInitialActualClass = null;
            }

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
                   "###wellKnownObjectsCombo"u8, string.Empty,
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
                ImGui.TextUnformatted("Refresh the Object"u8);
            }
            ImGui.SameLine(0.0f, itemInnerSpacing);
        } else {
            ImGui.SameLine(0.0f, itemInnerSpacing * 2.0f + refreshButtonWidth);
        }

        ImGui.TextUnformatted("Object Address"u8);

        switch (_vmStatus) {
            case 1:
                DrawSnapshot();
                break;
            case 2:
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground)) {
                    ImGui.TextUnformatted("Error"u8);
                }

                break;
        }
    }

    private void DrawSnapshot()
    {
        if (_vmSnapshot is null) {
            using var _ = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground);
            ImGui.TextUnformatted("Error"u8);
            return;
        }

        DrawClass(_vmSnapshot.Class);

        var liveSnapshot = _vmSnapshot.Live & _vmSnapshot.Address.HasValue;
        if (liveSnapshot) {
            ImGui.Checkbox("Link to Live Object (where applicable)"u8, ref _vmLive);
        }

        var inspectors = (_vmSnapshot.Class is not null
            ? _objectInspectorDispatcher.Value.GetInspectors(_vmSnapshot.Class)
            : []).ToList();

        foreach (var inspector in inspectors) {
            inspector.DrawAdditionalHeaderDetails(_vmSnapshot, liveSnapshot && _vmLive, this);
        }

        using var tabs = ImRaii.TabBar("###inspectorTabs"u8);
        if (!tabs) {
            return;
        }

        using (var tab = ImRaii.TabItem("Memory Snapshot"u8)) {
            if (tab) {
                _snapshotViewer.DrawHeader();
                using var _ = ImRaii.Child("###memorySnapshot"u8, -Vector2.One);
                _snapshotViewer.Draw();
            }
        }

        if (_vmSnapshot.Class?.Fields.Length > 0) {
            using var tab = ImRaii.TabItem("Class Fields"u8);
            if (tab) {
                _classFieldViewer.DrawHeader();

                using var _ = ImRaii.Child("###classFields"u8, -Vector2.One);
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
        using var id = ImRaii.PushId("###ClassInfo"u8);

        if (@class is null) {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Unknown Class"u8);
            ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
            DrawClassChangeButton();

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
            ImGui.TextUnformatted("Copy to clipboard"u8);
        }

        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawClassChangeButton();

        if (@class.DataYamlParents.Length > 0 && _vmShowParents) {
            using var indent = ImRaii.PushIndent(2);
            foreach (var parent in @class.DataYamlParents) {
                using var parentId = ImRaii.PushId(parent.Name);

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted($"Parent: {parent.Name}");
                ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
                if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Copy)) {
                    ImGui.SetClipboardText(parent.Name);
                }

                if (ImGui.IsItemHovered()) {
                    using var _ = ImRaii.Tooltip();
                    ImGui.TextUnformatted("Copy to clipboard"u8);
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
                ImGui.TextUnformatted("(from context)"u8);
            }
        } else if (sizeIsFromDtor && sizeIsFromManaged) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.SuccessForeground)) {
                ImGui.TextUnformatted("(from both managed type and dtor)"u8);
            }
        } else if (!sizeIsFromDtor && !sizeIsFromManaged) {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground)) {
                ImGui.TextUnformatted("(no valid source - using rest of page)"u8);
            }
        } else {
            if (sizeIsFromDtor) {
                ImGui.SameLine();
                ImGui.TextUnformatted("(from dtor)"u8);
            }

            if (sizeIsFromManaged) {
                ImGui.SameLine();
                ImGui.TextUnformatted("(from managed type)"u8);
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

    private void DrawClassChangeButton()
    {
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.InfoForeground, _vmReanalyzed)) {
            if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Cog)) {
                ImGui.OpenPopup("###ClassChange"u8);
            }
        }

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            ImGui.TextUnformatted("Re-analyze as another class"u8);
        }

        using var popup = ImRaii.Popup("###ClassChange"u8);
        if (!popup) {
            return;
        }

        var confirm = false;
        var hasInitialClass = _vmInitialClass is not null || _vmInitialClassIdHint is not null;
        ImGui.TextUnformatted("Re-analyze as..."u8);
        ImGui.RadioButton(
            "Automatically determined class###autoClass"u8, ref _vmReanalyze, _vmReanalyze is 1 && !hasInitialClass ? 1 : 0
        );
        if (hasInitialClass) {
            if (_vmInitialActualClass is not null) {
                ImGui.RadioButton("Original class:###initialClass"u8, ref _vmReanalyze, 1);
                using var indent = ImRaii.PushIndent(2);
                ImGui.TextUnformatted(_vmInitialActualClass.Name);
            } else {
                ImGui.RadioButton("Original class###initialClass"u8, ref _vmReanalyze, 1);
            }
        }

        ImGui.RadioButton("Specified class:###newClass"u8, ref _vmReanalyze, 2);
        using (ImRaii.PushIndent(2)) {
            confirm |= ImGui.InputText(
                "###newTypeName"u8, ref _vmNewTypeName, 2048, ImGuiInputTextFlags.EnterReturnsTrue
            );
            if (ImGui.IsItemEdited()) {
                _vmReanalyze = 2;
                _vmNewClass = _classRegistry.FromTypeName(_vmNewTypeName);
            }
        }

        var canConfirm = _vmReanalyze is not 2 || _vmNewClass is not null;

        ImGui.Dummy(
            new(
                ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Re-analyze"u8).X
                                                - ImGui.GetStyle().FramePadding.X * 2.0f, ImGui.GetFrameHeight()
            )
        );
        ImGui.SameLine(0.0f, 0.0f);
        using (ImRaii.Disabled(!canConfirm)) {
            confirm |= ImGui.Button("Re-analyze"u8);
        }

        if (!confirm) {
            return;
        }

        ImGui.CloseCurrentPopup();
        switch (_vmReanalyze) {
            case 0:
                RunInspection(null, null, _vmSnapshot?.Name, false);
                break;
            case 1:
                RunInspection(_vmInitialClass, _vmInitialClassIdHint, _vmSnapshot?.Name, false);
                break;
            case 2:
                RunInspection(_vmNewClass, null, _vmSnapshot?.Name, false);
                break;
        }
    }
}
