using System.Globalization;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Interop.Ipfd;
using Dynamis.Messaging;
using Dynamis.Resources;
using Dynamis.Utility;
using Microsoft.Extensions.Logging;
using static Dynamis.Utility.ChatGuiUtility;
using static Dynamis.Utility.SeStringUtility;

namespace Dynamis.UI.Windows;

public sealed class BreakpointWindowFactory(
    ILogger<BreakpointWindowFactory> logger,
    WindowSystem windowSystem,
    INotificationManager notificationManager,
    IChatGui chatGui,
    ImGuiComponents imGuiComponents,
    ObjectInspector objectInspector,
    Ipfd ipfd,
    ConfigurationContainer configuration,
    MessageHub messageHub,
    ResourceProvider resourceProvider)
    : WindowFactory<BreakpointWindow>(windowSystem),
        IMessageObserver<BreakpointDisposedMessage>,
        IMessageObserver<CommandMessage>
{
    private BreakpointWindow DoCreateWindow(Breakpoint breakpoint)
    {
        messageHub.Publish<DataYamlPreloadMessage>();

        return new(
            logger, WindowSystem, imGuiComponents, objectInspector, configuration, messageHub, breakpoint,
            GetFreeIndex()
        );
    }

    private BreakpointWindow CreateWindow(Breakpoint breakpoint)
    {
        var window = DoCreateWindow(breakpoint);
        SetupWindow(window);
        return window;
    }

    protected override BreakpointWindow? DoCreateWindow()
    {
        Breakpoint breakpoint;
        try {
            breakpoint = ipfd.AllocateBreakpoint();
        } catch (InvalidOperationException e) {
            notificationManager.AddNotification(
                new Notification()
                {
                    Content = e.Message,
                    Title = "Breakpoint allocation failed",
                    Type = NotificationType.Error,
                    Minimized = false,
                    IconTexture = resourceProvider.LoadManifestResourceImage("Dynamis64.png")!,
                }
            );
            return null;
        }

        return DoCreateWindow(breakpoint);
    }

    public void HandleMessage(BreakpointDisposedMessage message)
    {
        var window = OpenWindows.FirstOrDefault(window => window.Breakpoint == message.Breakpoint);
        if (window is not null) {
            window.IsOpen = false;
        }
    }

    private void Print(ref SeStringInterpolatedStringHandler handler, string? messageTag = null,
        ushort? tagColor = null)
        => chatGui.Print(BuildSeString(ref handler), messageTag, tagColor);

    BreakpointWindow? CreateWindowForCommand()
    {
        Breakpoint breakpoint;
        try {
            breakpoint = ipfd.AllocateBreakpoint();
        } catch (InvalidOperationException e) {
            Print($"Breakpoint allocation failed: {e.Message}", "Dynamis", Gold);
            return null;
        }

        return CreateWindow(breakpoint);
    }

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand("breakpoint", "brkp", "bp", "b")) {
            return;
        }

        if (message.Arguments.Equals(1, null)) {
            message.SetHandled();
            CreateWindowForCommand();
            return;
        }

        if (!nint.TryParse(message.Arguments[1], NumberStyles.HexNumber, null, out var address)) {
            return;
        }

        BreakpointFlags condition;
        if (message.Arguments.Equals(2, null, "execute", "exec", "x")) {
            condition = BreakpointFlags.InstructionExecution;
        } else if (message.Arguments.Equals(2, "write", "w")) {
            condition = BreakpointFlags.DataWrites;
        } else if (message.Arguments.Equals(2, "readwrite", "read", "rw", "r")) {
            condition = BreakpointFlags.DataReadsAndWrites;
        } else {
            return;
        }

        BreakpointFlags length;
        if (condition == BreakpointFlags.InstructionExecution || message.Arguments.Equals(3, null)) {
            length = BreakpointFlags.LengthOne;
        } else if (!message.Arguments.TryGetInteger(3, out var rawLength)) {
            return;
        } else {
            switch (rawLength) {
                case 1:
                    length = BreakpointFlags.LengthOne;
                    break;
                case 2:
                    length = BreakpointFlags.LengthTwo;
                    break;
                case 4:
                    length = BreakpointFlags.LengthFour;
                    break;
                case 8:
                    length = BreakpointFlags.LengthEight;
                    break;
                default:
                    return;
            }
        }

        var enabled = message.NamedArguments.ContainsKey("on");
        var hits = -1;
        if (message.NamedArguments.TryGetValue("hits", out var hitsArgs)) {
            if (!hitsArgs.TryGetInteger(0, out hits)) {
                return;
            }
        }

        message.SetHandled();
        CreateWindowForCommand()?.Configure(address, condition, length, enabled, hits);
    }
}
