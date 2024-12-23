using Dynamis.Interop;
using Dynamis.UI.Windows;

namespace Dynamis.UI.ObjectInspectors;

public interface IDynamicObjectInspector : IObjectInspector
{
    bool CanInspect(ClassInfo @class);

    void DrawAdditionalTooltipDetails(nint pointer, ClassInfo @class);

    void DrawAdditionalHeaderDetails(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window);

    void DrawAdditionalTabs(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window);
}
