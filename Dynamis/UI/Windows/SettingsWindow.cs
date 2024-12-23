using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.Utility;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class SettingsWindow : Window, ISingletonWindow
{
    private readonly ConfigurationContainer _configuration;
    private readonly ImGuiComponents        _imGuiComponents;
    private readonly MessageHub             _messageHub;

    public SettingsWindow(ConfigurationContainer configuration, ImGuiComponents imGuiComponents, MessageHub messageHub) : base(
        "Dynamis Settings",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
    )
    {
        _configuration = configuration;
        _imGuiComponents = imGuiComponents;
        _messageHub = messageHub;

        Size = new Vector2(512, 288);
        SizeCondition = ImGuiCond.Always;

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        if (ImGui.CollapsingHeader("Options", ImGuiTreeNodeFlags.DefaultOpen)) {
            DrawOptions();
        }

        if (ImGui.CollapsingHeader("Colors")) {
            DrawColors();
        }
    }

    private void DrawOptions()
    {
        var inputWidth = ImGui.GetContentRegionAvail().X * (2.0f / 3.0f);
        var innerSpacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var configuration = _configuration.Configuration;

        ImGui.SetNextItemWidth(inputWidth);
        var logLevel = (LogLevel)configuration.MinimumLogLevel;
        if (ImGuiComponents.ComboEnum("Log Level", ref logLevel)) {
            configuration.MinimumLogLevel = (int)logLevel;
            _configuration.Save(nameof(configuration.MinimumLogLevel));
        }

        ImGui.SetNextItemWidth(inputWidth - innerSpacing - ImGuiComponents.NormalizedIconButtonSize(FontAwesomeIcon.Sync).X);
        _imGuiComponents.InputFile(
            "###dataYamlPath", "data.yml{.yml,.yaml}", configuration.DataYamlPath,
            newPath =>
            {
                _configuration.Configuration.DataYamlPath = newPath;
                _configuration.Save(nameof(_configuration.Configuration.DataYamlPath));
            }
        );

        ImGui.SameLine(0.0f, innerSpacing);
        if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Sync)) {
            _messageHub.Publish(new ConfigurationChangedMessage(nameof(_configuration.Configuration.DataYamlPath)));
        }

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            ImGui.TextUnformatted("Refresh the file");
        }

        ImGui.SameLine(0.0f, innerSpacing);
        ImGui.TextUnformatted("ClientStructs' data.yml");

        var enableIpfd = configuration.EnableIpfd;
        if (ImGui.Checkbox("Enable IPFD (In-Process Faux Debugger)", ref enableIpfd)) {
            configuration.EnableIpfd = enableIpfd;
            _configuration.Save(nameof(configuration.EnableIpfd));
        }
    }

    private void DrawColors()
    {
        var inputWidth = ImGui.GetContentRegionAvail().X * (2.0f / 3.0f);
        var configuration = _configuration.Configuration;

        var hexViewerPalette = configuration.GetHexViewerPalette();
        for (var i = 0; i < hexViewerPalette.Length; ++i) {
            var color = hexViewerPalette[i].ToVector3();
            ImGui.SetNextItemWidth(inputWidth);
            if (ImGui.ColorEdit3(((HexViewerColor)i).ToString(), ref color)) {
                hexViewerPalette[i] = color.ToUInt32();
                _configuration.Save(nameof(_configuration.Configuration.HexViewerPalette));
            }
        }
    }
}
