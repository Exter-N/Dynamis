using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.Utility;

namespace Dynamis.UI.Windows;

public sealed class ToolboxWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    private readonly MessageHub             _messageHub;
    private readonly Toolbox                _toolbox;
    private readonly ConfigurationContainer _configuration;

    public ToolboxWindow(MessageHub messageHub, Toolbox toolbox, ConfigurationContainer configuration,
        ImGuiComponents imGuiComponents)
        : base(
            $"Dynamis {Assembly.GetExecutingAssembly().GetName().Version} Toolbox###DynamisToolbox",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
        )
    {
        _messageHub = messageHub;
        _toolbox = toolbox;
        _configuration = configuration;

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

        if (_configuration.Configuration.ReadChangeLogVersion < ChangeLogWindow.ChangeLogVersion) {
            ImGui.GetWindowDrawList()
                 .AddText(
                      ImGui.GetWindowPos() + new Vector2(
                          (ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize("(NEW!)"u8).X) * 0.94f,
                          ImGui.GetWindowContentRegionMin().Y
                      ), ImGuiColors.SuccessForeground.ToUInt32(), "(NEW!)"u8
                  );
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
