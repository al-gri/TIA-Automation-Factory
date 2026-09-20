using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Library;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal static class ValveProfileProbe
    {
        public static ValveProfileContract Probe(
            UserGlobalLibrary qualifiedLibrary,
            string profileIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity,
            string qualificationIdentity,
            string cpuTypeIdentifier)
        {
            var valveBlock = FindValveBlock(qualifiedLibrary);
            if (valveBlock == null)
            {
                throw new InvalidOperationException("fbValve_Solenoid not found in qualified library.");
            }

            if (string.IsNullOrEmpty(valveBlock.Version))
                throw new InvalidOperationException("Valve block version could not be determined from V21 library.");
            if (string.IsNullOrEmpty(valveBlock.Namespace))
                throw new InvalidOperationException("Valve block namespace could not be determined from V21 library.");
            if (valveBlock.Parameters == null || valveBlock.Parameters.Count == 0)
                throw new InvalidOperationException("Valve block has no parameters; cannot derive contract.");

            foreach (var param in valveBlock.Parameters)
            {
                if (string.IsNullOrEmpty(param.Name))
                    throw new InvalidOperationException("Valve parameter missing name.");
                if (string.IsNullOrEmpty(param.Direction))
                    throw new InvalidOperationException("Valve parameter '" + param.Name + "' missing direction.");
                if (string.IsNullOrEmpty(param.PlcType))
                    throw new InvalidOperationException("Valve parameter '" + param.Name + "' missing PLC type.");
                ValidateDirection(param.Direction);
            }

            var dependencies = DiscoverDependencyClosure(qualifiedLibrary, valveBlock);
            var instanceData = DetermineInstanceDataRequirements(qualifiedLibrary, valveBlock);
            var targetPrerequisites = DetermineTargetPrerequisites(qualifiedLibrary, cpuTypeIdentifier);

            return ValveProfileContract.Create(
                profileIdentity,
                sourceArchiveSha256,
                tiaBuildIdentity,
                qualificationIdentity,
                cpuTypeIdentifier,
                valveBlock,
                dependencies,
                instanceData,
                targetPrerequisites);
        }

        private static void ValidateDirection(string direction)
        {
            var validDirections = new[] { "In", "Out", "InOut", "Static", "Temp", "Constant" };
            if (!validDirections.Contains(direction))
                throw new InvalidOperationException("Invalid parameter direction: " + direction);
        }

        private static ValveBlockIdentity FindValveBlock(UserGlobalLibrary library)
        {
            foreach (var group in library.Groups)
            {
                var result = FindValveBlockInGroup(group);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static ValveBlockIdentity FindValveBlockInGroup(GlobalLibraryGroup group)
        {
            foreach (var type in group.Types)
            {
                if (type is GlobalLibraryUserType userType)
                {
                    if (string.Equals(userType.Name, "fbValve_Solenoid", StringComparison.OrdinalIgnoreCase))
                    {
                        return ExtractValveBlockIdentity(userType);
                    }
                }
            }

            foreach (var subGroup in group.Groups)
            {
                var result = FindValveBlockInGroup(subGroup);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static ValveBlockIdentity ExtractValveBlockIdentity(GlobalLibraryUserType userType)
        {
            var identity = new ValveBlockIdentity
            {
                Name = userType.Name,
                TypeName = userType.TypeName,
                Version = GetTypeVersion(userType),
                Namespace = GetTypeNamespace(userType)
            };

            if (string.IsNullOrEmpty(identity.Version))
                throw new InvalidOperationException("Type version not found in TypeIdentifier for " + userType.Name);
            if (string.IsNullOrEmpty(identity.Namespace))
                throw new InvalidOperationException("Type namespace not found in TypeIdentifier for " + userType.Name);

            if (userType.IsReleased)
            {
                var composition = userType.GetComposition();
                foreach (var instance in composition.Instances)
                {
                    if (instance is PlcBlockUserType plcBlock)
                    {
                        ExtractParameters(plcBlock, identity);
                    }
                }
            }

            return identity;
        }

        private static string GetTypeVersion(GlobalLibraryUserType userType)
        {
            if (userType.TypeIdentifier == null)
                return null;

            var parts = userType.TypeIdentifier.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Version=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring("Version=".Length);
                }
            }
            return null;
        }

        private static string GetTypeNamespace(GlobalLibraryUserType userType)
        {
            if (userType.TypeIdentifier == null)
                return null;

            var parts = userType.TypeIdentifier.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Namespace=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring("Namespace=".Length);
                }
            }
            return null;
        }

        private static void ExtractParameters(PlcBlockUserType plcBlock, ValveBlockIdentity identity)
        {
            var interfaceComposition = plcBlock.Interface;
            if (interfaceComposition == null)
                throw new InvalidOperationException("PlcBlockUserType has no interface composition.");

            foreach (var section in interfaceComposition.Sections)
            {
                foreach (var tag in section.Tags)
                {
                    var direction = GetParameterDirection(section);
                    var plcType = GetPlcTypeName(tag);
                    if (string.IsNullOrEmpty(plcType))
                        throw new InvalidOperationException("Could not determine PLC type for parameter: " + tag.Name);

                    var param = new ValveParameter
                    {
                        Name = tag.Name,
                        Direction = direction,
                        PlcType = plcType,
                        Description = tag.Comment ?? string.Empty
                    };
                    identity.Parameters.Add(param);
                }
            }
        }

        private static string GetParameterDirection(PlcInterfaceSection section)
        {
            var sectionName = section.Name.ToLowerInvariant();
            if (sectionName.Contains("inout") || sectionName.Contains("in_out"))
                return "InOut";
            if (sectionName.Contains("in") && !sectionName.Contains("out"))
                return "In";
            if (sectionName.Contains("out"))
                return "Out";
            if (sectionName.Contains("static"))
                return "Static";
            if (sectionName.Contains("temp"))
                return "Temp";
            if (sectionName.Contains("constant"))
                return "Constant";
            throw new InvalidOperationException("Unknown interface section type: " + section.Name);
        }

        private static string GetPlcTypeName(PlcTag tag)
        {
            if (tag.DataTypeName != null && !string.IsNullOrEmpty(tag.DataTypeName.Name))
                return tag.DataTypeName.Name;
            if (tag.DataType != null && !string.IsNullOrEmpty(tag.DataType.Name))
                return tag.DataType.Name;
            return null;
        }

        private static List<DependencyIdentity> DiscoverDependencyClosure(UserGlobalLibrary library, ValveBlockIdentity valveBlock)
        {
            var dependencies = new List<DependencyIdentity>();
            var visited = new HashSet<string>();

            var queue = new Queue<GlobalLibraryUserType>();
            var valveType = FindUserType(library, valveBlock.Name);
            if (valveType != null)
            {
                queue.Enqueue(valveType);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var typeId = GetTypeIdentityString(current);
                if (visited.Contains(typeId))
                    continue;
                visited.Add(typeId);

                if (!string.Equals(current.Name, valveBlock.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var depVersion = GetTypeVersion(current);
                    var depNamespace = GetTypeNamespace(current);
                    if (string.IsNullOrEmpty(depVersion))
                        throw new InvalidOperationException("Dependency type version not found for: " + current.Name);
                    if (string.IsNullOrEmpty(depNamespace))
                        throw new InvalidOperationException("Dependency type namespace not found for: " + current.Name);

                    dependencies.Add(new DependencyIdentity
                    {
                        Name = current.Name,
                        TypeName = current.TypeName,
                        Version = depVersion,
                        Namespace = depNamespace
                    });
                }

                var composition = current.GetComposition();
                foreach (var instance in composition.Instances)
                {
                    if (instance is PlcBlockUserType plcBlockInstance)
                    {
                        var usedTypes = GetUsedTypesFromBlock(plcBlockInstance);
                        foreach (var usedTypeId in usedTypes)
                        {
                            var depType = FindUserTypeByIdentity(library, usedTypeId);
                            if (depType != null)
                            {
                                queue.Enqueue(depType);
                            }
                        }
                    }
                }
            }

            return dependencies;
        }

        private static IEnumerable<string> GetUsedTypesFromBlock(PlcBlockUserType plcBlock)
        {
            var result = new List<string>();
            try
            {
                var composition = plcBlock.GetComposition();
                foreach (var instance in composition.Instances)
                {
                    if (instance is PlcBlockUserType nestedBlock)
                    {
                        var typeProp = nestedBlock.GetType().GetProperty("Type") ?? nestedBlock.GetType().GetProperty("UserType") ?? nestedBlock.GetType().GetProperty("BlockType");
                        if (typeProp != null)
                        {
                            var usedType = typeProp.GetValue(nestedBlock);
                            if (usedType != null)
                            {
                                var idProp = usedType.GetType().GetProperty("TypeIdentifier") ?? usedType.GetType().GetProperty("Identifier");
                                if (idProp != null)
                                {
                                    var id = idProp.GetValue(usedType)?.ToString();
                                    if (!string.IsNullOrEmpty(id))
                                        result.Add(id);
                                }
                            }
                        }
                        result.AddRange(GetUsedTypesFromBlock(nestedBlock));
                    }
                }
            }
            catch
            {
                throw new InvalidOperationException("Failed to extract used types from block: " + plcBlock.Name);
            }
            return result;
        }

        private static GlobalLibraryUserType FindUserType(UserGlobalLibrary library, string name)
        {
            foreach (var group in library.Groups)
            {
                var result = FindUserTypeInGroup(group, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static GlobalLibraryUserType FindUserTypeInGroup(GlobalLibraryGroup group, string name)
        {
            foreach (var type in group.Types)
            {
                if (type is GlobalLibraryUserType userType &&
                    string.Equals(userType.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return userType;
                }
            }

            foreach (var subGroup in group.Groups)
            {
                var result = FindUserTypeInGroup(subGroup, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static GlobalLibraryUserType FindUserTypeByIdentity(UserGlobalLibrary library, string typeIdentity)
        {
            foreach (var group in library.Groups)
            {
                var result = FindUserTypeByIdentityInGroup(group, typeIdentity);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static GlobalLibraryUserType FindUserTypeByIdentityInGroup(GlobalLibraryGroup group, string typeIdentity)
        {
            foreach (var type in group.Types)
            {
                if (type is GlobalLibraryUserType userType)
                {
                    var id = GetTypeIdentityString(userType);
                    if (string.Equals(id, typeIdentity, StringComparison.OrdinalIgnoreCase))
                    {
                        return userType;
                    }
                }
            }

            foreach (var subGroup in group.Groups)
            {
                var result = FindUserTypeByIdentityInGroup(subGroup, typeIdentity);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static string GetTypeIdentityString(GlobalLibraryUserType userType)
        {
            var ns = GetTypeNamespace(userType);
            var ver = GetTypeVersion(userType);
            if (string.IsNullOrEmpty(ns) || string.IsNullOrEmpty(ver))
                throw new InvalidOperationException("Cannot build type identity string: missing namespace or version for " + userType.Name);
            return $"{ns}.{userType.TypeName} v{ver}";
        }

        private static InstanceDataRequirements DetermineInstanceDataRequirements(UserGlobalLibrary library, ValveBlockIdentity valveBlock)
        {
            var valveType = FindUserType(library, valveBlock.Name);
            if (valveType == null)
                throw new InvalidOperationException("Valve type not found in library for instance data determination.");

            var composition = valveType.GetComposition();
            bool requiresInstanceDb = false;
            string instanceDbName = null;
            string dataBlockAccessMode = null;
            string multiInstancePath = null;

            foreach (var instance in composition.Instances)
            {
                if (instance is PlcBlockUserType plcBlock)
                {
                    requiresInstanceDb = true;
                    instanceDbName = "DB_" + plcBlock.Name;
                    dataBlockAccessMode = "Standard";
                    multiInstancePath = plcBlock.Name + "_Instance";
                    break;
                }
            }

            if (!requiresInstanceDb)
                throw new InvalidOperationException("Could not determine instance data requirements for valve block.");

            return new InstanceDataRequirements
            {
                RequiresInstanceDb = requiresInstanceDb,
                InstanceDbName = instanceDbName,
                DataBlockAccessMode = dataBlockAccessMode,
                MultiInstancePath = multiInstancePath
            };
        }

        private static TargetPrerequisites DetermineTargetPrerequisites(UserGlobalLibrary library, string cpuTypeIdentifier)
        {
            bool requiredSystemMemory = false;
            bool requiredClockMemory = false;
            string expectedSystemMemoryAddress = null;
            string expectedClockMemoryAddress = null;

            foreach (var group in library.Groups)
            {
                CheckGroupForPrerequisites(group, ref requiredSystemMemory, ref requiredClockMemory, ref expectedSystemMemoryAddress, ref expectedClockMemoryAddress);
            }

            return new TargetPrerequisites
            {
                CpuTypeIdentifier = cpuTypeIdentifier,
                RequiredSystemMemory = requiredSystemMemory,
                RequiredClockMemory = requiredClockMemory,
                ExpectedSystemMemoryAddress = expectedSystemMemoryAddress,
                ExpectedClockMemoryAddress = expectedClockMemoryAddress
            };
        }

        private static void CheckGroupForPrerequisites(GlobalLibraryGroup group, ref bool requiredSystemMemory, ref bool requiredClockMemory, ref string expectedSystemMemoryAddress, ref string expectedClockMemoryAddress)
        {
            foreach (var type in group.Types)
            {
                if (type is GlobalLibraryUserType userType)
                {
                    try
                    {
                        var composition = userType.GetComposition();
                        foreach (var instance in composition.Instances)
                        {
                            if (instance is PlcBlockUserType plcBlock)
                            {
                                var interfaceComposition = plcBlock.Interface;
                                if (interfaceComposition != null)
                                {
                                    foreach (var section in interfaceComposition.Sections)
                                    {
                                        foreach (var tag in section.Tags)
                                        {
                                            var tagName = tag.Name.ToLowerInvariant();
                                            if (tagName.Contains("system") && tagName.Contains("memory"))
                                            {
                                                requiredSystemMemory = true;
                                                if (string.IsNullOrEmpty(expectedSystemMemoryAddress))
                                                    expectedSystemMemoryAddress = "%MB" + tag.Offset?.ToString() ?? "100";
                                            }
                                            if (tagName.Contains("clock") && tagName.Contains("memory"))
                                            {
                                                requiredClockMemory = true;
                                                if (string.IsNullOrEmpty(expectedClockMemoryAddress))
                                                    expectedClockMemoryAddress = "%MB" + tag.Offset?.ToString() ?? "101";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            foreach (var subGroup in group.Groups)
            {
                CheckGroupForPrerequisites(subGroup, ref requiredSystemMemory, ref requiredClockMemory, ref expectedSystemMemoryAddress, ref expectedClockMemoryAddress);
            }
        }
    }
}