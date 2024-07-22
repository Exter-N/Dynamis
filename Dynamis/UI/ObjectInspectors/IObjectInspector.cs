using Dynamis.UI.Windows;

namespace Dynamis.UI.ObjectInspectors;

public interface IObjectInspector;

public unsafe interface IObjectInspector<T> : IObjectInspector where T : unmanaged
{
    void DrawAdditionalTooltipDetails(T* pointer);

    void DrawAdditionalHeaderDetails(T* pointer, ObjectInspectorWindow window);

    void DrawAdditionalTabs(T* pointer, ObjectInspectorWindow window);
}
