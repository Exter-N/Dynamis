using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dynamis.Interop;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Microsoft.Extensions.Hosting;

namespace Dynamis.UI;

public sealed class WindowManager(
    MessageHub messageHub,
    IUiBuilder uiBuilder,
    WindowSystem windowSystem,
    FileDialogManager fileDialogManager,
    ContextMenu contextMenu,
    TextureArraySlicer textureArraySlicer,
    IEnumerable<Lazy<Window>> windows)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        uiBuilder.Draw += Draw;
        uiBuilder.OpenMainUi += OpenMainUi;
        uiBuilder.OpenConfigUi += OpenConfigUi;

        foreach (var window in windows) {
            windowSystem.AddWindow(window.Value);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var window in windowSystem.Windows) {
            (window as IDisposable)?.Dispose();
        }
        windowSystem.RemoveAllWindows();
        uiBuilder.OpenConfigUi -= OpenConfigUi;
        uiBuilder.OpenMainUi -= OpenMainUi;
        uiBuilder.Draw -= Draw;
        return Task.CompletedTask;
    }

    private void Draw()
    {
        windowSystem.Draw();
        fileDialogManager.Draw();
        contextMenu.Draw();
        textureArraySlicer.Tick();
    }

    private void OpenMainUi()
        => messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();

    private void OpenConfigUi()
        => messageHub.Publish<OpenWindowMessage<SettingsWindow>>();
}
