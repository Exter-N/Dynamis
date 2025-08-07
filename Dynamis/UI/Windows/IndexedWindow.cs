using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Dynamis.UI.Windows;

public abstract class IndexedWindow : Window
{
    private readonly WindowSystem _windowSystem;
    private readonly int          _index;

    public int Index
        => _index;

    public event EventHandler? Close;

    protected IndexedWindow(string name, WindowSystem windowSystem, int index,
        ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(
        name, flags, forceMainWindow
    )
    {
        _windowSystem = windowSystem;
        _index = index;
    }

    public override void OnClose()
    {
        _windowSystem.RemoveWindow(this);
        Close?.Invoke(this, EventArgs.Empty);
    }
}
