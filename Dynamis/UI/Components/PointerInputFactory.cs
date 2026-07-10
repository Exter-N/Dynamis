using Dynamis.ClientStructs;
using Dynamis.Interop;

namespace Dynamis.UI.Components;

public sealed class PointerInputFactory(
    DataYamlContainer dataYamlContainer,
    ModuleAddressResolver moduleAddressResolver,
    ImGuiComponents imGuiComponents)
{
    public PointerInput Create(string label)
        => new(dataYamlContainer, moduleAddressResolver, imGuiComponents, label);
}
