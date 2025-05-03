using Dalamud.Game;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dynamis.ClientStructs;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Interop.Ipfd;
using Dynamis.Interop.Win32;
using Dynamis.Logging;
using Dynamis.Messaging;
using Dynamis.PsHost;
using Dynamis.Resources;
using Dynamis.UI;
using Dynamis.UI.Components;
using Dynamis.UI.ObjectInspectors;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dynamis;

public sealed class Plugin : IDalamudPlugin
{
    private readonly CancellationTokenSource _pluginCts = new();
    private readonly Task                    _hostBuilderRunTask;

    public static IPluginLog? Log { get; private set; }

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ITextureProvider textureProvider,
        ICommandManager commandManager,
        IChatGui chatGui,
        IDtrBar dtrBar,
        IPluginLog pluginLog,
        ISigScanner sigScanner,
        IGameInteropProvider gameInteropProvider,
        ITitleScreenMenu titleScreenMenu,
        INotificationManager notificationManager,
        IFramework framework,
        IObjectTable objectTable,
        IDataManager dataManager)
    {
        Log = pluginLog;

        _hostBuilderRunTask =
            new HostBuilder()
               .UseContentRoot(pluginInterface.ConfigDirectory.FullName)
               .ConfigureLogging(
                    lb =>
                    {
                        lb.ClearProviders();
                        lb.Services.TryAddSingleton<ILoggerProvider, DalamudLoggingProvider>();
                        lb.SetMinimumLevel(LogLevel.Trace);
                    }
                )
               .ConfigureServices(
                    collection =>
                    {
                        collection.AddSingleton(pluginInterface);
                        collection.AddSingleton(textureProvider);
                        collection.AddSingleton(commandManager);
                        collection.AddSingleton(chatGui);
                        collection.AddSingleton(dtrBar);
                        collection.AddSingleton(pluginLog);
                        collection.AddSingleton(sigScanner);
                        collection.AddSingleton(gameInteropProvider);
                        collection.AddSingleton(titleScreenMenu);
                        collection.AddSingleton(notificationManager);
                        collection.AddSingleton(framework);
                        collection.AddSingleton(objectTable);
                        collection.AddSingleton(dataManager);

                        collection.AddSingleton(pluginInterface.UiBuilder);

                        collection.AddSingleton(new Dalamud.Localization("Dynamis.Localization.", "", useEmbedded: true));
                        collection.AddSingleton(new WindowSystem("Dynamis"));

                        collection.AddSingleton(
                            new HttpClient()
                            {
                                Timeout = TimeSpan.FromSeconds(5),
                            }
                        );

                        collection.AddSingleton<FileDialogManager>();
                        collection.AddSingleton<MessageHub>();
                        collection.AddSingleton<IpcProvider>();
                        collection.AddSingleton<ResourceProvider>();
                        collection.AddSingleton<ConfigurationContainer>();
                        collection.AddSingleton<DataYamlContainer>();
                        collection.AddSingleton<MemoryHeuristics>();
                        collection.AddSingleton<SymbolApi>();
                        collection.AddSingleton<ModuleAddressResolver>();
                        collection.AddSingleton<AddressIdentifier>();
                        collection.AddSingleton<ClassRegistry>();
                        collection.AddSingleton<ObjectInspector>();
                        collection.AddSingleton<TextureArraySlicer>();
                        collection.AddSingleton<ImGuiComponents>();
                        collection.AddSingleton<ContextMenu>();
                        collection.AddSingleton<BootHelper>();
                        collection.AddSingleton<DynamicBoxFactory>();

                        collection.AddSingleton<SnapshotViewerFactory>();

                        collection.AddSingleton<ObjectInspectorWindowFactory>();
                        collection.AddSingleton<BreakpointWindowFactory>();
                        collection.AddSingleton<HostedPsWindowFactory>();

                        collection.AddImplementationSingletons<IObjectInspector>(typeof(Plugin).Assembly);
                        collection.AddImplementationAliases<IObjectInspector>();
                        collection.AddSingleton<ObjectInspectorDispatcher>();

                        collection.AddImplementationSingletons<ISingletonWindow>(typeof(Plugin).Assembly);

                        collection.AddSingleton<Ipfd>();

                        collection.AddSingleton<CommandHandler>();
                        collection.AddSingleton<LaunchButton>();
                        collection.AddSingleton<WindowManager>();

                        collection.AddLazySingletonAlias<IDalamudLoggingConfiguration, ConfigurationContainer>();

                        collection.AddLazyImplementationAliases<Window>();
                        collection.AddLazyImplementationAliases<ISingletonWindow>();
                        collection.AddSingletonWindowOpeners();
                        collection.AddLazyImplementationAliases<IMessageObserver>();
                        collection.AddLazyImplementationAliases<ObjectInspectorDispatcher>();
                        collection.AddImplementationAliases<IHostedService>();
                    }
                )
               .Build()
               .RunAsync(_pluginCts.Token);
    }

    public void Dispose()
    {
        _pluginCts.Cancel();
        _pluginCts.Dispose();
        _hostBuilderRunTask.Wait();
    }
}
