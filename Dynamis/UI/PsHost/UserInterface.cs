#if WITH_SMA
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Style;
using Dynamis.UI.PsHost.Input;
using Dynamis.UI.PsHost.Output;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using static Dynamis.Utility.SeStringUtility;

namespace Dynamis.UI.PsHost;

public sealed class UserInterface(HostedPsWindow window) : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
{
    public override PSHostRawUserInterface? RawUI
        => null;

    public override bool SupportsVirtualTerminal
        => false;

    public override string ReadLine()
        => window.Prompt(new LinePrompt(), true, window.CurrentCommandCancellationToken).Result;

    public override SecureString ReadLineAsSecureString()
        => window.Prompt(new SecureLinePrompt(), true, window.CurrentCommandCancellationToken).Result;

    private void Write(IEnumerable<Payload>? before, string value, IEnumerable<Payload>? after, bool appendEndOfLine)
    {
        if (string.IsNullOrEmpty(value)) {
            if (appendEndOfLine) {
                window.EndParagraph();
            }

            return;
        }

        foreach (var (line, eol) in value.Lines(appendEndOfLine)) {
            window.WriteInterpolated($"{before}{AnsiHelper.AnsiCodesToSeString(line)}{after}");
            if (eol) {
                window.EndParagraph();
            }
        }
    }

    public override void Write(string value)
        => Write(null, value, null, false);

    public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
    {
        if (string.IsNullOrEmpty(value)) {
            return;
        }

        if (foregroundColor is ConsoleColor.White && backgroundColor is ConsoleColor.Black) {
            Write(null, value, null, false);
        } else if (foregroundColor is ConsoleColor.White) {
            Write([UiGlow(backgroundColor.ToSeStringColor()),], value, [UiGlowOff,], false);
        } else if (backgroundColor is ConsoleColor.Black) {
            Write([UiForeground(foregroundColor.ToSeStringColor()),], value, [UiForegroundOff,], false);
        } else {
            Write(
                [UiForeground(foregroundColor.ToSeStringColor()), UiGlow(backgroundColor.ToSeStringColor())], value,
                [UiGlowOff, UiForegroundOff,],                                                                false
            );
        }
    }

    public override void WriteLine(string value)
        => Write(null, value, null, true);

    private static DalamudColors BuiltInColors
        => StyleModel.GetFromCurrent().BuiltInColors!;

    public override void WriteErrorLine(string message)
        => Write([Icon(BitmapFontIcon.Warning), UiForeground(16),], message, [UiForegroundOff,], true);

    public override void WriteDebugLine(string message)
        => Write([UiForeground(3),], message, [UiForegroundOff,], true);

    public override void WriteProgress(long sourceId, ProgressRecord record)
        => window.WriteProgress(sourceId, record);

    public override void WriteVerboseLine(string message)
        => Write([UiForeground(4),], message, [UiForegroundOff,], true);

    public override void WriteWarningLine(string message)
        => Write([Icon(BitmapFontIcon.Warning), UiForeground(31),], message, [UiForegroundOff,], true);

    public override Dictionary<string, PSObject> Prompt(string caption, string message,
        Collection<FieldDescription> descriptions)
        => window.Prompt(
                      new FormPrompt(caption, message, descriptions),
                      cancellationToken: window.CurrentCommandCancellationToken
                  )
                 .Result;

    public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        => PromptForCredential(
            caption, message, userName, targetName, PSCredentialTypes.Default, PSCredentialUIOptions.Default
        );

    public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName,
        PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        => window.Prompt(
                      new CredentialPrompt(caption, message, userName, targetName, options),
                      cancellationToken: window.CurrentCommandCancellationToken
                  )
                 .Result;

    public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices,
        int defaultChoice)
        => window.Prompt(
                      new ChoicePrompt(caption, message, choices, defaultChoice),
                      cancellationToken: window.CurrentCommandCancellationToken
                  )
                 .Result;

    public Collection<int> PromptForChoice(string? caption, string? message, Collection<ChoiceDescription> choices,
        IEnumerable<int>? defaultChoices)
        => window.Prompt(
                      new MultipleChoicePrompt(caption, message, choices, defaultChoices),
                      cancellationToken: window.CurrentCommandCancellationToken
                  )
                 .Result;
}
#endif
