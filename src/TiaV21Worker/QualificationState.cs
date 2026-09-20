using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal enum QualificationStateStatus
    {
        None,
        Staged,
        Completed,
        Corrupt,
        FalseSuccess
    }

    internal sealed class QualificationStateRecord
    {
        public int SchemaVersion { get; set; } = 1;
        public string SourceArchiveSha256 { get; set; }
        public string TiaBuildIdentity { get; set; }
        public string QualificationIdentity { get; set; }
        public string QualifiedArchiveName { get; set; }
        public string QualifiedArchiveSha256 { get; set; }
        public string VerificationProvenance { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public bool IsReuse { get; set; }
        public string OriginalVerificationProvenance { get; set; }
        public DateTime OriginalCompletedAtUtc { get; set; }
    }

    internal static class QualificationState
    {
        private const string StateFileExtension = ".qualstate.json";
        private const int SchemaVersion = 1;

        public static QualificationStateRecord LoadState(string qualificationOutputRoot, string qualificationIdentity)
        {
            string statePath = GetStatePath(qualificationOutputRoot, qualificationIdentity);
            if (!File.Exists(statePath))
                return null;

            try
            {
                string json = File.ReadAllText(statePath, new UTF8Encoding(false));
                var record = Deserialize(json);
                if (record.SchemaVersion != SchemaVersion)
                    return null;
                return record;
            }
            catch
            {
                return null;
            }
        }

        public static void SaveStaged(string qualificationOutputRoot, string qualificationIdentity, string sourceArchiveSha256, string tiaBuildIdentity, string qualifiedArchiveName)
        {
            var record = new QualificationStateRecord
            {
                SchemaVersion = SchemaVersion,
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                QualificationIdentity = qualificationIdentity,
                QualifiedArchiveName = qualifiedArchiveName,
                QualifiedArchiveSha256 = null,
                VerificationProvenance = null,
                CompletedAtUtc = DateTime.MinValue,
                IsReuse = false,
                OriginalVerificationProvenance = null,
                OriginalCompletedAtUtc = DateTime.MinValue
            };

            string statePath = GetStatePath(qualificationOutputRoot, qualificationIdentity);
            Directory.CreateDirectory(Path.GetDirectoryName(statePath));
            WriteAtomic(statePath, Serialize(record, QualificationStateStatus.Staged));
        }

        public static void SaveCompleted(string qualificationOutputRoot, string qualificationIdentity, QualificationStateRecord stagedRecord, string qualifiedArchiveSha256, string verificationProvenance)
        {
            var record = new QualificationStateRecord
            {
                SchemaVersion = SchemaVersion,
                SourceArchiveSha256 = stagedRecord.SourceArchiveSha256,
                TiaBuildIdentity = stagedRecord.TiaBuildIdentity,
                QualificationIdentity = stagedRecord.QualificationIdentity,
                QualifiedArchiveName = stagedRecord.QualifiedArchiveName,
                QualifiedArchiveSha256 = qualifiedArchiveSha256,
                VerificationProvenance = verificationProvenance,
                CompletedAtUtc = DateTime.UtcNow,
                IsReuse = false,
                OriginalVerificationProvenance = verificationProvenance,
                OriginalCompletedAtUtc = DateTime.UtcNow
            };

            string statePath = GetStatePath(qualificationOutputRoot, qualificationIdentity);
            WriteAtomic(statePath, Serialize(record, QualificationStateStatus.Completed));
        }

        public static void SaveReuse(string qualificationOutputRoot, string qualificationIdentity, QualificationStateRecord completedRecord)
        {
            if (completedRecord == null)
                throw new ArgumentNullException(nameof(completedRecord));

            var record = new QualificationStateRecord
            {
                SchemaVersion = SchemaVersion,
                SourceArchiveSha256 = completedRecord.SourceArchiveSha256,
                TiaBuildIdentity = completedRecord.TiaBuildIdentity,
                QualificationIdentity = completedRecord.QualificationIdentity,
                QualifiedArchiveName = completedRecord.QualifiedArchiveName,
                QualifiedArchiveSha256 = completedRecord.QualifiedArchiveSha256,
                VerificationProvenance = "Reuse of " + completedRecord.QualificationIdentity + " verified at " + completedRecord.OriginalCompletedAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CompletedAtUtc = DateTime.UtcNow,
                IsReuse = true,
                OriginalVerificationProvenance = completedRecord.OriginalVerificationProvenance,
                OriginalCompletedAtUtc = completedRecord.OriginalCompletedAtUtc
            };

            string statePath = GetStatePath(qualificationOutputRoot, qualificationIdentity);
            WriteAtomic(statePath, Serialize(record, QualificationStateStatus.Completed));
        }

        public static void MarkCorrupt(string qualificationOutputRoot, string qualificationIdentity)
        {
            var record = new QualificationStateRecord
            {
                SchemaVersion = SchemaVersion,
                QualificationIdentity = qualificationIdentity
            };
            string statePath = GetStatePath(qualificationOutputRoot, qualificationIdentity);
            WriteAtomic(statePath, Serialize(record, QualificationStateStatus.Corrupt));
        }

        public static void MarkFalseSuccess(string qualificationOutputRoot, string qualificationIdentity)
        {
            var record = new QualificationStateRecord
            {
                SchemaVersion = SchemaVersion,
                QualificationIdentity = qualificationIdentity
            };
            string statePath = GetStatePath(qualificationOutputRoot, qualificationIdentity);
            WriteAtomic(statePath, Serialize(record, QualificationStateStatus.FalseSuccess));
        }

        public static QualificationStateStatus GetStatus(string qualificationOutputRoot, string qualificationIdentity)
        {
            string statePath = GetStatePath(qualificationOutputRoot, qualificationIdentity);
            if (!File.Exists(statePath))
                return QualificationStateStatus.None;

            try
            {
                string json = File.ReadAllText(statePath, new UTF8Encoding(false));
                return ExtractStatus(json);
            }
            catch
            {
                return QualificationStateStatus.Corrupt;
            }
        }

        public static bool ValidateCompletedRecord(QualificationStateRecord record, string sourceArchiveSha256, string tiaBuildIdentity, string qualificationIdentity, string expectedArchiveSha256)
        {
            if (record == null)
                return false;

            if (record.SchemaVersion != SchemaVersion)
                return false;

            if (record.QualificationIdentity != qualificationIdentity)
                return false;

            if (record.SourceArchiveSha256 != sourceArchiveSha256)
                return false;

            if (record.TiaBuildIdentity != tiaBuildIdentity)
                return false;

            if (record.QualifiedArchiveSha256 != expectedArchiveSha256)
                return false;

            if (string.IsNullOrEmpty(record.VerificationProvenance))
                return false;

            if (record.CompletedAtUtc == DateTime.MinValue)
                return false;

            if (string.IsNullOrEmpty(record.OriginalVerificationProvenance))
                return false;

            if (record.OriginalCompletedAtUtc == DateTime.MinValue)
                return false;

            return true;
        }

        private static string GetStatePath(string qualificationOutputRoot, string qualificationIdentity)
        {
            return Path.Combine(qualificationOutputRoot, qualificationIdentity + StateFileExtension);
        }

        private static void WriteAtomic(string targetPath, string content)
        {
            string tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, content, new UTF8Encoding(false));
            File.Move(tempPath, targetPath, true);
        }

        private static string Serialize(QualificationStateRecord record, QualificationStateStatus status)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "schemaVersion", record.SchemaVersion.ToString(), false, true);
            AppendProperty(builder, "status", status.ToString(), true, true);
            AppendProperty(builder, "sourceArchiveSha256", record.SourceArchiveSha256, true, true);
            AppendProperty(builder, "tiaBuildIdentity", record.TiaBuildIdentity, true, true);
            AppendProperty(builder, "qualificationIdentity", record.QualificationIdentity, true, true);
            AppendProperty(builder, "qualifiedArchiveName", record.QualifiedArchiveName, true, true);
            AppendProperty(builder, "qualifiedArchiveSha256", record.QualifiedArchiveSha256, true, true);
            AppendProperty(builder, "verificationProvenance", record.VerificationProvenance, true, true);
            AppendProperty(builder, "completedAtUtc", record.CompletedAtUtc == DateTime.MinValue ? null : record.CompletedAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"), true, true);
            AppendProperty(builder, "isReuse", record.IsReuse ? "true" : "false", false, true);
            AppendProperty(builder, "originalVerificationProvenance", record.OriginalVerificationProvenance, true, true);
            AppendProperty(builder, "originalCompletedAtUtc", record.OriginalCompletedAtUtc == DateTime.MinValue ? null : record.OriginalCompletedAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"), true, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static QualificationStateRecord Deserialize(string json)
        {
            var record = new QualificationStateRecord();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"schemaVersion\":"))
                {
                    string value = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(value))
                        int.TryParse(value, out int sv);
                        record.SchemaVersion = sv;
                }
                else if (trimmed.StartsWith("\"sourceArchiveSha256\":"))
                    record.SourceArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"tiaBuildIdentity\":"))
                    record.TiaBuildIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualificationIdentity\":"))
                    record.QualificationIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveName\":"))
                    record.QualifiedArchiveName = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveSha256\":"))
                    record.QualifiedArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"verificationProvenance\":"))
                    record.VerificationProvenance = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"completedAtUtc\":"))
                {
                    string value = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(value))
                        DateTime.TryParse(value, out record.CompletedAtUtc);
                }
                else if (trimmed.StartsWith("\"isReuse\":"))
                    record.IsReuse = trimmed.Contains("true");
                else if (trimmed.StartsWith("\"originalVerificationProvenance\":"))
                    record.OriginalVerificationProvenance = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"originalCompletedAtUtc\":"))
                {
                    string value = ExtractValue(trimmed);
                    if (!string.IsNullOrEmpty(value))
                        DateTime.TryParse(value, out record.OriginalCompletedAtUtc);
                }
            }
            return record;
        }

        private static QualificationStateStatus ExtractStatus(string json)
        {
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"status\":"))
                {
                    string value = ExtractValue(trimmed);
                    if (Enum.TryParse<QualificationStateStatus>(value, out var status))
                        return status;
                }
            }
            return QualificationStateStatus.Corrupt;
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