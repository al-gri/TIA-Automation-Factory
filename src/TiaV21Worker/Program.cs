using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.ExternalSources;
using Siemens.Engineering.Library;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal static class Program
    {
        private const string OpennessFolder = @"C:\Program Files\Siemens\Automation\Portal V21\PublicAPI\V21\net48";

        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public static int Main(string[] args)
        {
            if (args.Length >= 1 && string.Equals(args[0], "qualify-library", StringComparison.OrdinalIgnoreCase))
            {
                return RunQualifyLibrary(args);
            }

            if (args.Length < 2 || args.Length > 3)
            {
                Console.Error.WriteLine("Usage: TiaV21Worker <absolute-source-file> <absolute-diagnostics-json> [absolute-work-root]");
                Console.Error.WriteLine("       TiaV21Worker qualify-library <absolute-source-zal19> <absolute-qualification-output-root> <absolute-manifest-json> [absolute-work-root]");
                return 64;
            }

            string sourcePath = Path.GetFullPath(args[0]);
            string diagnosticsPath = Path.GetFullPath(args[1]);
            string workRoot = args.Length == 3
                ? Path.GetFullPath(args[2])
                : Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TiaAutomationFactory"));

            Directory.CreateDirectory(Path.GetDirectoryName(diagnosticsPath));
            Directory.CreateDirectory(workRoot);

            WorkerResult result;
            try
            {
                result = new TiaSmokeTest().Run(sourcePath, workRoot);
            }
            catch (Exception exception)
            {
                result = WorkerResult.FromException(exception);
            }

            File.WriteAllText(diagnosticsPath, ResultJson.Serialize(result), new UTF8Encoding(false));
            Console.WriteLine(File.ReadAllText(diagnosticsPath));
            return result.Success ? 0 : 1;
        }

        private static int RunQualifyLibrary(string[] args)
        {
            if (args.Length < 4 || args.Length > 5)
            {
                Console.Error.WriteLine("Usage: TiaV21Worker qualify-library <absolute-source-zal19> <absolute-qualification-output-root> <absolute-manifest-json> [absolute-work-root]");
                return 64;
            }

            string sourceArchivePathRaw = args[1];
            string qualificationOutputRootRaw = args[2];
            string manifestPathRaw = args[3];
            string workRootRaw = args.Length == 5 ? args[4] : Path.Combine(Path.GetTempPath(), "TiaAutomationFactory");

            if (!Path.IsPathRooted(sourceArchivePathRaw) || !Path.IsPathRooted(qualificationOutputRootRaw) || !Path.IsPathRooted(manifestPathRaw) || !Path.IsPathRooted(workRootRaw))
            {
                Console.Error.WriteLine("All paths must be absolute.");
                return 64;
            }

            string sourceArchivePath = Path.GetFullPath(sourceArchivePathRaw);
            string qualificationOutputRoot = Path.GetFullPath(qualificationOutputRootRaw);
            string manifestPath = Path.GetFullPath(manifestPathRaw);
            string workRoot = Path.GetFullPath(workRootRaw);

            if (!string.Equals(Path.GetExtension(sourceArchivePath), ".zal19", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Source archive must have .zal19 extension.");
                return 64;
            }

            if (!File.Exists(sourceArchivePath))
            {
                Console.Error.WriteLine("Source archive not found: " + sourceArchivePath);
                return 64;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
            Directory.CreateDirectory(qualificationOutputRoot);
            Directory.CreateDirectory(workRoot);

            QualificationResult result;
            try
            {
                result = new LibraryQualifier().Qualify(sourceArchivePath, qualificationOutputRoot, workRoot);
            }
            catch (Exception exception)
            {
                result = QualificationResult.FromException(exception, sourceArchivePath);
            }

            File.WriteAllText(manifestPath, QualificationJson.Serialize(result), new UTF8Encoding(false));
            Console.WriteLine(File.ReadAllText(manifestPath));
            return result.Success ? 0 : 1;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName requestedAssemblyName = new AssemblyName(args.Name);
            string filePath = Path.Combine(OpennessFolder, requestedAssemblyName.Name + ".dll");

            if (!requestedAssemblyName.Name.StartsWith("Siemens.Engineering.", StringComparison.Ordinal) || !File.Exists(filePath))
                return null;

            Assembly loadedAssembly = Assembly.LoadFrom(filePath);
            if (!string.Equals(requestedAssemblyName.FullName, loadedAssembly.GetName().FullName, StringComparison.Ordinal))
                throw new FileNotFoundException("TIA Portal Openness assembly version does not match the referenced version.", filePath);

            return loadedAssembly;
        }
    }

    internal sealed class TiaSmokeTest
    {
        private const string CpuTypeIdentifier = "OrderNumber:6ES7 516-3AP03-0AB0/V4.0";

        public WorkerResult Run(string sourcePath, string workRoot)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Generated PLC source file was not found.", sourcePath);

            string runName = "Smoke_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string projectsPath = Path.GetFullPath(Path.Combine(workRoot, "projects"));
            if (!Path.IsPathRooted(projectsPath))
                throw new InvalidOperationException("TIA project target directory is not absolute: " + projectsPath);

            Directory.CreateDirectory(projectsPath);
            DirectoryInfo projectsDirectory = new DirectoryInfo(projectsPath);

            Console.WriteLine("TIA work root: " + workRoot);
            Console.WriteLine("TIA project target directory: " + projectsDirectory.FullName);
            Console.WriteLine("TIA project target rooted: " + Path.IsPathRooted(projectsDirectory.FullName));

            var output = new WorkerResult();

            using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithUserInterface))
            using (ExclusiveAccess exclusiveAccess = portal.ExclusiveAccess("TIA Automation Factory smoke test"))
            {
                Project project = null;
                try
                {
                    project = portal.Projects.Create(new DirectoryInfo(projectsDirectory.FullName), runName);
                    output.ProjectPath = project.Path.FullName;

                    Device station = project.Devices.CreateWithItem(CpuTypeIdentifier, "PLC_1", "S7_1500_Station_1");
                    PlcSoftware plcSoftware = GetPlcSoftware(station);
                    if (plcSoftware == null)
                        throw new InvalidOperationException("No PlcSoftware target was found in the generated S7-1500 station.");

                    PlcExternalSource externalSource = plcSoftware.ExternalSourceGroup.ExternalSources.CreateFromFile(
                        "AutomationFactorySource",
                        sourcePath);

                    externalSource.GenerateBlocksFromSource();

                    ICompilable compilable = plcSoftware.GetService<ICompilable>();
                    if (compilable == null)
                        throw new InvalidOperationException("The PLC software does not expose the ICompilable service.");

                    CompilerResult compilerResult = compilable.Compile();
                    output.State = compilerResult.State.ToString();
                    output.WarningCount = compilerResult.WarningCount;
                    output.ErrorCount = compilerResult.ErrorCount;
                    AddMessages(compilerResult.Messages, output.Diagnostics);
                    output.Success = compilerResult.ErrorCount == 0;

                    return output;
                }
                finally
                {
                    if (project != null)
                        project.Close();
                }
            }
        }

        private static PlcSoftware GetPlcSoftware(HardwareObject hardwareObject)
        {
            var queue = new Queue<HardwareObject>();
            queue.Enqueue(hardwareObject);

            while (queue.Count > 0)
            {
                foreach (DeviceItem deviceItem in queue.Dequeue().Items)
                {
                    if (!deviceItem.IsPlugged)
                        continue;

                    SoftwareContainer softwareContainer = deviceItem.GetService<SoftwareContainer>();
                    if (deviceItem.Classification.HasFlag(DeviceItemClassifications.CPU)
                        && softwareContainer != null
                        && softwareContainer.Software is PlcSoftware)
                    {
                        return (PlcSoftware)softwareContainer.Software;
                    }

                    queue.Enqueue(deviceItem);
                }
            }

            return null;
        }

        private static void AddMessages(CompilerResultMessageComposition messages, IList<DiagnosticRecord> target)
        {
            foreach (CompilerResultMessage message in messages)
            {
                target.Add(new DiagnosticRecord
                {
                    Path = Convert.ToString(message.Path),
                    State = message.State.ToString(),
                    Description = message.Description,
                    WarningCount = message.WarningCount,
                    ErrorCount = message.ErrorCount
                });

                AddMessages(message.Messages, target);
            }
        }
    }

    internal sealed class WorkerResult
    {
        public bool Success { get; set; }
        public string ProjectPath { get; set; }
        public string State { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public string Failure { get; set; }
        public List<DiagnosticRecord> Diagnostics { get; } = new List<DiagnosticRecord>();

        public static WorkerResult FromException(Exception exception)
        {
            return new WorkerResult
            {
                Success = false,
                State = "Exception",
                ErrorCount = 1,
                Failure = exception.ToString()
            };
        }
    }

    internal sealed class DiagnosticRecord
    {
        public string Path { get; set; }
        public string State { get; set; }
        public string Description { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
    }

    internal static class ResultJson
    {
        public static string Serialize(WorkerResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "success", result.Success ? "true" : "false", false, true);
            AppendProperty(builder, "projectPath", result.ProjectPath, true, true);
            AppendProperty(builder, "state", result.State, true, true);
            AppendProperty(builder, "warnings", result.WarningCount.ToString(), false, true);
            AppendProperty(builder, "errors", result.ErrorCount.ToString(), false, true);
            AppendProperty(builder, "failure", result.Failure, true, true);
            builder.AppendLine("  \"diagnostics\": [");

            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                DiagnosticRecord item = result.Diagnostics[i];
                builder.AppendLine("    {");
                AppendProperty(builder, "path", item.Path, true, true, 6);
                AppendProperty(builder, "state", item.State, true, true, 6);
                AppendProperty(builder, "description", item.Description, true, true, 6);
                AppendProperty(builder, "warnings", item.WarningCount.ToString(), false, true, 6);
                AppendProperty(builder, "errors", item.ErrorCount.ToString(), false, false, 6);
                builder.Append("    }");
                builder.AppendLine(i + 1 < result.Diagnostics.Count ? "," : string.Empty);
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
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

    internal sealed class LibraryQualifier
    {
        public QualificationResult Qualify(string sourceArchivePath, string qualificationOutputRoot, string workRoot)
        {
            string sourceBasename = Path.GetFileName(sourceArchivePath);
            string sourceSha256 = ComputeSha256(sourceArchivePath);
            string tiaBuildIdentity = GetTiaBuildIdentity();

            string qualificationIdentity = DeriveQualificationIdentity(sourceSha256, tiaBuildIdentity);
            string qualifiedArchiveName = qualificationIdentity + ".zal21";
            string qualifiedArchivePath = Path.Combine(qualificationOutputRoot, qualifiedArchiveName);

            var manifest = new QualificationResult
            {
                SourceArchiveBasename = sourceBasename,
                SourceArchiveSha256 = sourceSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                QualificationIdentity = qualificationIdentity,
                QualifiedArchiveName = qualifiedArchiveName,
                Success = false
            };

            if (File.Exists(qualifiedArchivePath))
            {
                string existingArchiveSha256 = ComputeSha256(qualifiedArchivePath);
                string existingManifestPath = Path.ChangeExtension(qualifiedArchivePath, ".manifest.json");
                if (File.Exists(existingManifestPath))
                {
                    var existingManifest = QualificationJson.Deserialize(File.ReadAllText(existingManifestPath));
                    if (existingManifest.QualificationIdentity == qualificationIdentity &&
                        existingManifest.SourceArchiveSha256 == sourceSha256 &&
                        existingManifest.TiaBuildIdentity == tiaBuildIdentity &&
                        existingManifest.QualifiedArchiveSha256 == existingArchiveSha256)
                    {
                        manifest.Success = true;
                        manifest.QualifiedArchiveSha256 = existingArchiveSha256;
                        manifest.NativeReopenSuccess = true;
                        manifest.NativeReopenDetails = "Already qualified; existing archive matches identity and hash.";
                        return manifest;
                    }
                }

                manifest.Failure = "Qualification identity conflict: archive exists with different hash or manifest mismatch.";
                manifest.FailureDetails = "Existing qualified archive at " + qualifiedArchivePath + " has SHA256 " + existingArchiveSha256 + " but current qualification requires identity " + qualificationIdentity + ".";
                return manifest;
            }

            string retrieveWorkPath = Path.Combine(workRoot, "retrieve_" + qualificationIdentity);
            Directory.CreateDirectory(retrieveWorkPath);

            using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithUserInterface))
            using (ExclusiveAccess exclusiveAccess = portal.ExclusiveAccess("TIA Automation Factory library qualification"))
            {
                UserGlobalLibrary userGlobalLibrary = null;
                try
                {
                    userGlobalLibrary = portal.GlobalLibraries.RetrieveWithUpgrade(
                        new FileInfo(sourceArchivePath),
                        new DirectoryInfo(retrieveWorkPath),
                        OpenMode.ReadWrite);

                    if (userGlobalLibrary == null)
                    {
                        manifest.Failure = "RetrieveWithUpgrade returned null; V19 archive could not be upgraded.";
                        manifest.FailureDetails = "Source: " + sourceBasename + ", SHA256: " + sourceSha256;
                        return manifest;
                    }

                    userGlobalLibrary.Save();

                    userGlobalLibrary.Archive(new DirectoryInfo(qualificationOutputRoot), qualifiedArchiveName, LibraryArchivationMode.Compressed);
                }
                finally
                {
                    if (userGlobalLibrary != null)
                    {
                        try { userGlobalLibrary.Close(); } catch { }
                    }
                }
            }

            string qualifiedArchiveSha256 = ComputeSha256(qualifiedArchivePath);
            manifest.QualifiedArchiveSha256 = qualifiedArchiveSha256;

            string verifyWorkPath = Path.Combine(workRoot, "verify_" + qualificationIdentity);
            Directory.CreateDirectory(verifyWorkPath);

            UserGlobalLibrary verifiedLibrary = null;
            bool nativeReopenSuccess = false;
            string nativeReopenDetails = "";
            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithUserInterface))
                using (ExclusiveAccess exclusiveAccess = portal.ExclusiveAccess("TIA Automation Factory library qualification verification"))
                {
                    verifiedLibrary = portal.GlobalLibraries.Retrieve(
                        new FileInfo(qualifiedArchivePath),
                        new DirectoryInfo(verifyWorkPath),
                        OpenMode.ReadWrite);

                    if (verifiedLibrary == null)
                    {
                        nativeReopenSuccess = false;
                        nativeReopenDetails = "Current-version Retrieve returned null; produced archive is not a valid native V21 library.";
                    }
                    else
                    {
                        nativeReopenSuccess = true;
                        nativeReopenDetails = "Successfully reopened with current-version GlobalLibraries.Retrieve.";
                    }

                    if (verifiedLibrary != null)
                    {
                        try { verifiedLibrary.Close(); } catch { }
                        verifiedLibrary = null;
                    }
                }
            }
            finally
            {
                if (verifiedLibrary != null)
                {
                    try { verifiedLibrary.Close(); } catch { }
                }
            }

            manifest.NativeReopenSuccess = nativeReopenSuccess;
            manifest.NativeReopenDetails = nativeReopenDetails;

            if (!nativeReopenSuccess)
            {
                manifest.Failure = "Native reopen verification failed.";
                return manifest;
            }

            manifest.Success = true;

            string manifestPath = Path.ChangeExtension(qualifiedArchivePath, ".manifest.json");
            File.WriteAllText(manifestPath, QualificationJson.Serialize(manifest), new UTF8Encoding(false));

            return manifest;
        }

        private static string ComputeSha256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string GetTiaBuildIdentity()
        {
            var assembly = typeof(TiaPortal).Assembly;
            var name = assembly.GetName();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            return "Siemens.Engineering v" + name.Version + " (file: " + fileVersion.FileVersion + ")";
        }

        private static string DeriveQualificationIdentity(string sourceSha256, string tiaBuildIdentity)
        {
            string combined = sourceSha256 + "|" + tiaBuildIdentity;
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return "olq-" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 32);
            }
        }
    }

    internal sealed class QualificationResult
    {
        public bool Success { get; set; }
        public string SourceArchiveBasename { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string TiaBuildIdentity { get; set; }
        public string QualificationIdentity { get; set; }
        public string QualifiedArchiveName { get; set; }
        public string QualifiedArchiveSha256 { get; set; }
        public bool NativeReopenSuccess { get; set; }
        public string NativeReopenDetails { get; set; }
        public string Failure { get; set; }
        public string FailureDetails { get; set; }

        public static QualificationResult FromException(Exception exception, string sourceArchivePath)
        {
            string sourceBasename = Path.GetFileName(sourceArchivePath);
            string sourceSha256 = "";
            try { sourceSha256 = ComputeSha256Static(sourceArchivePath); } catch { }

            return new QualificationResult
            {
                Success = false,
                SourceArchiveBasename = sourceBasename,
                SourceArchiveSha256 = sourceSha256,
                TiaBuildIdentity = GetTiaBuildIdentityStatic(),
                Failure = "Exception",
                FailureDetails = exception.ToString()
            };
        }

        private static string ComputeSha256Static(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string GetTiaBuildIdentityStatic()
        {
            var assembly = typeof(TiaPortal).Assembly;
            var name = assembly.GetName();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            return "Siemens.Engineering v" + name.Version + " (file: " + fileVersion.FileVersion + ")";
        }
    }

    internal static class QualificationJson
    {
        public static string Serialize(QualificationResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "success", result.Success ? "true" : "false", false, true);
            AppendProperty(builder, "sourceArchiveBasename", result.SourceArchiveBasename, true, true);
            AppendProperty(builder, "sourceArchiveSha256", result.SourceArchiveSha256, true, true);
            AppendProperty(builder, "tiaBuildIdentity", result.TiaBuildIdentity, true, true);
            AppendProperty(builder, "qualificationIdentity", result.QualificationIdentity, true, true);
            AppendProperty(builder, "qualifiedArchiveName", result.QualifiedArchiveName, true, true);
            AppendProperty(builder, "qualifiedArchiveSha256", result.QualifiedArchiveSha256, true, true);
            AppendProperty(builder, "nativeReopenSuccess", result.NativeReopenSuccess ? "true" : "false", false, true);
            AppendProperty(builder, "nativeReopenDetails", result.NativeReopenDetails, true, true);
            AppendProperty(builder, "failure", result.Failure, true, true);
            AppendProperty(builder, "failureDetails", result.FailureDetails, true, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        public static QualificationResult Deserialize(string json)
        {
            var result = new QualificationResult();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"success\":"))
                    result.Success = trimmed.Contains("true");
                else if (trimmed.StartsWith("\"sourceArchiveBasename\":"))
                    result.SourceArchiveBasename = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"sourceArchiveSha256\":"))
                    result.SourceArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"tiaBuildIdentity\":"))
                    result.TiaBuildIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualificationIdentity\":"))
                    result.QualificationIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveName\":"))
                    result.QualifiedArchiveName = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveSha256\":"))
                    result.QualifiedArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"nativeReopenSuccess\":"))
                    result.NativeReopenSuccess = trimmed.Contains("true");
                else if (trimmed.StartsWith("\"nativeReopenDetails\":"))
                    result.NativeReopenDetails = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"failure\":"))
                    result.Failure = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"failureDetails\":"))
                    result.FailureDetails = ExtractValue(trimmed);
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
}
