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
            if (qualifiedLibrary == null)
                throw new ArgumentNullException(nameof(qualifiedLibrary));

            var contract = new ValveProfileContract
            {
                ProfileIdentity = profileIdentity ?? throw new ArgumentNullException(nameof(profileIdentity)),
                SourceArchiveSha256 = sourceArchiveSha256 ?? throw new ArgumentNullException(nameof(sourceArchiveSha256)),
                TiaBuildIdentity = tiaBuildIdentity ?? throw new ArgumentNullException(nameof(tiaBuildIdentity)),
                CpuTypeIdentifier = TargetCpuTypeIdentifier,
                ValveBlockName = TargetValveBlockName
            };

            var valveType = FindValveType(qualifiedLibrary);
            if (valveType == null)
            {
                throw new InvalidOperationException("Valve block fbValve_Solenoid not found in qualified library");
            }

            contract.ValveTypeName = valveType.Name;
            contract.ValveVersion = GetTypeVersion(valveType);

            ExtractParameters(valveType, contract);
            ExtractDependencies(qualifiedLibrary, valveType, contract);

            contract.InstanceDataOwnership = DeriveInstanceDataOwnership(valveType);
            contract.DbPrerequisites = DeriveDbPrerequisites(qualifiedLibrary, valveType);

            contract.ContractHash = ValveProfileContract.ComputeContractHash(contract);
            return contract;
        }

        private Type FindValveType(UserGlobalLibrary library)
        {
            var masterCopies = library.MasterCopies;
            if (masterCopies == null)
                throw new InvalidOperationException("Qualified library has no master copies");

            return FindValveTypeRecursive(masterCopies);
        }

        private Type FindValveTypeRecursive(LibraryObjectComposition libraryObjects)
        {
            foreach (var obj in libraryObjects)
            {
                if (string.Equals(obj.Name, TargetValveBlockName, StringComparison.OrdinalIgnoreCase))
                {
                    return obj as Type ?? throw new InvalidOperationException("Valve object is not a Type");
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
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var version = type.GetAttribute("Version");
            if (version == null)
                throw new InvalidOperationException("Valve type Version attribute is missing");
            return version.ToString();
        }

        private void ExtractParameters(Type valveType, ValveProfileContract contract)
        {
            if (valveType == null)
                throw new ArgumentNullException(nameof(valveType));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            var parameters = valveType.Parameters;
            if (parameters == null)
                throw new InvalidOperationException("Valve type has no parameters collection");

            foreach (var param in parameters)
            {
                var declaration = param.Declaration;
                if (declaration == null)
                    throw new InvalidOperationException("Parameter " + param.Name + " has no declaration");

                string direction = declaration.GetAttribute("Direction")?.ToString();
                if (string.IsNullOrEmpty(direction))
                    throw new InvalidOperationException("Parameter " + param.Name + " has no Direction attribute");

                string plcType = GetPlcTypeName(param);
                if (string.IsNullOrEmpty(plcType) || plcType == "Unknown")
                    throw new InvalidOperationException("Parameter " + param.Name + " has unknown PLC type");

                contract.Parameters.Add(new ValveParameterContract
                {
                    Name = param.Name,
                    Direction = direction,
                    PlcType = plcType
                });
            }
        }

        private string GetPlcTypeName(PlcParameter param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            var dataType = param.DataType;
            if (dataType == null)
                throw new InvalidOperationException("Parameter " + param.Name + " has no DataType");

            return dataType.Name;
        }

        private void ExtractDependencies(UserGlobalLibrary library, Type valveType, ValveProfileContract contract)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (valveType == null)
                throw new ArgumentNullException(nameof(valveType));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dependencies = new List<ValveDependencyContract>();

            CollectDependencies(library, valveType, visited, dependencies);

            dependencies.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            contract.Dependencies = dependencies;
        }

        private void CollectDependencies(UserGlobalLibrary library, Type type, HashSet<string> visited, List<ValveDependencyContract> dependencies)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            string typeKey = type.Name + ":" + GetTypeVersion(type);
            if (visited.Contains(typeKey))
                return;
            visited.Add(typeKey);

            if (!string.Equals(type.Name, TargetValveBlockName, StringComparison.OrdinalIgnoreCase))
            {
                string typeNameAttr = type.GetAttribute("TypeName")?.ToString();
                if (string.IsNullOrEmpty(typeNameAttr))
                    throw new InvalidOperationException("Dependency type " + type.Name + " has no TypeName attribute");

                dependencies.Add(new ValveDependencyContract
                {
                    Name = type.Name,
                    TypeName = typeNameAttr,
                    Version = GetTypeVersion(type)
                });
            }

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

        private Type FindTypeInLibrary(UserGlobalLibrary library, string typeName)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));

            return FindTypeInLibraryRecursive(library.MasterCopies, typeName);
        }

        private Type FindTypeInLibraryRecursive(LibraryObjectComposition libraryObjects, string typeName)
        {
            if (libraryObjects == null)
                return null;

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

        private string DeriveInstanceDataOwnership(Type valveType)
        {
            if (valveType == null)
                throw new ArgumentNullException(nameof(valveType));

            return "Multi-instance inside calling FB/FC; no separate instance DB required by default";
        }

        private string DeriveDbPrerequisites(UserGlobalLibrary library, Type valveType)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (valveType == null)
                throw new ArgumentNullException(nameof(valveType));

            return "Standard (non-optimized) DB access if legacy alarm profile used; otherwise optimized access permitted";
        }
    }
}