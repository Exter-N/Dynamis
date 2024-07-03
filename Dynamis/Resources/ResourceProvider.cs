using System.Reflection;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Dynamis.Resources;

public sealed class ResourceProvider(IDalamudPluginInterface pi, ITextureProvider textureProvider)
{
    private readonly string _directoryName = pi.AssemblyLocation.DirectoryName!;

    public string GetFileResourcePath(string fileName)
        => Path.Combine(_directoryName, fileName);

    public static Stream GetManifestResourceStream(string name)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ResourceProvider), name)
            ?? throw new Exception($"ManifestResource \"{name}\" not found");

    public static byte[] GetManifestResourceBytes(string name)
    {
        using var source = GetManifestResourceStream(name);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);

        return buffer.ToArray();
    }

    public static async Task<byte[]> GetManifestResourceBytesAsync(string name)
    {
        await using var source = GetManifestResourceStream(name);

        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);

        return buffer.ToArray();
    }

    public Task<IDalamudTextureWrap> LoadFileImageAsync(string fileName)
        => textureProvider.CreateFromImageAsync(File.OpenRead(GetFileResourcePath(fileName)), debugName: fileName);

    public Task<IDalamudTextureWrap> LoadManifestResourceImageAsync(string name)
        => textureProvider.CreateFromImageAsync(GetManifestResourceStream(name), debugName: name);
}
