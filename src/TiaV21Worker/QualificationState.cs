using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TiaAutomationFactory.TiaV21Worker
{
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
            AppendProperty(builder, "schemaVersion", state.SchemaVersion, true, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        public static QualificationState Deserialize(string json)
        {
            var result = new QualificationState();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool hasSchemaVersion = false;
            bool hasStateKind = false;

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
            }

            if (!hasSchemaVersion)
                throw new InvalidDataException("Missing required schemaVersion field");
            if (!result.SchemaVersion.Equals("1.0", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported schemaVersion: " + result.SchemaVersion);
            if (!hasStateKind)
                throw new InvalidDataException("Missing required stateKind field");

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

        public static QualificationReuseCheck CheckReuse(QualificationState state, string sourceArchiveSha256, string tiaBuildIdentity, string qualificationOutputRoot)
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
            return check;
        }

        public static void MarkStaged(QualificationState state, string sourceArchiveSha256, string tiaBuildIdentity, string qualificationOutputRoot, string verificationRunId, DateTimeOffset completedAt)
        {
            state.StateKind = QualificationStateKind.Staged;
            state.SourceArchiveSha256 = sourceArchiveSha256;
            state.TiaBuildIdentity = tiaBuildIdentity;
            state.QualifiedArchiveSha256 = null;
            state.VerificationRunId = verificationRunId;
            state.CompletedAt = completedAt;
            state.NativeReopenVerified = false;
            state.FailureReason = null;
            state.Save(qualificationOutputRoot);
        }

        public static void MarkCompletedSuccess(QualificationState state, string qualifiedArchiveSha256, string qualificationOutputRoot, DateTimeOffset completedAt)
        {
            state.StateKind = QualificationStateKind.CompletedSuccess;
            state.QualifiedArchiveSha256 = qualifiedArchiveSha256;
            state.CompletedAt = completedAt;
            state.NativeReopenVerified = true;
            state.FailureReason = null;
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
    }

    internal sealed class QualificationReuseCheck
    {
        public bool CanReuse { get; set; }
        public string Reason { get; set; }
        public string OriginalVerificationRunId { get; set; }
        public DateTimeOffset OriginalCompletedAt { get; set; }
    }
}