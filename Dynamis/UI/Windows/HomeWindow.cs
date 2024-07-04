using System.Numerics;
using Dalamud.Game;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Messaging;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed partial class HomeWindow : Window, IMessageObserver<OpenWindowMessage<HomeWindow>>
{
    private readonly MessageHub             _messageHub;
    private readonly ILogger<HomeWindow>    _logger;
    private readonly ConfigurationContainer _configuration;
    private readonly ISigScanner            _sigScanner;
    private readonly MemoryHeuristics       _memoryHeuristics;

    public HomeWindow(MessageHub messageHub, ILogger<HomeWindow> logger, ConfigurationContainer configuration,
        ISigScanner sigScanner, MemoryHeuristics memoryHeuristics, ImGuiComponents imGuiComponents) : base(
        "Dynamis", 0
    )
    {
        _messageHub = messageHub;
        _logger = logger;
        _configuration = configuration;
        _sigScanner = sigScanner;
        _memoryHeuristics = memoryHeuristics;

        Size = new Vector2(512, 288);
        SizeCondition = ImGuiCond.Always;

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        if (ImGui.Button("Open Signature Scanner")) {
            _messageHub.Publish<OpenWindowMessage<SigScannerWindow>>();
        }

        if (ImGui.Button("Open Object Table")) {
            _messageHub.Publish<OpenWindowMessage<ObjectTableWindow>>();
        }

        if (ImGui.Button("Open Object Inspector")) {
            _messageHub.Publish<OpenWindowMessage<ObjectInspectorWindow>>();
        }

        if (ImGui.Button("Open Dalamud Console / Log window")) {
            _messageHub.Publish<OpenDalamudConsoleMessage>();
        }
    }

    public void HandleMessage(OpenWindowMessage<HomeWindow> _)
        => IsOpen = true;
}
