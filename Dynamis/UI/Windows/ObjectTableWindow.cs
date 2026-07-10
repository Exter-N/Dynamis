using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Dynamis.UI.Windows;

public sealed class ObjectTableWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    private readonly ImGuiComponents _imGuiComponents;
    private readonly IFramework      _framework;
    private readonly IObjectTable    _objectTable;
    private readonly MessageHub      _messageHub;

    private bool                _vmLive = true;
    private Task<TableEntry[]>? _vmSnapshot;

    public ObjectTableWindow(ImGuiComponents imGuiComponents, IFramework framework, IObjectTable objectTable,
        MessageHub messageHub) : base("Dynamis - Object Table", 0)
    {
        _imGuiComponents = imGuiComponents;
        _framework = framework;
        _objectTable = objectTable;
        _messageHub = messageHub;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(16384, 16384),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void OnOpen()
    {
        _messageHub.Publish<DataYamlPreloadMessage>();
    }

    public override void Draw()
    {
        ImGui.Checkbox("Show live table"u8, ref _vmLive);

        if (_vmLive) {
            DrawTableLive();
            return;
        }

        if (_vmSnapshot is null) {
            _vmSnapshot = _framework.RunOnFrameworkThread(TakeSnapshot);
        }

        ImGui.SameLine();
        if (ImGui.Button("Refresh"u8)) {
            _vmSnapshot = _framework.RunOnFrameworkThread(TakeSnapshot);
        }

        if (!_vmSnapshot.IsCompleted) {
            ImGui.TextUnformatted("Taking snapshot of object table..."u8);
        } else if (_vmSnapshot.Exception is not null) {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ErrorForeground)) {
                ImGui.TextUnformatted("Failed taking snapshot of object table:"u8);
            }

            ImGui.TextUnformatted(_vmSnapshot.Exception.ToString());
        } else if (_vmSnapshot.IsCompletedSuccessfully) {
            DrawTableSnapshot(_vmSnapshot.Result);
        }
    }

    private void DrawTableLive()
    {
        using var table = ImRaii.Table("##objectTable"u8, 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table) {
            return;
        }

        SetupAndDrawTableHeader();
        foreach (var obj in _objectTable) {
            DrawTableEntry(TableEntry.FromGameObject(obj));
        }
    }

    private void DrawTableSnapshot(TableEntry[] objectTable)
    {
        using var table = ImRaii.Table("##objectTable"u8, 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table) {
            return;
        }

        SetupAndDrawTableHeader();
        foreach (var entry in objectTable) {
            DrawTableEntry(in entry);
        }
    }

    private static void SetupAndDrawTableHeader()
    {
        ImGui.TableSetupColumn("Index"u8,               ImGuiTableColumnFlags.WidthStretch, 0.05f);
        ImGui.TableSetupColumn("Game Object ID"u8,      ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableSetupColumn("Name"u8,                ImGuiTableColumnFlags.WidthStretch, 0.3f);
        ImGui.TableSetupColumn("Game Object Address"u8, ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableSetupColumn("Draw Object Address"u8, ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableSetupColumn("Position"u8,            ImGuiTableColumnFlags.WidthStretch, 0.2f);
        ImGui.TableHeadersRow();
    }

    private void DrawTableEntry(in TableEntry entry)
    {
        ImGui.TableNextColumn();
        ImGuiComponents.DrawCopyable(entry.ObjectIndex.ToString(), true);

        ImGui.TableNextColumn();
        ImGuiComponents.DrawCopyable(entry.GameObjectId.ToString("X"), true);

        ImGui.TableNextColumn();
        var name = entry.Name;
        ImGuiComponents.DrawCopyable(name, false);

        ImGui.TableNextColumn();
        _imGuiComponents.DrawPointer(
            entry.GameObjectAddress, null, () => $"Game object of {name}",
            flags: ImGuiComponents.DrawPointerFlags.RightAligned
        );

        ImGui.TableNextColumn();
        _imGuiComponents.DrawPointer(
            entry.DrawObjectAddress, null, () => $"Draw object of {name}",
            flags: ImGuiComponents.DrawPointerFlags.RightAligned
        );

        ImGui.TableNextColumn();
        ImGuiComponents.DrawCopyable($"{entry.Position.X:F2}, {entry.Position.Y:F2}, {entry.Position.Z:F2}", true);
    }

    private TableEntry[] TakeSnapshot()
        => _objectTable.Select(TableEntry.FromGameObject).ToArray();

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand("objtable", "objecttable", "objtbl", "ot", "o")) {
            return;
        }

        if (message.Arguments.Equals(1, "close", "x")) {
            message.SetHandled();
            IsOpen = false;
            return;
        }

        message.SetHandled();
        IsOpen = true;
        BringToFront();

        if (message.Arguments.Equals(1, "refresh", "r")) {
            _vmSnapshot = _framework.RunOnFrameworkThread(TakeSnapshot);
        }
    }

    private readonly record struct TableEntry(
        ushort ObjectIndex,
        ulong GameObjectId,
        string Name,
        nint GameObjectAddress,
        nint DrawObjectAddress,
        Vector3 Position)
    {
        public static unsafe TableEntry FromGameObject(IGameObject obj)
        {
            var objStruct = (GameObject*)obj.Address;
            return new(
                obj.ObjectIndex, obj.GameObjectId, obj.Name.ToString(), obj.Address,
                objStruct is not null ? (nint)objStruct->GetDrawObject() : 0,
                obj.Position
            );
        }
    }
}
