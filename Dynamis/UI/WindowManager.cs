using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using Microsoft.Extensions.Hosting;

namespace Dynamis.UI;

public sealed class WindowManager(
    MessageHub messageHub,
    IUiBuilder uiBuilder,
    ConfigurationContainer configuration,
    ImGuiComponents imGuiComponents,
    DevMenuItem devMenuItem,
    WindowSystem windowSystem,
    FileDialogManager fileDialogManager,
    ContextMenu contextMenu,
    TextureArraySlicer textureArraySlicer,
    ObjectInspector objectInspector,
    AddressIdentifier addressIdentifier,
    ShortLivedSingleCacheFactory shortLivedSingleCacheFactory,
    IEnumerable<Lazy<Window>> windows)
    : IHostedService, IMessageObserver<ConfigurationChangedMessage>
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        ApplyConfiguration();
        uiBuilder.Draw += Draw;
        uiBuilder.OpenMainUi += OpenMainUi;
        uiBuilder.OpenConfigUi += OpenConfigUi;
        uiBuilder.DefaultStyleChanged += DefaultStyleChanged;

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
        uiBuilder.DefaultStyleChanged -= DefaultStyleChanged;
        uiBuilder.OpenConfigUi -= OpenConfigUi;
        uiBuilder.OpenMainUi -= OpenMainUi;
        uiBuilder.Draw -= Draw;
        return Task.CompletedTask;
    }

    private void Draw()
    {
        devMenuItem.Draw();
        windowSystem.Draw();
        fileDialogManager.Draw();
        contextMenu.Draw();
        textureArraySlicer.Tick();
        objectInspector.Tick();
        addressIdentifier.Tick();
        shortLivedSingleCacheFactory.Tick();
    }

    private void OpenMainUi()
        => messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();

    private void OpenConfigUi()
        => messageHub.Publish<OpenWindowMessage<SettingsWindow>>();

    private void DefaultStyleChanged()
        => imGuiComponents.Update();

    private void ApplyConfiguration()
    {
        var config = configuration.Configuration;

        uiBuilder.DisableAutomaticUiHide = config.DisableAutomaticUiHide;
        uiBuilder.DisableCutsceneUiHide = config.DisableCutsceneUiHide;
        uiBuilder.DisableGposeUiHide = config.DisableGposeUiHide;
        uiBuilder.DisableUserUiHide = config.DisableUserUiHide;
    }

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (message.IsPropertyChanged(nameof(configuration.Configuration.DisableAutomaticUiHide))
         || message.IsPropertyChanged(nameof(configuration.Configuration.DisableCutsceneUiHide))
         || message.IsPropertyChanged(nameof(configuration.Configuration.DisableGposeUiHide))
         || message.IsPropertyChanged(nameof(configuration.Configuration.DisableUserUiHide))) {
            ApplyConfiguration();
        }
    }
}
