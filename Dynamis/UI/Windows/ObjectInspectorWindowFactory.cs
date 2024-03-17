using Dalamud.Interface.Windowing;
using Dynamis.Interop;
using Dynamis.Messaging;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindowFactory : IMessageObserver<OpenWindowMessage<ObjectInspectorWindow>>
{
    private readonly ILogger<ObjectInspectorWindowFactory> _logger;
    private readonly WindowSystem                          _windowSystem;
    private readonly ImGuiComponents                       _imGuiComponents;
    private readonly ObjectInspector                       _objectInspector;

    private int _nextIndex = 0;

    public ObjectInspectorWindowFactory(ILogger<ObjectInspectorWindowFactory> logger, WindowSystem windowSystem,
        ImGuiComponents imGuiComponents, ObjectInspector objectInspector)
    {
        _logger = logger;
        _windowSystem = windowSystem;
        _imGuiComponents = imGuiComponents;
        _objectInspector = objectInspector;
    }

    public void CreateWindow(nint? objectAddress = null)
    {
        var window = new ObjectInspectorWindow(
            _logger, _windowSystem, _imGuiComponents, _objectInspector, _nextIndex++
        );
        _windowSystem.AddWindow(window);
        window.IsOpen = true;
        if (objectAddress.HasValue) {
            window.Inspect(objectAddress.Value);
        }
    }

    public void HandleMessage(OpenWindowMessage<ObjectInspectorWindow> _)
        => CreateWindow();
}
