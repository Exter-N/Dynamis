using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.Utility;

namespace Dynamis.UI.Windows;

public sealed class ToolboxWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>, Toolbox.IView
{
    private readonly IUiBuilder             _uiBuilder;
    private readonly MessageHub             _messageHub;
    private readonly Toolbox                _toolbox;
    private readonly ConfigurationContainer _configuration;

    private bool _startOfSection;

    public ToolboxWindow(IUiBuilder uiBuilder, MessageHub messageHub, Toolbox toolbox,
        ConfigurationContainer configuration, ImGuiComponents imGuiComponents)
        : base(
            $"Dynamis {Assembly.GetExecutingAssembly().GetName().Version} Toolbox###DynamisToolbox",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
        )
    {
        _uiBuilder = uiBuilder;
        _messageHub = messageHub;
        _toolbox = toolbox;
        _configuration = configuration;

        Size = new Vector2(384, 288);
        SizeCondition = ImGuiCond.Always;

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        _toolbox.Draw(this);

        ImGui.Dummy(new(16.0f, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing()));
        if (ImGui.Button("Dalamud Console / Log window"u8, new(ImGui.GetContentRegionAvail().X, -1.0f))) {
            _messageHub.Publish<OpenDalamudConsoleMessage>();
        }

        if (_configuration.Configuration.ReadChangelogVersion < ChangelogWindow.ChangelogVersion) {
            using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
            var icon = FontAwesomeIcon.ChevronUp.ToIconString();
            var style = ImGui.GetStyle();
            const float frequency = MathF.PI / 1024.0f;
            var animationFactor = _uiBuilder.ShouldUseReducedMotion
                ? 0.0f
                : MathF.Abs(MathF.Sin((Environment.TickCount64 & 1023) * frequency));
            ImGui.GetWindowDrawList()
                 .AddText(
                      ImGui.GetWindowPos() + new Vector2(
                          (ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize(icon).X) * 0.91f, // HACK
                          ImGui.GetWindowContentRegionMin().Y + animationFactor * 8.0f - style.WindowPadding.Y
                      ), Vector4.Lerp(ImGuiColors.SuccessForeground, ImGuiColors.SuccessBackground, animationFactor)
                                .ToUInt32(), icon
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

    void Toolbox.IView.Begin()
        => _startOfSection = true;

    bool Toolbox.IView.Item(ReadOnlySpan<byte> label)
    {
        if (_startOfSection) {
            _startOfSection = false;
        } else {
            ImGui.SameLine();
            if (ImGui.GetContentRegionAvail().X
              < ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2.0f) {
                ImGui.NewLine();
            }
        }

        return ImGui.Button(label);
    }

    void Toolbox.IView.Separator()
    {
        ImGui.Dummy(new(16.0f, 16.0f));
        _startOfSection = true;
    }
}
