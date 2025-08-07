using Dalamud.Bindings.ImGui;
using Dynamis.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Dynamis.Interop;

/// <summary>
/// Creates ImGui handles over slices of array textures, and manages their lifetime.
/// </summary>
public sealed unsafe class TextureArraySlicer : IDisposable
{
    private readonly ShortLivedCache<(nint XivTexture, byte SliceIndex), ShaderResourceView> _activeSlices = new();

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
            return new((nint)sliceSrv);
        }

        var srv = (ShaderResourceView)(nint)texture->D3D11ShaderResourceView;
        var description = srv.Description;
        switch (description.Dimension) {
            case ShaderResourceViewDimension.Texture1D:
            case ShaderResourceViewDimension.Texture2D:
            case ShaderResourceViewDimension.Texture2DMultisampled:
            case ShaderResourceViewDimension.Texture3D:
            case ShaderResourceViewDimension.TextureCube:
                // This function treats these as single-slice arrays.
                // As per the range check above, the only valid slice (i.e. 0) has been requested, therefore there is nothing to do.
                break;
            case ShaderResourceViewDimension.Texture1DArray:
                description.Texture1DArray.FirstArraySlice = sliceIndex;
                description.Texture2DArray.ArraySize = 1;
                break;
            case ShaderResourceViewDimension.Texture2DArray:
                description.Texture2DArray.FirstArraySlice = sliceIndex;
                description.Texture2DArray.ArraySize = 1;
                break;
            case ShaderResourceViewDimension.Texture2DMultisampledArray:
                description.Texture2DMSArray.FirstArraySlice = sliceIndex;
                description.Texture2DMSArray.ArraySize = 1;
                break;
            case ShaderResourceViewDimension.TextureCubeArray:
                description.TextureCubeArray.First2DArrayFace = sliceIndex * 6;
                description.TextureCubeArray.CubeCount = 1;
                break;
            default:
                throw new NotSupportedException(
                    $"{nameof(TextureArraySlicer)} does not support dimension {description.Dimension}"
                );
        }

        sliceSrv = new(srv.Device, srv.Resource, description);
        _activeSlices.Add(((nint)texture, sliceIndex), sliceSrv);
        return new((nint)sliceSrv);
    }

    public void Tick()
        => _activeSlices.Tick();

    public void Dispose()
        => _activeSlices.Dispose();
}

