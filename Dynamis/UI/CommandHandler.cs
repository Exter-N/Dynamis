using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Microsoft.Extensions.Hosting;

namespace Dynamis.UI;

public sealed class CommandHandler : IHostedService, IMessageObserver<OpenDalamudConsoleMessage>
{
    public const string MainCommandName = "/dynamis";

    private readonly MessageHub      _messageHub;
    private readonly ICommandManager _commandManager;

    public CommandHandler(MessageHub messageHub, ICommandManager commandManager)
    {
        _messageHub = messageHub;
        _commandManager = commandManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _commandManager.AddHandler(
            MainCommandName, new(OnCommand)
            {
                HelpMessage = "Open Dynamis' main window.",
            }
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _commandManager.RemoveHandler(MainCommandName);

        return Task.CompletedTask;
    }

    private void OnCommand(string command, string args)
        => _messageHub.Publish<OpenWindowMessage<ToolboxWindow>>();

    public void HandleMessage(OpenDalamudConsoleMessage _)
        => _commandManager.ProcessCommand("/xllog");
}
