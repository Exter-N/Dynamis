using System.Diagnostics.CodeAnalysis;
using Dynamis.Utility;
using ImGuiNET;

namespace Dynamis.UI.Components;

public class ScalarInput<T>(string label, T initialValue = default) : IInput<T> where T : unmanaged
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    private static readonly ImGuiDataType DataType;

    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    private static readonly string CFormat;

    private T _value = initialValue;

    static ScalarInput()
    {
        (DataType, CFormat) = (typeof(T).ToFieldType() ?? throw new ArgumentException(
            $"Unsupported scalar type {typeof(T).FullName}", nameof(T)
        )).ToImGui(false);
    }

    public unsafe bool Draw(ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        fixed (T* value = &_value) {
            return ImGui.InputScalar(label, DataType, (nint)value, 0, 0, CFormat, flags);
        }
    }

    public T GetValue()
        => _value;
}
