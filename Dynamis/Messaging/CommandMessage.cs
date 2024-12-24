using Dynamis.Utility;

namespace Dynamis.Messaging;

public record CommandMessage(CommandArguments Arguments, Dictionary<string, CommandArguments> NamedArguments)
{
    public bool Handled { get; private set; } = false;

    public bool Quiet
        => NamedArguments.ContainsKey("quiet") || NamedArguments.ContainsKey("silent")
                                               || NamedArguments.ContainsKey("q")
                                               || NamedArguments.ContainsKey("s");

    public bool IsSubCommand(string? subCommand)
        => Arguments.Equals(0, subCommand);

    public bool IsSubCommand(params string?[] subCommands)
        => subCommands.Any(IsSubCommand);

    public void SetHandled()
        => Handled = true;
}
