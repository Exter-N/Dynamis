using Dalamud.Interface;
using Dalamud.Interface.Textures;
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

    private IReadOnlyTitleScreenMenuEntry? _entry;

    public LaunchButton(MessageHub messageHub, IUiBuilder uiBuilder, ITitleScreenMenu titleScreenMenu, ResourceProvider resourceProvider)
    {
        _messageHub = messageHub;
        _uiBuilder = uiBuilder;
        _titleScreenMenu = titleScreenMenu;
        _resourceProvider = resourceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var icon = _resourceProvider.LoadManifestResourceImage("Dynamis64.png");
        _entry = _titleScreenMenu.AddEntry("Run: ****mi*", icon, OnTriggered);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_entry is not null) {
            _titleScreenMenu.RemoveEntry(_entry);
            _entry = null;
        }

        return Task.CompletedTask;
    }

    private void OnTriggered()
        => _messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();
}
