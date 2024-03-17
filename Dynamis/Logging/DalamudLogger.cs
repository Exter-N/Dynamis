using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Dynamis.Logging;

// Mostly borrowed from https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/Interop/DalamudLogger.cs
internal sealed class DalamudLogger : ILogger
{
    private readonly Lazy<IDalamudLoggingConfiguration> _configuration;
    private readonly string                             _name;
    private readonly IPluginLog                         _pluginLog;

    public DalamudLogger(string name, Lazy<IDalamudLoggingConfiguration> configuration, IPluginLog pluginLog)
    {
        _name = name;
        _configuration = configuration;
        _pluginLog = pluginLog;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel)
        => _configuration.Value.IsEnabled(_name, logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) {
            return;
        }

        if (logLevel == LogLevel.Trace) {
            _pluginLog.Verbose($"[{_name}]{{{(int)logLevel}}} {state}");
        } else if (logLevel == LogLevel.Debug) {
            _pluginLog.Debug($"[{_name}]{{{(int)logLevel}}} {state}");
        } else if (logLevel == LogLevel.Information) {
            _pluginLog.Information($"[{_name}]{{{(int)logLevel}}} {state}");
        } else {
            StringBuilder sb = new();
            sb.AppendLine($"[{_name}]{{{(int)logLevel}}} {state}: {exception?.Message}");
            sb.AppendLine(exception?.StackTrace);
            var innerException = exception?.InnerException;
            while (innerException is not null) {
                sb.AppendLine($"InnerException {innerException}: {innerException.Message}");
                sb.AppendLine(innerException.StackTrace);
                innerException = innerException.InnerException;
            }

            if (logLevel == LogLevel.Warning) {
                _pluginLog.Warning(sb.ToString());
            } else if (logLevel == LogLevel.Error) {
                _pluginLog.Error(sb.ToString());
            } else {
                _pluginLog.Fatal(sb.ToString());
            }
        }
    }
}
