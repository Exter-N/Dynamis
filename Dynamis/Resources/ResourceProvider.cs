using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Plugin;

namespace Dynamis.Resources;

public sealed class ResourceProvider
{
    private readonly UiBuilder _uiBuilder;
    private readonly string    _directoryName;

    public ResourceProvider(DalamudPluginInterface pi)
    {
        _uiBuilder = pi.UiBuilder;
        _directoryName = pi.AssemblyLocation.DirectoryName!;
    }

    public string GetFileResourcePath(string fileName)
        => Path.Combine(_directoryName, fileName);

    public static Stream? GetManifestResourceStream(string name)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ResourceProvider), name);

    public static byte[] GetManifestResourceBytes(string name)
    {
        using var source = GetManifestResourceStream(name);
        if (source is null) {
            throw new Exception($"ManifestResource \"{name}\" not found");
        }

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);

        return buffer.ToArray();
    }

    public static async Task<byte[]> GetManifestResourceBytesAsync(string name)
    {
        using var source = GetManifestResourceStream(name);
        if (source is null) {
            throw new Exception($"ManifestResource \"{name}\" not found");
        }

        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);

        return buffer.ToArray();
    }

    public IDalamudTextureWrap LoadFileImage(string fileName)
        => _uiBuilder.LoadImage(GetFileResourcePath(fileName));

    public Task<IDalamudTextureWrap> LoadFileImageAsync(string fileName)
        => _uiBuilder.LoadImageAsync(GetFileResourcePath(fileName));

    public IDalamudTextureWrap LoadManifestResourceImage(string name)
        => _uiBuilder.LoadImage(GetManifestResourceBytes(name));

    public async Task<IDalamudTextureWrap> LoadManifestResourceImageAsync(string name)
        => await _uiBuilder.LoadImageAsync(await GetManifestResourceBytesAsync(name));
}
