using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Interop.Ipfd;
using Dynamis.Messaging;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class BreakpointWindowFactory(
    ILogger<BreakpointWindowFactory> logger,
    WindowSystem windowSystem,
    INotificationManager notificationManager,
    ImGuiComponents imGuiComponents,
    ObjectInspector objectInspector,
    Ipfd ipfd,
    ConfigurationContainer configuration,
    MessageHub messageHub)
    : IMessageObserver<OpenWindowMessage<BreakpointWindow>>,
        IMessageObserver<BreakpointDisposedMessage>
{
    private readonly HashSet<BreakpointWindow> _openWindows     = [];
    private readonly HashSet<int>              _reusableIndices = [];
    private          int                       _nextIndex       = 0;

    private int GetFreeIndex()
    {
        foreach (var index in _reusableIndices) {
            _reusableIndices.Remove(index);
            return index;
        }

        return _nextIndex++;
    }

    private BreakpointWindow CreateWindow(Breakpoint breakpoint)
    {
        var window = new BreakpointWindow(
            logger, windowSystem, imGuiComponents, objectInspector, configuration, messageHub, breakpoint,
            GetFreeIndex()
        );
        window.Close += WindowClose;
        windowSystem.AddWindow(window);
        window.IsOpen = true;
        _openWindows.Add(window);
        window.BringToFront();

        return window;
    }

    private void WindowClose(object? sender, EventArgs e)
    {
        if (sender is not BreakpointWindow window) {
            return;
        }

        _openWindows.Remove(window);
        _reusableIndices.Add(window.Index);
    }

    public void HandleMessage(OpenWindowMessage<BreakpointWindow> _)
    {
        Breakpoint breakpoint;
        try {
            breakpoint = ipfd.AllocateBreakpoint();
        } catch (InvalidOperationException e) {
            notificationManager.AddNotification(new Notification()
            {
                Content = e.Message,
                Title = "Breakpoint allocation failed",
                Type = NotificationType.Error,
                Minimized = false,
            });
            return;
        }

        CreateWindow(breakpoint);
    }

    public void HandleMessage(BreakpointDisposedMessage message)
    {
        var window = _openWindows.FirstOrDefault(window => window.Breakpoint == message.Breakpoint);
        if (window is not null) {
            window.IsOpen = false;
        }
    }
}
