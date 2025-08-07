using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dynamis.ClientStructs;
using Dynamis.Configuration;
using Dynamis.Interop.Ipfd;
using Dynamis.Messaging;
using Dynamis.Utility;
using Microsoft.Extensions.Logging;
using static Dynamis.Utility.ChatGuiUtility;
using static Dynamis.Utility.SeStringUtility;
using ConfigurationEnumExtensions = Dynamis.Configuration.EnumExtensions;

namespace Dynamis.UI.Windows;

public sealed class SettingsWindow : Window, ISingletonWindow, IMessageObserver<CommandMessage>
{
    private readonly ConfigurationContainer _configuration;
    private readonly ImGuiComponents        _imGuiComponents;
    private readonly IChatGui               _chatGui;
    private readonly MessageHub             _messageHub;
    private readonly Ipfd                   _ipfd;

    public SettingsWindow(ConfigurationContainer configuration, ImGuiComponents imGuiComponents, IChatGui chatGui,
        MessageHub messageHub, Ipfd ipfd) : base(
        $"Dynamis {Assembly.GetExecutingAssembly().GetName().Version} Settings###DynamisSettings",
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
    )
    {
        _configuration = configuration;
        _imGuiComponents = imGuiComponents;
        _chatGui = chatGui;
        _messageHub = messageHub;
        _ipfd = ipfd;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(512, 288),
            MaximumSize = new(512, 16384),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public override void Draw()
    {
        if (ImGui.CollapsingHeader("Behavior")) {
            DrawBehavior();
        }

        if (ImGui.CollapsingHeader("Interface")) {
            DrawInterface();
        }

        if (ImGui.CollapsingHeader("Data")) {
            DrawData();
        }

        if (ImGui.CollapsingHeader("Object Inspector Colors")) {
            DrawColors();
        }
    }

    private void DrawBehavior()
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

        var enableIpfd = configuration.EnableIpfd;
        if (ImGui.Checkbox("Enable IPFD (In-Process Faux Debugger)", ref enableIpfd)) {
            configuration.EnableIpfd = enableIpfd;
            _configuration.Save(nameof(configuration.EnableIpfd));
        }

        ImGui.SameLine(0.0f, innerSpacing);
        DrawUnstableSettingWarning();

        ImGui.SameLine();
        using (ImRaii.Disabled(!_ipfd.Loaded)) {
            if (ImGui.Button("Force Unload##ipfd")) {
                _ipfd.Unload();
            }
        }

        var symbolHandlerMode = configuration.SymbolHandlerMode;
        if (Util.IsWine()) {
            var enableWineSymbolHandler = symbolHandlerMode == SymbolHandlerMode.ForceInitialize;
            if (ImGui.Checkbox("Enable Symbol Handler", ref enableWineSymbolHandler)) {
                symbolHandlerMode = enableWineSymbolHandler
                    ? SymbolHandlerMode.ForceInitialize
                    : SymbolHandlerMode.Disable;
                configuration.SymbolHandlerMode = symbolHandlerMode;
                _configuration.Save(nameof(configuration.SymbolHandlerMode));
            }
        } else {
            if (ImGuiComponents.ComboEnum(
                    "Symbol Handler Mode", ref symbolHandlerMode, ConfigurationEnumExtensions.Label
                )) {
                configuration.SymbolHandlerMode = symbolHandlerMode;
                _configuration.Save(nameof(configuration.SymbolHandlerMode));
            }
        }

        ImGui.SameLine(0.0f, innerSpacing);
        DrawUnstableSettingWarning();
    }

    private void DrawUnstableSettingWarning()
    {
        ImGuiComponents.NormalizedIcon(
            FontAwesomeIcon.ExclamationTriangle,
            StyleModel.GetFromCurrent().BuiltInColors!.DalamudOrange!.Value.ToUInt32()
        );

        if (ImGui.IsItemHovered()) {
            using var _ = ImRaii.Tooltip();
            ImGui.TextUnformatted("This setting may cause stability issues.");
            ImGui.TextUnformatted(
                $"Disabling it may then require hand-editing pluginConfigs{(Util.IsWine() ? '/' : '\\')}{_configuration.InternalName}.json."
            );
        }
    }

    private void DrawInterface()
    {
        using (var node = ImRaii.TreeNode("Memory Snapshots###Interface_MemorySnapshots", ImGuiTreeNodeFlags.DefaultOpen)) {
            if (node) {
                DrawInterface_MemorySnapshots();
            }
        }

        using (var node = ImRaii.TreeNode("Miscellaneous###Interface_Miscellaneous")) {
            if (node) {
                DrawInterface_Miscellaneous();
            }
        }
    }

    private void DrawInterface_MemorySnapshots()
    {
        var configuration = _configuration.Configuration;

        var snapshotsAnnotated = configuration.OpenSnapshotsAnnotated;
        if (ImGuiComponents.Combo(
                "Default Display Mode", ref snapshotsAnnotated, [null, false, true,],
                DescribeSnapshotsAnnotated
            )) {
            configuration.OpenSnapshotsAnnotated = snapshotsAnnotated;
            _configuration.Save(nameof(configuration.OpenSnapshotsAnnotated));
        }
    }

    private static string DescribeSnapshotsAnnotated(bool? annotated)
        => annotated switch
        {
            null  => "Inherit Last",
            false => "Compact",
            true  => "Annotated",
        };

    private void DrawInterface_Miscellaneous()
    {
        var configuration = _configuration.Configuration;

        var serious = configuration.Serious;
        if (ImGui.Checkbox("I don't like fun.", ref serious)) {
            configuration.Serious = serious;
            _configuration.Save(nameof(configuration.Serious));
        }
    }

    private void DrawData()
        => DrawDataYaml();

    private void DrawDataYaml()
    {
        var inputWidth = ImGui.GetContentRegionAvail().X * (2.0f / 3.0f);
        var innerSpacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var configuration = _configuration.Configuration;

        ImGui.SetNextItemWidth(
            inputWidth - innerSpacing * 2.0f - ImGuiComponents.NormalizedIconButtonSize(FontAwesomeIcon.Sync).X
          - ImGuiComponents.NormalizedIconButtonSize(
                                configuration.AutomaticDataYaml ? FontAwesomeIcon.Hdd : FontAwesomeIcon.CloudDownloadAlt
                            )
                           .X
        );
        if (configuration.AutomaticDataYaml) {
            using (ImRaii.Disabled()) {
                var dummy = "Automatically download from GitHub";
                ImGui.InputText(
                    "###dataYamlPathDummy", ref dummy, dummy.Length + 1, ImGuiInputTextFlags.ReadOnly
                );
            }

            ImGui.SameLine(0.0f, innerSpacing);
            if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Hdd)) {
                configuration.AutomaticDataYaml = false;
                _configuration.Save(nameof(configuration.AutomaticDataYaml));
            }

            if (ImGui.IsItemHovered()) {
                using var _ = ImRaii.Tooltip();
                ImGui.TextUnformatted("Use a local copy of the file instead");
            }
        } else {
            _imGuiComponents.InputFile(
                "###dataYamlPath", "data.yml{.yml,.yaml}", configuration.DataYamlPath,
                newPath =>
                {
                    _configuration.Configuration.DataYamlPath = newPath;
                    _configuration.Save(nameof(_configuration.Configuration.DataYamlPath));
                }
            );

            ImGui.SameLine(0.0f, innerSpacing);
            if (ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.CloudDownloadAlt)) {
                configuration.AutomaticDataYaml = true;
                _configuration.Save(nameof(configuration.AutomaticDataYaml));
            }

            if (ImGui.IsItemHovered()) {
                using var _ = ImRaii.Tooltip();
                ImGui.TextUnformatted("Automatically download the file from GitHub instead");
            }
        }

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

        DrawDisableLogCategoryCheckbox(typeof(DataYamlContainer), "Silence data.yml-related logs");
    }

    private void DrawDisableLogCategoryCheckbox(Type type, ImU8String label)
    {
        var typeName = type.FullName;
        if (typeName is null) {
            return;
        }

        var configuration = _configuration.Configuration;
        var disabledCategories = configuration.DisabledLogCategories;
        var index = Array.IndexOf(configuration.DisabledLogCategories, typeName);
        var disabled = index >= 0;
        if (ImGui.Checkbox(label, ref disabled)) {
            if (disabled) {
                if (index >= 0) {
                    return;
                }

                Array.Resize(ref disabledCategories, disabledCategories.Length + 1);
                disabledCategories[^1] = typeName;
            } else {
                if (index < 0) {
                    return;
                }

                disabledCategories[index] = disabledCategories[^1];
                Array.Resize(ref disabledCategories, disabledCategories.Length - 1);
            }

            configuration.DisabledLogCategories = disabledCategories;
            _configuration.Save(nameof(configuration.DisabledLogCategories));
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

    private void Print(ref SeStringInterpolatedStringHandler handler, string? messageTag = null,
        ushort? tagColor = null)
        => _chatGui.Print(BuildSeString(ref handler), messageTag, tagColor);

    private void PrintHelp()
    {
        Print($"Valid sub-commands for {UiForeground("/dynamis settings", Gold)}:");

        Print($"    》 {UiForeground("help", Gold)} - Display this help message.");

        Print($"    》 {UiForeground("loglevel", Gold)} - Display the current log level.");
        Print(
            $"        》 {UiForeground("loglevel [trace|debug|information|warning|error|critical|none]", Gold)} {UiForeground("[--quiet]", Red)} - Set the log level."
        );

        Print(
            $"    》 {UiForeground("datayml refresh", Gold)} {UiForeground("[--quiet]", Red)} - Refresh the ClientStructs' data.yml file."
        );

        Print($"    》 {UiForeground("ipfd",             Gold)} - Display IPFD status.");
        Print($"        》 {UiForeground("ipfd enable",  Gold)} {UiForeground("[--quiet]", Red)} - Enable IPFD.");
        Print($"        》 {UiForeground("ipfd disable", Gold)} {UiForeground("[--quiet]", Red)} - Disable IPFD.");
        Print(
            $"        》 {UiForeground("ipfd unload", Gold)} {UiForeground("[--quiet]", Red)} - Force unload IPFD, but doesn't prevent it from being reloaded."
        );
    }

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand("settings", "config", "st", "c")) {
            return;
        }

        if (message.Arguments.Equals(1, null)) {
            message.SetHandled();
            IsOpen = true;
            BringToFront();
            return;
        }

        if (message.Arguments.Equals(1, "help", "?")) {
            message.SetHandled();
            PrintHelp();
            return;
        }

        if (message.Arguments.Equals(1, "loglevel", "loglvl", "log")) {
            LogLevel level;
            if (message.Arguments.Equals(2, null, "query", "get", "status", "?")) {
                message.SetHandled();
                level = (LogLevel)_configuration.Configuration.MinimumLogLevel;
                Print(
                    $"The current log level is {UiForeground(level.ToString(), level == LogLevel.None ? Red : Gold)}.",
                    "Dynamis", Gold
                );
                return;
            }

            if (message.Arguments.Equals(2, "trace", "verbose", "trce", "vrb", "t", "v", "0")) {
                level = LogLevel.Trace;
            } else if (message.Arguments.Equals(2, "debug", "dbug", "dbg", "d", "1")) {
                level = LogLevel.Debug;
            } else if (message.Arguments.Equals(2, "information", "info", "inf", "i", "2")) {
                level = LogLevel.Information;
            } else if (message.Arguments.Equals(2, "warning", "warn", "wrn", "w", "3")) {
                level = LogLevel.Warning;
            } else if (message.Arguments.Equals(2, "error", "fail", "err", "e", "f", "4")) {
                level = LogLevel.Error;
            } else if (message.Arguments.Equals(2, "critical", "fatal", "crit", "ftl", "c", "5")) {
                level = LogLevel.Critical;
            } else if (message.Arguments.Equals(2, "none", "no", "off", "disable", "dis", "n", "-", "6")) {
                level = LogLevel.None;
            } else {
                return;
            }

            message.SetHandled();
            _configuration.Configuration.MinimumLogLevel = (int)level;
            _configuration.Save(nameof(_configuration.Configuration.MinimumLogLevel));
            if (!message.Quiet) {
                Print(
                    $"Log level set to {UiForeground(level.ToString(), level == LogLevel.None ? Red : Gold)}.",
                    "Dynamis", Gold
                );
            }

            return;
        }

        if (message.Arguments.Equals(1, "datayml", "datayaml", "data", "yaml", "yml")
         && message.Arguments.Equals(2, "refresh", "reload",   "r")) {
            message.SetHandled();
            _messageHub.Publish(new ConfigurationChangedMessage(nameof(_configuration.Configuration.DataYamlPath)));
            if (!message.Quiet) {
                Print(
                    $"ClientStructs' data.yml {UiForeground("refreshed", Gold)}.",
                    "Dynamis", Gold
                );
            }

            return;
        }

        if (message.Arguments.Equals(1, "ipfd")) {
            if (message.Arguments.Equals(2, null, "query", "get", "status", "?")) {
                message.SetHandled();
                if (_ipfd.Enabled) {
                    Print(
                        $"IPFD is currently {UiForeground(_ipfd.Loaded ? "enabled and loaded" : "enabled but not loaded", Green)}.",
                        "Dynamis", Gold
                    );
                } else {
                    Print(
                        $"IPFD is currently {UiForeground("disabled", Red)}.",
                        "Dynamis", Gold
                    );
                }

                return;
            }

            if (message.Arguments.Equals(2, "enable", "en", "on", "yes", "y", "+", "1")) {
                message.SetHandled();
                _configuration.Configuration.EnableIpfd = true;
                _configuration.Save(nameof(_configuration.Configuration.EnableIpfd));
                if (!message.Quiet) {
                    Print(
                        $"IPFD {UiForeground("enabled", Green)}.",
                        "Dynamis", Gold
                    );
                }

                return;
            }

            if (message.Arguments.Equals(2, "disable", "dis", "off", "no", "n", "-", "0")) {
                message.SetHandled();
                _configuration.Configuration.EnableIpfd = false;
                _configuration.Save(nameof(_configuration.Configuration.EnableIpfd));
                if (!message.Quiet) {
                    Print(
                        $"IPFD {UiForeground("disabled", Red)}.",
                        "Dynamis", Gold
                    );
                }

                return;
            }

            if (message.Arguments.Equals(
                    2, "unload", "forceunload", "terminate", "kill", "term", "un", "fu", "u", "k", "t"
                )) {
                message.SetHandled();
                _ipfd.Unload();
                if (!message.Quiet) {
                    Print(
                        $"IPFD {UiForeground("force unloaded", Red)}.",
                        "Dynamis", Gold
                    );
                }

                return;
            }
        }
    }
}
