#if WITH_SMA
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using Dynamis.UI.PsHost.Output;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.PsHost;

public sealed class BackgroundUserInterface(ILogger logger)
    : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
{
    public override PSHostRawUserInterface? RawUI
        => null;

    public override bool SupportsVirtualTerminal
        => false;

    public override string ReadLine()
        => string.Empty;

    public override SecureString ReadLineAsSecureString()
    {
        var str = new SecureString();
        str.MakeReadOnly();
        return str;
    }

    public override void Write(string value)
        => logger.LogInformation("{PSMessage}", AnsiHelper.StripAnsiCodes(value));

    public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        => logger.LogInformation("{PSMessage}", AnsiHelper.StripAnsiCodes(value));

    public override void WriteLine(string value)
        => logger.LogInformation("{PSMessage}", AnsiHelper.StripAnsiCodes(value));

    public override void WriteErrorLine(string value)
        => logger.LogError("{PSMessage}", AnsiHelper.StripAnsiCodes(value));

    public override void WriteDebugLine(string message)
        => logger.LogDebug("{PSMessage}", AnsiHelper.StripAnsiCodes(message));

    public override void WriteProgress(long sourceId, ProgressRecord record)
    {
    }

    public override void WriteVerboseLine(string message)
        => logger.LogTrace("{PSMessage}", AnsiHelper.StripAnsiCodes(message));

    public override void WriteWarningLine(string message)
        => logger.LogWarning("{PSMessage}", AnsiHelper.StripAnsiCodes(message));

    public override Dictionary<string, PSObject> Prompt(string caption, string message,
        Collection<FieldDescription> descriptions)
        => descriptions.ToDictionary(description => description.Name, description => description.DefaultValue);

    public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        => PSCredential.Empty;

    public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName,
        PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        => PSCredential.Empty;

    public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices,
        int defaultChoice)
        => defaultChoice;

    public Collection<int> PromptForChoice(string? caption, string? message, Collection<ChoiceDescription> choices,
        IEnumerable<int>? defaultChoices)
        => [..defaultChoices ?? [],];
}
#endif
