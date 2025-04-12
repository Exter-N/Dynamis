using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Dynamis.UI.PsHost.Output;

public sealed class Section(string title, int index, bool nested) : IParagraph
{
    private readonly List<IParagraph> _children = [];

    public void Add(IParagraph child)
    {
        lock (_children) {
            _children.Add(child);
        }
    }

    public Section AddSubSection(string subTitle)
    {
        lock (_children) {
            var section = new Section(subTitle, _children.Count, true);
            _children.Add(section);
            return section;
        }
    }

    public void Draw()
    {
        if (nested) {
            using var node = ImRaii.TreeNode($"{title}###{index}", ImGuiTreeNodeFlags.DefaultOpen);
            if (node) {
                DrawChildren();
            }
        } else {
            if (!ImGui.CollapsingHeader($"{title}###{index}", ImGuiTreeNodeFlags.DefaultOpen)) {
                return;
            }

            using (ImRaii.PushId(index)) {
                DrawChildren();
            }
        }
    }

    private void DrawChildren()
    {
        lock (_children) {
            foreach (var child in _children) {
                child.Draw();
            }
        }
    }
}
