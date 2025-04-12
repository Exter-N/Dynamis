using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Utility;
using ImGuiNET;

namespace Dynamis.UI.PsHost.Input;

public sealed class ChoicePrompt(
    string? caption,
    string? message,
    Collection<ChoiceDescription> choices,
    int defaultChoice) : BaseFormPrompt(caption, message), IPrompt<int>
{
    private int _choice = defaultChoice;

    protected override int FrameRows
        => choices.Count > 0 ? 1 : 0;

    public int Result
        => _choice;

    protected override bool DrawForm(ref bool focus)
    {
        focus = false;

        var ret = false;
        var i = -1;
        foreach (var choice in choices) {
            ++i;
            if (i > 0) {
                ImGui.SameLine();
            }

            using (var color = new ImRaii.Color()) {
                color.Push(ImGuiCol.Button,        ImGui.GetColorU32(ImGuiCol.Button).Emphasis());
                color.Push(ImGuiCol.ButtonHovered, ImGui.GetColorU32(ImGuiCol.ButtonHovered).Emphasis());
                color.Push(ImGuiCol.ButtonActive,  ImGui.GetColorU32(ImGuiCol.ButtonActive).Emphasis());
                if (ImGui.Button($"{choice.Label.ParseAccelerator().Label}###{i}")) {
                    _choice = i;
                    ret = true;
                }
            }

            if (!string.IsNullOrEmpty(choice.HelpMessage) && ImGui.IsItemHovered()) {
                using var tt = ImRaii.Tooltip();
                ImGui.TextUnformatted(choice.HelpMessage);
            }
        }

        return ret;
    }

    public override void Cancel()
    {
    }
}
