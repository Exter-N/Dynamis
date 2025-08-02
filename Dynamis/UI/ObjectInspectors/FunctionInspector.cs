using System.Numerics;
using System.Text;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dynamis.Interop;
using Dynamis.Messaging;
using Dynamis.UI.Windows;
using Iced.Intel;
using ImGuiNET;

namespace Dynamis.UI.ObjectInspectors;

public sealed class FunctionInspector(MessageHub messageHub, ContextMenu contextMenu) : IDynamicObjectInspector
{
    private static readonly Formatter Formatter = new IntelFormatter()
    {
        Options =
        {
            BranchLeadingZeros = false,
            LeadingZeros = false,
            SpaceAfterOperandSeparator = true,
            FirstOperandCharIndex = 10,
        },
    };

    public bool CanInspect(ClassInfo @class)
        => @class.Kind == ClassKind.Function;

    public void DrawAdditionalTooltipDetails(IntPtr pointer, ClassInfo @class)
    {
    }

    public void DrawAdditionalHeaderDetails(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
    }

    public void DrawAdditionalTabs(ObjectSnapshot snapshot, bool live, ObjectInspectorWindow window)
    {
        var body = snapshot.Class?.FunctionBody;
        if (body is null || body.Length == 0) {
            return;
        }

        using var tab = ImRaii.TabItem("Function Disassembly");
        if (!tab) {
            return;
        }

        using var _ = ImRaii.Child("###disassembly", -Vector2.One);

        var addressDigitCount = 16 - (BitOperations.LeadingZeroCount(body[^1].Instruction.IP) >> 2);
        var longestInstrLen = body.Select(instr => instr.Instruction.Length).Max();
        var farthestJumpColumn = body.Select(instr => instr.LocalJump?.DisplayColumn ?? -1).Max();
        if (farthestJumpColumn < 0) {
            --farthestJumpColumn;
        }
        var addressFormat = $"X{addressDigitCount}";
        var disassemblyStart = addressDigitCount + (snapshot.Address.HasValue ? longestInstrLen * 3 + 2 : 0)
                                                 + farthestJumpColumn + 6;
        var jumpStartingCursor = ImGui.GetCursorScreenPos();
        float jumpCellWidth;
        var jumpCellHeight = ImGui.GetTextLineHeight();
        var jumpCellHeightWithSpacing = ImGui.GetTextLineHeightWithSpacing();
        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
            jumpCellWidth = ImGui.CalcTextSize(" ").X;
            jumpStartingCursor.X += jumpCellWidth * disassemblyStart;
        }

        var sb = new StringBuilder();
        var output = new StringOutput(sb);
        foreach (var (instr, operand, annotation, maybeJump) in body) {
            sb.Append(' ');
            sb.AppendFormat(instr.IP.ToString(addressFormat));
            sb.Append(' ', 3);
            var bytes = ReadOnlySpan<byte>.Empty;
            if (snapshot.Address is
                {
                } address) {
                var offset = (int)unchecked((nint)instr.IP - address);
                bytes = snapshot.Data.AsSpan(offset, instr.Length);
                foreach (var b in bytes) {
                    sb.Append($"{b:X2} ");
                }

                sb.Append(' ', (longestInstrLen - instr.Length) * 3 + 2);
            }

            sb.Append(' ', farthestJumpColumn + 2);
            Formatter.Format(instr, output);
            var line = output.ToStringAndReset();

            using (ImRaii.PushFont(UiBuilder.MonoFont)) {
                if (ImGui.Selectable(line, false, ImGuiSelectableFlags.None)) {
                    contextMenu.Open(
                        new InstructionContextMenu(
                            messageHub, (nint)instr.IP, bytes, line.Substring(disassemblyStart), operand?.Address,
                            annotation
                        )
                    );
                }
            }

            if (annotation is not null) {
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiUtil.HalfTransparentText())) {
                    ImGui.TextUnformatted(annotation);
                }
            }
        }

        for (var i = 0; i < body.Length; i++) {
            if (body[i].LocalJump is not
                {
                } jump) {
                continue;
            }

            DrawArrow(
                Snap(
                    jumpStartingCursor + new Vector2(
                        (-1.5f - jump.DisplayColumn) * jumpCellWidth,
                        i * jumpCellHeightWithSpacing + 0.75f * jumpCellHeight
                    )
                ),
                Snap(
                    jumpStartingCursor + new Vector2(
                        -0.5f * jumpCellWidth, jump.Target * jumpCellHeightWithSpacing + 0.25f * jumpCellHeight
                    )
                )
            );
        }
    }

    private static void DrawArrow(Vector2 fromCorner, Vector2 toEnd)
    {
        var drawList = ImGui.GetWindowDrawList();
        Span<Vector2> polyline = [new(toEnd.X, fromCorner.Y), fromCorner, new(fromCorner.X, toEnd.Y), toEnd,];
        drawList.AddPolyline(ref polyline[0], polyline.Length, ImGui.GetColorU32(ImGuiCol.Text), 0, 1.0f);
        var globalScale = ImGui.GetIO().FontGlobalScale;
        Span<Vector2> arrowHead =
        [
            toEnd, toEnd + new Vector2(-3.0f, -3.0f) * globalScale, toEnd + new Vector2(-3.0f, 3.0f) * globalScale,
        ];
        drawList.AddConvexPolyFilled(ref arrowHead[0], arrowHead.Length, ImGui.GetColorU32(ImGuiCol.Text));
    }

    private static Vector2 Snap(Vector2 vec)
        => new(MathF.Floor(vec.X) + 0.5f, MathF.Floor(vec.Y) + 0.5f);

    private sealed class InstructionContextMenu(
        MessageHub messageHub,
        nint address,
        ReadOnlySpan<byte> bytes,
        string disassembly,
        nint? operand,
        string? annotation) : IDrawable
    {
        private readonly byte[] _bytes = bytes.ToArray();

        public bool Draw()
        {
            var ret = false;
            var separator = false;
            if (operand is
                {
                } operandAddress) {
                separator = true;
                if (ImGui.Selectable("Inspect operand")) {
                    messageHub.Publish(new InspectObjectMessage(operandAddress, null, null, null));
                    ret = true;
                }

                if (ImGui.Selectable("Copy operand")) {
                    ImGui.SetClipboardText($"{operandAddress:X}");
                    ret = true;
                }
            }

            if (annotation is not null) {
                separator = true;
                if (ImGui.Selectable("Copy annotation")) {
                    ImGui.SetClipboardText(annotation);
                    ret = true;
                }
            }

            if (separator) {
                ImGui.Separator();
            }

            if (ImGui.Selectable("Copy disassembled instruction")) {
                ImGui.SetClipboardText(disassembly);
                ret = true;
            }

            if (_bytes.Length > 0 && ImGui.Selectable("Copy instruction bytes")) {
                var sb = new StringBuilder();
                foreach (var b in _bytes) {
                    sb.Append($"{b:X2} ");
                }
                ImGui.SetClipboardText(sb.ToString(0, sb.Length - 1));
                ret = true;
            }

            ImGui.Separator();
            if (ImGui.Selectable("Copy effective address")) {
                ImGui.SetClipboardText(address.ToString("X"));
                ret = true;
            }

            return ret;
        }
    }
}
