using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dynamis.Interop;
using Dynamis.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.Interop;
using ImGuiNET;

namespace Dynamis.UI.Windows;

public class CtProtoWindow : Window, ISingletonWindow
{
    private readonly TextureArraySlicer _textureArraySlicer;

    private int _vmActivePair = 0;

    public CtProtoWindow(TextureArraySlicer textureArraySlicer) : base("Dynamis - CT Proto", 0)
    {
        _textureArraySlicer = textureArraySlicer;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(1240, 600),
            MaximumSize = new Vector2(1240, 4000),
        };
    }

    public override void Draw()
    {
        DrawPairSelector();
        DrawPairEditor();
    }

    private void DrawPairSelector()
    {
        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        using var alignment = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
        var random = new Random(unchecked((int)0xC0FFEEu));
        var style = ImGui.GetStyle();
        var itemSpacing = style.ItemSpacing.X;
        var itemInnerSpacing = style.ItemInnerSpacing.X;
        var framePadding = style.FramePadding;
        var buttonWidth = (ImGui.GetContentRegionAvail().X - itemSpacing * 7.0f) * 0.125f;
        var frameHeight = ImGui.GetFrameHeight();
        var highlighterSize = ImGuiComponents.NormalizedIconButtonSize(FontAwesomeIcon.Crosshairs);
        var spaceWidth = ImGui.CalcTextSize(" ").X;
        var spacePadding = (int)MathF.Ceiling((highlighterSize.X + framePadding.X + itemInnerSpacing) / spaceWidth);
        for (var i = 0; i < 16; i += 8) {
            for (var j = 0; j < 8; ++j) {
                using (var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), i + j == _vmActivePair)) {
                    if (ImGui.Button(
                            $"#{i + j + 1}".PadLeft(3 + spacePadding),
                            new Vector2(buttonWidth, ImGui.GetFrameHeightWithSpacing() + frameHeight)
                        )) {
                        _vmActivePair = i + j;
                    }
                }

                var rcMin = ImGui.GetItemRectMin() + framePadding;
                var rcMax = ImGui.GetItemRectMax() - framePadding;
                DrawCtBlendRect(
                    rcMin with
                    {
                        X = rcMax.X - frameHeight * 3 - itemInnerSpacing * 2,
                    }, rcMax with
                    {
                        X = rcMax.X - (frameHeight + itemInnerSpacing) * 2,
                    },
                    0xFF000000u | (uint)random.Next(0, 0x1000000),
                    0xFF000000u | (uint)random.Next(0, 0x1000000)
                );
                DrawCtBlendRect(
                    rcMin with
                    {
                        X = rcMax.X - frameHeight * 2 - itemInnerSpacing,
                    }, rcMax with
                    {
                        X = rcMax.X - frameHeight - itemInnerSpacing,
                    },
                    0xFF000000u | (uint)random.Next(0, 0x1000000),
                    0xFF000000u | (uint)random.Next(0, 0x1000000)
                );
                DrawCtBlendRect(
                    rcMin with
                    {
                        X = rcMax.X - frameHeight,
                    }, rcMax,
                    0xFF000000u | (uint)random.Next(0, 0x1000000),
                    0xFF000000u | (uint)random.Next(0, 0x1000000)
                );
                if (j < 7) {
                    ImGui.SameLine();
                }

                var cursor = ImGui.GetCursorScreenPos();
                ImGui.SetCursorScreenPos(
                    rcMin with
                    {
                        Y = float.Lerp(rcMin.Y, rcMax.Y, 0.5f) - highlighterSize.Y * 0.5f,
                    }
                );
                ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Crosshairs);
                ImGui.SetCursorScreenPos(cursor);
                if (ImGui.IsItemHovered()) {
                    font.Pop();
                    using var tt = ImRaii.Tooltip();
                    ImGui.TextUnformatted("Highlight this pair blabla");
                    font.Push(UiBuilder.MonoFont);
                }
            }
        }
    }

    private void DrawPairEditor()
    {
        var random = new Random(unchecked((int)0xC0FFEEu));
        for (var i = 0; i < _vmActivePair; ++i) {
            random.Next(0, 0x1000000);
            random.Next(0, 0x1000000);
            random.Next(0, 0x1000000);
            random.Next(0, 0x1000000);
            random.Next(0, 0x1000000);
            random.Next(0, 0x1000000);
        }

        var diffA = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0, 0x1000000));
        var diffB = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0, 0x1000000));
        var specA = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0, 0x1000000));
        var specB = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0, 0x1000000));
        var emiA = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0,  0x1000000));
        var emiB = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0,  0x1000000));

        using (var columns = new Columns(2, "ColorTable")) {
            ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Copy);
            SameLineInner();
            ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Paste);
            ImGui.SameLine();
            CenteredText($"Row {_vmActivePair + 1}A");
            columns.Next();
            ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Copy);
            SameLineInner();
            ImGuiComponents.NormalizedIconButton(FontAwesomeIcon.Paste);
            ImGui.SameLine();
            CenteredText($"Row {_vmActivePair + 1}B");
        }
        DrawHeader("Colors");
        using (var columns = new Columns(2, "ColorTable")) {
            using (var id = ImRaii.PushId("ColorsA")) {
                DrawColors(diffA, specA, emiA, random);
            }

            columns.Next();
            using (var id = ImRaii.PushId("ColorsB")) {
                DrawColors(diffB, specB, emiB, random);
            }
        }

        DrawHeader("Physical Parameters");
        using (var columns = new Columns(2, "ColorTable")) {
            using (var id = ImRaii.PushId("PbrA")) {
                DrawPbr(random);
            }

            columns.Next();
            using (var id = ImRaii.PushId("PbrB")) {
                DrawPbr(random);
            }
        }

        DrawHeader("Fresnel Parameters");
        using (var columns = new Columns(2, "ColorTable")) {
            using (var id = ImRaii.PushId("FresnelA")) {
                DrawFresnel(random);
            }

            columns.Next();
            using (var id = ImRaii.PushId("FresnelB")) {
                DrawFresnel(random);
            }
        }

        DrawHeader("Pair Blending");
        using (var columns = new Columns(2, "ColorTable")) {
            using (var id = ImRaii.PushId("BlendingA")) {
                DrawBlending(random, true);
            }

            columns.Next();
            using (var id = ImRaii.PushId("BlendingB")) {
                DrawBlending(random, false);
            }
        }

        DrawHeader("Material Template");
        using (var columns = new Columns(2, "ColorTable")) {
            using (var id = ImRaii.PushId("TemplateA")) {
                DrawTemplate(random);
            }

            columns.Next();
            using (var id = ImRaii.PushId("TemplateB")) {
                DrawTemplate(random);
            }
        }

        DrawHeader("Dye Properties");
        using (var columns = new Columns(2, "ColorTable")) {
            using (var id = ImRaii.PushId("DyeA")) {
                DrawDye(random);
            }

            columns.Next();
            using (var id = ImRaii.PushId("DyeB")) {
                DrawDye(random);
            }
        }

        DrawHeader("Further Content");
        using (var columns = new Columns(2, "ColorTable")) {
            using (var id = ImRaii.PushId("FurtherA")) {
                DrawFurther(random);
            }

            columns.Next();
            using (var id = ImRaii.PushId("FurtherB")) {
                DrawFurther(random);
            }
        }
    }

    private static void DrawHeader(string label)
    {
        var headerColor = ImGui.GetColorU32(ImGuiCol.Header);
        using var _ = ImRaii.PushColor(ImGuiCol.HeaderHovered, headerColor)
                            .Push(ImGuiCol.HeaderActive, headerColor);
        ImGui.CollapsingHeader("  " + label, ImGuiTreeNodeFlags.Leaf);
    }

    private static void DrawColors(Vector4 diffuse, Vector4 specular, Vector4 emissive, Random random)
    {
        var dyeOffset = ImGui.GetContentRegionAvail().X + ImGui.GetStyle().ItemSpacing.X
                      - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetFrameHeight() * 2.0f;

        var diffDye = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0, 0x1000000));
        var specDye = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0, 0x1000000));
        var emiDye = ImGui.ColorConvertU32ToFloat4(0xFF000000u | (uint)random.Next(0,  0x1000000));

        var diffApply = random.Next(0, 2) != 0;
        var specApply = random.Next(0, 2) != 0;
        var emiApply = random.Next(0,  2) != 0;

        ImGui.ColorEdit4(
            "Diffuse Color", ref diffuse,
            ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoInputs
        );
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##DiffuseColorApply", ref diffApply);
        SameLineInner();
        ImGui.ColorEdit4(
            "###DiffuseColorDye", ref diffDye,
            ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoInputs
          | ImGuiColorEditFlags.NoPicker
        );

        ImGui.ColorEdit4(
            "Specular Color", ref specular,
            ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoInputs
        );
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##SpecularColorApply", ref specApply);
        SameLineInner();
        ImGui.ColorEdit4(
            "###SpecularColorDye", ref specDye,
            ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoInputs
          | ImGuiColorEditFlags.NoPicker
        );

        ImGui.ColorEdit4(
            "Emissive Color", ref emissive,
            ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoInputs
        );
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##EmissiveColorApply", ref emiApply);
        SameLineInner();
        ImGui.ColorEdit4(
            "###EmissiveColorDye", ref emiDye,
            ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoInputs
          | ImGuiColorEditFlags.NoPicker
        );
    }

    private static void DrawBlending(Random random, bool rowA)
    {
        var dyeOffset = ImGui.GetContentRegionAvail().X + ImGui.GetStyle().ItemSpacing.X
                      - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetFrameHeight() - 50.0f * ImGuiHelpers.GlobalScale;

        var aniso = random.NextSingle();
        var anisoDye = random.NextSingle();
        var anisoApply = random.Next(0, 2) != 0;

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat(rowA ? "Anisotropy Degree" : "Field #19", ref aniso, 0.01f);
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##AnisotropyApply", ref anisoApply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("##AnisotropyDye", ref anisoDye, 0.01f);
    }

    private void DrawTemplate(Random random)
    {
        var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
        var dyeOffset = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetFrameHeight()
                      - 50.0f * ImGuiHelpers.GlobalScale - 64.0f;
        var subcolWidth = CalculateSubcolumnWidth(2);

        var shaderId = random.Next(0, 256);
        var sphereMap = (ushort)random.Next(0,    32);
        var sphereMapDye = (ushort)random.Next(0, 32);
        var sphereMapApply = random.Next(0,       2) != 0;
        var sphereMask = random.NextSingle();
        var sphereMaskDye = random.NextSingle();
        var sphereMaskApply = random.Next(0, 2) != 0;
        var tile = (ushort)random.Next(0,    64);
        var tileAlpha = random.NextSingle();
        var tileXfUU = random.NextSingle();
        var tileXfUV = random.NextSingle();
        var tileXfVU = random.NextSingle();
        var tileXfVV = random.NextSingle();

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragInt(
            "Shader ID", ref shaderId, 0.25f
        );

        ImGui.Dummy(new Vector2(16.0f));

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale + itemSpacing + 64.0f);
        CtSphereMapIndexPicker(
            "###SphereMapIndex", string.Empty, sphereMap, delegate
            {
            }
        );
        SameLineInner();
        ImGui.TextUnformatted("Sphere Map");
        var textRectMin = ImGui.GetItemRectMin();
        var textRectMax = ImGui.GetItemRectMax();
        ImGui.SameLine(dyeOffset);
        var cursor = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(
            cursor with
            {
                Y = float.Lerp(textRectMin.Y, textRectMax.Y, 0.5f) - ImGui.GetFrameHeight() * 0.5f,
            }
        );
        ImGui.Checkbox("##SphereMapIndexApply", ref sphereMapApply);
        SameLineInner();
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { Y = cursor.Y, });
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale + itemSpacing + 64.0f);
        CtSphereMapIndexPicker(
            "###SphereMapIndexDye", string.Empty, sphereMapDye, delegate
            {
            }
        );

        ImGui.Dummy(new Vector2(64.0f, 0.0f));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        sphereMask *= 100.0f;
        ImGui.DragFloat("Sphere Map Intensity", ref sphereMask, 1.0f, float.NegativeInfinity, float.PositiveInfinity, "%.0f%%");
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##SphereMaskApply", ref sphereMaskApply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        sphereMaskDye *= 100.0f;
        ImGui.DragFloat("##SphereMaskDye", ref sphereMaskDye, 1.0f, float.NegativeInfinity, float.PositiveInfinity, "%.0f%%");

        ImGui.Dummy(new Vector2(16.0f));

        var leftLineHeight = 64.0f + ImGui.GetStyle().FramePadding.Y * 2.0f;
        var rightLineHeight = 3.0f * ImGui.GetFrameHeight() + 2.0f * ImGui.GetStyle().ItemSpacing.Y;
        var lineHeight = Math.Max(leftLineHeight, rightLineHeight);
        var cursorPos = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(cursorPos + new Vector2(0.0f, (lineHeight - leftLineHeight) * 0.5f));
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale + (itemSpacing + 64.0f) * 2.0f);
        CtTileIndexPicker(
            "###TileIndex", string.Empty, tile, delegate
            {
            }
        );
        SameLineInner();
        ImGui.TextUnformatted("Tile");

        ImGui.SameLine(subcolWidth);
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { Y = cursorPos.Y + (lineHeight - rightLineHeight) * 0.5f, });
        using (var cld = ImRaii.Child("###TileProperties", new(ImGui.GetContentRegionAvail().X, float.Lerp(rightLineHeight, lineHeight, 0.5f)), false)) {
            ImGui.Dummy(new Vector2(50.0f * ImGuiHelpers.GlobalScale, 0.0f));
            SameLineInner();
            ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
            tileAlpha *= 100.0f;
            ImGui.DragFloat(
                "Tile Opacity", ref tileAlpha, 1.0f, float.NegativeInfinity, float.PositiveInfinity, "%.0f%%"
            );

            ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
            ImGui.DragFloat(
                "##TileTransformUU", ref tileXfUU, 0.1f, float.NegativeInfinity, float.PositiveInfinity, "%.2f"
            );
            SameLineInner();
            ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
            ImGui.DragFloat(
                "##TileTransformVV", ref tileXfVV, 0.1f, float.NegativeInfinity, float.PositiveInfinity, "%.2f"
            );

            ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
            ImGui.DragFloat(
                "##TileTransformUV", ref tileXfUV, 0.1f, float.NegativeInfinity, float.PositiveInfinity, "%.2f"
            );
            SameLineInner();
            ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
            ImGui.DragFloat(
                "##TileTransformVU", ref tileXfVU, 0.1f, float.NegativeInfinity, float.PositiveInfinity, "%.2f"
            );
            SameLineInner();
            ImGui.SetCursorScreenPos(
                ImGui.GetCursorScreenPos() - new Vector2(
                    0.0f, (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y) * 0.5f
                )
            );
            ImGui.TextUnformatted("Tile Transform");
        }
    }

    private static void DrawPbr(Random random)
    {
        var subcolWidth = CalculateSubcolumnWidth(2) + ImGui.GetStyle().ItemSpacing.X;
        var dyeOffset = subcolWidth - ImGui.GetStyle().ItemSpacing.X * 2.0f - ImGui.GetStyle().ItemInnerSpacing.X
                      - ImGui.GetFrameHeight()
                      - 50.0f * ImGuiHelpers.GlobalScale;

        var sclr16 = random.NextSingle();
        var sclr16Dye = random.NextSingle();
        var sclr16Apply = random.Next(0, 2) != 0;
        var sclr18 = random.NextSingle();
        var sclr18Dye = random.NextSingle();
        var sclr18Apply = random.Next(0, 2) != 0;

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Roughness", ref sclr16, 0.01f);
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##Scalar16Apply", ref sclr16Apply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("##Scalar16Dye", ref sclr16Dye, 0.01f);

        ImGui.SameLine(subcolWidth);
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Metalness", ref sclr18, 0.01f);
        ImGui.SameLine(subcolWidth + dyeOffset);
        ImGui.Checkbox("##Scalar18Apply", ref sclr18Apply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("##Scalar18Dye", ref sclr18Dye, 0.01f);
    }

    private static void DrawFresnel(Random random)
    {
        var subcolWidth = CalculateSubcolumnWidth(2) + ImGui.GetStyle().ItemSpacing.X;
        var dyeOffset = subcolWidth - ImGui.GetStyle().ItemSpacing.X * 2.0f - ImGui.GetStyle().ItemInnerSpacing.X
                      - ImGui.GetFrameHeight()
                      - 50.0f * ImGuiHelpers.GlobalScale;

        var sclr12 = random.NextSingle();
        var sclr12Dye = random.NextSingle();
        var sclr12Apply = random.Next(0, 2) != 0;
        var sclr13 = random.NextSingle();
        var sclr13Dye = random.NextSingle();
        var sclr13Apply = random.Next(0, 2) != 0;
        var sclr14 = random.NextSingle();
        var sclr14Dye = random.NextSingle();
        var sclr14Apply = random.Next(0, 2) != 0;

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Fresnel Y Term", ref sclr12, 0.01f);
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##Scalar12Apply", ref sclr12Apply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("##Scalar12Dye", ref sclr12Dye, 0.01f);

        ImGui.SameLine(subcolWidth);
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Fresnel Albedo", ref sclr13, 0.01f);
        ImGui.SameLine(subcolWidth + dyeOffset);
        ImGui.Checkbox("##Scalar13Apply", ref sclr13Apply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("##Scalar13Dye", ref sclr13Dye, 0.01f);

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Fresnel Z Ramp", ref sclr14, 0.01f);
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##Scalar14Apply", ref sclr14Apply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("##Scalar14Dye", ref sclr14Dye, 0.01f);
    }

    private static void DrawFurther(Random random)
    {
        var subcolWidth = CalculateSubcolumnWidth(2) + ImGui.GetStyle().ItemSpacing.X;
        var dyeOffset = subcolWidth - ImGui.GetStyle().ItemSpacing.X * 2.0f - ImGui.GetStyle().ItemInnerSpacing.X
                      - ImGui.GetFrameHeight()
                      - 50.0f * ImGuiHelpers.GlobalScale;

        var sclr3 = random.NextSingle();
        var sclr7 = random.NextSingle();
        var sclr11 = random.NextSingle();
        var sclr11Dye = random.NextSingle();
        var sclr11Apply = random.Next(0, 2) != 0;
        var sclr15 = random.NextSingle();
        var sclr17 = random.NextSingle();
        var sclr20 = random.NextSingle();
        var sclr22 = random.NextSingle();
        var sclr23 = random.NextSingle();

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #11", ref sclr11, 0.01f);
        ImGui.SameLine(dyeOffset);
        ImGui.Checkbox("##Scalar11Apply", ref sclr11Apply);
        SameLineInner();
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("##Scalar11Dye", ref sclr11Dye, 0.01f);

        ImGui.Dummy(new Vector2(16.0f));

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #3", ref sclr3, 0.01f);

        ImGui.SameLine(subcolWidth);
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #7", ref sclr7, 0.01f);

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #15", ref sclr15, 0.01f);

        ImGui.SameLine(subcolWidth);
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #17", ref sclr17, 0.01f);

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #20", ref sclr20, 0.01f);

        ImGui.SameLine(subcolWidth);
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #22", ref sclr22, 0.01f);

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat("Field #23", ref sclr23, 0.01f);
    }

    private static void DrawDye(Random random)
    {
        var applyButtonWidth = ImGuiComponents
                              .GetNormalizedIconTextButtonSize(FontAwesomeIcon.PaintBrush, "Apply Preview Dye")
                              .X;
        var subcolWidth = CalculateSubcolumnWidth(2, applyButtonWidth);

        var channel = random.Next(1,  3);
        var template = random.Next(0, 1000);

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragInt("Dye Channel", ref channel, 0.1f, 0, 1);
        ImGui.SameLine(subcolWidth);
        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ImGui.DragInt("Dye Template", ref template, 0.25f, 0, 999);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - applyButtonWidth + ImGui.GetStyle().ItemSpacing.X);
        ImGuiComponents.NormalizedIconTextButton(FontAwesomeIcon.PaintBrush, "Apply Preview Dye");
    }

    private static void DrawCtBlendRect(Vector2 rcMin, Vector2 rcMax, uint topColor, uint bottomColor)
    {
        var style = ImGui.GetStyle();
        var frameRounding = style.FrameRounding;
        var frameThickness = style.FrameBorderSize;
        var borderColor = ImGui.GetColorU32(ImGuiCol.Border);
        var drawList = ImGui.GetWindowDrawList();
        if (topColor == bottomColor) {
            drawList.AddRectFilled(rcMin, rcMax, topColor, frameRounding, ImDrawFlags.RoundCornersDefault);
        } else {
            drawList.AddRectFilled(
                rcMin, rcMax with
                {
                    Y = float.Lerp(rcMin.Y, rcMax.Y, 1.0f / 3),
                }, topColor, frameRounding, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersTopRight
            );
            drawList.AddRectFilledMultiColor(
                rcMin with
                {
                    Y = float.Lerp(rcMin.Y, rcMax.Y, 1.0f / 3),
                }, rcMax with
                {
                    Y = float.Lerp(rcMin.Y, rcMax.Y, 2.0f / 3),
                }, topColor, topColor, bottomColor, bottomColor
            );
            drawList.AddRectFilled(
                rcMin with
                {
                    Y = float.Lerp(rcMin.Y, rcMax.Y, 2.0f / 3),
                }, rcMax, bottomColor, frameRounding,
                ImDrawFlags.RoundCornersBottomLeft | ImDrawFlags.RoundCornersBottomRight
            );
        }
        drawList.AddRect(rcMin, rcMax, borderColor, frameRounding, ImDrawFlags.RoundCornersDefault, frameThickness);
    }

    private static void SameLineInner()
        => ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);

    private static void CenteredText(string text)
        => AlignedText(text, 0.5f);

    private static void AlignedText(string text, float alignment)
    {
        var width = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorScreenPos(
            ImGui.GetCursorScreenPos() + new Vector2((ImGui.GetContentRegionAvail().X - width) * alignment, 0.0f)
        );
        ImGui.TextUnformatted(text);
    }

    private static float CalculateSubcolumnWidth(int numSubcolumns, float reservedSpace = 0.0f)
    {
        var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
        return (ImGui.GetContentRegionAvail().X - reservedSpace - itemSpacing * (numSubcolumns - 1)) / numSubcolumns
             + itemSpacing;
    }

    private unsafe bool CtTileIndexPicker(string label, string description, ushort value, Action<ushort> setter)
    {
        var characterUtility = CharacterUtility.Instance();
        return characterUtility is not null && CtTextureArrayIndexPicker(
            label, description, value,
            [
                (TextureResourceHandle*)characterUtility->ResourceHandles[81].Value,
                (TextureResourceHandle*)characterUtility->ResourceHandles[82].Value,
            ],
            setter
        );
    }

    private unsafe bool CtSphereMapIndexPicker(string label, string description, ushort value, Action<ushort> setter)
    {
        var characterUtility = CharacterUtility.Instance();
        return characterUtility is not null && CtTextureArrayIndexPicker(
            label, description, value, [(TextureResourceHandle*)characterUtility->ResourceHandles[96].Value,], setter
        );
    }

    private unsafe bool CtTextureArrayIndexPicker(string label, string description, ushort value,
        ReadOnlySpan<Pointer<TextureResourceHandle>> textureRHs, Action<ushort> setter)
    {
        const float maxTextureSize = 64.0f;

        TextureResourceHandle* firstNonNullTextureRH = null;
        foreach (var texture in textureRHs) {
            if (texture.Value != null && texture.Value->Texture != null) {
                firstNonNullTextureRH = texture;
                break;
            }
        }

        var firstNonNullTexture = firstNonNullTextureRH != null ? firstNonNullTextureRH->Texture : null;

        var textureSize = firstNonNullTexture != null
            ? new Vector2(firstNonNullTexture->ActualWidth, firstNonNullTexture->ActualHeight).Contain(new Vector2(maxTextureSize))
            : Vector2.Zero;
        var count = firstNonNullTexture != null ? firstNonNullTexture->ArraySize : 0;

        var ret = false;

        var framePadding = ImGui.GetStyle().FramePadding;
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        using (var font = ImRaii.PushFont(UiBuilder.MonoFont)) {
            var spaceSize = ImGui.CalcTextSize(" ").X;
            var spaces = (int)((ImGui.CalcItemWidth() - framePadding.X * 2.0f
                                                      - (textureSize.X + itemSpacing.X) * textureRHs.Length)
                             / spaceSize);
            using var padding = ImRaii.PushStyle(
                ImGuiStyleVar.FramePadding,
                framePadding + new Vector2(0.0f, Math.Max(textureSize.Y - ImGui.GetFrameHeight() + itemSpacing.Y, 0.0f) * 0.5f)
            );
            using var combo = ImRaii.Combo(
                label, value.ToString().PadLeft(spaces), ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.HeightLarge
            );
            if (combo.Success && firstNonNullTextureRH != null) {
                var lineHeight = Math.Max(ImGui.GetTextLineHeightWithSpacing(), framePadding.Y * 2.0f + textureSize.Y);
                var itemWidth = ImGui.CalcTextSize("MMM").X + (itemSpacing.X + textureSize.X) * textureRHs.Length
                                                            + framePadding.X * 2.0f;
                using var center = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
                using var clipper = new ImRaiiListClipper(count, lineHeight);
                while (clipper.Step()) {
                    for (var i = clipper.DisplayStart; i < clipper.DisplayEnd && i < count; ++i) {
                        if (ImGui.Selectable($"{i,3}", i == value, 0, new(itemWidth, lineHeight))) {
                            ret = value != i;
                            value = (ushort)i;
                        }

                        var rectMin = ImGui.GetItemRectMin();
                        var rectMax = ImGui.GetItemRectMax();
                        var textureRegionStart = new Vector2(
                            rectMax.X - framePadding.X - textureSize.X * textureRHs.Length
                          - itemSpacing.X * (textureRHs.Length - 1),
                            rectMin.Y + framePadding.Y
                        );
                        var maxSize = textureSize with
                        {
                            Y = rectMax.Y - framePadding.Y - textureRegionStart.Y,
                        };
                        DrawTextureSlices(textureRegionStart, maxSize, itemSpacing.X, textureRHs, (byte)i);
                    }
                }
            }
        }
        var cbRectMin = ImGui.GetItemRectMin();
        var cbRectMax = ImGui.GetItemRectMax();
        var cbTextureRegionStart = new Vector2(
            cbRectMax.X - framePadding.X - textureSize.X * textureRHs.Length
          - itemSpacing.X * (textureRHs.Length - 1),
            cbRectMin.Y + framePadding.Y
        );
        var cbMaxSize = new Vector2(textureSize.X, cbRectMax.Y - framePadding.Y - cbTextureRegionStart.Y);
        DrawTextureSlices(cbTextureRegionStart, cbMaxSize, itemSpacing.X, textureRHs, (byte)value);

        if (description.Length > 0 && ImGui.IsItemHovered()) {
            using var tt = ImRaii.Tooltip();
            ImGui.TextUnformatted(description);
        }

        if (ret) {
            setter(value);
        }

        return ret;
    }

    private unsafe void DrawTextureSlices(Vector2 regionStart, Vector2 itemSize, float itemSpacing,
        ReadOnlySpan<Pointer<TextureResourceHandle>> textureRHs, byte sliceIndex)
    {
        for (var j = 0; j < textureRHs.Length; ++j) {
            if (textureRHs[j].Value == null) {
                continue;
            }

            var texture = textureRHs[j].Value->Texture;
            if (texture == null) {
                continue;
            }

            var handle = _textureArraySlicer.GetImGuiHandle(texture, sliceIndex);
            if (handle == 0) {
                continue;
            }

            var position = regionStart with
            {
                X = regionStart.X + (itemSize.X + itemSpacing) * j
            };
            var size = new Vector2(texture->ActualWidth, texture->ActualHeight).Contain(itemSize);
            position += (itemSize - size) * 0.5f;
            ImGui.GetWindowDrawList()
                 .AddImage(
                      handle, position, position + size, Vector2.Zero,
                      new Vector2(texture->ActualWidth / (float)texture->AllocatedWidth, texture->ActualHeight / (float)texture->AllocatedHeight)
                  );
        }
    }

    private ref struct Columns
    {
        public Columns(int count, string? id = null, bool border = true)
        {
            ImGui.Columns(count, id, border);
        }

        public void Next()
        {
            ImGui.NextColumn();
        }

        public void Dispose()
        {
            ImGui.Columns();
        }
    }
}
