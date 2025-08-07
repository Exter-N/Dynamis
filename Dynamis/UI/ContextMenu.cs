using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Dynamis.UI;

public sealed class ContextMenu
{
    private const string PopupId = "context";

    private IDrawable? _current;
    private bool       _openPending;

    public void Open(IDrawable menu)
    {
        _current = menu;
        _openPending = true;
    }

    public void Close()
    {
        if (_current is null) {
            return;
        }

        (_current as IDisposable)?.Dispose();
        _current = null;
        ImGui.CloseCurrentPopup();
    }

    public void Draw()
    {
        if (_current is null) {
            return;
        }

        if (_openPending) {
            _openPending = false;
            ImGui.OpenPopup(PopupId);
        }

        using var context = ImRaii.Popup(PopupId);
        if (!context) {
            (_current as IDisposable)?.Dispose();
            _current = null;
            return;
        }

        if (_current.Draw()) {
            Close();
        }
    }
}
