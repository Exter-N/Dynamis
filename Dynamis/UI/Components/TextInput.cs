using System.Text;
using ImGuiNET;

namespace Dynamis.UI.Components;

public class TextInput(string label, int capacity) : IInput<string>
{
    private readonly byte[] _buffer = new byte[capacity];

    public TextInput(string label, int capacity, string initialValue) : this(label, capacity)
    {
        var byteCount = Encoding.UTF8.GetBytes(initialValue, _buffer);
        if (byteCount < capacity) {
            _buffer[byteCount] = 0;
        }
    }

    public unsafe bool Draw(ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        ImGuiInputTextCallback? callback = null, nint userData = 0)
    {
        fixed (byte* buffer = _buffer) {
            return ImGui.InputText(label, (nint)buffer, (uint)_buffer.Length, flags, callback, userData);
        }
    }

    bool IInput.Draw(ImGuiInputTextFlags flags)
        => Draw(flags);

    public string GetValue()
    {
        ReadOnlySpan<byte> span = _buffer;
        var end = span.IndexOf((byte)0);
        if (end >= 0) {
            span = span[..end];
        }

        return Encoding.UTF8.GetString(span);
    }
}
