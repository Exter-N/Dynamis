using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.Utility;
using YamlDotNet.Serialization;

namespace Dynamis.ClientStructs;

public sealed class DataYamlContainer : IMessageObserver<ConfigurationChangedMessage>
{
    private readonly ConfigurationContainer _configuration;
    private readonly IDeserializer          _yamlDeserializer;

    private Lazy<DataYaml?>?                             _data;
    private Lazy<Dictionary<string, DataYaml.Address>?>? _globalsInverse;
    private Lazy<Dictionary<string, DataYaml.Address>?>? _functionsInverse;
    private Lazy<Dictionary<nint, string>?>?             _classesByInstance;
    private Lazy<Dictionary<nint, string>?>?             _classesByInstancePtr;
    private Lazy<Dictionary<nint, string>?>?             _classesByVtbl;

    public DataYaml? Data
        => _data!.Value;

    public Dictionary<string, DataYaml.Address>? GlobalsInverse
        => _globalsInverse!.Value;

    public Dictionary<string, DataYaml.Address>? FunctionsInverse
        => _functionsInverse!.Value;

    public Dictionary<nint, string>? ClassesByInstance
        => _classesByInstance!.Value;

    public Dictionary<nint, string>? ClassesByInstancePointer
        => _classesByInstancePtr!.Value;

    public Dictionary<nint, string>? ClassesByVtbl
        => _classesByVtbl!.Value;

    public DataYamlContainer(ConfigurationContainer configuration, IDeserializer yamlDeserializer)
    {
        _configuration = configuration;
        _yamlDeserializer = yamlDeserializer;
        Refresh();
    }

    private void Refresh()
    {
        _data = new(Load);
        _globalsInverse = new(() => MapData(data => data.Globals?.Inverse()));
        _functionsInverse = new(() => MapData(data => data.Functions?.Inverse()));
        _classesByInstance = new(() => MapData(data => CalculateClassesByInstance(data,    false)));
        _classesByInstancePtr = new(() => MapData(data => CalculateClassesByInstance(data, true)));
        _classesByVtbl = new(() => MapData(CalculateClassesByVtbl));
    }

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (message.IsPropertyChanged(nameof(Configuration.Configuration.DataYamlPath))) {
            Refresh();
        }
    }

    private DataYaml? Load()
    {
        var path = _configuration.Configuration.DataYamlPath;
        if (path.Length == 0) {
            return null;
        }

        if (!File.Exists(path)) {
            return null;
        }

        using var reader = File.OpenText(path);
        return _yamlDeserializer.Deserialize<DataYaml>(reader);
    }

    private T? MapData<T>(Func<DataYaml, T?> function) where T : class
    {
        var data = Data;
        return data is null ? null : function(data);
    }

    private static Dictionary<nint, string> CalculateClassesByInstance(DataYaml data, bool pointer)
    {
        var classesByInstance = new Dictionary<nint, string>();
        if (data.Classes is not null) {
            foreach (var (name, @class) in data.Classes) {
                if (@class?.Instances is null) {
                    continue;
                }

                foreach (var instance in @class.Instances) {
                    if (instance is null) {
                        continue;
                    }

                    if (instance.Pointer == pointer) {
                        classesByInstance.Add(instance.Ea, name);
                    }
                }
            }
        }

        return classesByInstance;
    }

    private static Dictionary<nint, string> CalculateClassesByVtbl(DataYaml data)
    {
        var classesByInstance = new Dictionary<nint, string>();
        if (data.Classes is not null) {
            foreach (var (name, @class) in data.Classes) {
                if (@class?.Vtbls is null) {
                    continue;
                }

                foreach (var vtbl in @class.Vtbls) {
                    if (vtbl is null) {
                        continue;
                    }

                    classesByInstance.Add(vtbl.Ea, name);
                }
            }
        }

        return classesByInstance;
    }
}
