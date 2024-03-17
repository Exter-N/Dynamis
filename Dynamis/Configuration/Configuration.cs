using Dalamud.Configuration;
using Microsoft.Extensions.Logging;

namespace Dynamis.Configuration;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public int MinimumLogLevel { get; set; } = (int)LogLevel.Information;

    public string DataYamlPath { get; set; } = string.Empty;
}
