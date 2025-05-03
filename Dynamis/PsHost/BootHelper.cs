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

    public SetupReverter Setup()
        => Setup(pi);

    public static UseReverter Use(RunspacePool runspacePool)
    {
        var tPool = runspacePool.GetType();
        var releaseRunspace = (Action<Runspace>)Delegate.CreateDelegate(
            typeof(Action<Runspace>), runspacePool,
            tPool.GetMethod("ReleaseRunspace", BindingFlags.Instance | BindingFlags.NonPublic)!
        );
        var result = (IAsyncResult)tPool.GetMethod("BeginGetRunspace", BindingFlags.Instance | BindingFlags.NonPublic)
            !.Invoke(runspacePool, [null, null,])!;
        result.AsyncWaitHandle.WaitOne();
        var runspace = (Runspace)tPool.GetMethod("EndGetRunspace", BindingFlags.Instance | BindingFlags.NonPublic)
            !.Invoke(runspacePool, [result,])!;
        var previous = Runspace.DefaultRunspace;
        Runspace.DefaultRunspace = runspace;
        return new(previous, () => releaseRunspace(runspace));
    }

    public static void Configure(RunspacePool runspacePool)
    {
        var tPool = runspacePool.GetType();
        var tEventArgs = tPool.Assembly.GetType("System.Management.Automation.Runspaces.RunspaceCreatedEventArgs")!;
        var tEventHandler = typeof(EventHandler<>).MakeGenericType(tEventArgs);
        var onRunspaceCreated = (EventHandler<EventArgs>)OnRunspaceCreated;
        tPool.GetEvent("RunspaceCreated", BindingFlags.Instance | BindingFlags.NonPublic)
            !.GetAddMethod(true)
            !.Invoke(runspacePool, [Delegate.CreateDelegate(tEventHandler, onRunspaceCreated.Method),]);
        var internalPool = tPool.GetField("_internalPool", BindingFlags.Instance | BindingFlags.NonPublic)
            !.GetValue(runspacePool)!;
        var runspaceList = (IEnumerable<Runspace>)internalPool.GetType()
                                                              .GetField(
                                                                   "runspaceList",
                                                                   BindingFlags.Instance | BindingFlags.NonPublic
                                                               )
                                                               !.GetValue(internalPool)!;
        foreach (var runspace in runspaceList) {
            ConfigureRunspace(runspace);
        }

        return;

        static void OnRunspaceCreated(object? sender, EventArgs e)
        {
            Plugin.Log!.Info("Created Runspace");
            var runspace =
                (Runspace)e.GetType().GetProperty("Runspace", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(
                    e
                )!;
            ConfigureRunspace(runspace);
        }

        static void ConfigureRunspace(Runspace runspace)
        {
            AddAssemblies(runspace);
        }
    }

    private static void AddAssemblies(Runspace runspace)
    {
        var context = runspace.GetType()
                              .GetProperty("GetExecutionContext", BindingFlags.Instance | BindingFlags.NonPublic)
                             !.GetValue(runspace)!;

        var cache = (Dictionary<string, Assembly>)context.GetType()
                                                         .GetProperty(
                                                              "AssemblyCache",
                                                              BindingFlags.Instance | BindingFlags.NonPublic
                                                          )
                                                          !.GetValue(context)!;

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

    private static SetupReverter Setup(IDalamudPluginInterface pi)
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
            var command = $"{cmdletAttribute.VerbName}-{cmdletAttribute.NounName}";
            state.Commands.Add(new SessionStateCmdletEntry(command, cmdletType, null));
            if (cmdletType.GetCustomAttribute<AliasAttribute>() is
                {
                } aliasAttribute) {
                foreach (var alias in aliasAttribute.AliasNames) {
                    state.Commands.Add(new SessionStateAliasEntry(alias, command));
                }
            }
        }

        state.ExecutionPolicy = ExecutionPolicy.Unrestricted;

        return state;
    }

    public readonly ref struct SetupReverter(bool revert, object? previous)
    {
        public void Dispose()
        {
            if (revert) {
                AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", previous);
            }
        }
    }

    public readonly ref struct UseReverter(Runspace? previous, Action onDispose)
    {
        public void Dispose()
        {
            Runspace.DefaultRunspace = previous;
            onDispose();
        }
    }
}
