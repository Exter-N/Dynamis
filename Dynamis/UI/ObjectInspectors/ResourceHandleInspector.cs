using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Interface.ImGuiFileDialog;
using Dynamis.Interop;
using Dynamis.UI.Windows;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed unsafe class ResourceHandleInspector : IObjectInspector<ResourceHandle>
{
    private readonly ImGuiComponents   _imGuiComponents;
    private readonly FileDialogManager _fileDialogManager;

    public ResourceHandleInspector(ImGuiComponents imGuiComponents, FileDialogManager fileDialogManager)
    {
        _imGuiComponents = imGuiComponents;
        _fileDialogManager = fileDialogManager;
    }

    public void DrawAdditionalTooltipDetails(ResourceHandle* pointer)
    {
        ImGui.TextUnformatted($"File Name: {pointer->FileName}");
        var length = pointer->GetLength();
        if (length > 0) {
            ImGui.TextUnformatted($"Resource Size: {length} (0x{length:X}) bytes");
        }
    }

    public void DrawAdditionalHeaderDetails(ResourceHandle* pointer, ObjectInspectorWindow window)
    {
        DrawAdditionalTooltipDetails(pointer);
        var data = pointer->GetData();
        var length = pointer->GetLength();
        if (data is null || length <= 0) {
            return;
        }

        ImGui.TextUnformatted("Resource Contents:");
        ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
        _imGuiComponents.DrawPointer(
            (nint)data, () => new ClassInfo
            {
                Name = $"<Resource Contents> {pointer->FileName}",
                EstimatedSize = (uint)length,
                SizeFromOuterContext = (uint)length,
            }
        );

        if (ImGui.Button("Save to File")) {
            var extension = $".{DecodeFileType(pointer->FileType)}";
            _fileDialogManager.SaveFileDialog(
                $"Save {pointer->FileName}", extension, Path.GetFileName(pointer->FileName.ToString()), extension, (success, newPath) =>
                {
                    if (!success) {
                        return;
                    }

                    File.WriteAllBytes(newPath, new Span<byte>(data, (int)length).ToArray());
                }
            );
        }
    }

    private static string DecodeFileType(uint fileType)
    {
        var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(ref fileType));
        return bytes.IndexOf((byte)0) switch
        {
            -1     => Encoding.UTF8.GetString([bytes[3], bytes[2], bytes[1], bytes[0],]),
            0      => string.Empty,
            1      => Encoding.UTF8.GetString([bytes[0],]),
            2      => Encoding.UTF8.GetString([bytes[1], bytes[0],]),
            3      => Encoding.UTF8.GetString([bytes[2], bytes[1], bytes[0],]),
            var sz => throw new InvalidOperationException($"Unexpected null byte position {sz}"),
        };
    }

    public void DrawAdditionalTabs(ResourceHandle* pointer, ObjectInspectorWindow window)
    {
    }
}
