using System.Reflection;
using Dalamud.Interface.Textures;
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
#if DEBUG
        ?? throw new Exception(
               $"ManifestResource \"{name}\" not found - Available resources: \"{string.Join(
                   "\", \"", Assembly.GetExecutingAssembly().GetManifestResourceNames()
                                     .Where(n => n.StartsWith("Dynamis.Resources."))
                                     .Select(n => n[18..]))}\""
           );
#else
        ?? throw new Exception($"ManifestResource \"{name}\" not found");
#endif

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

    public ISharedImmediateTexture LoadFileImage(string fileName)
        => textureProvider.GetFromFile(GetFileResourcePath(fileName));

    public ISharedImmediateTexture LoadManifestResourceImage(string name)
        => textureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), "Dynamis.Resources." + name);
}
