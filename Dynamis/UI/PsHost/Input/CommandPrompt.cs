using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using ImGuiNET;

namespace Dynamis.UI.PsHost.Input;

public sealed class CommandPrompt(string prompt, IList<string> commandHistory, Runspace? runspace) : LinePrompt
{
    private int    _historyCursor  = commandHistory.Count;
    private string _currentCommand = string.Empty;

    private string             _completionCommand = string.Empty;
    private int                _completionBytePosition;
    private int                _completionCharPosition;
    private int                _completionReplacementStart;
    private int                _completionReplacementEnd;
    private CommandCompletion? _completionResult;

    public override string Result
    {
        get
        {
            var result = base.Result;
            if (commandHistory.Count == 0 || commandHistory[^1].Trim() != result.Trim()) {
                commandHistory.Add(result);
            }

            return result;
        }
    }

    public override bool Draw(ref bool focus)
    {
        ImGui.Dummy(new(0.01f, Height));
        ImGui.SameLine(0.0f, 0.0f);
        ImGui.TextUnformatted(prompt);
        ImGui.SameLine(0.0f, 0.0f);
        return base.Draw(ref focus);
    }

    protected override unsafe bool DrawInput(byte[] buffer, uint length, ImGuiInputTextFlags flags)
        => ImGui.InputText(
            "###CommandPrompt", buffer, length,
            flags | ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.CallbackCompletion, InputCallback
        ) && buffer[0] != 0;

    private unsafe int InputCallback(ImGuiInputTextCallbackData* data)
    {
        if (data->EventFlag.HasFlag(ImGuiInputTextFlags.CallbackHistory)) {
            return InputCallbackHistory(data);
        }

        if (data->EventFlag.HasFlag(ImGuiInputTextFlags.CallbackCompletion)) {
            return InputCallbackCompletion(data);
        }

        return 0;
    }

    private unsafe int InputCallbackHistory(ImGuiInputTextCallbackData* data)
    {
        switch (data->EventKey) {
            case ImGuiKey.UpArrow:
                if (_historyCursor <= 0) {
                    return 0;
                }

                if (_historyCursor == commandHistory.Count) {
                    _currentCommand = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(data->Buf, data->BufTextLen));
                }

                --_historyCursor;
                break;
            case ImGuiKey.DownArrow:
                if (_historyCursor >= commandHistory.Count) {
                    return 0;
                }

                ++_historyCursor;
                break;
            default:
                return 0;
        }

        var newCommand = _historyCursor < commandHistory.Count ? commandHistory[_historyCursor] : _currentCommand;

        SetText(data, newCommand);
        MoveCursor(data, data->BufTextLen);

        return 0;
    }

    private unsafe int InputCallbackCompletion(ImGuiInputTextCallbackData* data)
    {
        var command = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(data->Buf, data->BufTextLen));
        if (!string.Equals(command, _completionCommand) || data->CursorPos != _completionBytePosition
                                                        || _completionResult is null) {
            _completionCommand = command;
            _completionBytePosition = data->CursorPos;
            _completionCharPosition = Encoding.UTF8.GetCharCount(data->Buf, data->CursorPos);
            using var ps = PowerShell.Create(runspace);
            _completionResult = CommandCompletion.CompleteInput(_completionCommand, _completionCharPosition, [], ps);
            _completionReplacementStart = _completionResult.ReplacementIndex;
            _completionReplacementEnd = _completionReplacementStart + _completionResult.ReplacementLength;
        }

        if (_completionResult.CompletionMatches.Count == 0) {
            return 0;
        }

        var replacement = _completionResult.GetNextResult(!ImGui.GetIO().KeyShift);
        if (replacement is null) {
            return 0;
        }

        command = string.Concat(
            command[.._completionReplacementStart], replacement.CompletionText, command[_completionReplacementEnd..]
        );

        _completionCommand = command;
        _completionReplacementEnd = _completionReplacementStart + replacement.CompletionText.Length;
        _completionCharPosition = _completionReplacementEnd;
        _completionBytePosition = Encoding.UTF8.GetByteCount(command.AsSpan(.._completionCharPosition));

        SetText(data, command);
        MoveCursor(data, _completionBytePosition);

        return 0;
    }
}
