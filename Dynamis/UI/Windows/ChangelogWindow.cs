using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;

namespace Dynamis.UI.Windows;

public sealed class ChangelogWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    public const int ChangelogVersion = 1;

    private readonly ConfigurationContainer _configuration;

    private int _readVersion;

    public ChangelogWindow(ConfigurationContainer configuration, ImGuiComponents imGuiComponents) : base(
        $"Dynamis {Assembly.GetExecutingAssembly().GetName().Version} Changelog###DynamisChangelog",
        ImGuiWindowFlags.NoDocking
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
        _readVersion = _configuration.Configuration.ReadChangelogVersion;
        if (_configuration.Configuration.ReadChangelogVersion < ChangelogVersion) {
            _configuration.Configuration.ReadChangelogVersion = ChangelogVersion;
            _configuration.Save(nameof(_configuration.Configuration.ReadChangelogVersion));
        }
    }

    public override void Draw()
    {
        DrawMarkAsUnread();
        using var twp = new ImRaiiTextWrapPos();
        ImGui.TextUnformatted(
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam finibus et augue eu viverra. Donec porta augue orci, sed sollicitudin quam pulvinar sit amet. In eget arcu ultrices, consequat ipsum eu, facilisis lectus. Donec cursus auctor orci vitae finibus. Curabitur tellus risus, mollis eget orci porttitor, eleifend congue felis. Mauris ex erat, placerat ac libero ac, maximus pellentesque felis. Vestibulum placerat, lacus ac porttitor tristique, nibh quam consectetur ex, a rhoncus magna nisi in arcu. Nulla sit amet nulla nec nunc porttitor faucibus. Morbi in laoreet turpis, ut consectetur odio. Donec vitae venenatis felis, a tristique mi. Integer scelerisque eleifend libero a ornare. Vivamus maximus lorem vel lobortis varius. Nam efficitur faucibus fringilla. Nam ac tristique turpis."u8
        );
    }

    private void DrawMarkAsUnread()
    {
        var markAsRead = _configuration.Configuration.ReadChangelogVersion < ChangelogVersion;
        var label = markAsRead ? "Mark as read"u8 : "Mark as unread"u8;

        ImGui.Dummy(
            new(
                ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(label).X
                                                - ImGui.GetStyle().FramePadding.X * 2.0f, ImGui.GetFrameHeight()
            )
        );
        ImGui.SameLine(0.0f, 0.0f);
        if (!ImGui.Button(label)) {
            return;
        }

        _configuration.Configuration.ReadChangelogVersion = markAsRead ? ChangelogVersion : 0;
        _configuration.Save(nameof(_configuration.Configuration.ReadChangelogVersion));
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
