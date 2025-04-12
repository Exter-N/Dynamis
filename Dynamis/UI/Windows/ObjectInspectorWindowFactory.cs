using System.Globalization;
using Dalamud.Interface.Windowing;
using Dynamis.ClientStructs;
using Dynamis.Interop;
using Dynamis.Messaging;
using Dynamis.UI.Components;
using Dynamis.UI.ObjectInspectors;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindowFactory(
    ILogger<ObjectInspectorWindowFactory> logger,
    WindowSystem windowSystem,
    ImGuiComponents imGuiComponents,
    DataYamlContainer dataYamlContainer,
    ObjectInspector objectInspector,
    SnapshotViewerFactory snapshotViewerFactory,
    Lazy<ObjectInspectorDispatcher> objectInspectorDispatcher)
    : WindowFactory<ObjectInspectorWindow>(windowSystem),
        IMessageObserver<InspectObjectMessage>,
        IMessageObserver<CommandMessage>
{
    protected override ObjectInspectorWindow DoCreateWindow()
    {
        dataYamlContainer.Preload();

        return new(
            logger, WindowSystem, imGuiComponents, dataYamlContainer, objectInspector, snapshotViewerFactory,
            objectInspectorDispatcher, GetFreeIndex()
        );
    }

    public void HandleMessage(InspectObjectMessage message)
    {
        var window = OpenWindows.FirstOrDefault(
            message.Snapshot is not null
                ? window => window.Snapshot == message.Snapshot
                : message.Class is not null
                    ? window => (window.Snapshot?.Address ?? window.ObjectAddress) == message.ObjectAddress
                             && window.Snapshot?.Class == message.Class
                    : window => (window.Snapshot?.Address ?? window.ObjectAddress) == message.ObjectAddress
        );
        if (window is not null) {
            window.BringToFront();
            return;
        }

        window = CreateWindow()!;
        if (message.Snapshot is not null) {
            window.Inspect(message.Snapshot);
        } else {
            window.Inspect(message.ObjectAddress, message.Class);
        }
    }

    public void HandleMessage(CommandMessage message)
    {
        if (!message.IsSubCommand("inspect", "inspector", "i")) {
            return;
        }

        if (message.Arguments.Equals(1, null)) {
            message.SetHandled();
            CreateWindow();
            return;
        }

        if (!nint.TryParse(message.Arguments[1], NumberStyles.HexNumber, null, out var address)) {
            return;
        }

        message.SetHandled();

        var window =
            OpenWindows.FirstOrDefault(window => (window.Snapshot?.Address ?? window.ObjectAddress) == address);
        if (window is not null) {
            window.BringToFront();
            return;
        }

        CreateWindow()!.Inspect(address, null);
    }
}
