using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Interop.Ipfd;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class BreakpointWindow : IndexedWindow
{
    private const int MaxSnapshotHistorySize = 32;

    private readonly ILogger                _logger;
    private readonly ImGuiComponents        _imGuiComponents;
    private readonly ObjectInspector        _objectInspector;
    private readonly ConfigurationContainer _configuration;
    private readonly MessageHub             _messageHub;
    private readonly Ipfd                   _ipfd;
    private readonly Breakpoint             _breakpoint;

    private nint            _vmNewAddress;
    private bool            _vmEnable;
    private BreakpointFlags _vmLength;
    private BreakpointFlags _vmCondition;
    private int             _vmMaximum = -1;
    private Task?           _vmSyncTask;

    private readonly List<SnapshotRecord>  _vmSnapshots   = [];
    private readonly HashSet<nint>         _vmIps         = [];
    private readonly HashSet<(nint, nint)> _vmIpsAndTypes = [];

    public Breakpoint Breakpoint
        => _breakpoint;

    public BreakpointWindow(ILogger logger, WindowSystem windowSystem, ImGuiComponents imGuiComponents,
        ObjectInspector objectInspector, ConfigurationContainer configuration, MessageHub messageHub,
        Ipfd ipfd, Breakpoint breakpoint, int index) : base($"Dynamis - IPFD Breakpoint##{index}", windowSystem, index, 0)
    {
        _logger = logger;
        _imGuiComponents = imGuiComponents;
        _objectInspector = objectInspector;
        _configuration = configuration;
        _messageHub = messageHub;
        _ipfd = ipfd;
        _breakpoint = breakpoint;

        breakpoint.Hit += BreakpointHit;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(16384, 16384),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public void Configure(nint address, BreakpointFlags condition, BreakpointFlags length, bool enable, int maximumHits)
    {
        _vmNewAddress = address;
        _vmCondition = condition;
        _vmLength = length;
        _vmEnable = enable;
        _vmMaximum = maximumHits;

        _vmSyncTask = _breakpoint.ModifyAsync(
            _vmNewAddress, (_vmEnable ? BreakpointFlags.LocalEnable : 0) | _vmLength | _vmCondition
        );
    }

    public override void Draw()
    {
        DrawBreakpointEditor();
        DrawBreakpointStatus(_breakpoint.Address, _breakpoint.Flags);
        DrawSnapshots();
    }

    private void DrawBreakpointEditor()
    {
        ImGui.Checkbox("Enable Breakpoint", ref _vmEnable);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.CalcTextSize("mmmmm").X + ImGui.GetStyle().FramePadding.X * 2.0f + ImGui.GetStyle().ItemInnerSpacing.X * 2.0f + ImGui.GetFrameHeight() * 2.0f);
        lock (this) {
            var max = _vmMaximum;
            ImGui.InputInt("Maximum Hits", ref max);
            if (ImGui.IsItemDeactivatedAfterEdit()) {
                _vmMaximum = Math.Clamp(max, -1, ushort.MaxValue);
            }
        }
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("mmmmmmmmmmmmmmmm").X + ImGui.GetStyle().FramePadding.X * 2.0f);
        }
        ImGuiComponents.InputPointer("Address", ref _vmNewAddress);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.CalcTextSize("Execute").X + ImGui.GetStyle().FramePadding.X * 2.0f + ImGui.GetFrameHeight());
        ImGuiComponents.Combo(
            "Condition", ref _vmCondition,
            [BreakpointFlags.InstructionExecution, BreakpointFlags.DataWrites, BreakpointFlags.DataReadsAndWrites,],
            condition => $"{GetCondition(condition)}"
        );
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.CalcTextSize("m").X + ImGui.GetStyle().FramePadding.X * 2.0f + ImGui.GetFrameHeight());
        using (ImRaii.Disabled(_vmCondition == BreakpointFlags.InstructionExecution)) {
            ImGuiComponents.Combo(
                "Length", ref _vmLength,
                [
                    BreakpointFlags.LengthOne,
                    BreakpointFlags.LengthTwo,
                    BreakpointFlags.LengthFour,
                    BreakpointFlags.LengthEight,
                ], length => $"{GetLength(length)}"
            );
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!(_vmSyncTask?.IsCompleted ?? true))) {
            if (ImGui.Button("Apply Configuration")) {
                _vmSyncTask = _breakpoint.ModifyAsync(
                    _vmNewAddress,
                    (_vmEnable ? BreakpointFlags.LocalEnable | BreakpointFlags.GlobalEnable : 0) | _vmLength
                  | _vmCondition
                );
            }
        }

        if (_vmCondition == BreakpointFlags.DataReadsAndWrites) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                ImGui.TextUnformatted("Read/Write watchpoints in IPFD are known to cause crashes in some circumstances. Use at your own risk!");
            }
        }
    }

    private static void DrawBreakpointStatus(nint address, BreakpointFlags flags)
    {
        ImGui.TextUnformatted("Current breakpoint status: ");
        ImGui.SameLine(0.0f, 0.0f);
        if (flags.HasFlag(BreakpointFlags.LocalEnable)) {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.ParsedGreen!.Value)) {
                ImGui.TextUnformatted("Enabled");
            }
        } else {
            using (ImRaii.PushColor(ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value)) {
                ImGui.TextUnformatted("Disabled");
            }
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("Address: ");
        ImGui.SameLine(0.0f, 0.0f);
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            ImGui.TextUnformatted($"0x{address:X}");
        }

        ImGui.SameLine();
        ImGui.TextUnformatted($"Condition: {GetCondition(flags)}");
        if ((flags & BreakpointFlags.DataReadsAndWrites) != BreakpointFlags.InstructionExecution) {
            ImGui.SameLine();
            ImGui.TextUnformatted($"Length: {GetLength(flags)}");
        }
    }

    private void DrawSnapshots()
    {
        using var table = ImRaii.Table("##snapshots", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table) {
            return;
        }

        ImGui.TableSetupColumn("Date/Time",    ImGuiTableColumnFlags.WidthStretch, 0.3f);
        ImGui.TableSetupColumn("Thread ID",    ImGuiTableColumnFlags.WidthStretch, 0.2f);
        ImGui.TableSetupColumn("Address",      ImGuiTableColumnFlags.WidthStretch, 0.25f);
        ImGui.TableSetupColumn("Thread State", ImGuiTableColumnFlags.WidthStretch, 0.25f);
        ImGui.TableHeadersRow();
        lock (_vmSnapshots) {
            var i = 0;
            foreach (var record in _vmSnapshots) {
                using var _ = ImRaii.PushId(i++);

                ImGui.TableNextColumn();
                ImGuiComponents.DrawCopyable($"{record.Time}", false);

                ImGui.TableNextColumn();
                ImGuiComponents.DrawCopyable($"{record.ThreadId}", false);

                ImGui.TableNextColumn();
                _imGuiComponents.DrawPointer(record.ExceptionAddress, null);

                ImGui.TableNextColumn();
                if (ImGui.Button("Inspect")) {
                    _messageHub.Publish(new InspectObjectMessage(record.Context));
                }
            }
        }
    }

    private static string GetCondition(BreakpointFlags flags)
        => (flags & BreakpointFlags.DataReadsAndWrites) switch
        {
            BreakpointFlags.InstructionExecution => "Execute",
            BreakpointFlags.DataWrites => "Write",
            BreakpointFlags.DataReadsAndWrites => "R/W",
            BreakpointFlags.IoReadsAndWrites => "I/O",
            _ => throw new Exception($"Unexpected breakpoint condition: {flags & BreakpointFlags.DataReadsAndWrites}"),
        };

    private static byte GetLength(BreakpointFlags flags)
        => (flags & BreakpointFlags.LengthFour) switch
        {
            BreakpointFlags.LengthOne => 1,
            BreakpointFlags.LengthTwo => 2,
            BreakpointFlags.LengthEight => 8,
            BreakpointFlags.LengthFour => 4,
            _ => throw new Exception($"Unexpected breakpoint length: {flags & BreakpointFlags.LengthFour}"),
        };

    public override void OnClose()
    {
        _breakpoint.Hit -= BreakpointHit;
        base.OnClose();
        _breakpoint.Dispose();
    }

    private unsafe void BreakpointHit(object? sender, BreakpointEventArgs e)
    {
        var pCtx = e.ExceptionInfo->ContextRecord;
        var @this = unchecked((nint)e.ExceptionInfo->ContextRecord->Rcx);
        var typeOfThis = VirtualMemory.GetProtection(@this).CanRead()
            ? (Ipfd.RequiresSafeRead(@this, pCtx) ? _ipfd.Read<nint>(@this) : *(nint*)@this)
            : 0;

        int maximum;
        lock (this) {
            maximum = _vmMaximum;
            if (_vmMaximum > 0) {
                _vmMaximum--;
            }

            if (maximum == 1) {
                _vmSyncTask = _breakpoint.DisableAsync();
            }
        }

        if (maximum == 0) {
            return;
        }

        var time = DateTime.Now;
        var (threadId, context) = _objectInspector.TakeThreadStateSnapshot(e.ExceptionInfo);
        var record = new SnapshotRecord(time, threadId, e.Address, 0, context);
        ThreadPool.QueueUserWorkItem(ProcessSnapshot, record, false);
    }

    private void ProcessSnapshot(SnapshotRecord record)
    {
        _objectInspector.CompleteSnapshot(record.Context);
        if (record.Context.AssociatedSnapshot is
            {
            } stack) {
            _objectInspector.CompleteSnapshot(stack);
        }

        lock (_vmSnapshots) {
            _vmSnapshots.Insert(0, record);
            if (_vmSnapshots.Count > MaxSnapshotHistorySize) {
                _vmSnapshots.RemoveAt(_vmSnapshots.Count - 1);
            }
        }
    }

    private record struct SnapshotRecord(
        DateTime Time,
        uint ThreadId,
        nint ExceptionAddress,
        nint TypeOfThis,
        ObjectSnapshot Context);
}
