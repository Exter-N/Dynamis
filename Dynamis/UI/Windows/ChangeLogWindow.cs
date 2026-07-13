using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;

namespace Dynamis.UI.Windows;

public sealed class ChangeLogWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    public const int ChangeLogVersion = 0;

    private readonly ConfigurationContainer _configuration;

    public ChangeLogWindow(ConfigurationContainer configuration, ImGuiComponents imGuiComponents) : base(
        $"Dynamis {Assembly.GetExecutingAssembly().GetName().Version} Change Log###DynamisChangeLog",
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
    )
    {
        _configuration = configuration;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(512, 576),
            MaximumSize = new(512, 16384),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void OnOpen()
    {
        if (_configuration.Configuration.ReadChangeLogVersion < ChangeLogVersion) {
            _configuration.Configuration.ReadChangeLogVersion = ChangeLogVersion;
            _configuration.Save(nameof(_configuration.Configuration.ReadChangeLogVersion));
        }
    }

    public override void Draw()
    {
        DrawMarkAsUnread();
        ImGui.TextUnformatted("TBD"u8);
    }

    private void DrawMarkAsUnread()
    {
        var markAsRead = _configuration.Configuration.ReadChangeLogVersion < ChangeLogVersion;
        var label = markAsRead ? "Mark as read"u8 : "Mark as unread"u8;

        ImGui.Dummy(
            new(
                ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(label).X
                                                - ImGui.GetStyle().FramePadding.X * 2.0f, ImGui.GetFrameHeight()
            )
        );
        ImGui.SameLine(0.0f, 0.0f);
        if (ImGui.Button(label)) {
            _configuration.Configuration.ReadChangeLogVersion = markAsRead ? ChangeLogVersion : 0;
            _configuration.Save(nameof(_configuration.Configuration.ReadChangeLogVersion));
        }
    }

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand(null, "changelog", "cl", "changes", "chg")) {
            return;
        }

        message.SetHandled();
        IsOpen = true;
        BringToFront();
    }
}
