using System.Numerics;
using Dalamud.Interface.Windowing;
using Dynamis.Messaging;
using ImGuiNET;

namespace Dynamis.UI.Windows;

public sealed class HomeWindow : Window, ISingletonWindow
{
    private readonly MessageHub _messageHub;

    public HomeWindow(MessageHub messageHub, ImGuiComponents imGuiComponents) : base(
        "Dynamis", 0
    )
    {
        _messageHub = messageHub;

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
}
