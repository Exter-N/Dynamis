using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;

namespace Dynamis.PsHost;

public sealed class Host(PSHostUserInterface ui, HostContext hostContext)
    : PSHost, IHostSupportsInteractiveSession
{
    private readonly Stack<Runspace> _runspaceStack = [];

    public override string Name
        => "DynamisPSHost";

    public override Version Version
        => typeof(Host).Assembly.GetName().Version ?? new();

    public override PSHostUserInterface? UI
        => ui;

    public override PSObject PrivateData
        => new(hostContext);

    public override Guid        InstanceId       { get; } = Guid.NewGuid();
    public override CultureInfo CurrentCulture   { get; } = Thread.CurrentThread.CurrentCulture;
    public override CultureInfo CurrentUICulture { get; } = Thread.CurrentThread.CurrentUICulture;

    public bool IsRunspacePushed
        => _runspaceStack.Count > 0;

    public Runspace? Runspace
        => _runspaceStack.Count > 0 ? _runspaceStack.Peek() : null;

    public int? ExitCode { get; private set; }

    public override void SetShouldExit(int exitCode)
    {
        ExitCode = exitCode;
    }

    public override void EnterNestedPrompt()
    {
    }

    public override void ExitNestedPrompt()
    {
    }

    public override void NotifyBeginApplication()
    {
    }

    public override void NotifyEndApplication()
    {
    }

    public void PushRunspace(Runspace runspace)
    {
        _runspaceStack.Push(runspace);
    }

    public void PopRunspace()
    {
        _runspaceStack.TryPop(out _);
    }
}
