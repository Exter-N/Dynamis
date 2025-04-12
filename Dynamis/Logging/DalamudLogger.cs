using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Dynamis.Logging;

// Mostly borrowed from https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/Interop/DalamudLogger.cs
internal sealed class DalamudLogger(string name, Lazy<IDalamudLoggingConfiguration> configuration, IPluginLog pluginLog)
    : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel)
        => configuration.Value.IsEnabled(name, logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) {
            return;
        }

        if (logLevel == LogLevel.Trace) {
            pluginLog.Verbose($"[{name}]{{{(int)logLevel}}} {state}");
        } else if (logLevel == LogLevel.Debug) {
            pluginLog.Debug($"[{name}]{{{(int)logLevel}}} {state}");
        } else if (logLevel == LogLevel.Information) {
            pluginLog.Information($"[{name}]{{{(int)logLevel}}} {state}");
        } else {
            StringBuilder sb = new();
            sb.AppendLine($"[{name}]{{{(int)logLevel}}} {state}: {exception?.Message}");
            sb.AppendLine(exception?.StackTrace);
            var innerException = exception?.InnerException;
            while (innerException is not null) {
                sb.AppendLine($"InnerException {innerException}: {innerException.Message}");
                sb.AppendLine(innerException.StackTrace);
                innerException = innerException.InnerException;
            }

            if (logLevel == LogLevel.Warning) {
                pluginLog.Warning(sb.ToString());
            } else if (logLevel == LogLevel.Error) {
                pluginLog.Error(sb.ToString());
            } else {
                pluginLog.Fatal(sb.ToString());
            }
        }
    }
}
