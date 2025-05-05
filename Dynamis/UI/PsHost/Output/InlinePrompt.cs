#if WITH_SMA
using Dynamis.UI.PsHost.Input;
using ImGuiNET;

namespace Dynamis.UI.PsHost.Output;

public sealed class InlinePrompt(IPrompt prompt, Action? onComplete, bool sameLine, bool focus) : IParagraph
{
    private bool    _focus      = focus;
    private Action? _onComplete = onComplete;

    public void Draw(ParagraphDrawFlags flags)
    {
        if (sameLine) {
            ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        }

        if (!prompt.Draw(ref _focus)) {
            return;
        }

        _onComplete?.Invoke();
        _onComplete = null;
    }
}
#endif
