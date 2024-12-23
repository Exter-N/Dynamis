using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using Dynamis.Resources;
using Dynamis.UI.Windows;
using Microsoft.Extensions.Hosting;

namespace Dynamis.UI;

public sealed class LaunchButton : IHostedService
{
    private readonly MessageHub       _messageHub;
    private readonly IUiBuilder       _uiBuilder;
    private readonly ITitleScreenMenu _titleScreenMenu;
    private readonly ResourceProvider _resourceProvider;

    private readonly object                         _syncRoot = new();
    private          bool                           _createPending;
    private          Task<IDalamudTextureWrap>?     _icon;
    private          IReadOnlyTitleScreenMenuEntry? _entry;

    public LaunchButton(MessageHub messageHub, IUiBuilder uiBuilder, ITitleScreenMenu titleScreenMenu, ResourceProvider resourceProvider)
    {
        _messageHub = messageHub;
        _uiBuilder = uiBuilder;
        _titleScreenMenu = titleScreenMenu;
        _resourceProvider = resourceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_syncRoot) {
            if (!_createPending) {
                _icon = _resourceProvider.LoadManifestResourceImageAsync("Dynamis64.png");
                _uiBuilder.Draw += CreateEntry;
                _createPending = true;
            }
        }

        return Task.CompletedTask;
    }

    private void CreateEntry()
    {
        lock (_syncRoot) {
            if (_icon is not null) {
                if (!_icon.IsCompleted) {
                    return;
                }
            }

            if (_createPending) {
                _uiBuilder.Draw -= CreateEntry;
                _createPending = false;
            }
        }

        if (_icon?.Result is not null) {
            _entry = _titleScreenMenu.AddEntry("Run: ****mi*", _icon.Result, OnTriggered);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task<IDalamudTextureWrap>? icon;
        lock (_syncRoot) {
            if (_createPending) {
                _uiBuilder.Draw -= CreateEntry;
                _createPending = false;
            }

            if (_entry is not null) {
                _titleScreenMenu.RemoveEntry(_entry);
                _entry = null;
            }

            icon = _icon;
            _icon = null;
        }

        if (icon is not null) {
            (await icon).Dispose();
        }
    }

    private void OnTriggered()
        => _messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();
}
