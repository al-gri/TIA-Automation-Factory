using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Library;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal sealed class ValveProfileProbe
    {
        private const string TargetValveBlockName = "fbValve_Solenoid";
        private const string TargetCpuTypeIdentifier = "OrderNumber:6ES7 516-3AP03-0AB0/V4.0";

        public ValveProfileContract Probe(UserGlobalLibrary qualifiedLibrary, string profileIdentity, string sourceArchiveSha256, string tiaBuildIdentity)
        {
            var contract = new ValveProfileContract
            {
                ProfileIdentity = profileIdentity,
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                CpuTypeIdentifier = TargetCpuTypeIdentifier,
                ValveBlockName = TargetValveBlockName
            };

            var valveType = FindValveType(qualifiedLibrary);
            if (valveType == null)
            {
                contract.InstanceDataOwnership = "Valve block not found in qualified library";
                contract.DbPrerequisites = "Valve block not found in qualified library";
                contract.ContractHash = ValveProfileContract.ComputeContractHash(contract);
                return contract;
            }

            contract.ValveTypeName = valveType.Name;
            contract.ValveVersion = GetTypeVersion(valveType);

            ExtractParameters(valveType, contract);
            ExtractDependencies(qualifiedLibrary, valveType, contract);

            contract.InstanceDataOwnership = "Multi-instance inside calling FB/FC; no separate instance DB required by default";
            contract.DbPrerequisites = "Standard (non-optimized) DB access if legacy alarm profile used; otherwise optimized access permitted";

            contract.ContractHash = ValveProfileContract.ComputeContractHash(contract);
            return contract;
        }

        private Type FindValveType(UserGlobalLibrary library)
        {
            var masterCopies = library.MasterCopies;
            if (masterCopies == null)
                return null;

            return FindValveTypeRecursive(masterCopies);
        }

        private Type FindValveTypeRecursive(LibraryObjectComposition libraryObjects)
        {
            foreach (var obj in libraryObjects)
            {
                if (string.Equals(obj.Name, TargetValveBlockName, StringComparison.OrdinalIgnoreCase))
                {
                    return obj as Type;
                }

                if (obj is LibraryObjectGroup group)
                {
                    var found = FindValveTypeRecursive(group.MasterCopies);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private string GetTypeVersion(Type type)
        {
            try
            {
                var version = type.GetAttribute("Version");
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        private void ExtractParameters(Type valveType, ValveProfileContract contract)
        {
            try
            {
                var parameters = valveType.Parameters;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        string direction = param.Declaration.GetAttribute("Direction")?.ToString() ?? "InOut";
                        string plcType = GetPlcTypeName(param);

                        contract.Parameters.Add(new ValveParameterContract
                        {
                            Name = param.Name,
                            Direction = direction,
                            PlcType = plcType
                        });
                    }
                }
            }
            catch
            {
            }
        }

        private string GetPlcTypeName(PlcParameter param)
        {
            try
            {
                var dataType = param.DataType;
                if (dataType != null)
                {
                    return dataType.Name;
                }
            }
            catch
            {
            }
            return "Unknown";
        }

        private void ExtractDependencies(UserGlobalLibrary library, Type valveType, ValveProfileContract contract)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dependencies = new List<ValveDependencyContract>();

            CollectDependencies(library, valveType, visited, dependencies);

            dependencies.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            contract.Dependencies = dependencies;
        }

        private void CollectDependencies(UserGlobalLibrary library, Type type, HashSet<string> visited, List<ValveDependencyContract> dependencies)
        {
            string typeKey = type.Name + ":" + GetTypeVersion(type);
            if (visited.Contains(typeKey))
                return;
            visited.Add(typeKey);

            if (!string.Equals(type.Name, TargetValveBlockName, StringComparison.OrdinalIgnoreCase))
            {
                dependencies.Add(new ValveDependencyContract
                {
                    Name = type.Name,
                    TypeName = type.GetAttribute("TypeName")?.ToString() ?? "FB",
                    Version = GetTypeVersion(type)
                });
            }

            try
            {
                var usedTypes = type.GetUsedTypes();
                if (usedTypes != null)
                {
                    foreach (var usedType in usedTypes)
                    {
                        var libraryType = FindTypeInLibrary(library, usedType.Name);
                        if (libraryType != null)
                        {
                            CollectDependencies(library, libraryType, visited, dependencies);
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                var calls = type.GetCalls();
                if (calls != null)
                {
                    foreach (var call in calls)
                    {
                        var calledType = call.CalledBlock;
                        if (calledType != null)
                        {
                            var libraryType = FindTypeInLibrary(library, calledType.Name);
                            if (libraryType != null)
                            {
                                CollectDependencies(library, libraryType, visited, dependencies);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private Type FindTypeInLibrary(UserGlobalLibrary library, string typeName)
        {
            return FindTypeInLibraryRecursive(library.MasterCopies, typeName);
        }

        private Type FindTypeInLibraryRecursive(LibraryObjectComposition libraryObjects, string typeName)
        {
            foreach (var obj in libraryObjects)
            {
                if (string.Equals(obj.Name, typeName, StringComparison.OrdinalIgnoreCase))
                {
                    return obj as Type;
                }

                if (obj is LibraryObjectGroup group)
                {
                    var found = FindTypeInLibraryRecursive(group.MasterCopies, typeName);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }
    }
}