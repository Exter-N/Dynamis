using System.Collections.Immutable;
using Dalamud.Configuration;
using Microsoft.Extensions.Logging;

namespace Dynamis.Configuration;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public static ImmutableArray<uint> GetDefaultHexViewerPalette()
        => [
            0xFFFFFFFF,
            0xFF808080,
            0xFFFF8080,
            0xFFFFFF80,
            0xFFFF80FF,
            0xFF80FF80,
            0xFF80FFFF,
            0xFF8080FF,
            0xFF0000FF,
            0xFFFF80C0,
            0xFFFFC080,
        ];

    public int Version { get; set; } = 0;

    public int MinimumLogLevel { get; set; } = (int)LogLevel.Information;

    public bool AutomaticDataYaml { get; set; } = true;

    public string DataYamlPath { get; set; } = string.Empty;

    public string DataYamlETag { get; set; } = string.Empty;

    public uint[] HexViewerPalette { get; set; } = [];

    public bool EnableIpfd { get; set; } = false;

    public bool EnableWineSymbolHandler { get; set; } = false;

    public uint[] GetHexViewerPalette()
    {
        var defaultPalette = GetDefaultHexViewerPalette();
        if (HexViewerPalette.Length >= defaultPalette.Length) {
            return HexViewerPalette;
        }

        var newPalette = defaultPalette.ToArray();
        Array.Copy(HexViewerPalette, newPalette, HexViewerPalette.Length);
        HexViewerPalette = newPalette;

        return HexViewerPalette;
    }
}
