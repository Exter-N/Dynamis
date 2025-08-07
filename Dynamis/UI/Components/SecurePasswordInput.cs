using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dynamis.Utility;

namespace Dynamis.UI.Components;

public sealed class SecurePasswordInput(string label, int capacity) : IInput<SecureString>, IDisposable
{
    // FIXME Figure out whether the password's memory can be protected during edition too.
    private readonly IMemoryOwner<byte> _buffer = new HGlobalBuffer<byte>(capacity, true, true);

    ~SecurePasswordInput()
        => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
        => _buffer.Dispose();

    public bool Draw(ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => ImGui.InputText(label, _buffer.Memory.Span, flags | ImGuiInputTextFlags.Password);

    public bool Draw<TContext>(ImGuiInputTextFlags flags,
        ImGui.ImGuiInputTextCallbackInContextDelegate<TContext> callback, TContext userData)
        => ImGui.InputText(label, _buffer.Memory.Span, flags | ImGuiInputTextFlags.Password, callback, userData);

    bool IInput.Draw(ImGuiInputTextFlags flags)
        => Draw(flags);

    [SkipLocalsInit]
    public unsafe SecureString GetValue(bool destructively = true)
    {
        var span = _buffer.Memory.Span;
        var end = span.IndexOf((byte)0);
        if (end >= 0) {
            span[end..].Clear();
            span = span[..end];
        }

        var charCount = Encoding.UTF8.GetCharCount(span);
        SecureString result;
        if (charCount >= 1024) {
            using var chars = new HGlobalBuffer<char>(charCount, false, true);
            var charSpan = chars.GetSpan();
            Encoding.UTF8.GetChars(span, charSpan);
            if (destructively) {
                span.Fill((byte)'*');
            }

            using var pinnedChars = chars.Pin();
            result = new((char*)pinnedChars.Pointer, charCount);
        } else {
            var chars = stackalloc char[charCount];
            var charSpan = new Span<char>(chars, charCount);
            try {
                Encoding.UTF8.GetChars(span, charSpan);
                if (destructively) {
                    span.Fill((byte)'*');
                }

                result = new(chars, charCount);
            } finally {
                charSpan.Clear();
            }
        }

        result.MakeReadOnly();
        return result;
    }

    SecureString IInput<SecureString>.GetValue()
        => GetValue();
}
