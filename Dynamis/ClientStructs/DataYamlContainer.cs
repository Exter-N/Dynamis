using System.Diagnostics;
using System.Net;
using Dalamud.Plugin;
using Dynamis.Configuration;
using Dynamis.Interop;
using Dynamis.Interop.Win32;
using Dynamis.Messaging;
using Dynamis.Utility;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Dynamis.ClientStructs;

public sealed class DataYamlContainer : IMessageObserver<ConfigurationChangedMessage>, IMessageObserver<DataYamlPreloadMessage>
{
    private const string DownloadSourceUri =
        "https://raw.githubusercontent.com/aers/FFXIVClientStructs/refs/heads/main/ida/data.yml";

    private readonly nint                       _exeOffset;
    private readonly ConfigurationContainer     _configuration;
    private readonly ILogger<DataYamlContainer> _logger;
    private readonly IDalamudPluginInterface    _pi;
    private readonly HttpClient                 _httpClient;

    private Lazy<DataYaml?>?                             _data;
    private Lazy<Dictionary<string, Address>?>?          _globalsInverse;
    private Lazy<Dictionary<string, Address>?>?          _functionsInverse;
    private Lazy<Dictionary<nint, InstanceName>?>?       _classesByInstance;
    private Lazy<Dictionary<nint, InstanceName>?>?       _classesByInstancePtr;
    private Lazy<Dictionary<nint, string>?>?             _classesByVtbl;
    private Lazy<Dictionary<nint, MemberFunctionName>?>? _memberFunctions;
    private Lazy<Dictionary<nint, MemberFunctionName>?>? _virtualFunctions;

    private string AutoPath
        => Path.Combine(_pi.GetPluginConfigDirectory(), "data.yml");

    public DataYaml? Data
        => _data!.Value;

    public Dictionary<string, Address>? GlobalsInverse
        => _globalsInverse!.Value;

    public Dictionary<string, Address>? FunctionsInverse
        => _functionsInverse!.Value;

    public Dictionary<nint, InstanceName>? ClassesByInstance
        => _classesByInstance!.Value;

    public Dictionary<nint, InstanceName>? ClassesByInstancePointer
        => _classesByInstancePtr!.Value;

    public Dictionary<nint, string>? ClassesByVtbl
        => _classesByVtbl!.Value;

    public Dictionary<nint, MemberFunctionName>? MemberFunctions
        => _memberFunctions!.Value;

    public Dictionary<nint, MemberFunctionName>? VirtualFunctions
        => _virtualFunctions!.Value;

    public DataYamlContainer(ConfigurationContainer configuration, ILogger<DataYamlContainer> logger, IDalamudPluginInterface pi, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _pi = pi;
        _httpClient = httpClient;
        var exeBaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
        var exeOriginalBaseAddress = ModuleAddressResolver.GetOriginalBaseAddress(exeBaseAddress);
        _exeOffset = exeBaseAddress - (exeOriginalBaseAddress != 0 ? exeOriginalBaseAddress : unchecked((nint)0x140000000));
        Refresh();
    }

    public nint GetLiveAddress(Address address)
        => address.Value + _exeOffset;

    public Address GetOriginalAddress(nint address)
        => new(address - _exeOffset);

    public unsafe AddressIdentification IdentifyAddress(nint address, AddressType typeHint = AddressType.All)
    {
        if (Data is null) {
            return AddressIdentification.Default;
        }

        if (typeHint.HasFlag(AddressType.Instance)
         && (ClassesByInstance?.TryGetValue(address, out var name) ?? false)) {
            return new(AddressType.Instance, name.ClassName, name.Name);
        }

        if (typeHint.HasFlag(AddressType.VirtualTable)
         && (ClassesByVtbl?.TryGetValue(address, out var clsName) ?? false)) {
            return new(AddressType.VirtualTable, clsName, null);
        }

        if (typeHint.HasFlag(AddressType.Function)
         && (Data.Functions?.TryGetValue(GetOriginalAddress(address), out var fnName) ?? false)) {
            return new(AddressType.Function, string.Empty, fnName);
        }

        if (typeHint.HasFlag(AddressType.Function)
         && (MemberFunctions?.TryGetValue(address, out var mfName) ?? false)) {
            return new(AddressType.Function, mfName.ClassName, mfName.FunctionName);
        }

        if (typeHint.HasFlag(AddressType.Function)
         && (Data.Globals?.TryGetValue(GetOriginalAddress(address), out var gName) ?? false)) {
            return new(AddressType.Global, string.Empty, gName);
        }

        if (address != 0) {
            if (typeHint.HasFlag(AddressType.Instance) && ClassesByInstancePointer is not null) {
                foreach (var (pointer, name2) in ClassesByInstancePointer) {
                    if (VirtualMemory.GetProtection(pointer).CanRead() && *(nint*)pointer == address) {
                        return new(AddressType.Instance, name2.ClassName, name2.Name);
                    }
                }
            }

            if (typeHint.HasFlag(AddressType.Function) && VirtualFunctions is not null) {
                foreach (var (pointer, vfName) in VirtualFunctions) {
                    if (VirtualMemory.GetProtection(pointer).CanRead() && *(nint*)pointer == address) {
                        return new(AddressType.Function, vfName.ClassName, vfName.FunctionName);
                    }
                }
            }
        }

        return AddressIdentification.Default;
    }

    public IEnumerable<KeyValuePair<nint, AddressIdentification>> GetWellKnownAddresses(AddressType types)
    {
        unsafe nint Resolve(DataYaml.Instance instance)
        {
            var ea = GetLiveAddress(instance.Ea);
            if (!instance.Pointer) {
                return ea;
            }

            if (!VirtualMemory.GetProtection(ea).CanRead()) {
                _logger.LogError("Cannot dereference ea pointer 0x{Ea:X}, returning nullptr", ea);
                return 0;
            }

            return *(nint*)ea;
        }

        unsafe nint ReadVfuncAddress(nint vtbl, uint index)
        {
            var ptr = ((nint*)vtbl) + index;
            if (!VirtualMemory.GetProtection((nint)ptr).CanRead()) {
                _logger.LogError("Cannot dereference vfunc pointer 0x{Ea:X}, returning nullptr", (nint)ptr);
                return 0;
            }

            return *ptr;
        }

        if (Data is null) {
            yield break;
        }

        if (types.HasFlag(AddressType.Global) && Data.Globals is not null) {
            foreach (var (ea, name) in Data.Globals) {
                yield return new(GetLiveAddress(ea), new(AddressType.Global, string.Empty, name));
            }
        }

        if (types.HasFlag(AddressType.Function) && Data.Functions is not null) {
            foreach (var (ea, name) in Data.Functions) {
                yield return new(GetLiveAddress(ea), new(AddressType.Function, string.Empty, name));
            }
        }

        if (Data.Classes is null) {
            yield break;
        }

        foreach (var (className, @class) in Data.Classes) {
            if (types.HasFlag(AddressType.Instance) && @class.Instances is not null) {
                foreach (var instance in @class.Instances) {
                    var ea = Resolve(instance);
                    if (ea != 0) {
                        yield return new(ea, new(AddressType.Instance, className, instance.Name));
                    }
                }
            }

            if (types.HasFlag(AddressType.VirtualTable) && @class.Vtbls is not null) {
                foreach (var vtbl in @class.Vtbls) {
                    yield return new(GetLiveAddress(vtbl.Ea), new(AddressType.VirtualTable, className, null));
                }
            }

            if (types.HasFlag(AddressType.Function)) {
                if (@class.Funcs is not null) {
                    foreach (var (ea, name) in @class.Funcs) {
                        yield return new(GetLiveAddress(ea), new(AddressType.Function, className, name));
                    }
                }

                var vtbl0 = @class.Vtbls?[0];
                if (vtbl0 is not null && @class.Vfuncs is not null) {
                    foreach (var (index, name) in @class.Vfuncs) {
                        var ea = ReadVfuncAddress(GetLiveAddress(vtbl0.Ea), index);
                        if (ea != 0) {
                            yield return new(ea, new(AddressType.Function, className, name));
                        }
                    }
                }
            }
        }
    }

    private void Refresh()
    {
        _data = new(Load);
        _globalsInverse = new(() => MapData(data => data.Globals?.Inverse()));
        _functionsInverse = new(() => MapData(data => data.Functions?.Inverse()));
        _classesByInstance = new(() => MapData(data => CalculateClassesByInstance(data,    false)));
        _classesByInstancePtr = new(() => MapData(data => CalculateClassesByInstance(data, true)));
        _classesByVtbl = new(() => MapData(CalculateClassesByVtbl));
        _memberFunctions = new(() => MapData(CalculateMemberFunctions));
        _virtualFunctions = new(() => MapData(CalculateVirtualFunctions));
    }

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (message.IsPropertyChanged(nameof(Configuration.Configuration.AutomaticDataYaml))
         || message.IsPropertyChanged(nameof(Configuration.Configuration.DataYamlPath))) {
            Refresh();
        }
    }

    public void HandleMessage(DataYamlPreloadMessage _)
        => Preload();

    public void Preload()
        => ThreadPool.QueueUserWorkItem(state => _ = Data);

    private async Task Download(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, DownloadSourceUri);
        var eTag = _configuration.Configuration.DataYamlETag;
        if (File.Exists(AutoPath) && !string.IsNullOrEmpty(eTag)) {
            request.Headers.Add("If-None-Match", eTag);
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotModified) {
            _logger.LogInformation("No change to data.yml, keeping cached version");
            return;
        }

        response.EnsureSuccessStatusCode();

        eTag = response.Headers.TryGetValues("ETag", out var eTags)
            ? eTags.FirstOrDefault(string.Empty)
            : string.Empty;
        var contents = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        await File.WriteAllBytesAsync(AutoPath, contents, cancellationToken);
        _configuration.Configuration.DataYamlETag = eTag;
        _configuration.Save(nameof(_configuration.Configuration.DataYamlETag));
        _logger.LogInformation("Updated data.yml to ETag {ETag}", eTag);
    }

    private DataYaml? Load()
    {
        string path;
        if (_configuration.Configuration.AutomaticDataYaml) {
            try {
                Download(CancellationToken.None).Wait();
            } catch (Exception e) {
                _logger.LogError(e, "Failed to download data.yml from {Source}", DownloadSourceUri);
            }

            path = AutoPath;
        } else {
            path = _configuration.Configuration.DataYamlPath;
        }

        if (path.Length == 0) {
            return null;
        }

        if (!File.Exists(path)) {
            _logger.LogError("Provided data.yml path {Path} does not exist", path);
            return null;
        }

        try {
            using var reader = File.OpenText(path);
            var yamlStream = new YamlStream();
            yamlStream.Load(reader);
            return DataYaml.Parse(yamlStream.Documents[0].RootNode, _logger);
        } catch (Exception e) {
            _logger.LogError(e, "Could not parse {Path}", path);
            return null;
        }
    }

    private T? MapData<T>(Func<DataYaml, T?> function) where T : class
    {
        var data = Data;
        return data is null ? null : function(data);
    }

    private Dictionary<nint, InstanceName> CalculateClassesByInstance(DataYaml data, bool pointer)
    {
        var classesByInstance = new Dictionary<nint, InstanceName>();
        if (data.Classes is not null) {
            foreach (var (className, @class) in data.Classes) {
                if (@class.Instances is null) {
                    continue;
                }

                foreach (var instance in @class.Instances) {
                    if (instance.Pointer == pointer) {
                        classesByInstance.Add(GetLiveAddress(instance.Ea), new(className, instance.Name));
                    }
                }
            }
        }

        return classesByInstance;
    }

    private Dictionary<nint, string> CalculateClassesByVtbl(DataYaml data)
    {
        var classesByInstance = new Dictionary<nint, string>();
        if (data.Classes is not null) {
            foreach (var (name, @class) in data.Classes) {
                if (@class.Vtbls is null) {
                    continue;
                }

                foreach (var vtbl in @class.Vtbls) {
                    classesByInstance.Add(GetLiveAddress(vtbl.Ea), name);
                }
            }
        }

        return classesByInstance;
    }

    private Dictionary<nint, MemberFunctionName> CalculateMemberFunctions(DataYaml data)
    {
        var memberFunctions = new Dictionary<nint, MemberFunctionName>();
        if (data.Classes is not null) {
            foreach (var (name, @class) in data.Classes) {
                if (@class.Funcs is null) {
                    continue;
                }

                foreach (var (address, fName) in @class.Funcs) {
                    memberFunctions.Add(GetLiveAddress(address), new(name, fName));
                }
            }
        }

        return memberFunctions;
    }

    private unsafe Dictionary<nint, MemberFunctionName> CalculateVirtualFunctions(DataYaml data)
    {
        var virtualFunctions = new Dictionary<nint, MemberFunctionName>();
        if (data.Classes is not null) {
            foreach (var (name, @class) in data.Classes) {
                if (@class.Vfuncs is null) {
                    continue;
                }

                var vtbl = @class.Vtbls?.FirstOrDefault();
                if (vtbl is null) {
                    continue;
                }

                var vtblEa = GetLiveAddress(vtbl.Ea);
                foreach (var (index, fName) in @class.Vfuncs) {
                    virtualFunctions.Add(vtblEa + (nint)index * sizeof(nint), new(name, fName));
                }
            }
        }

        return virtualFunctions;
    }

    public record struct InstanceName(string ClassName, string? Name);
    public record struct MemberFunctionName(string ClassName, string FunctionName);
}
