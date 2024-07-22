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

    private int _nextIndex = 0;

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

    public void CreateWindow(nint? objectAddress = null, ClassInfo? @class = null)
    {
        var window = new ObjectInspectorWindow(
            _logger, _windowSystem, _imGuiComponents, _objectInspector, _configuration, _messageHub,
            _objectInspectorDispatcher, _nextIndex++
        );
        _windowSystem.AddWindow(window);
        window.IsOpen = true;
        if (objectAddress.HasValue) {
            window.Inspect(objectAddress.Value, @class);
        }
    }

    public void HandleMessage(OpenWindowMessage<ObjectInspectorWindow> _)
        => CreateWindow();

    public void HandleMessage(InspectObjectMessage message)
        => CreateWindow(message.ObjectAddress, message.Class);
}
