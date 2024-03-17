using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Dynamis.Logging;

// Mostly borrowed from https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/Interop/DalamudLoggingProvider.cs
[ProviderAlias("Dalamud")]
public sealed class DalamudLoggingProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Lazy<IDalamudLoggingConfiguration> _configuration;
    private readonly IPluginLog                         _pluginLog;

    public DalamudLoggingProvider(Lazy<IDalamudLoggingConfiguration> configuration, IPluginLog pluginLog)
    {
        _configuration = configuration;
        _pluginLog = pluginLog;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var catName = categoryName.Split(".", StringSplitOptions.RemoveEmptyEntries).Last();
        if (catName.Length > 15) {
            catName = catName[..7] + "…" + catName[^7..];
        } else {
            catName = catName.PadLeft(15, ' ');
        }

        return _loggers.GetOrAdd(catName, name => new(name, _configuration, _pluginLog));
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
