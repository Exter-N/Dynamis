using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.Loader;
using Dalamud.Plugin;
using Microsoft.PowerShell;

namespace Dynamis.PsHost;

public sealed class BootHelper(IDalamudPluginInterface pi)
{
    private readonly InitialSessionState _initialSessionState = BuildInitialSessionState(pi);

    public InitialSessionState InitialSessionState
        => _initialSessionState;

    public Reverter Setup()
        => Setup(pi);

    public static void Configure(Runspace runspace)
    {
        AddAssemblies(runspace);
        RunPreProfile(runspace);
    }

    private static void AddAssemblies(Runspace runspace)
    {
        var context = runspace.GetType()
                              .GetProperty("GetExecutionContext", BindingFlags.Instance | BindingFlags.NonPublic)
                             ?.GetValue(runspace);

        var cache = context?.GetType()
                            .GetProperty(
                                 "AssemblyCache",
                                 BindingFlags.Instance | BindingFlags.NonPublic
                             )
                           ?.GetValue(context) as Dictionary<string, Assembly>;
        if (cache is null) {
            return;
        }

        AddLoadContext(cache, AssemblyLoadContext.Default);
        AddDefiningLoadContext(cache, typeof(IDalamudPluginInterface));
        AddDefiningLoadContext(cache, typeof(BootHelper));
        return;

        static void AddDefiningLoadContext(Dictionary<string, Assembly> cache, Type t)
        {
            if (AssemblyLoadContext.GetLoadContext(t.Assembly) is
                {
                } loadContext) {
                AddLoadContext(cache, loadContext);
            }
        }

        static void AddLoadContext(Dictionary<string, Assembly> cache, AssemblyLoadContext loadContext)
        {
            foreach (var assembly in loadContext.Assemblies) {
                AddAssembly(cache, assembly);
            }
        }

        static void AddAssembly(Dictionary<string, Assembly> cache, Assembly assembly)
            => cache.TryAdd(assembly.GetName().ToString(), assembly);
    }

    private static void RunPreProfile(Runspace runspace)
    {
        using var ps = PowerShell.Create(runspace);
        ps.AddScript(@"
function Out-Log {
    param (
        [Parameter(ValueFromPipeline)] [psobject] $InputObject,
        [Microsoft.Extensions.Logging.LogLevel] $LogLevel = [Microsoft.Extensions.Logging.LogLevel]::Information
    )
    Out-String -Stream -InputObject $InputObject | Write-Log
}

function Out-Chat {
    param (
        [Parameter(ValueFromPipeline)] [psobject] $InputObject,
        [Microsoft.Extensions.Logging.LogLevel] $LogLevel = [Microsoft.Extensions.Logging.LogLevel]::Information
    )
    Out-String -Stream -InputObject $InputObject | Write-Chat
}
");
        ps.Invoke();
    }

    private static Reverter Setup(IDalamudPluginInterface pi)
    {
        if (!string.IsNullOrEmpty(AppContext.BaseDirectory)) {
            return default;
        }

        var previous = AppContext.GetData("APP_CONTEXT_BASE_DIRECTORY");
        AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", pi.AssemblyLocation.Directory!.ToString());
        return new(true, previous);
    }

    private static InitialSessionState BuildInitialSessionState(IDalamudPluginInterface pi)
    {
        InitialSessionState state;
        using (Setup(pi)) {
            state = InitialSessionState.CreateDefault();
        }

        var currentAssembly = typeof(BootHelper).Assembly;
        foreach (var cmdletType in currentAssembly.GetTypes()
                                                  .Where(
                                                       t => typeof(Cmdlet).IsAssignableFrom(t)
                                                         && t.GetCustomAttribute<CmdletAttribute>() is not null
                                                   )) {
            var cmdletAttribute = cmdletType.GetCustomAttribute<CmdletAttribute>()!;
            state.Commands.Add(
                new SessionStateCmdletEntry($"{cmdletAttribute.VerbName}-{cmdletAttribute.NounName}", cmdletType, null)
            );
        }

        state.ExecutionPolicy = ExecutionPolicy.Unrestricted;

        return state;
    }

    public readonly ref struct Reverter(bool revert, object? previous)
    {
        public void Dispose()
        {
            if (revert) {
                AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", previous);
            }
        }
    }
}
