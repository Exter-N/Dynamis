using System.Security;
using Dalamud.Interface.Utility.Raii;
using Dynamis.UI.Components;
using ImGuiNET;

namespace Dynamis.UI.PsHost.Input;

public sealed class SecureLinePrompt : IPrompt<SecureString>
{
    private readonly int _id = IPrompt.AllocateId();

    private readonly SecurePasswordInput _input = new("###SecureLinePrompt", 2048);
    private          SecureString?       _finishedValue;

    public float Height
        => ImGui.GetFrameHeight();

    public SecureString Result
        => _finishedValue ?? throw new InvalidOperationException("Value not yet submitted");

    ~SecureLinePrompt()
        => _input.Dispose();

    public bool Draw(ref bool focus)
    {
        using var _ = ImRaii.PushId(_id);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

        if (_finishedValue is not null) {
            _input.Draw(ImGuiInputTextFlags.ReadOnly);
            return true;
        }

        if (focus) {
            ImGui.SetKeyboardFocusHere();
            focus = false;
        }

        if (!_input.Draw(ImGuiInputTextFlags.EnterReturnsTrue)) {
            return false;
        }

        _finishedValue = _input.GetValue();
        return true;
    }

    public void Cancel()
        => _finishedValue ??= _input.GetValue();
}
