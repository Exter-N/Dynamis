using System.Text;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Messaging;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

namespace Dynamis.UI.Windows;

public sealed class RsvWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    private const int RsfValueSize = 64;

    private readonly FileDialogManager _fileDialogManager;

    public RsvWindow(ImGuiComponents imGuiComponents, FileDialogManager fileDialogManager) : base(
        "Dynamis - RSV Viewer", 0
    )
    {
        _fileDialogManager = fileDialogManager;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(16384, 16384),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override unsafe void Draw()
    {
        var layoutWorld = LayoutWorld.Instance();
        if (layoutWorld is null) {
            return;
        }

        if (ImGui.Button("Export"u8)) {
            _fileDialogManager.SaveFileDialog(
                "Export RSV Map", ".json", "rsv.json", ".json", (ok, path) =>
                {
                    if (!ok) {
                        return;
                    }

                    ExportToFile(path);
                }
            );
        }

        DrawRsvMap(layoutWorld);
        DrawRsfMap(layoutWorld);
    }

    private static unsafe void DrawRsvMap(LayoutWorld* layoutWorld)
    {
        if (layoutWorld->RsvMap is null) {
            return;
        }

        ImGuiComponents.SeparatorText("RSV Map"u8);
        using var table = ImRaii.Table("##rsvMap"u8, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table) {
            return;
        }

        ImGui.TableSetupColumn("Key"u8,   ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Value"u8, ImGuiTableColumnFlags.WidthStretch, 0.65f);
        ImGui.TableHeadersRow();

        foreach (var (key, value) in *layoutWorld->RsvMap) {
            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(key.ToString(), false);

            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(value.ToString(), false);
        }
    }

    private static unsafe void DrawRsfMap(LayoutWorld* layoutWorld)
    {
        if (layoutWorld->RsfMap is null) {
            return;
        }

        ImGuiComponents.SeparatorText("RSF Map"u8);
        using var table = ImRaii.Table("##rsfMap"u8, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table) {
            return;
        }

        ImGui.TableSetupColumn("Path Hash"u8, ImGuiTableColumnFlags.WidthStretch, 0.2f);
        ImGui.TableSetupColumn("Bytes"u8,     ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableHeadersRow();

        foreach (var (key, value) in *layoutWorld->RsfMap) {
            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(key.ToString("X16"), true);

            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(Hex(new(value.Value, RsfValueSize)), true);
        }
    }

    private static string Hex(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder(data.Length * 2);
        foreach (var b in data) {
            sb.Append($"{b:X2}");
        }

        return sb.ToString();
    }

    private unsafe void ExportToFile(string path)
    {
        using var stream = File.Create(path);
        using var jsonWriter = new Utf8JsonWriter(
            stream, new JsonWriterOptions
            {
                Indented = true,
            }
        );

        var layoutWorld = LayoutWorld.Instance();

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("rsv"u8);
        if (layoutWorld is not null && layoutWorld->RsvMap is not null) {
            jsonWriter.WriteStartObject();
            foreach (var (key, value) in *layoutWorld->RsvMap) {
                jsonWriter.WriteString(key.AsSpan(), value.AsSpan());
            }

            jsonWriter.WriteEndObject();
        } else {
            jsonWriter.WriteNullValue();
        }

        jsonWriter.WritePropertyName("rsf"u8);
        if (layoutWorld is not null && layoutWorld->RsfMap is not null) {
            jsonWriter.WriteStartObject();
            foreach (var (key, value) in *layoutWorld->RsfMap) {
                jsonWriter.WriteString(key.ToString("X16"), Hex(new(value, RsfValueSize)));
            }

            jsonWriter.WriteEndObject();
        } else {
            jsonWriter.WriteNullValue();
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand(
                "rsvviewer", "rsvview", "rsvmap", "rsfviewer", "rsfview", "rsfmap", "rsv", "rsf", "spoilers", "spoiler"
            )) {
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
    }
}
