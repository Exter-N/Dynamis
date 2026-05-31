using System.Runtime.InteropServices;
using System.Text;
using BCnEncoder.Shared.ImageFiles;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Lumina.Data.Files;

namespace Dynamis.Interop;

public sealed class TextureDumper(ITextureReadbackProvider readbackProvider)
{
    public void SaveToFile(SafeTextureHandle texture, string path)
    {
        SaveToFileAsync(texture, path)
           .ContinueWith(task =>
                {
                    if (!task.IsCompletedSuccessfully) {
                        Plugin.Log!.Error(task.Exception, "Error while saving texture to file {Path}", path);
                    }
                }
            );
    }

    public async Task SaveToFileAsync(SafeTextureHandle texture, string path,
        CancellationToken cancellationToken = default)
    {
        var (mipLevels, images) = await readbackProvider.GetAllRawImagesAsync(texture, true, cancellationToken);
        await using var outputStream = File.Create(path);
        if (Path.GetExtension(path).ToLowerInvariant() is ".tex" or ".atex") {
            await SaveAsTexAsync(outputStream, mipLevels, images);
        } else {
            await SaveAsDdsAsync(outputStream, mipLevels, images);
        }
    }

    private static async Task SaveAsTexAsync(Stream outputStream, int mipLevels,
        (RawImageSpecification Specification, byte[] RawData)[] images)
    {
        var texHeader = new TexFile.TexHeader();
        FillTexHeader(ref texHeader, mipLevels, images);
        outputStream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<TexFile.TexHeader>(ref texHeader)));

        for (var i = 0; i < mipLevels && i < 13; ++i) {
            for (var j = 0; j < images.Length; j += mipLevels) {
                await outputStream.WriteAsync(images[i].RawData, 0, images[i].RawData.Length);
            }
        }
    }

    private static unsafe void FillTexHeader(ref TexFile.TexHeader header, int mipLevels,
        (RawImageSpecification Specification, byte[] RawData)[] images)
    {
        var spec0 = images[0].Specification;

        header.Type = images.Length > mipLevels
            ? TexFile.Attribute.TextureType2DArray
            : TexFile.Attribute.TextureType2D;
        header.Format = ToTexFormat((DxgiFormat)spec0.DxgiFormat);
        header.Width = (ushort)spec0.Width;
        header.Height = (ushort)spec0.Height;
        header.Depth = 1;
        header.MipCount = mipLevels;
        header.MipUnknownFlag = false;
        header.ArraySize = images.Length > mipLevels ? (byte)(images.Length / mipLevels) : (byte)0;
        header.LodOffset[0] = 0;
        header.LodOffset[1] = (uint)Math.Min(1, mipLevels - 1);
        header.LodOffset[2] = (uint)Math.Min(2, mipLevels - 1);
        for (var i = 0; i < 13; ++i) {
            header.OffsetToSurface[i] = 0;
        }

        var offset = sizeof(TexFile.TexHeader);
        for (var i = 0; i < mipLevels && i < 13; ++i) {
            header.OffsetToSurface[i] = (uint)offset;
            for (var j = 0; j < images.Length; j += mipLevels) {
                offset += images[i].RawData.Length;
            }
        }
    }

    private static async Task SaveAsDdsAsync(Stream outputStream, int mipLevels,
        (RawImageSpecification Specification, byte[] RawData)[] images)
    {
        var spec0 = images[0].Specification;

        using (var writer = new BinaryWriter(outputStream, Encoding.UTF8, true)) {
            writer.Write(0x20534444u);

            var ddsHeader = new DdsHeader();
            ddsHeader.dwSize = 124;
            ddsHeader.dwFlags = HeaderFlags.Required;
            ddsHeader.dwWidth = (uint)spec0.Width;
            ddsHeader.dwHeight = (uint)spec0.Height;
            ddsHeader.dwDepth = 1;
            ddsHeader.dwMipMapCount = (uint)mipLevels;
            ddsHeader.dwCaps = HeaderCaps.DdscapsTexture | HeaderCaps.DdscapsComplex | HeaderCaps.DdscapsMipmap;
            ddsHeader.ddsPixelFormat = new DdsPixelFormat
            {
                dwSize = 32,
                dwFlags = PixelFormatFlags.DdpfFourcc,
                dwFourCc = images.Length > mipLevels
                    ? DdsPixelFormat.Dx10
                    : ToDdsPixelFormat((DxgiFormat)spec0.DxgiFormat),
            };
            writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<DdsHeader>(ref ddsHeader)));

            if (ddsHeader.ddsPixelFormat.dwFourCc == DdsPixelFormat.Dx10) {
                var dx10Header = new DdsHeaderDx10();
                dx10Header.arraySize = (uint)Math.Max(images.Length / mipLevels, 1);
                dx10Header.dxgiFormat = (DxgiFormat)spec0.DxgiFormat;
                dx10Header.resourceDimension = D3D10ResourceDimension.D3D10ResourceDimensionTexture2D;
                writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<DdsHeaderDx10>(ref dx10Header)));
            }
        }

        foreach (var (_, surface) in images) {
            await outputStream.WriteAsync(surface, 0, surface.Length);
        }
    }

    public static TexFile.TextureFormat ToTexFormat(DxgiFormat format)
        => format switch
        {
            DxgiFormat.DxgiFormatR8Unorm              => TexFile.TextureFormat.L8,
            DxgiFormat.DxgiFormatA8Unorm              => TexFile.TextureFormat.A8,
            DxgiFormat.DxgiFormatB4G4R4A4Unorm        => TexFile.TextureFormat.B4G4R4A4,
            DxgiFormat.DxgiFormatB5G5R5A1Unorm        => TexFile.TextureFormat.B5G5R5A1,
            DxgiFormat.DxgiFormatB8G8R8A8Unorm        => TexFile.TextureFormat.B8G8R8A8,
            DxgiFormat.DxgiFormatB8G8R8X8Unorm        => TexFile.TextureFormat.B8G8R8X8,
            DxgiFormat.DxgiFormatR32Float             => TexFile.TextureFormat.R32F,
            DxgiFormat.DxgiFormatR16G16Float          => TexFile.TextureFormat.R16G16F,
            DxgiFormat.DxgiFormatR32G32Float          => TexFile.TextureFormat.R32G32F,
            DxgiFormat.DxgiFormatR16G16B16A16Float    => TexFile.TextureFormat.R16G16B16A16F,
            DxgiFormat.DxgiFormatR32G32B32A32Float    => TexFile.TextureFormat.R32G32B32A32F,
            DxgiFormat.DxgiFormatBc1Unorm             => TexFile.TextureFormat.BC1,
            DxgiFormat.DxgiFormatBc2Unorm             => TexFile.TextureFormat.BC2,
            DxgiFormat.DxgiFormatBc3Unorm             => TexFile.TextureFormat.BC3,
            DxgiFormat.DxgiFormatBc4Unorm             => TexFile.TextureFormat.BC4,
            DxgiFormat.DxgiFormatBc5Unorm             => TexFile.TextureFormat.BC5,
            DxgiFormat.DxgiFormatBc6HSf16             => TexFile.TextureFormat.BC6H,
            DxgiFormat.DxgiFormatBc7Unorm             => TexFile.TextureFormat.BC7,
            DxgiFormat.DxgiFormatR16G16B16A16Typeless => TexFile.TextureFormat.D16,
            DxgiFormat.DxgiFormatR24G8Typeless        => TexFile.TextureFormat.D24S8,
            DxgiFormat.DxgiFormatR16Typeless          => TexFile.TextureFormat.Shadow16,
            _                                         => TexFile.TextureFormat.Unknown,
        };

    private static uint ToDdsPixelFormat(DxgiFormat dxgiFormat)
        => dxgiFormat switch
        {
            DxgiFormat.DxgiFormatBc1Unorm => DdsPixelFormat.Dxt1,
            DxgiFormat.DxgiFormatBc2Unorm => DdsPixelFormat.Dxt3,
            DxgiFormat.DxgiFormatBc3Unorm => DdsPixelFormat.Dxt5,
            DxgiFormat.DxgiFormatBc4Unorm => DdsPixelFormat.Bc4U,
            DxgiFormat.DxgiFormatBc5Unorm => DdsPixelFormat.Ati2,
            _                             => DdsPixelFormat.Dx10,
        };
}
