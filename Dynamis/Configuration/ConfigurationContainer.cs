using Dalamud.Plugin;
using Dynamis.Logging;
using Dynamis.Messaging;
using Microsoft.Extensions.Logging;

namespace Dynamis.Configuration;

public sealed class ConfigurationContainer : IDalamudLoggingConfiguration
{
    private readonly MessageHub              _messageHub;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Lazy<Configuration>     _configuration;

    public string InternalName
        => _pluginInterface.InternalName;

    public Configuration Configuration
        => _configuration.Value;

    public ConfigurationContainer(MessageHub messageHub, IDalamudPluginInterface pi)
    {
        _messageHub = messageHub;
        _pluginInterface = pi;
        _configuration = new(() => _pluginInterface.GetPluginConfig() as Configuration ?? new());
    }

    public void Save(string? changedPropertyHint)
    {
        if (!_configuration.IsValueCreated) {
            return;
        }

        _pluginInterface.SavePluginConfig(_configuration.Value);
        _messageHub.Publish(new ConfigurationChangedMessage(changedPropertyHint));
    }

    public bool IsEnabled(string name, LogLevel logLevel)
        => (int)logLevel >= Configuration.MinimumLogLevel;
}
