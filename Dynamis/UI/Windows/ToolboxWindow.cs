using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;
using ImGuiNET;

namespace Dynamis.UI.Windows;

public sealed class ToolboxWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    private readonly MessageHub             _messageHub;
    private readonly ConfigurationContainer _configuration;

    public ToolboxWindow(MessageHub messageHub, ConfigurationContainer configuration, ImGuiComponents imGuiComponents) :
        base(
            $"Dynamis {Assembly.GetExecutingAssembly().GetName().Version} Toolbox###DynamisToolbox",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
        )
    {
        _messageHub = messageHub;
        _configuration = configuration;

        Size = new Vector2(384, 216);
        SizeCondition = ImGuiCond.Always;

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        if (ImGui.Button("Signature Scanner")) {
            _messageHub.Publish<OpenWindowMessage<SigScannerWindow>>();
        }

        ImGui.SameLine();
        if (ImGui.Button("Object Table")) {
            _messageHub.Publish<OpenWindowMessage<ObjectTableWindow>>();
        }

        ImGui.Dummy(new(16.0f, 16.0f));
        if (ImGui.Button("Object Inspector")) {
            _messageHub.Publish<OpenWindowMessage<ObjectInspectorWindow>>();
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!_configuration.Configuration.EnableIpfd)) {
            if (ImGui.Button("IPFD Breakpoint")) {
                _messageHub.Publish<OpenWindowMessage<BreakpointWindow>>();
            }
        }

        ImGui.Dummy(new(16.0f, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing()));
        if (ImGui.Button("Dalamud Console / Log window", new(ImGui.GetContentRegionAvail().X, -1.0f))) {
            _messageHub.Publish<OpenDalamudConsoleMessage>();
        }
    }

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand(null, "toolbox", "tb", "t", "main", "m")) {
            return;
        }

        message.SetHandled();
        IsOpen = true;
        BringToFront();
    }
}
