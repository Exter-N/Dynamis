using Dalamud.Plugin.Services;
using Dynamis.Messaging;
using Dynamis.Utility;
using Microsoft.Extensions.Hosting;
using static Dynamis.Utility.ChatGuiUtility;
using static Dynamis.Utility.SeStringUtility;

namespace Dynamis.UI;

public sealed class CommandHandler(MessageHub messageHub, ICommandManager commandManager, IChatGui chatGui)
    : IHostedService, IMessageObserver<OpenDalamudConsoleMessage>, IMessageObserver<CommandMessage>
{
    private const string MainCommandName = "/dynamis";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        commandManager.AddHandler(
            MainCommandName, new(OnCommand)
            {
                HelpMessage = "Open Dynamis' toolbox. Use /dynamis help to list valid sub-commands.",
            }
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        commandManager.RemoveHandler(MainCommandName);

        return Task.CompletedTask;
    }

    private void Print(ref SeStringInterpolatedStringHandler handler, string? messageTag = null,
        ushort? tagColor = null)
        => chatGui.Print(BuildSeString(ref handler), messageTag, tagColor);

    private void OnCommand(string command, string args)
    {
        if (!MainCommandName.Equals(command, StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        var arguments = CommandArguments.ParseWithNamed(args);

        var commandMessage = new CommandMessage(arguments.Positional, arguments.Named);
        messageHub.Publish(commandMessage);
        if (!commandMessage.Handled) {
            Print($"Unrecognized {UiForeground("/dynamis", Gold)} sub-command. Use {UiForeground("/dynamis help", Gold)} to list valid sub-commands.");
        }
    }

    private void PrintHelp()
    {
        Print($"Valid sub-commands for {UiForeground("/dynamis", Gold)}:");

        Print($"    》 {UiForeground("help", Gold)} - Display this help message.");

        Print($"    》 {UiForeground("toolbox", Gold)} - Open the Dynamis toolbox.");

        Print($"    》 {UiForeground("sigscan", Gold)} - Open the signature scanner.");
        Print(
            $"        》 {UiForeground("sigscan", Gold)} {UiForeground("signature", Purple)} {UiForeground("[text|data|module|static]", Gold)} {UiForeground("[offset]", Green)} - Scan the given signature."
        );
        Print($"        》 {UiForeground("sigscan clear", Gold)} - Clear the signature scanner history.");

        Print($"    》 {UiForeground("objtable",             Gold)} - Open the object table.");
        Print($"        》 {UiForeground("objtable refresh", Gold)} - Refresh the object table.");

        Print($"    》 {UiForeground("inspect", Gold)} - Open an object inspector.");
        Print(
            $"        》 {UiForeground("inspect", Gold)} {UiForeground("address", Blue)} - Inspect the object at the given address."
        );

        Print($"    》 {UiForeground("breakpoint", Gold)} - Open an IPFD breakpoint.");
        Print(
            $"        》 {UiForeground("breakpoint", Gold)} {UiForeground("address", Blue)} {UiForeground("[execute|write|rw]", Gold)} {UiForeground("[length]", Green)} {UiForeground("[--on]", Red)} {UiForeground("[--hits=", Red)}{UiForeground("N", Green)}{UiForeground("]", Red)} - Set an IPFD breakpoint at the given address."
        );

        Print($"    》 {UiForeground("settings", Gold)} - Open the Dynamis settings.");
        Print(
            $"        》 {UiForeground("settings help", Gold)} - Displays a more detailed help message about settings sub-commands."
        );
    }

    public void HandleMessage(OpenDalamudConsoleMessage _)
        => commandManager.ProcessCommand("/xllog");

    public void HandleMessage(CommandMessage command)
    {
        if (!command.IsSubCommand("help", "?")) {
            return;
        }

        command.SetHandled();
        PrintHelp();
    }
}
