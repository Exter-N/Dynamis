using Dynamis.Interop;
using Dynamis.UI.Windows;

namespace Dynamis.UI.ObjectInspectors;

public interface IObjectInspector;

public unsafe interface IObjectInspector<T> : IObjectInspector where T : unmanaged
{
    void DrawAdditionalTooltipDetails(T* pointer);

    void DrawAdditionalHeaderDetails(T* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window);

    void DrawAdditionalTabs(T* pointer, ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window);
}
