using Microsoft.Extensions.Logging;

namespace Dynamis.Logging;

public interface IDalamudLoggingConfiguration
{
    bool IsEnabled(string name, LogLevel logLevel);
}
