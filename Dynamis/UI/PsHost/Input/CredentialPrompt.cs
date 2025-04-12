using System.Management.Automation;
using Dalamud.Interface.Utility.Raii;
using Dynamis.UI.Components;
using ImGuiNET;

namespace Dynamis.UI.PsHost.Input;

public sealed class CredentialPrompt(
    string? caption,
    string? message,
    string? userName,
    string? targetName,
    PSCredentialUIOptions options) : BaseFormPrompt(caption, message), IPrompt<PSCredential>
{
    private string _userName = string.IsNullOrEmpty(targetName)
        ? userName ?? string.Empty
        : $"{targetName}\\{userName ?? string.Empty}";

    private readonly SecurePasswordInput _passwordInput = new("Password", 2048);
    private          PSCredential?       _finishedValue;

    protected override int FrameRows
        => string.IsNullOrEmpty(_userName) && options.HasFlag(PSCredentialUIOptions.ReadOnlyUserName) ? 1 : 2;

    public PSCredential Result
        => _finishedValue ?? throw new InvalidOperationException("Value not yet submitted");

    ~CredentialPrompt()
        => _passwordInput.Dispose();

    protected override bool DrawForm(ref bool focus)
    {
        var readOnlyUserName = options.HasFlag(PSCredentialUIOptions.ReadOnlyUserName);
        if (!string.IsNullOrEmpty(_userName) || !readOnlyUserName) {
            if (_finishedValue is null && !readOnlyUserName && focus) {
                ImGui.SetKeyboardFocusHere();
                focus = false;
            }

            if (ImGui.InputText(
                    "User", ref _userName, 2048,
                    _finishedValue is not null || readOnlyUserName
                        ? ImGuiInputTextFlags.ReadOnly
                        : ImGuiInputTextFlags.EnterReturnsTrue
                )) {
                ImGui.SetKeyboardFocusHere();
            }

            using (ImRaii.Disabled()) {
                ImGui.Button("Submit");
            }
        }

        if (_finishedValue is not null) {
            _passwordInput.Draw(ImGuiInputTextFlags.ReadOnly);
            return true;
        }

        if (focus) {
            ImGui.SetKeyboardFocusHere();
            focus = false;
        }

        var enter = _passwordInput.Draw(ImGuiInputTextFlags.EnterReturnsTrue);

        enter |= ImGui.Button("Submit");

        if (enter) {
            _finishedValue = GetValue();
        }

        return enter;
    }

    public override void Cancel()
        => _finishedValue ??= GetValue();

    private PSCredential GetValue(bool destructively = true)
        => new(
            string.IsNullOrEmpty(_userName) && options.HasFlag(PSCredentialUIOptions.ReadOnlyUserName)
                ? "user"
                : _userName, _passwordInput.GetValue(destructively)
        );
}
