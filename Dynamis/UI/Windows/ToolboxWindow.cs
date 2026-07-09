using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;

namespace Dynamis.UI.Windows;

public sealed class ToolboxWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    private readonly MessageHub _messageHub;
    private readonly Toolbox    _toolbox;

    public ToolboxWindow(MessageHub messageHub, Toolbox toolbox, ImGuiComponents imGuiComponents) :
        base(
            $"Dynamis {Assembly.GetExecutingAssembly().GetName().Version} Toolbox###DynamisToolbox",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
        )
    {
        _messageHub = messageHub;
        _toolbox = toolbox;

        Size = new Vector2(384, 216);
        SizeCondition = ImGuiCond.Always;

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        _toolbox.Draw(static label => ImGui.Button(label), static () => ImGui.Dummy(new(16.0f, 16.0f)), true);

        ImGui.Dummy(new(16.0f, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing()));
        if (ImGui.Button("Dalamud Console / Log window"u8, new(ImGui.GetContentRegionAvail().X, -1.0f))) {
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
