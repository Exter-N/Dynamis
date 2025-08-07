#if WITH_SMA
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Dynamis.UI.PsHost.Input;

public abstract class BaseFormPrompt(string? caption, string? message) : IPrompt
{
    private readonly int _id = IPrompt.AllocateId();

    public float Height
    {
        get
        {
            var rows = 0;
            var height = 0.0f;
            if (!string.IsNullOrEmpty(caption)) {
                ++rows;
                height += ImGui.GetFrameHeight();
            }

            if (!string.IsNullOrEmpty(message)) {
                ++rows;
                height += ImGui.GetTextLineHeight();
            }

            var frameRows = FrameRows;
            rows += frameRows;
            height += frameRows * ImGui.GetFrameHeight();

            return height + ImGui.GetStyle().ItemSpacing.Y * Math.Max(0, rows - 1);
        }
    }

    protected abstract int FrameRows { get; }

    public bool Draw(ref bool focus)
    {
        using var _ = ImRaii.PushId(_id);

        if (!string.IsNullOrEmpty(caption)) {
            ImGui.CollapsingHeader(caption, ImGuiTreeNodeFlags.Leaf);
        }

        if (!string.IsNullOrEmpty(message)) {
            ImGui.TextUnformatted(message);
        }

        return DrawForm(ref focus);
    }

    protected abstract bool DrawForm(ref bool focus);

    public abstract void Cancel();
}
#endif
