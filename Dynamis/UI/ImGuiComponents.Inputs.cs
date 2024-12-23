using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Dynamis.UI;

partial class ImGuiComponents
{

    public void InputFile(string label, string filters, string path, Action<string> setPath)
    {
        using var id = ImRaii.PushId(label);

        var innerSpacing = ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - NormalizedIconButtonSize(FontAwesomeIcon.Folder).X - innerSpacing);
        if (ImGui.InputText(
                "###path", ref path, 260, ImGuiInputTextFlags.EnterReturnsTrue
            )) {
            setPath(path);
        }

        ImGui.SameLine(0.0f, innerSpacing);
        if (NormalizedIconButton(FontAwesomeIcon.Folder)) {
            fileDialogManager.OpenFileDialog(
                "Pick " + label, filters, (success, newPath) =>
                {
                    if (!success) {
                        return;
                    }

                    setPath(newPath);
                }
            );
        }

        if (!label.StartsWith("###")) {
            ImGui.SameLine(0.0f, innerSpacing);
            ImGui.TextUnformatted(label);
        }
    }

    public static bool ComboEnum<T>(string label, ref T value, Func<T, string>? toString = null,
        ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, Enum
        => Combo(label, ref value, Enum.GetValues<T>(), toString, flags);

    public static bool Combo<T>(string label, ref T value, IEnumerable<T> values,
        Func<T, string>? toString = null, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        toString ??= value => value?.ToString() ?? string.Empty;
        using var combo = ImRaii.Combo(label, toString(value), flags);
        if (!combo) {
            return false;
        }

        var boxedValue = (object?)value;
        var changed = false;
        foreach (var v in values) {
            var selected = Equals(v, boxedValue);
            if (ImGui.Selectable(toString(v), selected)) {
                value = v;
                changed = true;
            }

            if (selected) {
                ImGui.SetItemDefaultFocus();
            }
        }

        return changed;
    }

    public static unsafe bool InputText(string label, Span<char> charSpan, bool forceNullTerminator,
        ImGuiInputTextFlags flags = 0, ImGuiInputTextCallback? callback = null, nint userData = 0)
    {
        var refExpectedBytes = Encoding.UTF8.GetByteCount(charSpan);
        byte* refByteBuffer;
        if (refExpectedBytes <= 2048) {
            var refAlloc = stackalloc byte[refExpectedBytes + 1];
            refByteBuffer = refAlloc;
        } else {
            refByteBuffer = (byte*)Marshal.AllocHGlobal(refExpectedBytes + 1);
        }

        try {
            var nBytes = Encoding.UTF8.GetBytes(charSpan, new(refByteBuffer, refExpectedBytes));
            refByteBuffer[nBytes] = 0;
            var refByteSpan = new ReadOnlySpan<byte>(refByteBuffer, nBytes + 1);

            var expectedBytes = Math.Max(nBytes, charSpan.Length);
            byte* byteBuffer;
            if (expectedBytes <= 2048) {
                var alloc = stackalloc byte[expectedBytes + 1];
                byteBuffer = alloc;
            } else {
                byteBuffer = (byte*)Marshal.AllocHGlobal(expectedBytes + 1);
            }

            try {
                var byteSpan = new Span<byte>(byteBuffer, expectedBytes + 1);
                refByteSpan.CopyTo(byteSpan);

                var ret = ImGui.InputText(
                    label, (nint)byteBuffer, (uint)(expectedBytes + 1), flags, callback, userData
                );

                if (!refByteSpan.SequenceEqual(byteSpan[..refByteSpan.Length])) {
                    var terminator = byteSpan.IndexOf((byte)0);
                    if (terminator >= 0) {
                        byteSpan = byteSpan[..terminator];
                    }

                    var nChars = Encoding.UTF8.GetChars(byteSpan, charSpan);
                    if (nChars < charSpan.Length) {
                        charSpan[nChars] = '\0';
                    } else if (forceNullTerminator) {
                        charSpan[^1] = '\0';
                    }
                }

                return ret;
            } finally {
                if (expectedBytes > 2048) {
                    Marshal.FreeHGlobal((nint)byteBuffer);
                }
            }
        } finally {
            if (refExpectedBytes > 2048) {
                Marshal.FreeHGlobal((nint)refByteBuffer);
            }
        }
    }

    public static unsafe bool InputPointer(string label, ref nint value, ImGuiInputTextFlags flags = 0,
        bool showLabel = true)
    {
        var config = GetPointerConfiguration();

        using var id = ImRaii.PushId(label);

        bool changed;
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            fixed (nint* pValue = &value) {
                changed = ImGui.InputScalar(
                    "###pointer", config.DataType, new(pValue), 0, 0, config.CFormat,
                    ImGuiInputTextFlags.CharsHexadecimal | flags
                );
            }
        }

        if (showLabel) {
            ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.TextUnformatted(label);
        }

        return changed;
    }

    public static Vector2 CalcPointerSize(nint value)
    {
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            return ImGui.CalcTextSize(value.ToString(GetPointerConfiguration().DotnetFormat));
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
