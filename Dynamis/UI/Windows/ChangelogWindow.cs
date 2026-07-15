using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;

namespace Dynamis.UI.Windows;

public sealed partial class ChangelogWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
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
        using var child = ImRaii.Child("###entries"u8);
        using var twp = new ImRaiiTextWrapPos();
        Draw0_1_4_0();
        Draw0_1_3_14();
        Draw0_1_3_13();
        Draw0_1_3_12();
        Draw0_1_3_11();
        Draw0_1_3_10();
        Draw0_1_3_9();
        Draw0_1_3_8();
        Draw0_1_3_7();
        Draw0_1_3_6();
        Draw0_1_3_5();
        Draw0_1_3_3();
        Draw0_1_3_2();
        Draw0_1_3_1();
        Draw0_1_3_0();
        Draw0_1_2_1();
        Draw0_1_2_0();
        Draw0_1_1_0();
        Draw0_1_0_0();
        Draw0_0_1_15();
        Draw0_0_1_14();
        Draw0_0_1_13();
        Draw0_0_1_12();
        Draw0_0_1_5();
        Draw0_0_1_4();
        Draw0_0_1_3();
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

    private bool DrawVersionHeader(int major, int minor, int build, int revision, int changelogVersion)
    {
        using var color = ImRaii.PushColor(
            ImGuiCol.Text, ImGuiColors.SuccessForeground, _readVersion < changelogVersion
        );
        return ImGui.CollapsingHeader(
            $"Version {major}.{minor}.{build}.{revision}",
            _readVersion < changelogVersion ? ImGuiTreeNodeFlags.DefaultOpen : 0
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BulletText(ReadOnlySpan<byte> text)
    {
        // ImGui.BulletText doesn't respect TextWrapPos.
        ImGui.Bullet();
        ImGui.TextUnformatted(text);
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
