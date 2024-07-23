using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Messaging;
using Dynamis.Utility;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class SigScannerWindow : Window, IMessageObserver<OpenWindowMessage<SigScannerWindow>>
{
    private const int MaxResultHistorySize = 32;

    private readonly ILogger<SigScannerWindow> _logger;
    private readonly ISigScanner               _sigScanner;
    private readonly ImGuiComponents           _imGuiComponents;

    private          string           _vmSignature = string.Empty;
    private          int              _vmOffset    = 0;
    private readonly List<ScanResult> _vmResults   = new();

    public SigScannerWindow(ILogger<SigScannerWindow> logger, ISigScanner sigScanner,
        ImGuiComponents imGuiComponents) : base("Dynamis - Signature Scanner", 0)
    {
        _logger = logger;
        _sigScanner = sigScanner;
        _imGuiComponents = imGuiComponents;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(float.MaxValue, float.MaxValue),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        var innerSpacing = ImGui.GetStyle().ItemInnerSpacing.X;

        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            ImGui.InputText("##signature", ref _vmSignature, 2048);
        }

        ImGui.SameLine(0.0f, innerSpacing);
        ImGui.TextUnformatted("Signature");

        ImGui.InputInt("Offset (for Static Address)", ref _vmOffset);

        if (ImGui.Button("Scan Text")) {
            RunScan(ScanType.Text);
        }

        ImGui.SameLine(0.0f, innerSpacing);
        if (ImGui.Button("Scan Data")) {
            RunScan(ScanType.Data);
        }

        ImGui.SameLine(0.0f, innerSpacing);
        if (ImGui.Button("Scan Module")) {
            RunScan(ScanType.Module);
        }

        ImGui.SameLine(0.0f, innerSpacing);
        if (ImGui.Button("Scan Static Address")) {
            RunScan(ScanType.StaticAddress);
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear History")) {
            _vmResults.Clear();
        }

        DrawResults();
    }

    private void DrawResults()
    {
        using var table = ImRaii.Table("##results", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table) {
            return;
        }

        ImGui.TableSetupColumn("Signature", ImGuiTableColumnFlags.WidthStretch, 0.6f);
        ImGui.TableSetupColumn("Scan Type", ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableSetupColumn("Offset",    ImGuiTableColumnFlags.WidthStretch, 0.1f);
        ImGui.TableSetupColumn("Address",   ImGuiTableColumnFlags.WidthStretch, 0.15f);
        ImGui.TableHeadersRow();
        foreach (var result in _vmResults) {
            ImGui.TableNextColumn();
            ImGuiComponents.DrawCopyable(result.Signature, true);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(result.Type.ToString().InsertSpacesBetweenWords());

            ImGui.TableNextColumn();
            if (result.IsOffsetRelevant) {
                ImGui.TextUnformatted(result.Offset.ToString());
            }

            ImGui.TableNextColumn();
            if (result.Success) {
                _imGuiComponents.DrawPointer(result.Address, null);
            } else {
                using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                    if (result.Exception is not null) {
                        ImGui.Selectable("Error");
                    } else {
                        ImGui.TextUnformatted("Not Found");
                    }
                }
                if (result.Exception is not null && ImGui.IsItemHovered()) {
                    using var _ = ImRaii.Tooltip();
                    ImGui.TextUnformatted(result.Exception.ToString());
                }
            }
        }
    }

    private void RunScan(ScanType type)
    {
        var signature = _vmSignature.NormalizeSpaces().ToUpperInvariant();
        var offset = _vmOffset;
        ScanResult result;

        try {
            nint address;
            var success = type switch
            {
                ScanType.Text          => _sigScanner.TryScanText(signature, out address),
                ScanType.Data          => _sigScanner.TryScanData(signature, out address),
                ScanType.Module        => _sigScanner.TryScanModule(signature, out address),
                ScanType.StaticAddress => _sigScanner.TryGetStaticAddressFromSig(signature, out address, offset),
                _                      => throw new InvalidOperationException($"Invalid scan type {type}"),
            };

            result = new(signature, type, offset, success, address, null);
        } catch (Exception e) {
            _logger.LogError(e, "Signature scan failed for signature {Signature} with type {Type} and offset {Offset}", signature, type, offset);
            result = new(signature, type, offset, false, 0, e);
        }

        _vmResults.RemoveAll(result.QueryEquals);
        _vmResults.Insert(0, result);
        if (_vmResults.Count > MaxResultHistorySize) {
            _vmResults.RemoveAt(_vmResults.Count - 1);
        }
    }

    public void HandleMessage(OpenWindowMessage<SigScannerWindow> _)
    {
        IsOpen = true;
        BringToFront();
    }

    private enum ScanType
    {
        Text,
        Data,
        Module,
        StaticAddress,
    }

    private readonly record struct ScanResult(
        string Signature,
        ScanType Type,
        int Offset,
        bool Success,
        nint Address,
        Exception? Exception)
    {
        public bool IsOffsetRelevant
            => Type is ScanType.StaticAddress;

        public bool QueryEquals(ScanResult other)
            => Signature.Equals(other.Signature, StringComparison.Ordinal)
            && Type == other.Type && (!IsOffsetRelevant || Offset == other.Offset);
    }
}
