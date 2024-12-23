using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Dynamis.ClientStructs;

[Serializable]
public sealed class DataYaml
{
    public string? Version { get; set; }

    public Dictionary<Address, string>? Globals { get; set; }

    public Dictionary<Address, string>? Functions { get; set; }

    public Dictionary<string, Class>? Classes { get; set; }

    public static DataYaml Parse(YamlNode root, ILogger? logger = null)
    {
        var rootMap = (YamlMappingNode)root;
        var result = new DataYaml();
        if (rootMap.Children.TryGetValue("version", out var versionNode)) {
            if (TryGetScalar(versionNode, out var version)) {
                result.Version = version;
            } else {
                logger?.LogWarning("Could not parse version");
            }
        }

        if (rootMap.Children.TryGetValue("globals", out var globalsNode)) {
            if (globalsNode is YamlMappingNode globalsMap) {
                result.Globals = ParseNameMap(globalsMap, logger);
            } else {
                logger?.LogWarning("Could not parse globals map");
            }
        }

        if (rootMap.Children.TryGetValue("functions", out var functionsNode)) {
            if (functionsNode is YamlMappingNode functionsMap) {
                result.Functions = ParseNameMap(functionsMap, logger);
            } else {
                logger?.LogWarning("Could not parse functions map");
            }
        }

        if (rootMap.Children.TryGetValue("classes", out var classesNode)) {
            if (classesNode is YamlMappingNode classesMap) {
                result.Classes = new();
                foreach (var (nameNode, classNode) in classesMap.Children) {
                    if (!TryGetScalar(nameNode, out var name)) {
                        logger?.LogWarning("Could not parse class name {Name}", nameNode);
                        continue;
                    }

                    if (Class.TryParse(classNode, out var @class, name, logger)) {
                        result.Classes.Add(name, @class);
                    }
                }
            } else {
                logger?.LogWarning("Could not parse classes map");
            }
        }

        return result;
    }

    private static Dictionary<Address, string> ParseNameMap(YamlMappingNode nameMap, ILogger? logger)
    {
        var names = new Dictionary<Address, string>();
        foreach (var (addressNode, nameNode) in nameMap.Children) {
            if (!Address.TryParse(addressNode, out var address)) {
                logger?.LogWarning("Could not parse named address {Address}", addressNode);
                continue;
            }

            if (TryGetScalar(nameNode, out var name)) {
                names.Add(address, name);
            } else {
                logger?.LogWarning("Could not parse named element {Address}: {Name}", address, nameNode);
            }
        }

        return names;
    }

    private static bool TryGetScalar(YamlNode node, [NotNullWhen(true)] out string? scalar)
    {
        if (node is YamlScalarNode scalarNode) {
            scalar = scalarNode.Value;
            return scalar is not null;
        }

        scalar = null;
        return false;
    }

    [Serializable]
    public sealed class Class
    {
        public List<Instance>? Instances { get; set; }

        public List<VTable>? Vtbls { get; set; }

        public Dictionary<uint, string>? Vfuncs { get; set; }

        public Dictionary<Address, string>? Funcs { get; set; }

        public static bool TryParse(YamlNode node, [NotNullWhen(true)] out Class? @class, string? className = null,
            ILogger? logger = null)
        {
            if (node is not YamlMappingNode map) {
                logger?.LogWarning("Could not parse class {ClassName}", className);
                @class = null;
                return false;
            }

            @class = new();
            if (map.Children.TryGetValue("instances", out var instancesNode)) {
                if (instancesNode is YamlSequenceNode instancesSeq) {
                    @class.Instances = [];
                    foreach (var instanceNode in instancesSeq.Children) {
                        if (Instance.TryParse(instanceNode, out var instance, className, logger)) {
                            @class.Instances.Add(instance);
                        }
                    }
                } else {
                    logger?.LogWarning("Could not parse instances list for class {ClassName}", className);
                }
            }

            if (map.Children.TryGetValue("vtbls", out var vtblsNode)) {
                if (vtblsNode is YamlSequenceNode vtblsSeq) {
                    @class.Vtbls = [];
                    foreach (var vtblNode in vtblsSeq.Children) {
                        if (VTable.TryParse(vtblNode, out var vtbl, className, logger)) {
                            @class.Vtbls.Add(vtbl);
                        }
                    }
                } else {
                    logger?.LogWarning("Could not parse vtbls list for class {ClassName}", className);
                }
            }

            if (map.Children.TryGetValue("vfuncs", out var vfuncsNode)) {
                if (vfuncsNode is YamlMappingNode vfuncsMap) {
                    @class.Vfuncs = new();
                    foreach (var (indexNode, vfuncNode) in vfuncsMap.Children) {
                        if (!TryGetScalar(indexNode, out var indexStr) || !uint.TryParse(indexStr, out var index)) {
                            logger?.LogWarning(
                                "Could not parse vfunc index {Index} for class {ClassName}", indexNode, className
                            );
                            continue;
                        }

                        if (TryGetScalar(vfuncNode, out var vfunc)) {
                            @class.Vfuncs.Add(index, vfunc);
                        } else {
                            logger?.LogWarning(
                                "Could not parse vfunc {Index}: {Vfunc} for class {ClassName}", index, vfuncNode,
                                className
                            );
                        }
                    }
                } else {
                    logger?.LogWarning("Could not parse vfuncs map for class {ClassName}", className);
                }
            }

            if (map.Children.TryGetValue("funcs", out var funcsNode)) {
                if (funcsNode is YamlMappingNode funcsMap) {
                    @class.Funcs = new();
                    foreach (var (addressNode, funcNode) in funcsMap.Children) {
                        if (!Address.TryParse(addressNode, out var address)) {
                            logger?.LogWarning(
                                "Could not parse func address {Address} for class {ClassName}", addressNode, className
                            );
                            continue;
                        }

                        if (TryGetScalar(funcNode, out var func)) {
                            @class.Funcs.Add(address, func);
                        } else {
                            logger?.LogWarning(
                                "Could not parse func {Address}: {Func} for class {ClassName}", address, funcNode,
                                className
                            );
                        }
                    }
                } else {
                    logger?.LogWarning("Could not parse funcs map for class {ClassName}", className);
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class Instance
    {
        public Address Ea { get; set; }

        public string? Name { get; set; }

        public bool Pointer { get; set; } = true;

        public static bool TryParse(YamlNode node, [NotNullWhen(true)] out Instance? instance, string? className = null,
            ILogger? logger = null)
        {
            if (node is not YamlMappingNode map) {
                logger?.LogWarning("Could not parse instance of class {ClassName}", className);
                instance = null;
                return false;
            }

            instance = new();
            if (map.Children.TryGetValue("ea", out var eaNode)) {
                if (Address.TryParse(eaNode, out var ea)) {
                    instance.Ea = ea;
                } else {
                    logger?.LogWarning("Could not parse ea {Ea} for instance of class {ClassName}", eaNode, className);
                }
            }

            if (map.Children.TryGetValue("name", out var nameNode)) {
                if (TryGetScalar(nameNode, out var name)) {
                    instance.Name = name;
                } else {
                    logger?.LogWarning(
                        "Could not parse name {Name} for instance of class {ClassName}", nameNode, className
                    );
                }
            }

            if (map.Children.TryGetValue("pointer", out var pointerNode)) {
                if (TryGetScalar(pointerNode, out var pointerStr) && bool.TryParse(pointerStr, out var pointer)) {
                    instance.Pointer = pointer;
                } else {
                    logger?.LogWarning(
                        "Could not parse pointer field {Pointer} for instance of class {ClassName}", pointerNode,
                        className
                    );
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class VTable
    {
        public Address Ea { get; set; }

        public string? Base { get; set; }

        public static bool TryParse(YamlNode node, [NotNullWhen(true)] out VTable? vtbl, string? className = null,
            ILogger? logger = null)
        {
            if (node is not YamlMappingNode map) {
                logger?.LogWarning("Could not parse vtable of class {ClassName}", className);
                vtbl = null;
                return false;
            }

            vtbl = new();
            if (map.Children.TryGetValue("ea", out var eaNode)) {
                if (Address.TryParse(eaNode, out var ea)) {
                    vtbl.Ea = ea;
                } else {
                    logger?.LogWarning("Could not parse ea {Ea} for instance of class {ClassName}", eaNode, className);
                }
            }

            if (map.Children.TryGetValue("base", out var baseNode)) {
                if (TryGetScalar(baseNode, out var @base)) {
                    vtbl.Base = @base;
                } else {
                    logger?.LogWarning(
                        "Could not parse base {Base} for instance of class {ClassName}", baseNode, className
                    );
                }
            }

            return true;
        }
    }
}
