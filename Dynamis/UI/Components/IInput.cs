using ImGuiNET;

namespace Dynamis.UI.Components;

public interface IInput
{
    bool Draw(ImGuiInputTextFlags flags = ImGuiInputTextFlags.None);
}

public interface IInput<out T> : IInput
{
    T GetValue();
}
