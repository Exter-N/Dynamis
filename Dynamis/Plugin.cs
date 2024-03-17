using Dalamud.Game;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dynamis.ClientStructs;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Logging;
using Dynamis.Messaging;
using Dynamis.Resources;
using Dynamis.UI;
using Dynamis.UI.Windows;
using Dynamis.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dynamis;

public sealed class Plugin : IDalamudPlugin
{
    private readonly CancellationTokenSource _pluginCts = new();
    private readonly Task                    _hostBuilderRunTask;

    public Plugin(
        DalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IPluginLog pluginLog,
        ISigScanner sigScanner,
        IGameInteropProvider gameInteropProvider,
        ITitleScreenMenu titleScreenMenu)
    {
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
                        collection.AddSingleton(commandManager);
                        collection.AddSingleton(pluginLog);
                        collection.AddSingleton(sigScanner);
                        collection.AddSingleton(gameInteropProvider);
                        collection.AddSingleton(titleScreenMenu);

                        collection.AddSingleton(pluginInterface.UiBuilder);

                        collection.AddSingleton(
                            new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance)
                               .Build()
                        );

                        collection.AddSingleton(new Dalamud.Localization("Dynamis.Localization.", "", useEmbedded: true));
                        collection.AddSingleton(new WindowSystem("Dynamis"));

                        collection.AddSingleton<FileDialogManager>();
                        collection.AddSingleton<MessageHub>();
                        collection.AddSingleton<ResourceProvider>();
                        collection.AddSingleton<ConfigurationContainer>();
                        collection.AddSingleton<DataYamlContainer>();
                        collection.AddSingleton<MemoryHeuristics>();
                        collection.AddSingleton<ObjectInspector>();
                        collection.AddSingleton<ImGuiComponents>();

                        collection.AddSingleton<HomeWindow>();
                        collection.AddSingleton<SettingsWindow>();
                        collection.AddSingleton<SigScannerWindow>();
                        collection.AddSingleton<ObjectInspectorWindowFactory>();

                        collection.AddSingleton<CommandHandler>();
                        collection.AddSingleton<LaunchButton>();
                        collection.AddSingleton<WindowManager>();

                        collection.AddLazySingletonAlias<IDalamudLoggingConfiguration, ConfigurationContainer>();

                        collection.AddLazyImplementationAliases<Window>();
                        collection.AddLazyImplementationAliases<IMessageObserver>();
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
