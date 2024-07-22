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

    public static bool ComboEnum<T>(string label, ref T value, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, Enum
    {
        using var combo = ImRaii.Combo(label, value.ToString(), flags);
        if (!combo) {
            return false;
        }

        var boxedValue = (object)value;
        var changed = false;
        foreach (var v in Enum.GetValues<T>()) {
            var selected = v.Equals(boxedValue);
            if (ImGui.Selectable(v.ToString(), selected)) {
                value = v;
                changed = true;
            }

            if (selected) {
                ImGui.SetItemDefaultFocus();
            }
        }

        return changed;
    }

    public static unsafe bool InputPointer(string label, ref nint value, ImGuiInputTextFlags flags = 0)
    {
        var config = GetPointerConfiguration();

        using var id = ImRaii.PushId(label);

        bool changed;
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            fixed (nint* pValue = &value) {
                changed = ImGui.InputScalar(
                    "###pointer", config.DataType, new(pValue), 0, 0, config.Format,
                    ImGuiInputTextFlags.CharsHexadecimal | flags
                );
            }
        }

        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.TextUnformatted(label);

        return changed;
    }

    private static (ImGuiDataType DataType, string Format) GetPointerConfiguration()
        => nint.Size switch
        {
            4 => (ImGuiDataType.U32, "%08X"),
            8 => (ImGuiDataType.U64, "%016llX"),
            _ => throw new NotSupportedException("Only 32-bit and 64-bit pointers are supported"),
        };
}
