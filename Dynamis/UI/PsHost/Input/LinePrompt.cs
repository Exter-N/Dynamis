#if WITH_SMA
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Utility;

namespace Dynamis.UI.PsHost.Input;

public class LinePrompt : IPrompt<string>
{
    private readonly int _id = IPrompt.AllocateId();

    private          bool   _finished;
    private readonly byte[] _value = new byte[2048];

    public float Height
        => ImGui.GetFrameHeight();

    public virtual string Result
    {
        get
        {
            var span = _value.AsSpan();
            var end = span.IndexOf((byte)0);
            if (end >= 0) {
                span = span[..end];
            }

            return Encoding.UTF8.GetString(span);
        }
    }

    public virtual bool Draw(ref bool focus)
    {
        using var _ = ImRaii.PushId(_id);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

        if (_finished) {
            DrawInput(_value, ImGuiInputTextFlags.ReadOnly);
            return true;
        }

        if (focus) {
            ImGui.SetKeyboardFocusHere();
            focus = false;
        }

        if (!DrawInput(_value, ImGuiInputTextFlags.EnterReturnsTrue)) {
            return false;
        }

        _finished = true;
        return true;
    }

    protected virtual bool DrawInput(byte[] buffer, ImGuiInputTextFlags flags)
        => ImGui.InputText("###LinePrompt", buffer, flags);

    public void Cancel()
        => _finished = true;

    protected static void SetText(scoped ref ImGuiInputTextCallbackData data, ReadOnlySpan<char> text)
    {
        data.BufTextLen = text.WriteNullTerminated(data.BufSpan);
        data.BufDirty = 1;
    }

    protected static void MoveCursor(scoped ref ImGuiInputTextCallbackData data, int position)
    {
        data.CursorPos = position;
        data.SelectionStart = position;
        data.SelectionEnd = position;
    }
}
#endif
