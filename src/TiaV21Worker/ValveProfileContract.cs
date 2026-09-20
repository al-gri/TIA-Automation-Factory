using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal sealed class ValveProfileContract
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string ProfileIdentity { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string TiaBuildIdentity { get; set; }
        public string QualificationIdentity { get; set; }
        public string CpuTypeIdentifier { get; set; }
        public ValveBlockIdentity ValveBlock { get; set; }
        public List<DependencyIdentity> Dependencies { get; set; } = new List<DependencyIdentity>();
        public InstanceDataRequirements InstanceData { get; set; }
        public TargetPrerequisites TargetPrerequisites { get; set; }
        public string ContractHash { get; set; }
        public DateTimeOffset DiscoveredAt { get; set; }

        public static ValveProfileContract Create(
            string profileIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity,
            string qualificationIdentity,
            string cpuTypeIdentifier,
            ValveBlockIdentity valveBlock,
            IEnumerable<DependencyIdentity> dependencies,
            InstanceDataRequirements instanceData,
            TargetPrerequisites targetPrerequisites)
        {
            var contract = new ValveProfileContract
            {
                ProfileIdentity = profileIdentity,
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                QualificationIdentity = qualificationIdentity,
                CpuTypeIdentifier = cpuTypeIdentifier,
                ValveBlock = valveBlock,
                Dependencies = dependencies.ToList(),
                InstanceData = instanceData,
                TargetPrerequisites = targetPrerequisites,
                DiscoveredAt = DateTimeOffset.UtcNow
            };

            contract.ContractHash = ComputeContractHash(contract);
            return contract;
        }

        private static string ComputeContractHash(ValveProfileContract contract)
        {
            using (var sha256 = SHA256.Create())
            {
                var builder = new StringBuilder();
                builder.Append(contract.SchemaVersion).Append('|');
                builder.Append(contract.ProfileIdentity).Append('|');
                builder.Append(contract.SourceArchiveSha256).Append('|');
                builder.Append(contract.TiaBuildIdentity).Append('|');
                builder.Append(contract.QualificationIdentity).Append('|');
                builder.Append(contract.CpuTypeIdentifier).Append('|');
                builder.Append(contract.ValveBlock?.GetIdentityString() ?? "").Append('|');

                foreach (var dep in contract.Dependencies.OrderBy(d => d.GetIdentityString()))
                {
                    builder.Append(dep.GetIdentityString()).Append(';');
                }
                builder.Append('|');

                builder.Append(contract.InstanceData?.GetIdentityString() ?? "").Append('|');
                builder.Append(contract.TargetPrerequisites?.GetIdentityString() ?? "");

                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public string Serialize()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "schemaVersion", SchemaVersion, true, true);
            AppendProperty(builder, "profileIdentity", ProfileIdentity, true, true);
            AppendProperty(builder, "sourceArchiveSha256", SourceArchiveSha256, true, true);
            AppendProperty(builder, "tiaBuildIdentity", TiaBuildIdentity, true, true);
            AppendProperty(builder, "qualificationIdentity", QualificationIdentity, true, true);
            AppendProperty(builder, "cpuTypeIdentifier", CpuTypeIdentifier, true, true);
            builder.AppendLine("  \"valveBlock\": {");
            AppendProperty(builder, "name", ValveBlock?.Name, true, true, 4);
            AppendProperty(builder, "typeName", ValveBlock?.TypeName, true, true, 4);
            AppendProperty(builder, "version", ValveBlock?.Version, true, true, 4);
            AppendProperty(builder, "namespace", ValveBlock?.Namespace, true, true, 4);
            builder.Append("    \"parameters\": [");
            if (ValveBlock?.Parameters != null)
            {
                for (int i = 0; i < ValveBlock.Parameters.Count; i++)
                {
                    var p = ValveBlock.Parameters[i];
                    builder.AppendLine();
                    builder.Append("      {");
                    AppendProperty(builder, "name", p.Name, true, true, 8);
                    AppendProperty(builder, "direction", p.Direction, true, true, 8);
                    AppendProperty(builder, "plcType", p.PlcType, true, true, 8);
                    AppendProperty(builder, "description", p.Description, true, i == ValveBlock.Parameters.Count - 1 ? false : true, 8);
                    builder.Append("      }");
                    if (i < ValveBlock.Parameters.Count - 1) builder.Append(",");
                }
            }
            builder.AppendLine();
            builder.Append("    ]");
            builder.AppendLine();
            builder.AppendLine("  },");
            builder.AppendLine("  \"dependencies\": [");
            if (Dependencies != null)
            {
                for (int i = 0; i < Dependencies.Count; i++)
                {
                    var d = Dependencies[i];
                    builder.AppendLine("    {");
                    AppendProperty(builder, "name", d.Name, true, true, 6);
                    AppendProperty(builder, "typeName", d.TypeName, true, true, 6);
                    AppendProperty(builder, "version", d.Version, true, true, 6);
                    AppendProperty(builder, "namespace", d.Namespace, true, i == Dependencies.Count - 1 ? false : true, 6);
                    builder.Append("    }");
                    if (i < Dependencies.Count - 1) builder.Append(",");
                    builder.AppendLine();
                }
            }
            builder.AppendLine("  ],");
            builder.AppendLine("  \"instanceData\": {");
            AppendProperty(builder, "requiresInstanceDb", InstanceData?.RequiresInstanceDb ? "true" : "false", false, true, 4);
            AppendProperty(builder, "instanceDbName", InstanceData?.InstanceDbName, true, true, 4);
            AppendProperty(builder, "dataBlockAccessMode", InstanceData?.DataBlockAccessMode, true, true, 4);
            AppendProperty(builder, "multiInstancePath", InstanceData?.MultiInstancePath, true, false, 4);
            builder.AppendLine("  },");
            builder.AppendLine("  \"targetPrerequisites\": {");
            AppendProperty(builder, "cpuTypeIdentifier", TargetPrerequisites?.CpuTypeIdentifier, true, true, 4);
            AppendProperty(builder, "requiredSystemMemory", TargetPrerequisites?.RequiredSystemMemory ? "true" : "false", false, true, 4);
            AppendProperty(builder, "requiredClockMemory", TargetPrerequisites?.RequiredClockMemory ? "true" : "false", false, true, 4);
            AppendProperty(builder, "expectedSystemMemoryAddress", TargetPrerequisites?.ExpectedSystemMemoryAddress, true, true, 4);
            AppendProperty(builder, "expectedClockMemoryAddress", TargetPrerequisites?.ExpectedClockMemoryAddress, true, false, 4);
            builder.AppendLine("  },");
            AppendProperty(builder, "contractHash", ContractHash, true, true);
            AppendProperty(builder, "discoveredAt", DiscoveredAt.ToString("o"), true, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        public static ValveProfileContract Deserialize(string json)
        {
            var result = new ValveProfileContract();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool inValveBlock = false;
            bool inParameters = false;
            bool inDependencies = false;
            bool inInstanceData = false;
            bool inTargetPrerequisites = false;
            var currentParam = new ValveParameter();
            var currentDep = new DependencyIdentity();
            int paramIndex = -1;
            int depIndex = -1;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"schemaVersion\":"))
                    result.SchemaVersion = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"profileIdentity\":"))
                    result.ProfileIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"sourceArchiveSha256\":"))
                    result.SourceArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"tiaBuildIdentity\":"))
                    result.TiaBuildIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualificationIdentity\":"))
                    result.QualificationIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"cpuTypeIdentifier\":"))
                    result.CpuTypeIdentifier = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"valveBlock\":"))
                    inValveBlock = true;
                else if (trimmed.StartsWith("\"dependencies\":"))
                {
                    inValveBlock = false;
                    inDependencies = true;
                }
                else if (trimmed.StartsWith("\"instanceData\":"))
                {
                    inDependencies = false;
                    inInstanceData = true;
                }
                else if (trimmed.StartsWith("\"targetPrerequisites\":"))
                {
                    inInstanceData = false;
                    inTargetPrerequisites = true;
                }
                else if (trimmed.StartsWith("\"contractHash\":"))
                {
                    inTargetPrerequisites = false;
                    result.ContractHash = ExtractValue(trimmed);
                }
                else if (trimmed.StartsWith("\"discoveredAt\":"))
                {
                    DateTimeOffset.TryParse(ExtractValue(trimmed), out result.DiscoveredAt);
                }

                if (inValveBlock && !inParameters && !inDependencies && !inInstanceData && !inTargetPrerequisites)
                {
                    if (trimmed.StartsWith("\"name\":"))
                        result.ValveBlock = new ValveBlockIdentity { Name = ExtractValue(trimmed) };
                    else if (trimmed.StartsWith("\"typeName\":"))
                        result.ValveBlock.TypeName = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"version\":"))
                        result.ValveBlock.Version = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"namespace\":"))
                        result.ValveBlock.Namespace = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"parameters\":"))
                        inParameters = true;
                }

                if (inParameters)
                {
                    if (trimmed == "{")
                    {
                        currentParam = new ValveParameter();
                        paramIndex++;
                    }
                    else if (trimmed.StartsWith("\"name\":"))
                        currentParam.Name = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"direction\":"))
                        currentParam.Direction = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"plcType\":"))
                        currentParam.PlcType = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"description\":"))
                        currentParam.Description = ExtractValue(trimmed);
                    else if (trimmed.Contains("}"))
                    {
                        if (result.ValveBlock.Parameters == null)
                            result.ValveBlock.Parameters = new List<ValveParameter>();
                        result.ValveBlock.Parameters.Add(currentParam);
                    }
                }

                if (inDependencies)
                {
                    if (trimmed == "{")
                    {
                        currentDep = new DependencyIdentity();
                        depIndex++;
                    }
                    else if (trimmed.StartsWith("\"name\":"))
                        currentDep.Name = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"typeName\":"))
                        currentDep.TypeName = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"version\":"))
                        currentDep.Version = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"namespace\":"))
                        currentDep.Namespace = ExtractValue(trimmed);
                    else if (trimmed.Contains("}"))
                    {
                        result.Dependencies.Add(currentDep);
                    }
                }

                if (inInstanceData)
                {
                    if (trimmed.StartsWith("\"requiresInstanceDb\":"))
                        result.InstanceData = new InstanceDataRequirements { RequiresInstanceDb = ExtractValue(trimmed).Contains("true") };
                    else if (result.InstanceData != null && trimmed.StartsWith("\"instanceDbName\":"))
                        result.InstanceData.InstanceDbName = ExtractValue(trimmed);
                    else if (result.InstanceData != null && trimmed.StartsWith("\"dataBlockAccessMode\":"))
                        result.InstanceData.DataBlockAccessMode = ExtractValue(trimmed);
                    else if (result.InstanceData != null && trimmed.StartsWith("\"multiInstancePath\":"))
                        result.InstanceData.MultiInstancePath = ExtractValue(trimmed);
                }

                if (inTargetPrerequisites)
                {
                    if (trimmed.StartsWith("\"cpuTypeIdentifier\":"))
                        result.TargetPrerequisites = new TargetPrerequisites { CpuTypeIdentifier = ExtractValue(trimmed) };
                    else if (result.TargetPrerequisites != null && trimmed.StartsWith("\"requiredSystemMemory\":"))
                        result.TargetPrerequisites.RequiredSystemMemory = ExtractValue(trimmed).Contains("true");
                    else if (result.TargetPrerequisites != null && trimmed.StartsWith("\"requiredClockMemory\":"))
                        result.TargetPrerequisites.RequiredClockMemory = ExtractValue(trimmed).Contains("true");
                    else if (result.TargetPrerequisites != null && trimmed.StartsWith("\"expectedSystemMemoryAddress\":"))
                        result.TargetPrerequisites.ExpectedSystemMemoryAddress = ExtractValue(trimmed);
                    else if (result.TargetPrerequisites != null && trimmed.StartsWith("\"expectedClockMemoryAddress\":"))
                        result.TargetPrerequisites.ExpectedClockMemoryAddress = ExtractValue(trimmed);
                }
            }

            return result;
        }

        private static string ExtractValue(string line)
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex < 0) return null;
            string value = line.Substring(colonIndex + 1).Trim();
            if (value.EndsWith(","))
                value = value.Substring(0, value.Length - 1).Trim();
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2);
            return Unescape(value);
        }

        private static string Unescape(string value)
        {
            if (value == null) return null;
            return value
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\r", "\r")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
        }

        private static void AppendProperty(StringBuilder builder, string name, string value, bool quote, bool comma, int indent = 2)
        {
            builder.Append(' ', indent);
            builder.Append('"').Append(Escape(name)).Append("\": ");
            if (value == null)
                builder.Append("null");
            else if (quote)
                builder.Append('"').Append(Escape(value)).Append('"');
            else
                builder.Append(value);

            if (comma)
                builder.Append(',');
            builder.AppendLine();
        }

        private static string Escape(string value)
        {
            if (value == null)
                return null;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }

    internal sealed class ValveBlockIdentity
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }
        public List<ValveParameter> Parameters { get; set; } = new List<ValveParameter>();

        public string GetIdentityString()
        {
            return $"{Namespace}.{TypeName} v{Version}";
        }
    }

    internal sealed class ValveParameter
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public string PlcType { get; set; }
        public string Description { get; set; }
    }

    internal sealed class DependencyIdentity
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }

        public string GetIdentityString()
        {
            return $"{Namespace}.{TypeName} v{Version}";
        }
    }

    internal sealed class InstanceDataRequirements
    {
        public bool RequiresInstanceDb { get; set; }
        public string InstanceDbName { get; set; }
        public string DataBlockAccessMode { get; set; }
        public string MultiInstancePath { get; set; }

        public string GetIdentityString()
        {
            return $"InstanceDb={RequiresInstanceDb};Name={InstanceDbName};Mode={DataBlockAccessMode};Path={MultiInstancePath}";
        }
    }

    internal sealed class TargetPrerequisites
    {
        public string CpuTypeIdentifier { get; set; }
        public bool RequiredSystemMemory { get; set; }
        public bool RequiredClockMemory { get; set; }
        public string ExpectedSystemMemoryAddress { get; set; }
        public string ExpectedClockMemoryAddress { get; set; }

        public string GetIdentityString()
        {
            return $"CPU={CpuTypeIdentifier};SysMem={RequiredSystemMemory};ClkMem={RequiredClockMemory};SysAddr={ExpectedSystemMemoryAddress};ClkAddr={ExpectedClockMemoryAddress}";
        }
    }
}