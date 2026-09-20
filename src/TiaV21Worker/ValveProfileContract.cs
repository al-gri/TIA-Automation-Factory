using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal sealed class ValveParameterContract
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public string PlcType { get; set; }
    }

    internal sealed class ValveDependencyContract
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Version { get; set; }
    }

    internal sealed class ValveProfileContract
    {
        public int SchemaVersion { get; set; } = 1;
        public string ProfileIdentity { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string TiaBuildIdentity { get; set; }
        public string CpuTypeIdentifier { get; set; }
        public string ValveBlockName { get; set; }
        public string ValveTypeName { get; set; }
        public string ValveVersion { get; set; }
        public List<ValveParameterContract> Parameters { get; set; } = new List<ValveParameterContract>();
        public List<ValveDependencyContract> Dependencies { get; set; } = new List<ValveDependencyContract>();
        public string InstanceDataOwnership { get; set; }
        public string DbPrerequisites { get; set; }
        public string ContractHash { get; set; }

        public static string ComputeContractHash(ValveProfileContract contract)
        {
            using (var sha256 = SHA256.Create())
            {
                var builder = new StringBuilder();
                builder.Append(contract.ProfileIdentity ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.SourceArchiveSha256 ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.TiaBuildIdentity ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.CpuTypeIdentifier ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.ValveBlockName ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.ValveTypeName ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.ValveVersion ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.InstanceDataOwnership ?? string.Empty);
                builder.Append('|');
                builder.Append(contract.DbPrerequisites ?? string.Empty);
                builder.Append('|');

                var sortedParams = new List<ValveParameterContract>(contract.Parameters);
                sortedParams.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                foreach (var p in sortedParams)
                {
                    builder.Append(p.Name).Append(':').Append(p.Direction).Append(':').Append(p.PlcType).Append(';');
                }
                builder.Append('|');

                var sortedDeps = new List<ValveDependencyContract>(contract.Dependencies);
                sortedDeps.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                foreach (var d in sortedDeps)
                {
                    builder.Append(d.Name).Append(':').Append(d.TypeName).Append(':').Append(d.Version).Append(';');
                }

                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 32);
            }
        }
    }

    internal static class ValveProfileContractJson
    {
        public static string Serialize(ValveProfileContract contract)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "schemaVersion", contract.SchemaVersion.ToString(), false, true);
            AppendProperty(builder, "profileIdentity", contract.ProfileIdentity, true, true);
            AppendProperty(builder, "sourceArchiveSha256", contract.SourceArchiveSha256, true, true);
            AppendProperty(builder, "tiaBuildIdentity", contract.TiaBuildIdentity, true, true);
            AppendProperty(builder, "cpuTypeIdentifier", contract.CpuTypeIdentifier, true, true);
            AppendProperty(builder, "valveBlockName", contract.ValveBlockName, true, true);
            AppendProperty(builder, "valveTypeName", contract.ValveTypeName, true, true);
            AppendProperty(builder, "valveVersion", contract.ValveVersion, true, true);
            AppendProperty(builder, "instanceDataOwnership", contract.InstanceDataOwnership, true, true);
            AppendProperty(builder, "dbPrerequisites", contract.DbPrerequisites, true, true);
            AppendProperty(builder, "contractHash", contract.ContractHash, true, true);

            builder.AppendLine("  \"parameters\": [");
            for (int i = 0; i < contract.Parameters.Count; i++)
            {
                var p = contract.Parameters[i];
                builder.AppendLine("    {");
                AppendProperty(builder, "name", p.Name, true, true, 6);
                AppendProperty(builder, "direction", p.Direction, true, true, 6);
                AppendProperty(builder, "plcType", p.PlcType, true, false, 6);
                builder.Append("    }");
                builder.AppendLine(i + 1 < contract.Parameters.Count ? "," : string.Empty);
            }
            builder.AppendLine("  ],");

            builder.AppendLine("  \"dependencies\": [");
            for (int i = 0; i < contract.Dependencies.Count; i++)
            {
                var d = contract.Dependencies[i];
                builder.AppendLine("    {");
                AppendProperty(builder, "name", d.Name, true, true, 6);
                AppendProperty(builder, "typeName", d.TypeName, true, true, 6);
                AppendProperty(builder, "version", d.Version, true, false, 6);
                builder.Append("    }");
                builder.AppendLine(i + 1 < contract.Dependencies.Count ? "," : string.Empty);
            }
            builder.AppendLine("  ]");

            builder.AppendLine("}");
            return builder.ToString();
        }

        public static ValveProfileContract Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be null or empty", nameof(json));

            var contract = new ValveProfileContract();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            bool inParameters = false;
            bool inDependencies = false;
            ValveParameterContract currentParam = null;
            ValveDependencyContract currentDep = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("\"schemaVersion\":"))
                {
                    string value = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(value) && !int.TryParse(value, out int sv))
                        throw new FormatException("Invalid schemaVersion value");
                    contract.SchemaVersion = sv;
                }
                else if (trimmed.StartsWith("\"profileIdentity\":"))
                    contract.ProfileIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"sourceArchiveSha256\":"))
                    contract.SourceArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"tiaBuildIdentity\":"))
                    contract.TiaBuildIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"cpuTypeIdentifier\":"))
                    contract.CpuTypeIdentifier = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"valveBlockName\":"))
                    contract.ValveBlockName = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"valveTypeName\":"))
                    contract.ValveTypeName = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"valveVersion\":"))
                    contract.ValveVersion = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"instanceDataOwnership\":"))
                    contract.InstanceDataOwnership = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"dbPrerequisites\":"))
                    contract.DbPrerequisites = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"contractHash\":"))
                    contract.ContractHash = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"parameters\":"))
                    inParameters = true;
                else if (trimmed.StartsWith("\"dependencies\":"))
                {
                    inParameters = false;
                    inDependencies = true;
                }
                else if (inParameters && trimmed.StartsWith("{"))
                {
                    currentParam = new ValveParameterContract();
                }
                else if (inParameters && currentParam != null)
                {
                    if (trimmed.StartsWith("\"name\":"))
                        currentParam.Name = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"direction\":"))
                        currentParam.Direction = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"plcType\":"))
                        currentParam.PlcType = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("}"))
                    {
                        if (string.IsNullOrEmpty(currentParam.Name) || string.IsNullOrEmpty(currentParam.Direction) || string.IsNullOrEmpty(currentParam.PlcType))
                            throw new FormatException("Incomplete parameter entry");
                        contract.Parameters.Add(currentParam);
                        currentParam = null;
                    }
                }
                else if (inDependencies && trimmed.StartsWith("{"))
                {
                    currentDep = new ValveDependencyContract();
                }
                else if (inDependencies && currentDep != null)
                {
                    if (trimmed.StartsWith("\"name\":"))
                        currentDep.Name = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"typeName\":"))
                        currentDep.TypeName = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"version\":"))
                        currentDep.Version = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("}"))
                    {
                        if (string.IsNullOrEmpty(currentDep.Name) || string.IsNullOrEmpty(currentDep.TypeName) || string.IsNullOrEmpty(currentDep.Version))
                            throw new FormatException("Incomplete dependency entry");
                        contract.Dependencies.Add(currentDep);
                        currentDep = null;
                    }
                }
            }

            if (contract.SchemaVersion != 1)
                throw new FormatException("Unsupported schema version: " + contract.SchemaVersion);

            if (string.IsNullOrEmpty(contract.ProfileIdentity) ||
                string.IsNullOrEmpty(contract.SourceArchiveSha256) ||
                string.IsNullOrEmpty(contract.TiaBuildIdentity) ||
                string.IsNullOrEmpty(contract.CpuTypeIdentifier) ||
                string.IsNullOrEmpty(contract.ValveBlockName) ||
                string.IsNullOrEmpty(contract.ValveTypeName) ||
                string.IsNullOrEmpty(contract.ValveVersion) ||
                string.IsNullOrEmpty(contract.InstanceDataOwnership) ||
                string.IsNullOrEmpty(contract.DbPrerequisites) ||
                string.IsNullOrEmpty(contract.ContractHash))
            {
                throw new FormatException("Missing required contract fields");
            }

            return contract;
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
}