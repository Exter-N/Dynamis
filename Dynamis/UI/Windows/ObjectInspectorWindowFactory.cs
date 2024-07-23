using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Messaging;
using Dynamis.UI.ObjectInspectors;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindowFactory(
    ILogger<ObjectInspectorWindowFactory> logger,
    WindowSystem windowSystem,
    ImGuiComponents imGuiComponents,
    ObjectInspector objectInspector,
    ConfigurationContainer configuration,
    MessageHub messageHub,
    Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher)
    : IMessageObserver<OpenWindowMessage<ObjectInspectorWindow>>,
        IMessageObserver<InspectObjectMessage>
{
    private readonly HashSet<ObjectInspectorWindow> _openWindows     = [];
    private readonly HashSet<int>                   _reusableIndices = [];
    private          int                            _nextIndex       = 0;

    private int GetFreeIndex()
    {
        foreach (var index in _reusableIndices) {
            _reusableIndices.Remove(index);
            return index;
        }

        return _nextIndex++;
    }

    private ObjectInspectorWindow CreateWindow()
    {
        var window = new ObjectInspectorWindow(
            logger, windowSystem, imGuiComponents, objectInspector, configuration, messageHub,
            objectInspectorDispatcher, GetFreeIndex()
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
        if (sender is not ObjectInspectorWindow window) {
            return;
        }

        _openWindows.Remove(window);
        _reusableIndices.Add(window.Index);
    }

    public void HandleMessage(OpenWindowMessage<ObjectInspectorWindow> _)
        => CreateWindow();

    public void HandleMessage(InspectObjectMessage message)
    {
        var window = _openWindows.FirstOrDefault(
            window => window.ObjectAddress == message.ObjectAddress
                   && (message.Class is null || window.Class == message.Class)
        );
        if (window is not null) {
            window.BringToFront();
            return;
        }

        CreateWindow().Inspect(message.ObjectAddress, message.Class);
    }
}
