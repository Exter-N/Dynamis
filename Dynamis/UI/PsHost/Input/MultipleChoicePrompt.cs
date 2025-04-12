using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Utility;
using ImGuiNET;

namespace Dynamis.UI.PsHost.Input;

public sealed class MultipleChoicePrompt(
    string? caption,
    string? message,
    Collection<ChoiceDescription> choices,
    IEnumerable<int>? defaultChoices) : BaseFormPrompt(caption, message), IPrompt<Collection<int>>
{
    private readonly HashSet<int> _choices = [..defaultChoices ?? [],];

    protected override int FrameRows
        => choices.Count + 1;

    public Collection<int> Result
        => [.._choices,];

    protected override bool DrawForm(ref bool focus)
    {
        focus = false;

        var i = -1;
        foreach (var choice in choices) {
            ++i;
            var selected = _choices.Contains(i);
            if (ImGui.Checkbox($"{choice.Label.ParseAccelerator().Label}###{i}", ref selected)) {
                if (selected) {
                    _choices.Add(i);
                } else {
                    _choices.Remove(i);
                }
            }

            if (!string.IsNullOrEmpty(choice.HelpMessage)) {
                ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
                ImGuiComponents.NormalizedIcon(
                    FontAwesomeIcon.InfoCircle, StyleModel.GetFromCurrent()!.BuiltInColors!.ParsedBlue!.Value.ToUInt32()
                );

                if (ImGui.IsItemHovered()) {
                    using var tt = ImRaii.Tooltip();
                    ImGui.TextUnformatted(choice.HelpMessage);
                }
            }
        }

        return ImGui.Button("Submit");
    }

    public override void Cancel()
    {
    }
}
