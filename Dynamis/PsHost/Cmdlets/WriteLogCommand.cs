using System.Management.Automation;
using Dynamis.UI.PsHost.Output;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dynamis.PsHost.Cmdlets;

[Cmdlet(VerbsCommunications.Write, "Log")]
public sealed class WriteLogCommand : Cmdlet
{
    private ILogger? _logger;

    [Parameter(Position = 0, ValueFromPipeline = true)]
    public object? Object { get; set; }

    [Parameter]
    public LogLevel Level { get; set; } = LogLevel.Information;

    protected override void BeginProcessing()
    {
        _logger = CommandRuntime.GetServiceProvider().GetRequiredService<ILogger<WriteLogCommand>>();
    }

    protected override void ProcessRecord()
    {
        if (_logger is null) {
            throw new RuntimeException("Logger not initialized");
        }

        var message = Object?.ToString();
        if (!string.IsNullOrEmpty(message)) {
            _logger.Log(Level, "{PSMessage}", AnsiHelper.StripAnsiCodes(message));
        }
    }
}
