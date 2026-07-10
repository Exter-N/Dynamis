using System.Globalization;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dynamis.ClientStructs;
using Dynamis.Interop;
using Dynamis.Utility;

namespace Dynamis.UI.Components;

public sealed class PointerInput(
    DataYamlContainer dataYamlContainer,
    ModuleAddressResolver moduleAddressResolver,
    ImGuiComponents imGuiComponents,
    string label) : IInput<nint>
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
            changed = ParseValue();
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

    private bool ParseValue()
    {
        var value = ((ReadOnlySpan<byte>)_buffer).BeforeNull();
        if (value.IsEmpty) {
            return false;
        }

        if (nint.TryParse(value, NumberStyles.HexNumber, null, out var parsedHex)) {
            _value = parsedHex;
            return true;
        }

        var separator = value.IndexOf((byte)'+');
        if (separator >= 0 && nint.TryParse(value[(separator + 1)..], NumberStyles.HexNumber, null, out parsedHex)) {
            var baseValue = value[..separator];
            separator = baseValue.IndexOf((byte)'!');
            ModuleAddress moduleAddress;
            if (separator >= 0) {
                moduleAddress = new(
                    Encoding.UTF8.GetString(baseValue[..separator]),
                    Encoding.UTF8.GetString(baseValue[(separator + 1)..]), parsedHex, -1
                );
            } else {
                moduleAddress = new(Encoding.UTF8.GetString(baseValue), null, parsedHex, -1);
            }

            if (moduleAddressResolver.GetAddress(moduleAddress) is
                {
                } address) {
                _value = address;
                return true;
            }

            return false;
        }

        separator = value.IndexOf((byte)'!');
        if (separator >= 0) {
            var moduleAddress = new ModuleAddress(
                Encoding.UTF8.GetString(value[..separator]),
                Encoding.UTF8.GetString(value[(separator + 1)..]), 0, -1
            );

            if (moduleAddressResolver.GetAddress(moduleAddress) is
                {
                } address) {
                _value = address;
                return true;
            }

            return false;
        }

        separator = value.IndexOf((byte)'_');
        if (separator >= 0 && nint.TryParse(value[(separator + 1)..], NumberStyles.HexNumber, null, out parsedHex)) {
            _value = dataYamlContainer.GetLiveAddress(new(parsedHex));
            return true;
        }

        return false;
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
