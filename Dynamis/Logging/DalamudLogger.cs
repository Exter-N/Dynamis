using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Dynamis.Logging;

// Mostly borrowed from https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/Interop/DalamudLogger.cs
internal sealed class DalamudLogger(string name, Lazy<IDalamudLoggingConfiguration> configuration, IPluginLog pluginLog)
    : ILogger
{
    private readonly string _shortName = ToShortName(name);

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

        var sb = new StringBuilder();
        sb.Append($"[{_shortName}]{{{(int)logLevel}}} {state}");
        if (exception is not null) {
            sb.AppendLine($": {exception.Message}");
            sb.AppendLine(exception.StackTrace);
            for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException) {
                sb.AppendLine($"Inner exception: {inner.Message}");
                sb.AppendLine(inner.StackTrace);
            }
        }

        switch (logLevel) {
            case LogLevel.Trace:
                pluginLog.Verbose(sb.ToString());
                break;
            case LogLevel.Debug:
                pluginLog.Debug(sb.ToString());
                break;
            case LogLevel.Information:
                pluginLog.Information(sb.ToString());
                break;
            case LogLevel.Warning:
                pluginLog.Warning(sb.ToString());
                break;
            case LogLevel.Error:
                pluginLog.Error(sb.ToString());
                break;
            case LogLevel.Critical:
                pluginLog.Fatal(sb.ToString());
                break;
        }
    }

    private static string ToShortName(string name)
    {
        var shortName = name.Split(".", StringSplitOptions.RemoveEmptyEntries).Last();
        return shortName.Length > 15
            ? shortName[..7] + "…" + shortName[^7..]
            : shortName.PadLeft(15, ' ');
    }
}
