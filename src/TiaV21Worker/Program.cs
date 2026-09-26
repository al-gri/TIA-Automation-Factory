using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            // Returns exit code 1 when result.Success is false; 0 on success.
            // RunQualifyLibrary already returns nonzero for Success=false; no workflow change is included
            // to chase the run1ExitCode observation from trusted run 35430943663.
            if (args.Length < 4 || args.Length > 5)
            {
                Console.Error.WriteLine("Usage: TiaV21Worker qualify-library <absolute-source-zal19> <absolute-qualification-output-root> <absolute-manifest-json> [absolute-work-root]");
                return 64;
            }

            string sourceArchivePathRaw = args[1];
            string qualificationOutputRootRaw = args[2];
            string manifestPathRaw = args[3];
            string workRootRaw = args.Length == 5 ? args[4] : Path.Combine(Path.GetTempPath(), "TiaAutomationFactory");

            if (!IsFullyQualifiedAbsolutePath(sourceArchivePathRaw) || !IsFullyQualifiedAbsolutePath(qualificationOutputRootRaw) || !IsFullyQualifiedAbsolutePath(manifestPathRaw) || !IsFullyQualifiedAbsolutePath(workRootRaw))
            {
                Console.Error.WriteLine("All paths must be absolute (drive-absolute X:\\... or UNC \\\\server\\share\\...).");
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

        private static bool IsFullyQualifiedAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.StartsWith(@"\\"))
                return true;

            if (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\')
                return true;

            return false;
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

            var whitelistResult = WhitelistManager.SynchronizeWhitelist();
            if (!whitelistResult.Success)
            {
                if (whitelistResult.BootstrapRequired)
                {
                    output.Success = false;
                    output.State = "BootstrapRequired";
                    output.ErrorCount = 1;
                    output.Failure = whitelistResult.Message;
                    return output;
                }
                output.Success = false;
                output.State = "WhitelistSyncFailed";
                output.ErrorCount = 1;
                output.Failure = whitelistResult.Message ?? "Whitelist synchronization failed.";
                return output;
            }

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
        private enum QualificationPhase
        {
            RetrieveWithUpgrade,
            Save,
            Archive,
            UpgradeClose,
            NativeRetrieve,
            NativeClose
        }

        public QualificationResult Qualify(string sourceArchivePath, string qualificationOutputRoot, string workRoot)
        {
            string sourceBasename = Path.GetFileName(sourceArchivePath);
            string sourceSha256 = ComputeSha256(sourceArchivePath);
            string tiaBuildIdentity = GetTiaBuildIdentity();
            string recipeIdentity = QualificationState.CurrentRecipeIdentity;

            string qualificationIdentity = DeriveQualificationIdentity(
                sourceSha256,
                tiaBuildIdentity,
                recipeIdentity);
            string qualifiedArchiveName = qualificationIdentity + ".zal21";
            string qualifiedArchivePath = Path.Combine(qualificationOutputRoot, qualifiedArchiveName);

            var stateStore = new QualificationStateStore(
                qualificationOutputRoot,
                qualificationIdentity);

            var manifest = new QualificationResult
            {
                SourceArchiveBasename = sourceBasename,
                SourceArchiveSha256 = sourceSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                QualificationIdentity = qualificationIdentity,
                QualificationRecipeIdentity = recipeIdentity,
                QualifiedArchiveName = qualifiedArchiveName,
                IsReuse = false,
                NativeReopenSuccess = false,
                Success = false
            };

            QualificationState existingCompleted;
            if (stateStore.TryLoadCompleted(out existingCompleted))
            {
                if (!File.Exists(qualifiedArchivePath))
                {
                    manifest.Failure = "phase:reuse-validation type:FileNotFoundException hresult:0x80070002";
                    manifest.FailureDetails = "Verified completed state exists but its qualified archive is unavailable.";
                    return manifest;
                }

                string existingArchiveSha256 = ComputeSha256(qualifiedArchivePath);
                if (!QualificationStateStore.MatchesReuseIdentity(
                    existingCompleted,
                    qualificationIdentity,
                    sourceSha256,
                    tiaBuildIdentity,
                    recipeIdentity,
                    existingArchiveSha256))
                {
                    manifest.Failure = "phase:reuse-validation type:InvalidOperationException hresult:0x80131509";
                    manifest.FailureDetails = "Verified completed evidence does not match the current TXN-002 identity.";
                    return manifest;
                }

                manifest.Success = true;
                manifest.IsReuse = true;
                manifest.NativeReopenSuccess = false;
                manifest.QualifiedArchiveSha256 = existingArchiveSha256;
                manifest.OriginalProvenanceRunId = existingCompleted.OriginalProvenanceRunId;
                manifest.OriginalCompletedAtUtc = existingCompleted.OriginalCompletedAtUtc;
                manifest.NativeReopenDetails = "Reuse of immutable verified completed evidence; no fresh migration or reopen performed.";
                return manifest;
            }

            string preflightId = Guid.NewGuid().ToString("N").Substring(0, 12);
            if (stateStore.CompletedManifestExists())
            {
                string recoveryError;
                if (!stateStore.TryQuarantineInvalidCompleted(preflightId, out recoveryError))
                {
                    manifest.Failure = "phase:state-recovery type:InvalidOperationException hresult:0x80131509";
                    manifest.FailureDetails = recoveryError;
                    return manifest;
                }
            }

            if (File.Exists(qualifiedArchivePath))
            {
                string orphanError;
                if (!stateStore.TryQuarantineOrphanFile(
                    qualifiedArchivePath,
                    preflightId,
                    out orphanError))
                {
                    manifest.Failure = "phase:archive-recovery type:InvalidOperationException hresult:0x80131509";
                    manifest.FailureDetails = orphanError;
                    return manifest;
                }
            }

            string attemptId = Guid.NewGuid().ToString("N").Substring(0, 12);
            string attemptRoot = Path.Combine(
                qualificationOutputRoot,
                ".attempts",
                qualificationIdentity + "." + attemptId);
            string archiveStagingRoot = Path.Combine(attemptRoot, "archive");
            string stagedArchivePath = Path.Combine(archiveStagingRoot, qualifiedArchiveName);
            string retrieveWorkPath = Path.Combine(workRoot, "retrieve_" + qualificationIdentity + "_" + attemptId);
            string verifyWorkPath = Path.Combine(workRoot, "verify_" + qualificationIdentity + "_" + attemptId);

            Directory.CreateDirectory(archiveStagingRoot);
            Directory.CreateDirectory(retrieveWorkPath);
            Directory.CreateDirectory(verifyWorkPath);

            stateStore.WriteStaging(
                attemptId,
                QualificationState.CreateInProgress(
                    qualificationIdentity,
                    sourceSha256,
                    tiaBuildIdentity));

            var whitelistResult = WhitelistManager.SynchronizeWhitelist();
            if (!whitelistResult.Success)
            {
                manifest.Failure = whitelistResult.BootstrapRequired
                    ? "phase:bootstrap-required type:UnauthorizedAccessException hresult:0x80070005"
                    : "phase:whitelist-sync type:InvalidOperationException hresult:0x80131509";
                manifest.FailureDetails = whitelistResult.Message ?? "Whitelist synchronization failed.";
                RecordFailedAttempt(
                    stateStore,
                    attemptId,
                    qualificationIdentity,
                    sourceSha256,
                    tiaBuildIdentity,
                    manifest.Failure,
                    "Whitelist prerequisite failed.");
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            Exception retrieveWithUpgradeException = null;
            Exception saveException = null;
            Exception archiveException = null;
            Exception upgradeCloseException = null;
            UserGlobalLibrary userGlobalLibrary = null;

            using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithUserInterface))
            using (ExclusiveAccess exclusiveAccess = portal.ExclusiveAccess("TIA Automation Factory library qualification"))
            {
                try
                {
                    userGlobalLibrary = portal.GlobalLibraries.RetrieveWithUpgrade(
                        new FileInfo(sourceArchivePath),
                        new DirectoryInfo(retrieveWorkPath),
                        OpenMode.ReadWrite);
                }
                catch (Exception ex)
                {
                    retrieveWithUpgradeException = ex;
                }

                if (retrieveWithUpgradeException == null)
                {
                    if (userGlobalLibrary == null)
                    {
                        retrieveWithUpgradeException =
                            new InvalidOperationException("RetrieveWithUpgrade returned null");
                    }
                    else
                    {
                        try
                        {
                            userGlobalLibrary.Save();
                        }
                        catch (Exception ex)
                        {
                            saveException = ex;
                        }
                    }
                }

                if (retrieveWithUpgradeException == null &&
                    userGlobalLibrary != null &&
                    saveException == null)
                {
                    try
                    {
                        userGlobalLibrary.Archive(
                            new DirectoryInfo(archiveStagingRoot),
                            qualifiedArchiveName,
                            LibraryArchivationMode.Compressed);
                    }
                    catch (Exception ex)
                    {
                        archiveException = ex;
                    }
                }

                try
                {
                    if (userGlobalLibrary != null)
                        userGlobalLibrary.Close();
                }
                catch (Exception ex)
                {
                    upgradeCloseException = ex;
                }
            }

            if (retrieveWithUpgradeException != null)
            {
                manifest.Failure = FormatFailure(QualificationPhase.RetrieveWithUpgrade, retrieveWithUpgradeException);
                manifest.FailureDetails = retrieveWithUpgradeException.ToString();
                RecordFailedAttempt(
                    stateStore, attemptId, qualificationIdentity, sourceSha256, tiaBuildIdentity,
                    manifest.Failure, "RetrieveWithUpgrade failed.");
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            if (saveException != null)
            {
                manifest.Failure = FormatFailure(QualificationPhase.Save, saveException);
                manifest.FailureDetails = saveException.ToString();
                RecordFailedAttempt(
                    stateStore, attemptId, qualificationIdentity, sourceSha256, tiaBuildIdentity,
                    manifest.Failure, "Save failed.");
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            if (archiveException != null)
            {
                manifest.Failure = FormatFailure(QualificationPhase.Archive, archiveException);
                manifest.FailureDetails = archiveException.ToString();
                RecordFailedAttempt(
                    stateStore, attemptId, qualificationIdentity, sourceSha256, tiaBuildIdentity,
                    manifest.Failure, "Archive failed.");
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            if (upgradeCloseException != null)
            {
                manifest.Failure = FormatFailure(QualificationPhase.UpgradeClose, upgradeCloseException);
                manifest.FailureDetails = upgradeCloseException.ToString();
                RecordFailedAttempt(
                    stateStore, attemptId, qualificationIdentity, sourceSha256, tiaBuildIdentity,
                    manifest.Failure, "Upgrade close failed.");
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            if (!File.Exists(stagedArchivePath))
            {
                manifest.Failure = "phase:archive type:FileNotFoundException hresult:0x80070002";
                manifest.FailureDetails = "Archive operation completed without the expected staged archive.";
                RecordFailedAttempt(
                    stateStore, attemptId, qualificationIdentity, sourceSha256, tiaBuildIdentity,
                    manifest.Failure, "Expected staged archive was missing.");
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            string qualifiedArchiveSha256 = ComputeSha256(stagedArchivePath);
            manifest.QualifiedArchiveSha256 = qualifiedArchiveSha256;

            var whitelistResult2 = WhitelistManager.SynchronizeWhitelist();
            if (!whitelistResult2.Success)
            {
                manifest.Failure = whitelistResult2.BootstrapRequired
                    ? "phase:bootstrap-required type:UnauthorizedAccessException hresult:0x80070005"
                    : "phase:whitelist-sync type:InvalidOperationException hresult:0x80131509";
                manifest.FailureDetails = whitelistResult2.Message ?? "Whitelist synchronization failed.";
                RecordFailedAttempt(
                    stateStore, attemptId, qualificationIdentity, sourceSha256, tiaBuildIdentity,
                    manifest.Failure, "Verification whitelist prerequisite failed.");
                string quarantineError;
                stateStore.TryQuarantineOrphanFile(
                    stagedArchivePath,
                    attemptId + ".verification-prerequisite",
                    out quarantineError);
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            bool nativeReopenSuccess = false;
            string nativeReopenDetails = "";
            Exception verifyOperationException = null;
            Exception verifyCloseException = null;
            UserGlobalLibrary verifiedLibrary = null;

            using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithUserInterface))
            using (ExclusiveAccess exclusiveAccess = portal.ExclusiveAccess("TIA Automation Factory library qualification verification"))
            {
                try
                {
                    verifiedLibrary = portal.GlobalLibraries.Retrieve(
                        new FileInfo(stagedArchivePath),
                        new DirectoryInfo(verifyWorkPath),
                        OpenMode.ReadWrite);
                }
                catch (Exception ex)
                {
                    verifyOperationException = ex;
                }

                if (verifyOperationException == null && verifiedLibrary != null)
                {
                    nativeReopenSuccess = true;
                    nativeReopenDetails = "Successfully reopened staged archive with current-version GlobalLibraries.Retrieve.";
                }
                else if (verifyOperationException == null)
                {
                    nativeReopenDetails = "Current-version Retrieve returned null; staged archive is not a valid native V21 library.";
                }

                try
                {
                    if (verifiedLibrary != null)
                        verifiedLibrary.Close();
                }
                catch (Exception ex)
                {
                    verifyCloseException = ex;
                }
            }

            if (verifyOperationException != null || verifyCloseException != null || !nativeReopenSuccess)
            {
                Exception failureException = verifyOperationException ??
                    verifyCloseException ??
                    new InvalidOperationException(nativeReopenDetails);
                QualificationPhase failurePhase = verifyOperationException != null || !nativeReopenSuccess
                    ? QualificationPhase.NativeRetrieve
                    : QualificationPhase.NativeClose;
                manifest.Failure = FormatFailure(failurePhase, failureException);
                manifest.FailureDetails = failureException.ToString();
                RecordFailedAttempt(
                    stateStore, attemptId, qualificationIdentity, sourceSha256, tiaBuildIdentity,
                    manifest.Failure, "Native V21 verification failed.");
                string quarantineError;
                stateStore.TryQuarantineOrphanFile(
                    stagedArchivePath,
                    attemptId + ".verification-failed",
                    out quarantineError);
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            string provenanceRunId = QualificationStateStore.GenerateProvenanceRunId();
            string completedAtUtc = QualificationStateStore.GetCurrentUtcTimestamp();
            var completedState = QualificationState.CreateCompletedSuccess(
                qualificationIdentity,
                sourceSha256,
                tiaBuildIdentity,
                qualifiedArchiveSha256,
                provenanceRunId,
                completedAtUtc);
            stateStore.WriteStaging(attemptId, completedState);

            if (File.Exists(qualifiedArchivePath))
            {
                QualificationState concurrentCompleted;
                if (stateStore.TryLoadCompleted(out concurrentCompleted))
                {
                    manifest.Failure = "phase:publication type:IOException hresult:0x80131620";
                    manifest.FailureDetails = "Completed evidence appeared concurrently; current attempt did not overwrite it.";
                    stateStore.QuarantineStaging(attemptId, "concurrent-completed");
                    TryDeleteAttemptDirectory(attemptRoot);
                    return manifest;
                }

                string quarantineError;
                if (!stateStore.TryQuarantineOrphanFile(
                    qualifiedArchivePath,
                    attemptId + ".publish-conflict",
                    out quarantineError))
                {
                    manifest.Failure = "phase:publication type:IOException hresult:0x80131620";
                    manifest.FailureDetails = quarantineError;
                    stateStore.QuarantineStaging(attemptId, "archive-conflict");
                    TryDeleteAttemptDirectory(attemptRoot);
                    return manifest;
                }
            }

            try
            {
                File.Move(stagedArchivePath, qualifiedArchivePath);
            }
            catch (Exception ex)
            {
                manifest.Failure = "phase:publication type:" + ex.GetType().Name +
                    " hresult:0x" + ex.HResult.ToString("X8");
                manifest.FailureDetails = "Verified archive could not be published without overwrite.";
                stateStore.QuarantineStaging(attemptId, "archive-publication");
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            string promoteError;
            if (!stateStore.TryPromoteStagingToCompleted(attemptId, out promoteError))
            {
                string quarantineError;
                stateStore.TryQuarantineOrphanFile(
                    qualifiedArchivePath,
                    attemptId + ".manifest-publication",
                    out quarantineError);
                stateStore.QuarantineStaging(attemptId, "manifest-publication");
                manifest.Failure = "phase:state-publication type:InvalidOperationException hresult:0x80131509";
                manifest.FailureDetails = promoteError;
                TryDeleteAttemptDirectory(attemptRoot);
                return manifest;
            }

            TryDeleteAttemptDirectory(attemptRoot);

            manifest.Success = true;
            manifest.IsReuse = false;
            manifest.NativeReopenSuccess = true;
            manifest.NativeReopenDetails = nativeReopenDetails;
            manifest.QualifiedArchiveSha256 = qualifiedArchiveSha256;
            manifest.OriginalProvenanceRunId = provenanceRunId;
            manifest.OriginalCompletedAtUtc = completedAtUtc;
            return manifest;
        }

        private static void RecordFailedAttempt(
            QualificationStateStore stateStore,
            string attemptId,
            string qualificationIdentity,
            string sourceSha256,
            string tiaBuildIdentity,
            string failure,
            string boundedFailureDetails)
        {
            try
            {
                stateStore.WriteStaging(
                    attemptId,
                    QualificationState.CreateStoredFalseSuccess(
                        qualificationIdentity,
                        sourceSha256,
                        tiaBuildIdentity,
                        failure,
                        boundedFailureDetails));
                stateStore.QuarantineStaging(attemptId, "failed");
            }
            catch
            {
                try { stateStore.CleanupStaging(attemptId); } catch { }
            }
        }

        private static void TryDeleteAttemptDirectory(string attemptRoot)
        {
            try
            {
                if (Directory.Exists(attemptRoot))
                    Directory.Delete(attemptRoot, true);
            }
            catch
            {
            }
        }

        private static string FormatFailure(QualificationPhase phase, Exception exception)
        {
            string phaseName = GetPhaseToken(phase);
            string exceptionType = exception.GetType().Name;
            string hresultHex = "0x" + exception.HResult.ToString("X8");
            string baseToken = "phase:" + phaseName + " type:" + exceptionType + " hresult:" + hresultHex;

            if (exception is Siemens.Engineering.EngineeringException engException)
            {
                string diagnosticSuffix = ClassifyEngineeringException(engException);
                if (!string.IsNullOrEmpty(diagnosticSuffix))
                {
                    return baseToken + " " + diagnosticSuffix;
                }
            }

            return baseToken;
        }

        private static string ClassifyEngineeringException(Siemens.Engineering.EngineeringException exception)
        {
            try
            {
                var tags = new List<string>();
                int detailCount = 0;
                string messageDataFingerprint = null;
                string detailDataAggregateFingerprint = null;

                if (!string.IsNullOrEmpty(exception.MessageData.Text))
                {
                    string messageText = exception.MessageData.Text;
                    messageDataFingerprint = ComputeSha256Truncated(messageText, 16);
                    tags.AddRange(DeriveTagsFromText(messageText));
                }

                if (exception.DetailMessageData != null)
                {
                    var detailTexts = new List<string>();
                    detailCount = exception.DetailMessageData.Count;
                    foreach (var detail in exception.DetailMessageData)
                    {
                        if (!string.IsNullOrEmpty(detail.Text))
                        {
                            detailTexts.Add(detail.Text);
                            tags.AddRange(DeriveTagsFromText(detail.Text));
                        }
                    }
                    if (detailTexts.Count > 0)
                    {
                        detailDataAggregateFingerprint = ComputeAggregateFingerprint(detailTexts, 16);
                    }
                }

                if (tags.Count == 0)
                {
                    tags.Add("unknown");
                }
                else
                {
                    tags.Sort();
                    tags = tags.Distinct().ToList();
                }

                var parts = new List<string>();
                parts.Add("tags:" + string.Join(",", tags));
                parts.Add("detail-count:" + detailCount);
                if (messageDataFingerprint != null)
                {
                    parts.Add("msgfp:" + messageDataFingerprint);
                }
                if (detailDataAggregateFingerprint != null)
                {
                    parts.Add("dtlfp:" + detailDataAggregateFingerprint);
                }

                return string.Join(" ", parts);
            }
            catch
            {
                return "tags:unknown";
            }
        }

        private static string ComputeAggregateFingerprint(List<string> texts, int length)
        {
            using (var sha256 = SHA256.Create())
            {
                foreach (string text in texts)
                {
                    byte[] textBytes = Encoding.UTF8.GetBytes(text);
                    byte[] lengthBytes = BitConverter.GetBytes(textBytes.Length);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(lengthBytes);
                    sha256.TransformBlock(lengthBytes, 0, lengthBytes.Length, null, 0);
                    sha256.TransformBlock(textBytes, 0, textBytes.Length, null, 0);
                }
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                string hex = BitConverter.ToString(sha256.Hash).Replace("-", "").ToLowerInvariant();
                return hex.Substring(0, Math.Min(length, hex.Length));
            }
        }

        private static List<string> DeriveTagsFromText(string text)
        {
            var tags = new List<string>();
            string lowerText = text.ToLowerInvariant();

            if (ContainsAny(lowerText, "missing product", "product not found", "product missing"))
                tags.Add("missing-product");
            if (ContainsAny(lowerText, "unreleased content", "not released", "pre-release version"))
                tags.Add("unreleased-content");
            if (ContainsAny(lowerText, "unsupported version", "version not supported", "incompatible version"))
                tags.Add("unsupported-version");
            if (ContainsAny(lowerText, "invalid archive", "corrupt archive", "archive corrupt", "not a valid archive"))
                tags.Add("invalid-archive");
            if (ContainsAny(lowerText, "access denied", "permission denied", "unauthorized access", "no access"))
                tags.Add("access-denied");
            if (ContainsAny(lowerText, "user abort", "cancelled by user", "aborted by user"))
                tags.Add("user-abort");
            if (ContainsAny(lowerText, "target conflict", "conflict with target"))
                tags.Add("target-conflict");
            if (ContainsAny(lowerText, "license missing", "license not found", "no license"))
                tags.Add("license-missing");

            return tags;
        }

        private static bool ContainsAny(string text, params string[] phrases)
        {
            foreach (string phrase in phrases)
            {
                if (text.Contains(phrase))
                    return true;
            }
            return false;
        }

        private static string ComputeSha256Truncated(string input, int length)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return hex.Substring(0, Math.Min(length, hex.Length));
            }
        }

        private static string GetPhaseToken(QualificationPhase phase)
        {
            switch (phase)
            {
                case QualificationPhase.RetrieveWithUpgrade:
                    return "retrieve-with-upgrade";
                case QualificationPhase.Save:
                    return "save";
                case QualificationPhase.Archive:
                    return "archive";
                case QualificationPhase.UpgradeClose:
                    return "upgrade-close";
                case QualificationPhase.NativeRetrieve:
                    return "native-reopen";
                case QualificationPhase.NativeClose:
                    return "native-reopen-close";
                default:
                    throw new ArgumentOutOfRangeException("phase");
            }
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

        private static string DeriveQualificationIdentity(
            string sourceSha256,
            string tiaBuildIdentity,
            string recipeIdentity)
        {
            string combined = sourceSha256 + "|" + tiaBuildIdentity + "|" + recipeIdentity;
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
        public string QualificationRecipeIdentity { get; set; }
        public string QualifiedArchiveName { get; set; }
        public string QualifiedArchiveSha256 { get; set; }
        public bool NativeReopenSuccess { get; set; }
        public string NativeReopenDetails { get; set; }
        public bool IsReuse { get; set; }
        public string OriginalProvenanceRunId { get; set; }
        public string OriginalCompletedAtUtc { get; set; }
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
                QualificationRecipeIdentity = QualificationState.CurrentRecipeIdentity,
                IsReuse = false,
                Failure = FormatTopLevelFailure(exception),
                FailureDetails = exception.ToString()
            };
        }

        private static string FormatTopLevelFailure(Exception exception)
        {
            string exceptionType = exception.GetType().Name;
            string hresultHex = "0x" + exception.HResult.ToString("X8");
            return "phase:top-level type:" + exceptionType + " hresult:" + hresultHex;
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
            AppendProperty(builder, "qualificationRecipeIdentity", result.QualificationRecipeIdentity, true, true);
            AppendProperty(builder, "qualifiedArchiveName", result.QualifiedArchiveName, true, true);
            AppendProperty(builder, "qualifiedArchiveSha256", result.QualifiedArchiveSha256, true, true);
            AppendProperty(builder, "nativeReopenSuccess", result.NativeReopenSuccess ? "true" : "false", false, true);
            AppendProperty(builder, "nativeReopenDetails", result.NativeReopenDetails, true, true);
            AppendProperty(builder, "isReuse", result.IsReuse ? "true" : "false", false, true);
            AppendProperty(builder, "originalProvenanceRunId", result.OriginalProvenanceRunId, true, true);
            AppendProperty(builder, "originalCompletedAtUtc", result.OriginalCompletedAtUtc, true, true);
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
                else if (trimmed.StartsWith("\"qualificationRecipeIdentity\":"))
                    result.QualificationRecipeIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveName\":"))
                    result.QualifiedArchiveName = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveSha256\":"))
                    result.QualifiedArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"nativeReopenSuccess\":"))
                    result.NativeReopenSuccess = trimmed.Contains("true");
                else if (trimmed.StartsWith("\"nativeReopenDetails\":"))
                    result.NativeReopenDetails = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"isReuse\":"))
                    result.IsReuse = trimmed.Contains("true");
                else if (trimmed.StartsWith("\"originalProvenanceRunId\":"))
                    result.OriginalProvenanceRunId = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"originalCompletedAtUtc\":"))
                    result.OriginalCompletedAtUtc = ExtractValue(trimmed);
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
