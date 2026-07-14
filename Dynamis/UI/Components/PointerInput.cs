using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Interop;
using Dynamis.Utility;

namespace Dynamis.UI.Components;

public sealed class PointerInput(PointerParser pointerParser, ImGuiComponents imGuiComponents, string label)
    : IInput<nint>
{
    private const int BufferCapacity = 2048;

    private readonly byte[] _buffer = InitBuffer();

    private nint _value;

    public string? SubText { get; set; }

    public nint GetValue()
        => _value;

    private static byte[] InitBuffer()
    {
        var buffer = new byte[BufferCapacity];
        FormatValue(buffer, 0);
        return buffer;
    }

    public void SetValue(nint value)
    {
        _value = value;
        FormatValue(_buffer, value);
    }

    public void SetValue(nint? value)
    {
        if (value.HasValue) {
            SetValue(value.Value);
        }
    }

    public bool Draw(ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        bool changed;
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            changed = ImGui.InputText(label, _buffer, flags | ImGuiInputTextFlags.AutoSelectAll);
        }

        if (changed) {
            changed = pointerParser.TryParse(((ReadOnlySpan<byte>)_buffer).BeforeNull(), out var newValue);
            if (changed) {
                _value = newValue;
            }
        }

        if (ImGui.IsItemDeactivatedAfterEdit()) {
            SetValue(_value);
        }

        if (!ImGui.IsItemActive() && SubText is not null) {
            Vector2 mainTextSize;
            using (ImRaii.PushFont(UiBuilder.MonoFont)) {
                mainTextSize = ImGui.CalcTextSize(_buffer.AsSpan().BeforeNull());
            }

            ImGuiComponents.AddInputSubText(mainTextSize.X, SubText);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            imGuiComponents.OpenPointerCopyOnlyContextMenu(_value);
        }

        return changed;
    }

    private static void FormatValue(Span<byte> buffer, nint value)
    {
        if (value.TryFormat(buffer, out var end, GetPointerConfiguration().DotnetFormat) && end < buffer.Length) {
            buffer[end] = 0;
        }
    }

    private static (ImGuiDataType DataType, string CFormat, string DotnetFormat) GetPointerConfiguration()
        => nint.Size switch
        {
            4 => (ImGuiDataType.U32, "%08X", "X8"),
            8 => (ImGuiDataType.U64, "%016llX", "X16"),
            _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
        };
}
