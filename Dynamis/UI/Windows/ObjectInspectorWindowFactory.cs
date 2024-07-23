using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Messaging;
using Dynamis.UI.ObjectInspectors;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindowFactory : IMessageObserver<OpenWindowMessage<ObjectInspectorWindow>>,
    IMessageObserver<InspectObjectMessage>
{
    private readonly ILogger<ObjectInspectorWindowFactory> _logger;
    private readonly WindowSystem                          _windowSystem;
    private readonly ImGuiComponents                       _imGuiComponents;
    private readonly ObjectInspector                       _objectInspector;
    private readonly ConfigurationContainer                _configuration;
    private readonly MessageHub                            _messageHub;
    private readonly Lazy<ObjectInspectorDispatcher>       _objectInspectorDispatcher;

    private readonly HashSet<ObjectInspectorWindow> _openWindows     = [];
    private readonly HashSet<int>                   _reusableIndices = [];
    private          int                            _nextIndex       = 0;

    public ObjectInspectorWindowFactory(ILogger<ObjectInspectorWindowFactory> logger, WindowSystem windowSystem,
        ImGuiComponents imGuiComponents, ObjectInspector objectInspector, ConfigurationContainer configuration,
        MessageHub messageHub, Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher)
    {
        _logger = logger;
        _windowSystem = windowSystem;
        _imGuiComponents = imGuiComponents;
        _objectInspector = objectInspector;
        _configuration = configuration;
        _messageHub = messageHub;
        _objectInspectorDispatcher = objectInspectorDispatcher;
    }

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
            _logger, _windowSystem, _imGuiComponents, _objectInspector, _configuration, _messageHub,
            _objectInspectorDispatcher, GetFreeIndex()
        );
        window.Close += WindowClose;
        _windowSystem.AddWindow(window);
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
