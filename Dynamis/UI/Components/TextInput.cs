using System.Text;
using Dalamud.Bindings.ImGui;

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

    public bool Draw(ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => ImGui.InputText(label, _buffer, flags);

    public bool Draw<TContext>(ImGuiInputTextFlags flags,
        ImGui.ImGuiInputTextCallbackInContextDelegate<TContext> callback, TContext userData)
        => ImGui.InputText(label, _buffer, flags, callback, userData);

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
