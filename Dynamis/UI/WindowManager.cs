using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Microsoft.Extensions.Hosting;

namespace Dynamis.UI;

public sealed class WindowManager : IHostedService
{
    private readonly MessageHub                _messageHub;
    private readonly UiBuilder                 _uiBuilder;
    private readonly WindowSystem              _windowSystem;
    private readonly FileDialogManager         _fileDialogManager;
    private readonly IEnumerable<Lazy<Window>> _windows;

    public WindowManager(MessageHub messageHub, UiBuilder uiBuilder, WindowSystem windowSystem, FileDialogManager fileDialogManager, IEnumerable<Lazy<Window>> windows)
    {
        _messageHub = messageHub;
        _uiBuilder = uiBuilder;
        _windowSystem = windowSystem;
        _fileDialogManager = fileDialogManager;
        _windows = windows;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _uiBuilder.Draw += Draw;
        _uiBuilder.OpenMainUi += OpenMainUi;
        _uiBuilder.OpenConfigUi += OpenConfigUi;

        foreach (var window in _windows) {
            _windowSystem.AddWindow(window.Value);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _windowSystem.RemoveAllWindows();
        _uiBuilder.OpenConfigUi -= OpenConfigUi;
        _uiBuilder.OpenMainUi -= OpenMainUi;
        _uiBuilder.Draw -= Draw;
        return Task.CompletedTask;
    }

    private void Draw()
    {
        _windowSystem.Draw();
        _fileDialogManager.Draw();
    }

    private void OpenMainUi()
        => _messageHub.Publish<OpenWindowMessage<HomeWindow>>();

    private void OpenConfigUi()
        => _messageHub.Publish<OpenWindowMessage<SettingsWindow>>();
}
