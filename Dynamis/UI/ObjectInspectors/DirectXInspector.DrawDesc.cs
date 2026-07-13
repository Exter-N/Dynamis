using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dynamis.Utility;
using InteropGenerator.Runtime;
using TerraFX.Interop.DirectX;

namespace Dynamis.UI.ObjectInspectors;

partial class DirectXInspector
{
    private void DrawDesc(in D3D11_BLEND_DESC desc)
    {
        ImGui.TextUnformatted($"Alpha to Coverage: {(desc.AlphaToCoverageEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Independent Blend: {(desc.IndependentBlendEnable ? "Enabled" : "Disabled")}");
        var renderTargets = (ReadOnlySpan<D3D11_RENDER_TARGET_BLEND_DESC>)desc.RenderTarget;
        if (desc.IndependentBlendEnable) {
            for (var i = 0; i < renderTargets.Length; i++) {
                ImGui.TextUnformatted($"[{i}]");
                ImGui.SameLine();
                DrawDesc(in desc.RenderTarget[i]);
            }
        } else {
            DrawDesc(in renderTargets[0]);
        }
    }

    private void DrawDesc(in D3D11_BLEND_DESC1 desc)
    {
        ImGui.TextUnformatted($"Alpha to Coverage: {(desc.AlphaToCoverageEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Independent Blend: {(desc.IndependentBlendEnable ? "Enabled" : "Disabled")}");
        var renderTargets = (ReadOnlySpan<D3D11_RENDER_TARGET_BLEND_DESC1>)desc.RenderTarget;
        if (desc.IndependentBlendEnable) {
            for (var i = 0; i < renderTargets.Length; i++) {
                ImGui.TextUnformatted($"[{i}]");
                ImGui.SameLine();
                DrawDesc(in desc.RenderTarget[i]);
            }
        } else {
            DrawDesc(in renderTargets[0]);
        }
    }

    private void DrawDesc(in D3D11_BUFFER_DESC desc)
    {
        ImGui.TextUnformatted($"Buffer Size: {desc.ByteWidth} (0x{desc.ByteWidth:X}) bytes");
        ImGui.TextUnformatted($"Usage: {desc.Usage}");
        ImGui.TextUnformatted($"Bind Flags: {(D3D11_BIND_FLAG)desc.BindFlags}");
        ImGui.TextUnformatted($"CPU Access Flags: {(D3D11_CPU_ACCESS_FLAG)desc.CPUAccessFlags}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_RESOURCE_MISC_FLAG)desc.MiscFlags}");
        ImGui.TextUnformatted($"Structure Stride: {desc.StructureByteStride} (0x{desc.StructureByteStride:X}) bytes");
    }

    private void DrawDesc(in D3D11_CLASS_INSTANCE_DESC desc)
    {
        ImGui.TextUnformatted($"Instance ID: {desc.InstanceId}");
        ImGui.TextUnformatted($"Instance Index: {desc.InstanceIndex}");
        ImGui.TextUnformatted($"Type ID: {desc.TypeId}");
        ImGui.TextUnformatted($"Constant Buffer: slot {desc.ConstantBuffer}, offset {desc.BaseConstantBufferOffset}");
        ImGui.TextUnformatted($"Base Texture: {desc.BaseTexture}");
        ImGui.TextUnformatted($"Base Sampler: {desc.BaseSampler}");
        ImGui.TextUnformatted($"Created: {(desc.Created ? "Yes" : "No")}");
    }

    private void DrawDesc(in D3D11_COMPUTE_SHADER_TRACE_DESC desc)
    {
        ImGui.TextUnformatted($"Invocation: {desc.Invocation}");
        ImGui.TextUnformatted($"Thread ID in Group: {desc.ThreadIDInGroup[0]}, {desc.ThreadIDInGroup[1]}, {desc.ThreadIDInGroup[2]}");
        ImGui.TextUnformatted($"Thread Group ID: {desc.ThreadGroupID[0]}, {desc.ThreadGroupID[1]}, {desc.ThreadGroupID[2]}");
    }

    private void DrawDesc(in D3D11_COUNTER_DESC desc)
    {
        ImGui.TextUnformatted($"Counter: {desc.Counter}");
        ImGui.TextUnformatted($"Misc Flags: 0x{desc.MiscFlags:X}");
    }

    private void DrawDesc(in D3D11_DEPTH_STENCIL_DESC desc)
    {
        ImGui.TextUnformatted($"Depth: {(desc.DepthEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Depth Write Mask: {desc.DepthWriteMask}");
        ImGui.TextUnformatted($"Depth Func: {desc.DepthFunc}");
        ImGui.TextUnformatted($"Stencil: {(desc.StencilEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Stencil Read Mask: 0x{desc.StencilReadMask:X}");
        ImGui.TextUnformatted($"Stencil Write Mask: 0x{desc.StencilWriteMask:X}");

        ImGui.TextUnformatted("Front: "u8);
        ImGui.SameLine();
        DrawDesc(in desc.FrontFace);

        ImGui.TextUnformatted("Back: "u8);
        ImGui.SameLine();
        DrawDesc(in desc.BackFace);
    }

    private void DrawDesc(in D3D11_DEPTH_STENCILOP_DESC desc)
    {
        ImGui.TextUnformatted(
            $"Fail {desc.StencilFailOp.ToShortString()}, Depth Fail {desc.StencilDepthFailOp.ToShortString()}, Pass {desc.StencilPassOp.ToShortString()}, Func {desc.StencilFunc.ToShortString("D3D11_COMPARISON_")}"
        );
    }

    private void DrawDesc(in D3D11_DEPTH_STENCIL_VIEW_DESC desc)
    {
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"View Dimension: {desc.ViewDimension}");
        ImGui.TextUnformatted($"Flags: {(D3D11_DSV_FLAG)desc.Flags}");
        switch (desc.ViewDimension) {
            case D3D11_DSV_DIMENSION.D3D11_DSV_DIMENSION_TEXTURE1D:
                DrawDesc(in desc.Texture1D);
                break;
            case D3D11_DSV_DIMENSION.D3D11_DSV_DIMENSION_TEXTURE1DARRAY:
                DrawDesc(in desc.Texture1DArray);
                break;
            case D3D11_DSV_DIMENSION.D3D11_DSV_DIMENSION_TEXTURE2D:
                DrawDesc(in desc.Texture2D);
                break;
            case D3D11_DSV_DIMENSION.D3D11_DSV_DIMENSION_TEXTURE2DARRAY:
                DrawDesc(in desc.Texture2DArray);
                break;
            case D3D11_DSV_DIMENSION.D3D11_DSV_DIMENSION_TEXTURE2DMS:
                DrawDesc(in desc.Texture2DMS);
                break;
            case D3D11_DSV_DIMENSION.D3D11_DSV_DIMENSION_TEXTURE2DMSARRAY:
                DrawDesc(in desc.Texture2DMSArray);
                break;
        }
    }

    private void DrawDesc(in D3D11_TEX1D_DSV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
    }

    private void DrawDesc(in D3D11_TEX1D_ARRAY_DSV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX2D_DSV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
    }

    private void DrawDesc(in D3D11_TEX2D_ARRAY_DSV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawDesc(in D3D11_TEX2DMS_DSV desc)
    {
        // Nothing to draw.
    }

    private void DrawDesc(in D3D11_TEX2DMS_ARRAY_DSV desc)
    {
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_DOMAIN_SHADER_TRACE_DESC desc)
    {
        ImGui.TextUnformatted($"Invocation: {desc.Invocation}");
    }

    private unsafe void DrawDesc(in D3D11_FUNCTION_DESC desc)
    {
        ImGui.TextUnformatted($"Version: 0x{desc.Version:X}");
        ImGui.TextUnformatted($"Creator: {new CStringPointer((byte*)desc.Creator)}");
        ImGui.TextUnformatted($"Flags: 0x{desc.Flags:X}");
        if (desc.ConstantBuffers > 0) {
            ImGui.TextUnformatted($"Constant Buffers: {desc.ConstantBuffers}");
        }

        if (desc.BoundResources > 0) {
            ImGui.TextUnformatted($"Bound Resources: {desc.BoundResources}");
        }

        if (desc.InstructionCount > 0) {
            ImGui.TextUnformatted($"Instruction Count: {desc.InstructionCount}");
        }

        if (desc.TempRegisterCount > 0) {
            ImGui.TextUnformatted($"Temp Register Count: {desc.TempRegisterCount}");
        }

        if (desc.TempArrayCount > 0) {
            ImGui.TextUnformatted($"Temp Array Count: {desc.TempArrayCount}");
        }

        if (desc.DefCount > 0) {
            ImGui.TextUnformatted($"Def Count: {desc.DefCount}");
        }

        if (desc.DclCount > 0) {
            ImGui.TextUnformatted($"Dcl Count: {desc.DclCount}");
        }

        if (desc.TextureNormalInstructions > 0) {
            ImGui.TextUnformatted($"Texture Normal Instructions: {desc.TextureNormalInstructions}");
        }

        if (desc.TextureLoadInstructions > 0) {
            ImGui.TextUnformatted($"Texture Load Instructions: {desc.TextureLoadInstructions}");
        }

        if (desc.TextureCompInstructions > 0) {
            ImGui.TextUnformatted($"Texture Comp Instructions: {desc.TextureCompInstructions}");
        }

        if (desc.TextureBiasInstructions > 0) {
            ImGui.TextUnformatted($"Texture Bias Instructions: {desc.TextureBiasInstructions}");
        }

        if (desc.TextureGradientInstructions > 0) {
            ImGui.TextUnformatted($"Texture Gradient Instructions: {desc.TextureGradientInstructions}");
        }

        if (desc.FloatInstructionCount > 0) {
            ImGui.TextUnformatted($"Float Instruction Count: {desc.FloatInstructionCount}");
        }

        if (desc.IntInstructionCount > 0) {
            ImGui.TextUnformatted($"Int Instruction Count: {desc.IntInstructionCount}");
        }

        if (desc.UintInstructionCount > 0) {
            ImGui.TextUnformatted($"Uint Instruction Count: {desc.UintInstructionCount}");
        }

        if (desc.StaticFlowControlCount > 0) {
            ImGui.TextUnformatted($"Static Flow Control Count: {desc.StaticFlowControlCount}");
        }

        if (desc.DynamicFlowControlCount > 0) {
            ImGui.TextUnformatted($"Dynamic Flow Control Count: {desc.DynamicFlowControlCount}");
        }

        if (desc.MacroInstructionCount > 0) {
            ImGui.TextUnformatted($"Macro Instruction Count: {desc.MacroInstructionCount}");
        }

        if (desc.ArrayInstructionCount > 0) {
            ImGui.TextUnformatted($"Array Instruction Count: {desc.ArrayInstructionCount}");
        }

        if (desc.MovInstructionCount > 0) {
            ImGui.TextUnformatted($"Mov Instruction Count: {desc.MovInstructionCount}");
        }

        if (desc.MovcInstructionCount > 0) {
            ImGui.TextUnformatted($"Movc Instruction Count: {desc.MovcInstructionCount}");
        }

        if (desc.ConversionInstructionCount > 0) {
            ImGui.TextUnformatted($"Conversion Instruction Count: {desc.ConversionInstructionCount}");
        }

        if (desc.BitwiseInstructionCount > 0) {
            ImGui.TextUnformatted($"Bitwise Instruction Count: {desc.BitwiseInstructionCount}");
        }

        ImGui.TextUnformatted($"Minimum Feature Level: {desc.MinFeatureLevel}");
        ImGui.TextUnformatted($"Required Feature Flags: 0x{desc.RequiredFeatureFlags:X}");
        ImGui.TextUnformatted($"Name: {new CStringPointer((byte*)desc.Name)}");
        ImGui.TextUnformatted($"Function Parameter Count: {desc.FunctionParameterCount}");
        ImGui.TextUnformatted($"Has Return: {(desc.HasReturn ? "Yes" : "No")}");
        if (desc.Has10Level9VertexShader) {
            ImGui.TextUnformatted("Has 10 Level 9 Vertex Shader"u8);
        }

        if (desc.Has10Level9PixelShader) {
            ImGui.TextUnformatted("Has 10 Level 9 Pixel Shader"u8);
        }
    }

    private void DrawDesc(in D3D11_GEOMETRY_SHADER_TRACE_DESC desc)
    {
        ImGui.TextUnformatted($"Invocation: {desc.Invocation}");
    }

    private void DrawDesc(in D3D11_HULL_SHADER_TRACE_DESC desc)
    {
        ImGui.TextUnformatted($"Invocation: {desc.Invocation}");
    }

    private unsafe void DrawDesc(in D3D11_INFO_QUEUE_FILTER_DESC desc)
    {
        var categories = new ReadOnlySpan<D3D11_MESSAGE_CATEGORY>(desc.pCategoryList, (int)desc.NumCategories);
        var sb = new StringBuilder();
        sb.Append("Categories: ");
        AppendList(sb, categories);
        ImGui.TextUnformatted(sb.ToString());

        var severities = new ReadOnlySpan<D3D11_MESSAGE_SEVERITY>(desc.pSeverityList, (int)desc.NumSeverities);
        sb.Clear();
        sb.Append("Severities: ");
        AppendList(sb, severities);
        ImGui.TextUnformatted(sb.ToString());

        var ids = new ReadOnlySpan<D3D11_MESSAGE_ID>(desc.pIDList, (int)desc.NumIDs);
        sb.Clear();
        sb.Append("IDs: ");
        AppendList(sb, ids);
        ImGui.TextUnformatted(sb.ToString());
    }

    private unsafe void DrawDesc(in D3D11_INPUT_ELEMENT_DESC desc)
    {
        ImGui.TextUnformatted($"Semantic: {new CStringPointer((byte*)desc.SemanticName)}{desc.SemanticIndex}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"Input Slot: {desc.InputSlot}");
        ImGui.TextUnformatted($"Aligned Byte Offset: {desc.AlignedByteOffset}");
        ImGui.TextUnformatted($"Input Slot Class: {desc.InputSlotClass}");
        ImGui.TextUnformatted($"Instance Data Step Rate: {desc.InstanceDataStepRate}");
    }

    private unsafe void DrawDesc(in D3D11_LIBRARY_DESC desc)
    {
        ImGui.TextUnformatted($"Creator: {new CStringPointer((byte*)desc.Creator)}");
        ImGui.TextUnformatted($"Flags: 0x{desc.Flags:X}");
        ImGui.TextUnformatted($"Function Count: {desc.FunctionCount}");
    }

    private void DrawDesc(in D3D11_PACKED_MIP_DESC desc)
    {
        ImGui.TextUnformatted($"Standard Mips: {desc.NumStandardMips}");
        ImGui.TextUnformatted($"Packed Mips: {desc.NumPackedMips}");
        ImGui.TextUnformatted($"Tiles for Packed Mips: {desc.NumTilesForPackedMips}");
        ImGui.TextUnformatted($"Start Tile Index in Overall Resource: {desc.StartTileIndexInOverallResource}");
    }

    private unsafe void DrawDesc(in D3D11_PARAMETER_DESC desc)
    {
        ImGui.TextUnformatted($"Name: {new CStringPointer((byte*)desc.Name)}");
        ImGui.TextUnformatted($"Semantic: {new CStringPointer((byte*)desc.SemanticName)}");
        ImGui.TextUnformatted($"Type: {desc.Type}");
        ImGui.TextUnformatted($"Class: {desc.Class}");
        ImGui.TextUnformatted($"Rows: {desc.Rows}");
        ImGui.TextUnformatted($"Columns: {desc.Columns}");
        ImGui.TextUnformatted($"Interpolation: {desc.InterpolationMode}");
        ImGui.TextUnformatted($"Flags: {desc.Flags}");
        ImGui.TextUnformatted($"First In: {desc.FirstInRegister}, component {desc.FirstInComponent}");
        ImGui.TextUnformatted($"First Out: {desc.FirstOutRegister}, component {desc.FirstOutComponent}");
    }

    private void DrawDesc(in D3D11_PIXEL_SHADER_TRACE_DESC desc)
    {
        ImGui.TextUnformatted($"Invocation: {desc.Invocation}");
        ImGui.TextUnformatted($"Position: {desc.X}, {desc.Y}");
        ImGui.TextUnformatted($"Sample Mask: 0x{desc.SampleMask:X}");
    }

    private void DrawDesc(in D3D11_QUERY_DESC desc)
    {
        ImGui.TextUnformatted($"Query: {desc.Query}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_QUERY_MISC_FLAG)desc.MiscFlags}");
    }

    private void DrawDesc(in D3D11_QUERY_DESC1 desc)
    {
        ImGui.TextUnformatted($"Query: {desc.Query}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_QUERY_MISC_FLAG)desc.MiscFlags}");
        ImGui.TextUnformatted($"Context Type: {desc.ContextType}");
    }

    private void DrawDesc(in D3D11_RASTERIZER_DESC desc)
    {
        ImGui.TextUnformatted($"Fill Mode: {desc.FillMode}");
        ImGui.TextUnformatted($"Cull Mode: {desc.CullMode}");
        ImGui.TextUnformatted($"Front: {(desc.FrontCounterClockwise ? "Counterclockwise" : "Clockwise")}");
        ImGui.TextUnformatted($"Depth Bias: {desc.DepthBias}");
        ImGui.TextUnformatted($"Depth Bias Clamp: {desc.DepthBiasClamp}");
        ImGui.TextUnformatted($"Slope Scaled Depth Bias: {desc.SlopeScaledDepthBias}");
        ImGui.TextUnformatted($"Depth Clip: {(desc.DepthClipEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Scissor: {(desc.ScissorEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Multisample: {(desc.MultisampleEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Antialiased Line: {(desc.AntialiasedLineEnable ? "Enabled" : "Disabled")}");
    }

    private void DrawDesc(in D3D11_RASTERIZER_DESC1 desc)
    {
        ImGui.TextUnformatted($"Fill Mode: {desc.FillMode}");
        ImGui.TextUnformatted($"Cull Mode: {desc.CullMode}");
        ImGui.TextUnformatted($"Front: {(desc.FrontCounterClockwise ? "Counterclockwise" : "Clockwise")}");
        ImGui.TextUnformatted($"Depth Bias: {desc.DepthBias}");
        ImGui.TextUnformatted($"Depth Bias Clamp: {desc.DepthBiasClamp}");
        ImGui.TextUnformatted($"Slope Scaled Depth Bias: {desc.SlopeScaledDepthBias}");
        ImGui.TextUnformatted($"Depth Clip: {(desc.DepthClipEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Scissor: {(desc.ScissorEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Multisample: {(desc.MultisampleEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Antialiased Line: {(desc.AntialiasedLineEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Forced Sample Count: {desc.ForcedSampleCount}");
    }

    private void DrawDesc(in D3D11_RASTERIZER_DESC2 desc)
    {
        ImGui.TextUnformatted($"Fill Mode: {desc.FillMode}");
        ImGui.TextUnformatted($"Cull Mode: {desc.CullMode}");
        ImGui.TextUnformatted($"Front: {(desc.FrontCounterClockwise ? "Counterclockwise" : "Clockwise")}");
        ImGui.TextUnformatted($"Depth Bias: {desc.DepthBias}");
        ImGui.TextUnformatted($"Depth Bias Clamp: {desc.DepthBiasClamp}");
        ImGui.TextUnformatted($"Slope Scaled Depth Bias: {desc.SlopeScaledDepthBias}");
        ImGui.TextUnformatted($"Depth Clip: {(desc.DepthClipEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Scissor: {(desc.ScissorEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Multisample: {(desc.MultisampleEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Antialiased Line: {(desc.AntialiasedLineEnable ? "Enabled" : "Disabled")}");
        ImGui.TextUnformatted($"Forced Sample Count: {desc.ForcedSampleCount}");
        ImGui.TextUnformatted($"Conservative Raster: {desc.ConservativeRaster}");
    }

    private void DrawDesc(in D3D11_RENDER_TARGET_BLEND_DESC desc)
    {
        if (desc.BlendEnable) {
            ImGui.TextUnformatted(
                $"Src {desc.SrcBlend.ToShortString()}, Dest {desc.DestBlend.ToShortString()}, Op {desc.BlendOp.ToShortString()} / SrcA {desc.SrcBlendAlpha.ToShortString()}, DestA {desc.DestBlendAlpha.ToShortString()}, OpA {desc.BlendOpAlpha.ToShortString()} / WM 0x{desc.RenderTargetWriteMask:X}"
            );
        } else {
            ImGui.TextUnformatted($"Disabled / WM 0x{desc.RenderTargetWriteMask:X}");
        }
    }

    private void DrawDesc(in D3D11_RENDER_TARGET_BLEND_DESC1 desc)
    {
        if (desc.BlendEnable) {
            ImGui.TextUnformatted(
                $"Src {desc.SrcBlend.ToShortString()}, Dest {desc.DestBlend.ToShortString()}, Op {desc.BlendOp.ToShortString()} / SrcA {desc.SrcBlendAlpha.ToShortString()}, DestA {desc.DestBlendAlpha.ToShortString()}, OpA {desc.BlendOpAlpha.ToShortString()} / WM 0x{desc.RenderTargetWriteMask:X}"
            );
        } else if (desc.LogicOpEnable) {
            ImGui.TextUnformatted($"Logic Op {desc.LogicOp.ToShortString()} / WM 0x{desc.RenderTargetWriteMask:X}");
        } else {
            ImGui.TextUnformatted($"Disabled / WM 0x{desc.RenderTargetWriteMask:X}");
        }
    }

    private void DrawDesc(in D3D11_RENDER_TARGET_VIEW_DESC desc)
    {
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"View Dimension: {desc.ViewDimension}");
        switch (desc.ViewDimension) {
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_BUFFER:
                DrawDesc(in desc.Buffer);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE1D:
                DrawDesc(in desc.Texture1D);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE1DARRAY:
                DrawDesc(in desc.Texture1DArray);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D:
                DrawDesc(in desc.Texture2D);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2DARRAY:
                DrawDesc(in desc.Texture2DArray);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2DMS:
                DrawDesc(in desc.Texture2DMS);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2DMSARRAY:
                DrawDesc(in desc.Texture2DMSArray);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE3D:
                DrawDesc(in desc.Texture3D);
                break;
        }
    }

    private void DrawDesc(in D3D11_RENDER_TARGET_VIEW_DESC1 desc)
    {
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"View Dimension: {desc.ViewDimension}");
        switch (desc.ViewDimension) {
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_BUFFER:
                DrawDesc(in desc.Buffer);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE1D:
                DrawDesc(in desc.Texture1D);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE1DARRAY:
                DrawDesc(in desc.Texture1DArray);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D:
                DrawDesc(in desc.Texture2D);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2DARRAY:
                DrawDesc(in desc.Texture2DArray);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2DMS:
                DrawDesc(in desc.Texture2DMS);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2DMSARRAY:
                DrawDesc(in desc.Texture2DMSArray);
                break;
            case D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE3D:
                DrawDesc(in desc.Texture3D);
                break;
        }
    }

    private void DrawDesc(in D3D11_BUFFER_RTV desc)
    {
        ImGui.TextUnformatted($"First Element / Element Offset: {desc.FirstElement}");
        ImGui.TextUnformatted($"Num Elements / Element Width: {desc.NumElements}");
    }

    private void DrawDesc(in D3D11_TEX1D_RTV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
    }

    private void DrawDesc(in D3D11_TEX1D_ARRAY_RTV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX2D_RTV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
    }

    private void DrawDesc(in D3D11_TEX2D_RTV1 desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Plane Slice: {desc.PlaneSlice}");
    }

    private void DrawDesc(in D3D11_TEX2D_ARRAY_RTV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX2D_ARRAY_RTV1 desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
        ImGui.TextUnformatted($"Plane Slice: {desc.PlaneSlice}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawDesc(in D3D11_TEX2DMS_RTV desc)
    {
        // Nothing to draw.
    }

    private void DrawDesc(in D3D11_TEX2DMS_ARRAY_RTV desc)
    {
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX3D_RTV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"W Slices: Start {desc.FirstWSlice}, Size {desc.WSize}");
    }

    private void DrawDesc(in D3D11_SAMPLER_DESC desc)
    {
        ImGui.TextUnformatted($"Filter: {desc.Filter}");
        ImGui.TextUnformatted(
            $"Address: U {desc.AddressU.ToShortString("D3D11_TEXTURE_ADDRESS_")}, V {desc.AddressV.ToShortString("D3D11_TEXTURE_ADDRESS_")}, W {desc.AddressW.ToShortString("D3D11_TEXTURE_ADDRESS_")}"
        );
        ImGui.TextUnformatted($"Mip LoD Bias: {desc.MipLODBias}");
        ImGui.TextUnformatted($"Max Anisotropy: {desc.MaxAnisotropy}");
        ImGui.TextUnformatted($"Comparison Func: {desc.ComparisonFunc}");
        ref readonly var borderColor =
            ref MemoryMarshal.Cast<D3D11_SAMPLER_DESC._BorderColor_e__FixedBuffer, Vector4>(
                new(in desc.BorderColor)
            )[0];
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted($"Border Color: {borderColor}");
        ImGui.SameLine();
        ImGui.Dummy(new(ImGui.GetFrameHeight()));
        ImGui.GetWindowDrawList()
             .AddRectFilled(
                  ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), borderColor.ToUInt32(), ImGui.GetStyle().FrameRounding
              );
        ImGui.TextUnformatted($"Min LoD: {desc.MinLOD}");
        ImGui.TextUnformatted($"Max LoD: {desc.MaxLOD}");
    }

    private unsafe void DrawDesc(in D3D11_SHADER_BUFFER_DESC desc)
    {
        ImGui.TextUnformatted($"Name: {new CStringPointer((byte*)desc.Name)}");
        ImGui.TextUnformatted($"Type: {desc.Type}");
        ImGui.TextUnformatted($"Variables: {desc.Variables}");
        ImGui.TextUnformatted($"Size: {desc.Size}");
        ImGui.TextUnformatted($"Flags: {(D3D_SHADER_CBUFFER_FLAGS)desc.uFlags}");
    }

    private unsafe void DrawDesc(in D3D11_SHADER_DESC desc)
    {
        ImGui.TextUnformatted($"Version: 0x{desc.Version:X}");
        ImGui.TextUnformatted($"Creator: {new CStringPointer((byte*)desc.Creator)}");
        ImGui.TextUnformatted($"Flags: 0x{desc.Flags:X}");
        if (desc.ConstantBuffers > 0) {
            ImGui.TextUnformatted($"Constant Buffers: {desc.ConstantBuffers}");
        }

        if (desc.BoundResources > 0) {
            ImGui.TextUnformatted($"Bound Resources: {desc.BoundResources}");
        }

        ImGui.TextUnformatted($"Input Parameters: {desc.InputParameters}");
        ImGui.TextUnformatted($"Output Parameters: {desc.OutputParameters}");

        if (desc.InstructionCount > 0) {
            ImGui.TextUnformatted($"Instruction Count: {desc.InstructionCount}");
        }

        if (desc.TempRegisterCount > 0) {
            ImGui.TextUnformatted($"Temp Register Count: {desc.TempRegisterCount}");
        }

        if (desc.TempArrayCount > 0) {
            ImGui.TextUnformatted($"Temp Array Count: {desc.TempArrayCount}");
        }

        if (desc.DefCount > 0) {
            ImGui.TextUnformatted($"Def Count: {desc.DefCount}");
        }

        if (desc.DclCount > 0) {
            ImGui.TextUnformatted($"Dcl Count: {desc.DclCount}");
        }

        if (desc.TextureNormalInstructions > 0) {
            ImGui.TextUnformatted($"Texture Normal Instructions: {desc.TextureNormalInstructions}");
        }

        if (desc.TextureLoadInstructions > 0) {
            ImGui.TextUnformatted($"Texture Load Instructions: {desc.TextureLoadInstructions}");
        }

        if (desc.TextureCompInstructions > 0) {
            ImGui.TextUnformatted($"Texture Comp Instructions: {desc.TextureCompInstructions}");
        }

        if (desc.TextureBiasInstructions > 0) {
            ImGui.TextUnformatted($"Texture Bias Instructions: {desc.TextureBiasInstructions}");
        }

        if (desc.TextureGradientInstructions > 0) {
            ImGui.TextUnformatted($"Texture Gradient Instructions: {desc.TextureGradientInstructions}");
        }

        if (desc.FloatInstructionCount > 0) {
            ImGui.TextUnformatted($"Float Instruction Count: {desc.FloatInstructionCount}");
        }

        if (desc.IntInstructionCount > 0) {
            ImGui.TextUnformatted($"Int Instruction Count: {desc.IntInstructionCount}");
        }

        if (desc.UintInstructionCount > 0) {
            ImGui.TextUnformatted($"Uint Instruction Count: {desc.UintInstructionCount}");
        }

        if (desc.StaticFlowControlCount > 0) {
            ImGui.TextUnformatted($"Static Flow Control Count: {desc.StaticFlowControlCount}");
        }

        if (desc.DynamicFlowControlCount > 0) {
            ImGui.TextUnformatted($"Dynamic Flow Control Count: {desc.DynamicFlowControlCount}");
        }

        if (desc.MacroInstructionCount > 0) {
            ImGui.TextUnformatted($"Macro Instruction Count: {desc.MacroInstructionCount}");
        }

        if (desc.ArrayInstructionCount > 0) {
            ImGui.TextUnformatted($"Array Instruction Count: {desc.ArrayInstructionCount}");
        }

        if (desc.CutInstructionCount > 0) {
            ImGui.TextUnformatted($"Cut Instruction Count: {desc.CutInstructionCount}");
        }

        if (desc.EmitInstructionCount > 0) {
            ImGui.TextUnformatted($"Emit Instruction Count: {desc.EmitInstructionCount}");
        }

        ImGui.TextUnformatted($"GS Output Topology: {desc.GSOutputTopology}");
        if (desc.GSMaxOutputVertexCount > 0) {
            ImGui.TextUnformatted($"GS Max Output Vertex Count: {desc.GSMaxOutputVertexCount}");
        }

        ImGui.TextUnformatted($"Input Primitive: {desc.InputPrimitive}");

        if (desc.PatchConstantParameters > 0) {
            ImGui.TextUnformatted($"Patch Constant Parameters: {desc.PatchConstantParameters}");
        }

        if (desc.cGSInstanceCount > 0) {
            ImGui.TextUnformatted($"GS Instance Count: {desc.cGSInstanceCount}");
        }

        if (desc.cControlPoints > 0) {
            ImGui.TextUnformatted($"Control Points: {desc.cControlPoints}");
        }

        ImGui.TextUnformatted($"HS Output Primitive: {desc.HSOutputPrimitive}");
        ImGui.TextUnformatted($"HS Partitioning: {desc.HSPartitioning}");
        ImGui.TextUnformatted($"Tessellator Domain: {desc.TessellatorDomain}");

        if (desc.cBarrierInstructions > 0) {
            ImGui.TextUnformatted($"Barrier Instructions: {desc.cBarrierInstructions}");
        }

        if (desc.cInterlockedInstructions > 0) {
            ImGui.TextUnformatted($"Interlocked Instructions: {desc.cInterlockedInstructions}");
        }

        if (desc.cTextureStoreInstructions > 0) {
            ImGui.TextUnformatted($"Texture Store Instructions: {desc.cTextureStoreInstructions}");
        }
    }

    private unsafe void DrawDesc(in D3D11_SHADER_INPUT_BIND_DESC desc)
    {
        ImGui.TextUnformatted($"Name: {new CStringPointer((byte*)desc.Name)}");
        ImGui.TextUnformatted($"Type: {desc.Type}");
        ImGui.TextUnformatted($"Bind Point: {desc.BindPoint}, Count {desc.BindCount}");
        ImGui.TextUnformatted($"Flags: 0x{desc.uFlags:X}");
        ImGui.TextUnformatted($"Return Type: {desc.ReturnType}");
        ImGui.TextUnformatted($"Dimension: {desc.Dimension}");
        ImGui.TextUnformatted($"Num Samples: {desc.NumSamples}");
    }

    private void DrawDesc(in D3D11_SHADER_RESOURCE_VIEW_DESC desc)
    {
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"View Dimension: {desc.ViewDimension}");
        switch (desc.ViewDimension) {
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_BUFFER:
                DrawDesc(in desc.Buffer);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE1D:
                DrawDesc(in desc.Texture1D);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE1DARRAY:
                DrawDesc(in desc.Texture1DArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2D:
                DrawDesc(in desc.Texture2D);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2DARRAY:
                DrawDesc(in desc.Texture2DArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2DMS:
                DrawDesc(in desc.Texture2DMS);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2DMSARRAY:
                DrawDesc(in desc.Texture2DMSArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE3D:
                DrawDesc(in desc.Texture3D);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURECUBE:
                DrawDesc(in desc.TextureCube);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURECUBEARRAY:
                DrawDesc(in desc.TextureCubeArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_BUFFEREX:
                DrawDesc(in desc.BufferEx);
                break;
        }
    }

    private void DrawDesc(in D3D11_SHADER_RESOURCE_VIEW_DESC1 desc)
    {
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"View Dimension: {desc.ViewDimension}");
        switch (desc.ViewDimension) {
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_BUFFER:
                DrawDesc(in desc.Buffer);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE1D:
                DrawDesc(in desc.Texture1D);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE1DARRAY:
                DrawDesc(in desc.Texture1DArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2D:
                DrawDesc(in desc.Texture2D);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2DARRAY:
                DrawDesc(in desc.Texture2DArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2DMS:
                DrawDesc(in desc.Texture2DMS);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE2DMSARRAY:
                DrawDesc(in desc.Texture2DMSArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURE3D:
                DrawDesc(in desc.Texture3D);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURECUBE:
                DrawDesc(in desc.TextureCube);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_TEXTURECUBEARRAY:
                DrawDesc(in desc.TextureCubeArray);
                break;
            case D3D_SRV_DIMENSION.D3D_SRV_DIMENSION_BUFFEREX:
                DrawDesc(in desc.BufferEx);
                break;
        }
    }

    private void DrawDesc(in D3D11_BUFFER_SRV desc)
    {
        ImGui.TextUnformatted($"First Element / Element Offset: {desc.FirstElement}");
        ImGui.TextUnformatted($"Num Elements / Element Width: {desc.NumElements}");
    }

    private void DrawDesc(in D3D11_TEX1D_SRV desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
    }

    private void DrawDesc(in D3D11_TEX1D_ARRAY_SRV desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX2D_SRV desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
    }

    private void DrawDesc(in D3D11_TEX2D_SRV1 desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Plane Slice: {desc.PlaneSlice}");
    }

    private void DrawDesc(in D3D11_TEX2D_ARRAY_SRV desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX2D_ARRAY_SRV1 desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
        ImGui.TextUnformatted($"Plane Slice: {desc.PlaneSlice}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawDesc(in D3D11_TEX2DMS_SRV desc)
    {
        // Nothing to draw.
    }

    private void DrawDesc(in D3D11_TEX2DMS_ARRAY_SRV desc)
    {
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX3D_SRV desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
    }

    private void DrawDesc(in D3D11_TEXCUBE_SRV desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
    }

    private void DrawDesc(in D3D11_TEXCUBE_ARRAY_SRV desc)
    {
        ImGui.TextUnformatted($"Most Detailed Mip: {desc.MostDetailedMip}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"First 2D Array Face: {desc.First2DArrayFace}");
        ImGui.TextUnformatted($"Num Cubes: {desc.NumCubes}");
    }

    private void DrawDesc(in D3D11_BUFFEREX_SRV desc)
    {
        ImGui.TextUnformatted($"First Element: {desc.FirstElement}");
        ImGui.TextUnformatted($"Num Elements: {desc.NumElements}");
        ImGui.TextUnformatted($"Flags: {(D3D11_BUFFEREX_SRV_FLAG)desc.Flags}");
    }

    private void DrawDesc(in D3D11_SHADER_TRACE_DESC desc)
    {
        ImGui.TextUnformatted($"Type: {desc.Type}");
        ImGui.TextUnformatted($"Flags: {desc.Flags}");
        switch (desc.Type) {
            case D3D11_SHADER_TYPE.D3D11_VERTEX_SHADER:
                DrawDesc(in desc.VertexShaderTraceDesc);
                break;
            case D3D11_SHADER_TYPE.D3D11_HULL_SHADER:
                DrawDesc(in desc.HullShaderTraceDesc);
                break;
            case D3D11_SHADER_TYPE.D3D11_DOMAIN_SHADER:
                DrawDesc(in desc.DomainShaderTraceDesc);
                break;
            case D3D11_SHADER_TYPE.D3D11_GEOMETRY_SHADER:
                DrawDesc(in desc.GeometryShaderTraceDesc);
                break;
            case D3D11_SHADER_TYPE.D3D11_PIXEL_SHADER:
                DrawDesc(in desc.PixelShaderTraceDesc);
                break;
            case D3D11_SHADER_TYPE.D3D11_COMPUTE_SHADER:
                DrawDesc(in desc.ComputeShaderTraceDesc);
                break;
        }
    }

    private unsafe void DrawDesc(in D3D11_SHADER_TYPE_DESC desc)
    {
        ImGui.TextUnformatted($"Class: {desc.Class}");
        ImGui.TextUnformatted($"Type: {desc.Type}");
        ImGui.TextUnformatted($"Rows: {desc.Rows}");
        ImGui.TextUnformatted($"Columns: {desc.Columns}");
        ImGui.TextUnformatted($"Elements: {desc.Elements}");
        ImGui.TextUnformatted($"Members: {desc.Members}");
        ImGui.TextUnformatted($"Offset: {desc.Offset}");
        ImGui.TextUnformatted($"Name: {new CStringPointer((byte*)desc.Name)}");
    }

    private unsafe void DrawDesc(in D3D11_SHADER_VARIABLE_DESC desc)
    {
        ImGui.TextUnformatted($"Name: {new CStringPointer((byte*)desc.Name)}");
        ImGui.TextUnformatted($"Start Offset: {desc.StartOffset}");
        ImGui.TextUnformatted($"Size: {desc.Size}");
        ImGui.TextUnformatted($"Flags: {(D3D_SHADER_VARIABLE_FLAGS)desc.uFlags}");
        if (desc.Size > 0) {
            var defaultValue = new ReadOnlySpan<byte>(desc.DefaultValue, unchecked((int)desc.Size));
            ImGui.TextUnformatted($"Default Value: {defaultValue.ToHexString()}");
        }

        ImGui.TextUnformatted($"Textures: Start {desc.StartTexture}, Size {desc.TextureSize}");
        ImGui.TextUnformatted($"Samplers: Start {desc.StartSampler}, Size {desc.SamplerSize}");
    }

    private unsafe void DrawDesc(in D3D11_SIGNATURE_PARAMETER_DESC desc)
    {
        ImGui.TextUnformatted($"Semantic: {new CStringPointer((byte*)desc.SemanticName)}{desc.SemanticIndex}");
        ImGui.TextUnformatted($"Register: {desc.Register}");
        ImGui.TextUnformatted($"System Value Type: {desc.SystemValueType}");
        ImGui.TextUnformatted($"Component Type: {desc.ComponentType}");
        ImGui.TextUnformatted($"Mask: 0x{desc.Mask:X}");
        ImGui.TextUnformatted($"Read Write Mask: 0x{desc.ReadWriteMask:X}");
        ImGui.TextUnformatted($"Stream: {desc.Stream}");
        ImGui.TextUnformatted($"Min Precision: {desc.MinPrecision}");
    }

    private void DrawDesc(in D3D11_TEXTURE1D_DESC desc)
    {
        ImGui.TextUnformatted($"Width: {desc.Width}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Array Size: {desc.ArraySize}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"Usage: {desc.Usage}");
        ImGui.TextUnformatted($"Bind Flags: {(D3D11_BIND_FLAG)desc.BindFlags}");
        ImGui.TextUnformatted($"CPU Access Flags: {(D3D11_CPU_ACCESS_FLAG)desc.CPUAccessFlags}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_RESOURCE_MISC_FLAG)desc.MiscFlags}");
    }

    private void DrawDesc(in D3D11_TEXTURE2D_DESC desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Array Size: {desc.ArraySize}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        DrawDesc(in desc.SampleDesc);
        ImGui.TextUnformatted($"Usage: {desc.Usage}");
        ImGui.TextUnformatted($"Bind Flags: {(D3D11_BIND_FLAG)desc.BindFlags}");
        ImGui.TextUnformatted($"CPU Access Flags: {(D3D11_CPU_ACCESS_FLAG)desc.CPUAccessFlags}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_RESOURCE_MISC_FLAG)desc.MiscFlags}");
    }

    private void DrawDesc(in D3D11_TEXTURE2D_DESC1 desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Array Size: {desc.ArraySize}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        DrawDesc(in desc.SampleDesc);
        ImGui.TextUnformatted($"Usage: {desc.Usage}");
        ImGui.TextUnformatted($"Bind Flags: {(D3D11_BIND_FLAG)desc.BindFlags}");
        ImGui.TextUnformatted($"CPU Access Flags: {(D3D11_CPU_ACCESS_FLAG)desc.CPUAccessFlags}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_RESOURCE_MISC_FLAG)desc.MiscFlags}");
        ImGui.TextUnformatted($"Texture Layout: {desc.TextureLayout}");
    }

    private void DrawDesc(in D3D11_TEXTURE3D_DESC desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height} × {desc.Depth}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"Usage: {desc.Usage}");
        ImGui.TextUnformatted($"Bind Flags: {(D3D11_BIND_FLAG)desc.BindFlags}");
        ImGui.TextUnformatted($"CPU Access Flags: {(D3D11_CPU_ACCESS_FLAG)desc.CPUAccessFlags}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_RESOURCE_MISC_FLAG)desc.MiscFlags}");
    }

    private void DrawDesc(in D3D11_TEXTURE3D_DESC1 desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height} × {desc.Depth}");
        ImGui.TextUnformatted($"Mip Levels: {desc.MipLevels}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"Usage: {desc.Usage}");
        ImGui.TextUnformatted($"Bind Flags: {(D3D11_BIND_FLAG)desc.BindFlags}");
        ImGui.TextUnformatted($"CPU Access Flags: {(D3D11_CPU_ACCESS_FLAG)desc.CPUAccessFlags}");
        ImGui.TextUnformatted($"Misc Flags: {(D3D11_RESOURCE_MISC_FLAG)desc.MiscFlags}");
        ImGui.TextUnformatted($"Texture Layout: {desc.TextureLayout}");
    }

    private void DrawDesc(in D3D11_UNORDERED_ACCESS_VIEW_DESC desc)
    {
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"View Dimension: {desc.ViewDimension}");
        switch (desc.ViewDimension) {
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_BUFFER:
                DrawDesc(in desc.Buffer);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE1D:
                DrawDesc(in desc.Texture1D);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE1DARRAY:
                DrawDesc(in desc.Texture1DArray);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE2D:
                DrawDesc(in desc.Texture2D);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE2DARRAY:
                DrawDesc(in desc.Texture2DArray);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE3D:
                DrawDesc(in desc.Texture3D);
                break;
        }
    }

    private void DrawDesc(in D3D11_UNORDERED_ACCESS_VIEW_DESC1 desc)
    {
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"View Dimension: {desc.ViewDimension}");
        switch (desc.ViewDimension) {
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_BUFFER:
                DrawDesc(in desc.Buffer);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE1D:
                DrawDesc(in desc.Texture1D);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE1DARRAY:
                DrawDesc(in desc.Texture1DArray);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE2D:
                DrawDesc(in desc.Texture2D);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE2DARRAY:
                DrawDesc(in desc.Texture2DArray);
                break;
            case D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE3D:
                DrawDesc(in desc.Texture3D);
                break;
        }
    }

    private void DrawDesc(in D3D11_BUFFER_UAV desc)
    {
        ImGui.TextUnformatted($"First Element: {desc.FirstElement}");
        ImGui.TextUnformatted($"Num Elements: {desc.NumElements}");
        ImGui.TextUnformatted($"Flags: {(D3D11_BUFFER_UAV_FLAG)desc.Flags}");
    }

    private void DrawDesc(in D3D11_TEX1D_UAV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
    }

    private void DrawDesc(in D3D11_TEX1D_ARRAY_UAV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX2D_UAV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
    }

    private void DrawDesc(in D3D11_TEX2D_UAV1 desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Plane Slice: {desc.PlaneSlice}");
    }

    private void DrawDesc(in D3D11_TEX2D_ARRAY_UAV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
    }

    private void DrawDesc(in D3D11_TEX2D_ARRAY_UAV1 desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"Array Slices: Start {desc.FirstArraySlice}, Size {desc.ArraySize}");
        ImGui.TextUnformatted($"Plane Slice: {desc.PlaneSlice}");
    }

    private void DrawDesc(in D3D11_TEX3D_UAV desc)
    {
        ImGui.TextUnformatted($"Mip Slice: {desc.MipSlice}");
        ImGui.TextUnformatted($"W Slices: Start {desc.FirstWSlice}, Size {desc.WSize}");
    }

    private void DrawDesc(in D3D11_VERTEX_SHADER_TRACE_DESC desc)
    {
        ImGui.TextUnformatted($"Invocation: {desc.Invocation}");
    }

    /* Video stuff is considered out of scope for now, will consider doing if there's a need for it.

    void DrawDesc(in D3D11_VIDEO_DECODER_BUFFER_DESC desc);

    void DrawDesc(in D3D11_VIDEO_DECODER_BUFFER_DESC1 desc);

    void DrawDesc(in D3D11_VIDEO_DECODER_BUFFER_DESC2 desc);

    void DrawDesc(in D3D11_VIDEO_DECODER_DESC desc);

    void DrawDesc(in D3D11_VIDEO_DECODER_OUTPUT_VIEW_DESC desc);

    void DrawDesc(in D3D11_VIDEO_PROCESSOR_CONTENT_DESC desc);

    void DrawDesc(in D3D11_VIDEO_PROCESSOR_INPUT_VIEW_DESC desc);

    void DrawDesc(in D3D11_VIDEO_PROCESSOR_OUTPUT_VIEW_DESC desc);

    void DrawDesc(in D3D11_VIDEO_SAMPLE_DESC desc);
    */

    private void DrawDesc(in DXGI_ADAPTER_DESC desc)
    {
        ImGui.TextUnformatted($"Description: {((ReadOnlySpan<char>)desc.Description).BeforeNull()}");
        ImGui.TextUnformatted($"Vendor ID: {desc.VendorId} (0x{desc.VendorId:X})");
        ImGui.TextUnformatted($"Device ID: {desc.DeviceId} (0x{desc.DeviceId:X})");
        ImGui.TextUnformatted($"Sub Sys ID: {desc.SubSysId} (0x{desc.SubSysId:X})");
        ImGui.TextUnformatted($"Revision: {desc.Revision} (0x{desc.Revision:X})");
        ImGui.TextUnformatted($"Dedicated Video Memory: {desc.DedicatedVideoMemory} (0x{desc.DedicatedVideoMemory:X}) bytes");
        ImGui.TextUnformatted($"Dedicated System Memory: {desc.DedicatedSystemMemory} (0x{desc.DedicatedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Shared System Memory: {desc.SharedSystemMemory} (0x{desc.SharedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Adapter Luid: 0x{(unchecked((ulong)desc.AdapterLuid.HighPart) << 32) | desc.AdapterLuid.LowPart:X}");
    }

    private void DrawDesc(in DXGI_ADAPTER_DESC1 desc)
    {
        ImGui.TextUnformatted($"Description: {((ReadOnlySpan<char>)desc.Description).BeforeNull()}");
        ImGui.TextUnformatted($"Vendor ID: {desc.VendorId} (0x{desc.VendorId:X})");
        ImGui.TextUnformatted($"Device ID: {desc.DeviceId} (0x{desc.DeviceId:X})");
        ImGui.TextUnformatted($"Sub Sys ID: {desc.SubSysId} (0x{desc.SubSysId:X})");
        ImGui.TextUnformatted($"Revision: {desc.Revision} (0x{desc.Revision:X})");
        ImGui.TextUnformatted($"Dedicated Video Memory: {desc.DedicatedVideoMemory} (0x{desc.DedicatedVideoMemory:X}) bytes");
        ImGui.TextUnformatted($"Dedicated System Memory: {desc.DedicatedSystemMemory} (0x{desc.DedicatedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Shared System Memory: {desc.SharedSystemMemory} (0x{desc.SharedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Adapter Luid: 0x{(unchecked((ulong)desc.AdapterLuid.HighPart) << 32) | desc.AdapterLuid.LowPart:X}");
        ImGui.TextUnformatted($"Flags: {(DXGI_ADAPTER_FLAG)desc.Flags}");
    }

    private void DrawDesc(in DXGI_ADAPTER_DESC2 desc)
    {
        ImGui.TextUnformatted($"Description: {((ReadOnlySpan<char>)desc.Description).BeforeNull()}");
        ImGui.TextUnformatted($"Vendor ID: {desc.VendorId} (0x{desc.VendorId:X})");
        ImGui.TextUnformatted($"Device ID: {desc.DeviceId} (0x{desc.DeviceId:X})");
        ImGui.TextUnformatted($"Sub Sys ID: {desc.SubSysId} (0x{desc.SubSysId:X})");
        ImGui.TextUnformatted($"Revision: {desc.Revision} (0x{desc.Revision:X})");
        ImGui.TextUnformatted($"Dedicated Video Memory: {desc.DedicatedVideoMemory} (0x{desc.DedicatedVideoMemory:X}) bytes");
        ImGui.TextUnformatted($"Dedicated System Memory: {desc.DedicatedSystemMemory} (0x{desc.DedicatedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Shared System Memory: {desc.SharedSystemMemory} (0x{desc.SharedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Adapter Luid: 0x{(unchecked((ulong)desc.AdapterLuid.HighPart) << 32) | desc.AdapterLuid.LowPart:X}");
        ImGui.TextUnformatted($"Flags: {(DXGI_ADAPTER_FLAG)desc.Flags}");
        ImGui.TextUnformatted($"Graphics Preemption Granularity: {desc.GraphicsPreemptionGranularity}");
        ImGui.TextUnformatted($"Compute Preemption Granularity: {desc.ComputePreemptionGranularity}");
    }

    private void DrawDesc(in DXGI_ADAPTER_DESC3 desc)
    {
        ImGui.TextUnformatted($"Description: {((ReadOnlySpan<char>)desc.Description).BeforeNull()}");
        ImGui.TextUnformatted($"Vendor ID: {desc.VendorId} (0x{desc.VendorId:X})");
        ImGui.TextUnformatted($"Device ID: {desc.DeviceId} (0x{desc.DeviceId:X})");
        ImGui.TextUnformatted($"Sub Sys ID: {desc.SubSysId} (0x{desc.SubSysId:X})");
        ImGui.TextUnformatted($"Revision: {desc.Revision} (0x{desc.Revision:X})");
        ImGui.TextUnformatted($"Dedicated Video Memory: {desc.DedicatedVideoMemory} (0x{desc.DedicatedVideoMemory:X}) bytes");
        ImGui.TextUnformatted($"Dedicated System Memory: {desc.DedicatedSystemMemory} (0x{desc.DedicatedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Shared System Memory: {desc.SharedSystemMemory} (0x{desc.SharedSystemMemory:X}) bytes");
        ImGui.TextUnformatted($"Adapter Luid: 0x{(unchecked((ulong)desc.AdapterLuid.HighPart) << 32) | desc.AdapterLuid.LowPart:X}");
        ImGui.TextUnformatted($"Flags: {desc.Flags}");
        ImGui.TextUnformatted($"Graphics Preemption Granularity: {desc.GraphicsPreemptionGranularity}");
        ImGui.TextUnformatted($"Compute Preemption Granularity: {desc.ComputePreemptionGranularity}");
    }

    private void DrawDesc(in DXGI_DECODE_SWAP_CHAIN_DESC desc)
    {
        ImGui.TextUnformatted($"Flags: 0x{desc.Flags:X}");
    }

    private unsafe void DrawDesc(in DXGI_INFO_QUEUE_FILTER_DESC desc)
    {
        var categories = new ReadOnlySpan<DXGI_INFO_QUEUE_MESSAGE_CATEGORY>(desc.pCategoryList, (int)desc.NumCategories);
        var sb = new StringBuilder();
        sb.Append("Categories: ");
        AppendList(sb, categories);
        ImGui.TextUnformatted(sb.ToString());

        var severities = new ReadOnlySpan<DXGI_INFO_QUEUE_MESSAGE_SEVERITY>(desc.pSeverityList, (int)desc.NumSeverities);
        sb.Clear();
        sb.Append("Severities: ");
        AppendList(sb, severities);
        ImGui.TextUnformatted(sb.ToString());

        var ids = new ReadOnlySpan<uint>(desc.pIDList, (int)desc.NumIDs);
        sb.Clear();
        sb.Append("IDs: ");
        AppendList(sb, ids);
        ImGui.TextUnformatted(sb.ToString());
    }

    private void DrawDesc(in DXGI_MODE_DESC desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height}");
        ImGui.TextUnformatted($"Refresh Rate: {desc.RefreshRate.Numerator} / {desc.RefreshRate.Denominator}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"Scanline Ordering: {desc.ScanlineOrdering}");
        ImGui.TextUnformatted($"Scaling: {desc.Scaling}");
    }

    private void DrawDesc(in DXGI_MODE_DESC1 desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height}");
        ImGui.TextUnformatted($"Refresh Rate: {desc.RefreshRate.Numerator} / {desc.RefreshRate.Denominator}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"Scanline Ordering: {desc.ScanlineOrdering}");
        ImGui.TextUnformatted($"Scaling: {desc.Scaling}");
        ImGui.TextUnformatted($"Stereo: {(desc.Stereo ? "Yes" : "No")}");
    }

    private void DrawDesc(in DXGI_OUTDUPL_DESC desc)
    {
        DrawDesc(in desc.ModeDesc);
        ImGui.TextUnformatted($"Rotation: {desc.Rotation}");
        ImGui.TextUnformatted($"Desktop Image in System Memory: {(desc.DesktopImageInSystemMemory ? "Yes" : "No")}");
    }

    private void DrawDesc(in DXGI_OUTPUT_DESC desc)
    {
        ImGui.TextUnformatted($"Device Name: {((ReadOnlySpan<char>)desc.DeviceName).BeforeNull()}");
        ImGui.TextUnformatted(
            $"Desktop Coordinates: {desc.DesktopCoordinates.right - desc.DesktopCoordinates.left} × {desc.DesktopCoordinates.bottom - desc.DesktopCoordinates.top} at <{desc.DesktopCoordinates.left}, {desc.DesktopCoordinates.top}>"
        );
        ImGui.TextUnformatted($"Attached to Desktop: {(desc.AttachedToDesktop ? "Yes" : "No")}");
        ImGui.TextUnformatted($"Rotation: {desc.Rotation}");
        ImGui.TextUnformatted($"Monitor: 0x{(nuint)desc.Monitor:X}");
    }

    private void DrawDesc(in DXGI_OUTPUT_DESC1 desc)
    {
        ImGui.TextUnformatted($"Device Name: {((ReadOnlySpan<char>)desc.DeviceName).BeforeNull()}");
        ImGui.TextUnformatted(
            $"Desktop Coordinates: {desc.DesktopCoordinates.right - desc.DesktopCoordinates.left} × {desc.DesktopCoordinates.bottom - desc.DesktopCoordinates.top} at <{desc.DesktopCoordinates.left}, {desc.DesktopCoordinates.top}>"
        );
        ImGui.TextUnformatted($"Attached to Desktop: {(desc.AttachedToDesktop ? "Yes" : "No")}");
        ImGui.TextUnformatted($"Rotation: {desc.Rotation}");
        ImGui.TextUnformatted($"Monitor: 0x{(nuint)desc.Monitor:X}");
        ImGui.TextUnformatted($"Bits per Color: {desc.BitsPerColor}");
#pragma warning disable CA1416
        ImGui.TextUnformatted($"Color Space: {desc.ColorSpace}");
#pragma warning restore CA1416
        ImGui.TextUnformatted(
            $"Red Primary: {MemoryMarshal.Cast<DXGI_OUTPUT_DESC1._RedPrimary_e__FixedBuffer, Vector2>(new(in desc.RedPrimary))[0]}"
        );
        ImGui.TextUnformatted(
            $"Green Primary: {MemoryMarshal.Cast<DXGI_OUTPUT_DESC1._GreenPrimary_e__FixedBuffer, Vector2>(new(in desc.GreenPrimary))[0]}"
        );
        ImGui.TextUnformatted(
            $"Blue Primary: {MemoryMarshal.Cast<DXGI_OUTPUT_DESC1._BluePrimary_e__FixedBuffer, Vector2>(new(in desc.BluePrimary))[0]}"
        );
        ImGui.TextUnformatted(
            $"White Point: {MemoryMarshal.Cast<DXGI_OUTPUT_DESC1._WhitePoint_e__FixedBuffer, Vector2>(new(in desc.WhitePoint))[0]}"
        );
        ImGui.TextUnformatted($"Min Luminance: {desc.MinLuminance}");
        ImGui.TextUnformatted($"Max Luminance: {desc.MaxLuminance}");
        ImGui.TextUnformatted($"Max Full Frame Luminance: {desc.MaxFullFrameLuminance}");
    }

    private void DrawDesc(in DXGI_SAMPLE_DESC desc)
    {
        ImGui.TextUnformatted($"Sample Count: {desc.Count}");
        ImGui.TextUnformatted($"Sample Quality: {desc.Quality}");
    }

    private void DrawDesc(in DXGI_SURFACE_DESC desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        DrawDesc(in desc.SampleDesc);
    }

    private void DrawDesc(in DXGI_SWAP_CHAIN_DESC desc)
    {
        DrawDesc(in desc.BufferDesc);
        DrawDesc(in desc.SampleDesc);
        ImGui.TextUnformatted($"Buffer Usage: 0x{desc.BufferUsage:X}");
        ImGui.TextUnformatted($"Buffer Count: {desc.BufferCount}");
        ImGui.TextUnformatted($"Output Window: 0x{(nuint)desc.OutputWindow:X}");
        ImGui.TextUnformatted($"Windowed: {(desc.Windowed ? "Yes" : "No")}");
        ImGui.TextUnformatted($"Swap Effect: {desc.SwapEffect}");
        ImGui.TextUnformatted($"Flags: {(DXGI_SWAP_CHAIN_FLAG)desc.Flags}");
    }

    private void DrawDesc(in DXGI_SWAP_CHAIN_DESC1 desc)
    {
        ImGui.TextUnformatted($"Dimensions: {desc.Width} × {desc.Height}");
        ImGui.TextUnformatted($"Format: {desc.Format}");
        ImGui.TextUnformatted($"Stereo: {(desc.Stereo ? "Yes" : "No")}");
        DrawDesc(in desc.SampleDesc);
        ImGui.TextUnformatted($"Buffer Usage: 0x{desc.BufferUsage:X}");
        ImGui.TextUnformatted($"Buffer Count: {desc.BufferCount}");
        ImGui.TextUnformatted($"Scaling: {desc.Scaling}");
        ImGui.TextUnformatted($"Swap Effect: {desc.SwapEffect}");
        ImGui.TextUnformatted($"Alpha Mode: {desc.AlphaMode}");
        ImGui.TextUnformatted($"Flags: {(DXGI_SWAP_CHAIN_FLAG)desc.Flags}");
    }

    private void DrawDesc(in DXGI_SWAP_CHAIN_FULLSCREEN_DESC desc)
    {
        ImGui.TextUnformatted($"Refresh Rate: {desc.RefreshRate.Numerator} / {desc.RefreshRate.Denominator}");
        ImGui.TextUnformatted($"Scanline Ordering: {desc.ScanlineOrdering}");
        ImGui.TextUnformatted($"Scaling: {desc.Scaling}");
        ImGui.TextUnformatted($"Windowed: {(desc.Windowed ? "Yes" : "No")}");
    }
}
