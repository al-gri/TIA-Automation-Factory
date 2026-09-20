using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal sealed class DiagnosticRecord
    {
        public string Path { get; set; }
        public string State { get; set; }
        public string Description { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
    }

    internal enum QualificationStateKind
    {
        None,
        Staged,
        CompletedSuccess,
        CompletedFailure,
        Corrupt
    }

    internal sealed class QualificationState
    {
        public string QualificationIdentity { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string TiaBuildIdentity { get; set; }
        public string QualifiedArchiveSha256 { get; set; }
        public QualificationStateKind StateKind { get; set; }
        public string VerificationRunId { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public bool NativeReopenVerified { get; set; }
        public string FailureReason { get; set; }
        public string SchemaVersion { get; set; } = "1.0";
        public string ProfileRecipeIdentity { get; set; }
        public ValveProfileContract ValveProfile { get; set; }
        public ReferenceValidationResult ReferenceValidation { get; set; }

        public static string GetStateFilePath(string qualificationOutputRoot, string qualificationIdentity)
        {
            return Path.Combine(qualificationOutputRoot, "state", qualificationIdentity + ".state.json");
        }

        public static string GetManifestFilePath(string qualificationOutputRoot, string qualificationIdentity)
        {
            return Path.Combine(qualificationOutputRoot, qualificationIdentity + ".manifest.json");
        }

        public static string GetArchiveFilePath(string qualificationOutputRoot, string qualificationIdentity)
        {
            return Path.Combine(qualificationOutputRoot, qualificationIdentity + ".zal21");
        }

        public static QualificationState LoadOrCreate(string qualificationOutputRoot, string qualificationIdentity)
        {
            string statePath = GetStateFilePath(qualificationOutputRoot, qualificationIdentity);
            if (File.Exists(statePath))
            {
                return Deserialize(File.ReadAllText(statePath));
            }
            return new QualificationState
            {
                QualificationIdentity = qualificationIdentity,
                StateKind = QualificationStateKind.None
            };
        }

        public void Save(string qualificationOutputRoot)
        {
            string statePath = GetStateFilePath(qualificationOutputRoot, QualificationIdentity);
            Directory.CreateDirectory(Path.GetDirectoryName(statePath));
            string tempPath = statePath + ".tmp";
            File.WriteAllText(tempPath, Serialize(this), new UTF8Encoding(false));
            File.Move(tempPath, statePath, overwrite: true);
        }

        public static string Serialize(QualificationState state)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "qualificationIdentity", state.QualificationIdentity, true, true);
            AppendProperty(builder, "sourceArchiveSha256", state.SourceArchiveSha256, true, true);
            AppendProperty(builder, "tiaBuildIdentity", state.TiaBuildIdentity, true, true);
            AppendProperty(builder, "qualifiedArchiveSha256", state.QualifiedArchiveSha256, true, true);
            AppendProperty(builder, "stateKind", state.StateKind.ToString(), true, true);
            AppendProperty(builder, "verificationRunId", state.VerificationRunId, true, true);
            AppendProperty(builder, "completedAt", state.CompletedAt.ToString("o"), false, true);
            AppendProperty(builder, "nativeReopenVerified", state.NativeReopenVerified ? "true" : "false", false, true);
            AppendProperty(builder, "failureReason", state.FailureReason, true, true);
            AppendProperty(builder, "schemaVersion", state.SchemaVersion, true, true);
            AppendProperty(builder, "profileRecipeIdentity", state.ProfileRecipeIdentity, true, true);
            if (state.ValveProfile != null)
            {
                AppendProperty(builder, "valveProfile", state.ValveProfile.Serialize().Replace("\n", "\n  "), false, true);
            }
            else
            {
                AppendProperty(builder, "valveProfile", "null", false, true);
            }
            if (state.ReferenceValidation != null)
            {
                builder.Append("  \"referenceValidation\": ");
                builder.AppendLine(SerializeReferenceValidation(state.ReferenceValidation));
            }
            else
            {
                AppendProperty(builder, "referenceValidation", "null", false, false);
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string SerializeReferenceValidation(ReferenceValidationResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "success", result.Success ? "true" : "false", false, true, 4);
            AppendProperty(builder, "errorCount", result.ErrorCount.ToString(), false, true, 4);
            AppendProperty(builder, "warningCount", result.WarningCount.ToString(), false, true, 4);
            AppendProperty(builder, "projectPath", result.ProjectPath, true, true, 4);
            AppendProperty(builder, "state", result.State, true, true, 4);
            AppendProperty(builder, "saveReopenVerified", result.SaveReopenVerified ? "true" : "false", false, true, 4);
            AppendProperty(builder, "failure", result.Failure, true, true, 4);
            AppendProperty(builder, "failureDetails", result.FailureDetails, true, true, 4);
            builder.AppendLine("    \"diagnostics\": [");
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                var item = result.Diagnostics[i];
                builder.AppendLine("      {");
                AppendProperty(builder, "path", item.Path, true, true, 8);
                AppendProperty(builder, "state", item.State, true, true, 8);
                AppendProperty(builder, "description", item.Description, true, true, 8);
                AppendProperty(builder, "warnings", item.WarningCount.ToString(), false, true, 8);
                AppendProperty(builder, "errors", item.ErrorCount.ToString(), false, i == result.Diagnostics.Count - 1 ? false : true, 8);
                builder.Append("      }");
                if (i < result.Diagnostics.Count - 1) builder.Append(",");
                builder.AppendLine();
            }
            builder.AppendLine("    ]");
            builder.Append("  }");
            return builder.ToString();
        }

        public static QualificationState Deserialize(string json)
        {
            var result = new QualificationState();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool hasSchemaVersion = false;
            bool hasStateKind = false;
            bool inValveProfile = false;
            bool inReferenceValidation = false;
            var valveProfileJson = new StringBuilder();
            var refValJson = new StringBuilder();
            int braceDepth = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"qualificationIdentity\":"))
                    result.QualificationIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"sourceArchiveSha256\":"))
                    result.SourceArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"tiaBuildIdentity\":"))
                    result.TiaBuildIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveSha256\":"))
                    result.QualifiedArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"stateKind\":"))
                {
                    var value = ExtractValue(trimmed);
                    if (!Enum.TryParse(value, out result.StateKind))
                        throw new InvalidDataException("Invalid stateKind value: " + value);
                    hasStateKind = true;
                }
                else if (trimmed.StartsWith("\"verificationRunId\":"))
                    result.VerificationRunId = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"completedAt\":"))
                {
                    var value = ExtractValue(trimmed);
                    if (!DateTimeOffset.TryParse(value, out result.CompletedAt))
                        throw new InvalidDataException("Invalid completedAt value: " + value);
                }
                else if (trimmed.StartsWith("\"nativeReopenVerified\":"))
                {
                    var value = ExtractValue(trimmed);
                    if (value != "true" && value != "false")
                        throw new InvalidDataException("Invalid nativeReopenVerified value: " + value);
                    result.NativeReopenVerified = value == "true";
                }
                else if (trimmed.StartsWith("\"failureReason\":"))
                    result.FailureReason = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"schemaVersion\":"))
                {
                    result.SchemaVersion = ExtractValue(trimmed);
                    hasSchemaVersion = true;
                }
                else if (trimmed.StartsWith("\"profileRecipeIdentity\":"))
                    result.ProfileRecipeIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"valveProfile\":"))
                {
                    inValveProfile = true;
                    braceDepth = 0;
                    valveProfileJson.Clear();
                }
                else if (trimmed.StartsWith("\"referenceValidation\":"))
                {
                    inReferenceValidation = true;
                    braceDepth = 0;
                    refValJson.Clear();
                }

                if (inValveProfile)
                {
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == '{') braceDepth++;
                        if (line[i] == '}') braceDepth--;
                    }
                    valveProfileJson.AppendLine(line);
                    if (braceDepth == 0 && valveProfileJson.Length > 0)
                    {
                        inValveProfile = false;
                        var vpJson = valveProfileJson.ToString().Trim();
                        if (!vpJson.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                result.ValveProfile = ValveProfileContract.Deserialize(vpJson);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidDataException("Failed to deserialize valveProfile: " + ex.Message);
                            }
                        }
                    }
                }

                if (inReferenceValidation)
                {
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == '{') braceDepth++;
                        if (line[i] == '}') braceDepth--;
                    }
                    refValJson.AppendLine(line);
                    if (braceDepth == 0 && refValJson.Length > 0)
                    {
                        inReferenceValidation = false;
                        var rvJson = refValJson.ToString().Trim();
                        if (!rvJson.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                result.ReferenceValidation = DeserializeReferenceValidation(rvJson);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidDataException("Failed to deserialize referenceValidation: " + ex.Message);
                            }
                        }
                    }
                }
            }

            if (!hasSchemaVersion)
                throw new InvalidDataException("Missing required schemaVersion field");
            if (!result.SchemaVersion.Equals("1.0", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported schemaVersion: " + result.SchemaVersion);
            if (!hasStateKind)
                throw new InvalidDataException("Missing required stateKind field");

            return result;
        }

        private static ReferenceValidationResult DeserializeReferenceValidation(string json)
        {
            var result = new ReferenceValidationResult();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool inDiagnostics = false;
            var currentDiag = new DiagnosticRecord();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"success\":"))
                    result.Success = trimmed.Contains("true");
                else if (trimmed.StartsWith("\"errorCount\":"))
                    result.ErrorCount = int.Parse(ExtractValue(trimmed));
                else if (trimmed.StartsWith("\"warningCount\":"))
                    result.WarningCount = int.Parse(ExtractValue(trimmed));
                else if (trimmed.StartsWith("\"projectPath\":"))
                    result.ProjectPath = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"state\":"))
                    result.State = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"saveReopenVerified\":"))
                    result.SaveReopenVerified = trimmed.Contains("true");
                else if (trimmed.StartsWith("\"failure\":"))
                    result.Failure = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"failureDetails\":"))
                    result.FailureDetails = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"diagnostics\":"))
                    inDiagnostics = true;
                else if (inDiagnostics)
                {
                    if (trimmed == "{")
                    {
                        currentDiag = new DiagnosticRecord();
                    }
                    else if (trimmed.StartsWith("\"path\":"))
                        currentDiag.Path = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"state\":"))
                        currentDiag.State = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"description\":"))
                        currentDiag.Description = ExtractValue(trimmed);
                    else if (trimmed.StartsWith("\"warnings\":"))
                        currentDiag.WarningCount = int.Parse(ExtractValue(trimmed));
                    else if (trimmed.StartsWith("\"errors\":"))
                        currentDiag.ErrorCount = int.Parse(ExtractValue(trimmed));
                    else if (trimmed.Contains("}"))
                    {
                        result.Diagnostics.Add(currentDiag);
                    }
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

    internal static class QualificationStateManager
    {
        public static QualificationState LoadState(string qualificationOutputRoot, string qualificationIdentity)
        {
            return QualificationState.LoadOrCreate(qualificationOutputRoot, qualificationIdentity);
        }

        public static QualificationReuseCheck CheckReuse(QualificationState state, string sourceArchiveSha256, string tiaBuildIdentity, string qualificationOutputRoot, string profileRecipeIdentity)
        {
            var check = new QualificationReuseCheck();

            if (state.StateKind != QualificationStateKind.CompletedSuccess)
            {
                check.CanReuse = false;
                check.Reason = "No completed-success state found.";
                return check;
            }

            if (state.SourceArchiveSha256 != sourceArchiveSha256)
            {
                check.CanReuse = false;
                check.Reason = "Source archive SHA256 mismatch.";
                return check;
            }

            if (state.TiaBuildIdentity != tiaBuildIdentity)
            {
                check.CanReuse = false;
                check.Reason = "TIA build identity mismatch.";
                return check;
            }

            if (state.ProfileRecipeIdentity != profileRecipeIdentity)
            {
                check.CanReuse = false;
                check.Reason = "Profile recipe identity mismatch.";
                return check;
            }

            string archivePath = QualificationState.GetArchiveFilePath(qualificationOutputRoot, state.QualificationIdentity);
            if (!File.Exists(archivePath))
            {
                check.CanReuse = false;
                check.Reason = "Qualified archive file missing.";
                return check;
            }

            string actualArchiveSha256 = ComputeSha256(archivePath);
            if (state.QualifiedArchiveSha256 != actualArchiveSha256)
            {
                check.CanReuse = false;
                check.Reason = "Qualified archive SHA256 mismatch.";
                return check;
            }

            if (!state.NativeReopenVerified)
            {
                check.CanReuse = false;
                check.Reason = "Native reopen was not verified in original qualification.";
                return check;
            }

            check.CanReuse = true;
            check.Reason = "Valid completed-success reuse.";
            check.OriginalVerificationRunId = state.VerificationRunId;
            check.OriginalCompletedAt = state.CompletedAt;
            check.ValveProfile = state.ValveProfile;
            check.ReferenceValidation = state.ReferenceValidation;
            return check;
        }

        public static void MarkStaged(QualificationState state, string sourceArchiveSha256, string tiaBuildIdentity, string qualificationOutputRoot, string verificationRunId, DateTimeOffset completedAt, string profileRecipeIdentity)
        {
            state.StateKind = QualificationStateKind.Staged;
            state.SourceArchiveSha256 = sourceArchiveSha256;
            state.TiaBuildIdentity = tiaBuildIdentity;
            state.QualifiedArchiveSha256 = null;
            state.VerificationRunId = verificationRunId;
            state.CompletedAt = completedAt;
            state.NativeReopenVerified = false;
            state.FailureReason = null;
            state.ProfileRecipeIdentity = profileRecipeIdentity;
            state.ValveProfile = null;
            state.ReferenceValidation = null;
            state.Save(qualificationOutputRoot);
        }

        public static void MarkCompletedSuccess(QualificationState state, string qualifiedArchiveSha256, string qualificationOutputRoot, DateTimeOffset completedAt, ValveProfileContract valveProfile, ReferenceValidationResult referenceValidation)
        {
            state.StateKind = QualificationStateKind.CompletedSuccess;
            state.QualifiedArchiveSha256 = qualifiedArchiveSha256;
            state.CompletedAt = completedAt;
            state.NativeReopenVerified = true;
            state.FailureReason = null;
            state.ValveProfile = valveProfile;
            state.ReferenceValidation = referenceValidation;
            state.Save(qualificationOutputRoot);
        }

        public static void MarkCompletedFailure(QualificationState state, string failureReason, string qualificationOutputRoot, DateTimeOffset completedAt)
        {
            state.StateKind = QualificationStateKind.CompletedFailure;
            state.CompletedAt = completedAt;
            state.NativeReopenVerified = false;
            state.FailureReason = failureReason;
            state.Save(qualificationOutputRoot);
        }

        public static void MarkCorrupt(QualificationState state, string qualificationOutputRoot, DateTimeOffset completedAt)
        {
            state.StateKind = QualificationStateKind.Corrupt;
            state.CompletedAt = completedAt;
            state.NativeReopenVerified = false;
            state.FailureReason = "State marked corrupt due to inconsistency.";
            state.Save(qualificationOutputRoot);
        }

        public static bool IsValidStagedState(QualificationState state, string sourceArchiveSha256, string tiaBuildIdentity)
        {
            return state.StateKind == QualificationStateKind.Staged
                && state.SourceArchiveSha256 == sourceArchiveSha256
                && state.TiaBuildIdentity == tiaBuildIdentity;
        }

        public static bool DetectFalseSuccess(QualificationState state, string qualificationOutputRoot)
        {
            if (state.StateKind != QualificationStateKind.CompletedSuccess)
                return false;

            string archivePath = QualificationState.GetArchiveFilePath(qualificationOutputRoot, state.QualificationIdentity);
            if (!File.Exists(archivePath))
                return true;

            string actualArchiveSha256 = ComputeSha256(archivePath);
            if (state.QualifiedArchiveSha256 != actualArchiveSha256)
                return true;

            return false;
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

        public static string DeriveProfileRecipeIdentity(string cpuTypeIdentifier, string valveLibraryObject)
        {
            string combined = cpuTypeIdentifier + "|" + valveLibraryObject;
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return "pr-" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 32);
            }
        }
    }

    internal sealed class QualificationReuseCheck
    {
        public bool CanReuse { get; set; }
        public string Reason { get; set; }
        public string OriginalVerificationRunId { get; set; }
        public DateTimeOffset OriginalCompletedAt { get; set; }
        public ValveProfileContract ValveProfile { get; set; }
        public ReferenceValidationResult ReferenceValidation { get; set; }
    }

    internal sealed class ReferenceValidationResult
    {
        public bool Success { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public string ProjectPath { get; set; }
        public string State { get; set; }
        public List<DiagnosticRecord> Diagnostics { get; set; } = new List<DiagnosticRecord>();
        public bool SaveReopenVerified { get; set; }
        public string Failure { get; set; }
        public string FailureDetails { get; set; }
    }
}