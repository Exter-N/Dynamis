using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Messaging;

namespace Dynamis.UI.Components;

public sealed class SnapshotViewerFactory(
    ConfigurationContainer configuration,
    MessageHub messageHub,
    ObjectInspector objectInspector,
    ModuleAddressResolver moduleAddressResolver,
    ContextMenu contextMenu,
    ImGuiComponents imGuiComponents)
{
    public SnapshotViewer Create()
        => new(configuration, messageHub, objectInspector, moduleAddressResolver, contextMenu, imGuiComponents);
}
