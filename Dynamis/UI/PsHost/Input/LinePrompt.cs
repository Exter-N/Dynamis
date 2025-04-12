using System.Text;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

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
            DrawInput(_value, (uint)_value.Length, ImGuiInputTextFlags.ReadOnly);
            return true;
        }

        if (focus) {
            ImGui.SetKeyboardFocusHere();
            focus = false;
        }

        if (!DrawInput(_value, (uint)_value.Length, ImGuiInputTextFlags.EnterReturnsTrue)) {
            return false;
        }

        _finished = true;
        return true;
    }

    protected virtual bool DrawInput(byte[] buffer, uint length, ImGuiInputTextFlags flags)
        => ImGui.InputText("###LinePrompt", buffer, length, flags);

    public void Cancel()
        => _finished = true;

    protected static unsafe void SetText(ImGuiInputTextCallbackData* data, ReadOnlySpan<char> text)
    {
        var byteCount = Encoding.UTF8.GetBytes(text, new(data->Buf, data->BufSize));
        if (byteCount == data->BufSize) {
            while (byteCount > 0 && (data->Buf[byteCount] & 0xC0) == 0x80) {
                --byteCount;
            }

            --byteCount;
        }

        data->Buf[byteCount] = 0;
        data->BufTextLen = byteCount;
        data->BufDirty = 1;
    }

    protected static unsafe void MoveCursor(ImGuiInputTextCallbackData* data, int position)
    {
        data->CursorPos = position;
        data->SelectionStart = position;
        data->SelectionEnd = position;
    }
}
