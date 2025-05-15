using Dalamud.Interface;
using Dalamud.Plugin.Services;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.Resources;
using Dynamis.UI.Windows;
using Microsoft.Extensions.Hosting;

namespace Dynamis.UI;

public sealed class LaunchButton : IHostedService, IMessageObserver<ConfigurationChangedMessage>
{
    private readonly MessageHub             _messageHub;
    private readonly ITitleScreenMenu       _titleScreenMenu;
    private readonly ResourceProvider       _resourceProvider;
    private readonly ConfigurationContainer _configuration;

    private IReadOnlyTitleScreenMenuEntry? _entry;

    public LaunchButton(MessageHub messageHub, ITitleScreenMenu titleScreenMenu, ResourceProvider resourceProvider,
        ConfigurationContainer configuration)
    {
        _messageHub = messageHub;
        _titleScreenMenu = titleScreenMenu;
        _resourceProvider = resourceProvider;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        CreateEntry();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        DestroyEntry();
        return Task.CompletedTask;
    }

    private void CreateEntry()
    {
        DestroyEntry();
        var icon = _resourceProvider.LoadManifestResourceImage("Dynamis64.png");
        _entry = _titleScreenMenu.AddEntry(_configuration.Configuration.Serious ? "Dynamis Toolbox" : "Run: ****mi*", icon, OnTriggered);
    }

    private void DestroyEntry()
    {
        if (_entry is not null) {
            _titleScreenMenu.RemoveEntry(_entry);
            _entry = null;
        }
    }

    private void OnTriggered()
        => _messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (_entry is not null && message.IsPropertyChanged(nameof(_configuration.Configuration.Serious))) {
            CreateEntry();
        }
    }
}
