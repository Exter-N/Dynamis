using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dynamis.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dynamis.Interop;

/// <summary>
/// Creates ImGui handles over slices of array textures, and manages their lifetime.
/// </summary>
public sealed unsafe class TextureArraySlicer : IDisposable
{
    private readonly ShortLivedCache<(nint XivTexture, byte SliceIndex), SafeComHandle<ID3D11ShaderResourceView>> _activeSlices = new();

    /// <remarks> Caching this across frames will cause a crash to desktop. </remarks>
    public ImTextureID GetImGuiHandle(Texture* texture, byte sliceIndex)
    {
        if (texture is null) {
            throw new ArgumentNullException(nameof(texture));
        }

        if (sliceIndex >= texture->ArraySize) {
            throw new ArgumentOutOfRangeException(
                nameof(sliceIndex),
                $"Slice index ({sliceIndex}) is greater than or equal to the texture array size ({texture->ArraySize})"
            );
        }

        if (_activeSlices.TryGetValue(((nint)texture, sliceIndex), out var sliceSrv)) {
            return new((nint)sliceSrv.Object);
        }

        using var srv = new SafeComHandle<ID3D11ShaderResourceView>(
            (ID3D11ShaderResourceView*)texture->D3D11ShaderResourceView, true
        );
        D3D11_SHADER_RESOURCE_VIEW_DESC description;
        srv.Object->GetDesc(&description);
        switch (description.ViewDimension) {
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE1D:
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D:
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2DMS:
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE3D:
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURECUBE:
                // This function treats these as single-slice arrays.
                // As per the range check above, the only valid slice (i.e. 0) has been requested, therefore there is nothing to do.
                break;
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE1DARRAY:
                description.Texture1DArray.FirstArraySlice = sliceIndex;
                description.Texture1DArray.ArraySize = 1;
                break;
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2DARRAY:
                description.Texture2DArray.FirstArraySlice = sliceIndex;
                description.Texture2DArray.ArraySize = 1;
                break;
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2DMSARRAY:
                description.Texture2DMSArray.FirstArraySlice = sliceIndex;
                description.Texture2DMSArray.ArraySize = 1;
                break;
            case D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURECUBEARRAY:
                description.TextureCubeArray.First2DArrayFace = sliceIndex * 6u;
                description.TextureCubeArray.NumCubes = 1;
                break;
            default:
                throw new NotSupportedException(
                    $"{nameof(TextureArraySlicer)} does not support dimension {description.ViewDimension}"
                );
        }

        ID3D11ShaderResourceView* slice = null;
        ID3D11Device* device = null;
        ID3D11Resource* resource = null;
        try {
            srv.Object->GetDevice(&device);
            srv.Object->GetResource(&resource);
            Marshal.ThrowExceptionForHR(device->CreateShaderResourceView(resource, &description, &slice));
        } finally {
            if (resource is not null) {
                resource->Release();
            }

            if (device is not null) {
                device->Release();
            }
        }

        sliceSrv = new(slice, false);
        _activeSlices.Add(((nint)texture, sliceIndex), sliceSrv);
        return new((nint)slice);
    }

    public void Tick()
        => _activeSlices.Tick();

    public void Dispose()
        => _activeSlices.Dispose();
}
