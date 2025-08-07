#if WITH_SMA
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

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

    public void Draw(ParagraphDrawFlags flags)
    {
        if (nested) {
            using var node = ImRaii.TreeNode($"{title}###{index}", ImGuiTreeNodeFlags.DefaultOpen);
            if (node) {
                DrawChildren(flags);
            }
        } else {
            if (!ImGui.CollapsingHeader($"{title}###{index}", ImGuiTreeNodeFlags.DefaultOpen)) {
                return;
            }

            using (ImRaii.PushId(index)) {
                DrawChildren(flags);
            }
        }
    }

    private void DrawChildren(ParagraphDrawFlags flags)
    {
        lock (_children) {
            foreach (var child in _children) {
                child.Draw(flags);
            }
        }
    }
}
#endif
