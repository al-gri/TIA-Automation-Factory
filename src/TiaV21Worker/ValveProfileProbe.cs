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

            var dependencies = DiscoverDependencyClosure(qualifiedLibrary, valveBlock);
            var instanceData = DetermineInstanceDataRequirements(valveBlock);
            var targetPrerequisites = DetermineTargetPrerequisites(cpuTypeIdentifier);

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
            try
            {
                if (userType.TypeIdentifier != null)
                {
                    var parts = userType.TypeIdentifier.Split(',');
                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.StartsWith("Version=", StringComparison.OrdinalIgnoreCase))
                        {
                            return trimmed.Substring("Version=".Length);
                        }
                    }
                }
            }
            catch { }
            return "1.0.0.0";
        }

        private static string GetTypeNamespace(GlobalLibraryUserType userType)
        {
            try
            {
                if (userType.TypeIdentifier != null)
                {
                    var parts = userType.TypeIdentifier.Split(',');
                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.StartsWith("Namespace=", StringComparison.OrdinalIgnoreCase))
                        {
                            return trimmed.Substring("Namespace=".Length);
                        }
                    }
                }
            }
            catch { }
            return "Siemens.OpenLibrary";
        }

        private static void ExtractParameters(PlcBlockUserType plcBlock, ValveBlockIdentity identity)
        {
            try
            {
                var interfaceComposition = plcBlock.Interface;
                if (interfaceComposition != null)
                {
                    foreach (var section in interfaceComposition.Sections)
                    {
                        foreach (var tag in section.Tags)
                        {
                            var param = new ValveParameter
                            {
                                Name = tag.Name,
                                Direction = GetParameterDirection(section),
                                PlcType = GetPlcTypeName(tag),
                                Description = tag.Comment ?? string.Empty
                            };
                            identity.Parameters.Add(param);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static string GetParameterDirection(PlcInterfaceSection section)
        {
            var sectionName = section.Name.ToLowerInvariant();
            if (sectionName.Contains("in") && !sectionName.Contains("out"))
                return "In";
            if (sectionName.Contains("out"))
                return "Out";
            if (sectionName.Contains("inout") || sectionName.Contains("in_out"))
                return "InOut";
            if (sectionName.Contains("static"))
                return "Static";
            if (sectionName.Contains("temp"))
                return "Temp";
            if (sectionName.Contains("constant"))
                return "Constant";
            return "Unknown";
        }

        private static string GetPlcTypeName(PlcTag tag)
        {
            try
            {
                return tag.DataTypeName?.Name ?? tag.DataType?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
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
                    dependencies.Add(new DependencyIdentity
                    {
                        Name = current.Name,
                        TypeName = current.TypeName,
                        Version = GetTypeVersion(current),
                        Namespace = GetTypeNamespace(current)
                    });
                }

                var composition = current.GetComposition();
                foreach (var instance in composition.Instances)
                {
                    var instanceType = instance.GetType();
                    if (instanceType.Name.Contains("PlcBlock") || instanceType.Name.Contains("TypeInstance"))
                    {
                        var usedTypes = GetUsedTypes(instance);
                        foreach (var usedType in usedTypes)
                        {
                            var depType = FindUserTypeByIdentity(library, usedType);
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
            return $"{GetTypeNamespace(userType)}.{userType.TypeName} v{GetTypeVersion(userType)}";
        }

        private static IEnumerable<string> GetUsedTypes(object instance)
        {
            var result = new List<string>();
            try
            {
                var type = instance.GetType();
                var typeProp = type.GetProperty("Type") ?? type.GetProperty("UserType") ?? type.GetProperty("BlockType");
                if (typeProp != null)
                {
                    var usedType = typeProp.GetValue(instance);
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
            }
            catch { }
            return result;
        }

        private static InstanceDataRequirements DetermineInstanceDataRequirements(ValveBlockIdentity valveBlock)
        {
            return new InstanceDataRequirements
            {
                RequiresInstanceDb = true,
                InstanceDbName = $"DB_{valveBlock.Name}",
                DataBlockAccessMode = "Standard",
                MultiInstancePath = $"{valveBlock.Name}_Instance"
            };
        }

        private static TargetPrerequisites DetermineTargetPrerequisites(string cpuTypeIdentifier)
        {
            return new TargetPrerequisites
            {
                CpuTypeIdentifier = cpuTypeIdentifier,
                RequiredSystemMemory = true,
                RequiredClockMemory = true,
                ExpectedSystemMemoryAddress = "%MB100",
                ExpectedClockMemoryAddress = "%MB101"
            };
        }
    }
}