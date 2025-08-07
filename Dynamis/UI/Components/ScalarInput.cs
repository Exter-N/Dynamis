using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dynamis.Utility;

namespace Dynamis.UI.Components;

public class ScalarInput<T>(string label, T initialValue = default) : IInput<T> where T : unmanaged, IBinaryNumber<T>
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

    public bool Draw(ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => ImGui.InputScalar(label, DataType, ref _value, T.Zero, T.Zero, CFormat, flags);

    public T GetValue()
        => _value;
}
