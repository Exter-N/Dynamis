using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Interop;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace Dynamis.UI.Windows;

public sealed class ObjectInspectorWindow : Window
{
    private readonly ILogger<ObjectInspectorWindowFactory> _logger;
    private readonly WindowSystem                          _windowSystem;
    private readonly ImGuiComponents                       _imGuiComponents;
    private readonly ObjectInspector                       _objectInspector;

    private nint       _vmAddress = 0;
    private int        _vmStatus  = 0;
    private ClassInfo? _vmClass;
    private byte[]     _vmSnapshot = Array.Empty<byte>();

    public ObjectInspectorWindow(ILogger<ObjectInspectorWindowFactory> logger, WindowSystem windowSystem,
        ImGuiComponents imGuiComponents, ObjectInspector objectInspector, int index) : base(
        $"Dynamis - Object Inspector##{index}", 0
    )
    {
        _logger = logger;
        _windowSystem = windowSystem;
        _imGuiComponents = imGuiComponents;
        _objectInspector = objectInspector;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(768, 432),
            MaximumSize = new(float.MaxValue, float.MaxValue),
        };

        imGuiComponents.AddTitleBarButtons(this);
    }

    public void Inspect(nint address)
    {
        _vmAddress = address;
        RunInspection();
    }

    private unsafe void RunInspection()
    {
        _vmClass = null;
        _vmSnapshot = Array.Empty<byte>();
        try {
            _vmClass = _objectInspector.DetermineClass(_vmAddress);
            _vmSnapshot = new byte[_vmClass.EstimatedSize];
            Marshal.Copy(_vmAddress, _vmSnapshot, 0, _vmSnapshot.Length);
            _vmStatus = 1;
        } catch (Exception e) {
            _logger.LogError(e, "Object inspection failed for address 0x{Address:X}", _vmAddress);
            _vmClass = null;
            _vmSnapshot = Array.Empty<byte>();
            _vmStatus = 2;
        }
    }

    private static string ToHex(byte[] bytes)
    {
        var resultBuilder = new StringBuilder();
        for (var i = 0; i < bytes.Length; ) {
            if (i > 0) {
                resultBuilder.AppendLine();
            }

            for (var j = 0; j < 16 && i < bytes.Length; ++j, ++i) {
                if (j > 0) {
                    resultBuilder.Append(' ');
                }

                resultBuilder.Append(bytes[i].ToString("X2"));
            }
        }

        return resultBuilder.ToString();
    }

    public override void Draw()
    {
        if (ImGuiComponents.InputPointer("Object Address", ref _vmAddress, ImGuiInputTextFlags.EnterReturnsTrue)) {
            RunInspection();
        }

        switch (_vmStatus) {
            case 1:
                ImGui.TextUnformatted($"Class Name: {_vmClass!.Name}");
                ImGui.TextUnformatted($"In ClientStructs: {_vmClass.ClientStructsType != null}");
                ImGui.TextUnformatted($"Estimated Size: {_vmClass.EstimatedSize} (0x{_vmClass.EstimatedSize:X})");
                var sizeIsFromDtor = _vmClass.SizeFromDtor.HasValue
                                  && _vmClass.SizeFromDtor.Value == _vmClass.EstimatedSize;
                var sizeIsFromCs = _vmClass.SizeFromClientStructs.HasValue
                                && _vmClass.SizeFromClientStructs.Value == _vmClass.EstimatedSize;
                if (sizeIsFromDtor && sizeIsFromCs) {
                    ImGui.SameLine();
                    using (ImRaii.PushColor(
                               ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.ParsedGreen!.Value
                           )) {
                        ImGui.TextUnformatted("(from both ClientStructs and dtor)");
                    }
                } else if (!sizeIsFromDtor && !sizeIsFromCs) {
                    ImGui.SameLine();
                    using (ImRaii.PushColor(
                               ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudRed!.Value
                           )) {
                        ImGui.TextUnformatted("(no valid source - using rest of page)");
                    }
                } else {
                    if (sizeIsFromDtor) {
                        ImGui.SameLine();
                        ImGui.TextUnformatted("(from dtor)");
                    }

                    if (sizeIsFromCs) {
                        ImGui.SameLine();
                        ImGui.TextUnformatted("(from ClientStructs)");
                    }
                }

                if (_vmClass.SizeFromClientStructs.HasValue
                 && _vmClass.SizeFromClientStructs.Value != _vmClass.EstimatedSize) {
                    using (ImRaii.PushColor(
                               ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudYellow!.Value
                           )) {
                        ImGui.TextUnformatted(
                            $"Size from ClientStructs: {_vmClass.SizeFromClientStructs.Value} (0x{_vmClass.SizeFromClientStructs.Value:X})"
                        );
                    }
                }

                if (_vmClass.SizeFromDtor.HasValue && _vmClass.SizeFromDtor.Value != _vmClass.EstimatedSize) {
                    using (ImRaii.PushColor(
                               ImGuiCol.Text, StyleModel.GetFromCurrent().BuiltInColors!.DalamudYellow!.Value
                           )) {
                        ImGui.TextUnformatted(
                            $"Size from dtor: {_vmClass.SizeFromDtor.Value} (0x{_vmClass.SizeFromDtor.Value:X})"
                        );
                    }
                }

                using (ImRaii.PushFont(UiBuilder.MonoFont)) {
                    ImGui.TextUnformatted(ToHex(_vmSnapshot));
                }

                break;
            case 2:
                ImGui.TextUnformatted("Error");
                break;
        }
    }

    public override void OnClose()
    {
        _windowSystem.RemoveWindow(this);
    }
}
