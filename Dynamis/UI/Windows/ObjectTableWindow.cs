using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectTableWindow : Window, IMessageObserver<OpenWindowMessage<ObjectTableWindow>>
{
    private readonly MessageHub                 _messageHub;
    private readonly ILogger<ObjectTableWindow> _logger;
    private readonly ImGuiComponents            _imGuiComponents;
    private readonly IFramework                 _framework;
    private readonly IObjectTable               _objectTable;

    private Task<TableEntry[]>? _vmTable;

    public ObjectTableWindow(MessageHub messageHub, ILogger<ObjectTableWindow> logger, ImGuiComponents imGuiComponents,
        IFramework framework, IObjectTable objectTable) : base("Dynamis - Object Table", 0)
    {
        _messageHub = messageHub;
        _logger = logger;
        _imGuiComponents = imGuiComponents;
        _framework = framework;
        _objectTable = objectTable;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(float.MaxValue, float.MaxValue),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        if (_vmTable is null) {
            _vmTable = _framework.RunOnFrameworkThread(TakeSnapshot);
        }

        if (ImGui.Button("Refresh")) {
            _vmTable = _framework.RunOnFrameworkThread(TakeSnapshot);
        }

        if (!_vmTable.IsCompleted) {
            ImGui.TextUnformatted("Taking snapshot of object table...");
        } else if (_vmTable.Exception is not null) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                ImGui.TextUnformatted("Failed taking snapshot of object table:");
            }
            ImGui.TextUnformatted(_vmTable.Exception.ToString());
        } else if (_vmTable.IsCompletedSuccessfully) {
            DrawTable(_vmTable.Result);
        }
    }

    private void DrawTable(TableEntry[] objectTable)
    {
        using var table = ImRaii.Table("##objectTable", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table) {
            return;
        }

        ImGui.TableSetupColumn("Index",               ImGuiTableColumnFlags.WidthStretch, 0.05f);
        ImGui.TableSetupColumn("Game Object ID",      ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableSetupColumn("Name",                ImGuiTableColumnFlags.WidthStretch, 0.3f);
        ImGui.TableSetupColumn("Game Object Address", ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableSetupColumn("Draw Object Address", ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableSetupColumn("Position",            ImGuiTableColumnFlags.WidthStretch, 0.2f);
        ImGui.TableHeadersRow();
        foreach (var entry in objectTable) {
            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(entry.ObjectIndex.ToString(), true);

            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(entry.GameObjectId.ToString("X"), true);

            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(entry.Name, false);

            ImGui.TableNextColumn();
            _imGuiComponents.DrawPointer(entry.GameObjectAddress, null);

            ImGui.TableNextColumn();
            _imGuiComponents.DrawPointer(entry.DrawObjectAddress, null);

            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable($"{entry.Position.X:F2}, {entry.Position.Y:F2}, {entry.Position.Z:F2}", true);
        }
    }

    private TableEntry[] TakeSnapshot()
        => _objectTable.Select(TableEntry.FromGameObject).ToArray();

    public void HandleMessage(OpenWindowMessage<ObjectTableWindow> _)
        => IsOpen = true;

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
