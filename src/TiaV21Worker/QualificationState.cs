using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal enum QualificationStateKind
    {
        None,
        InProgress,
        CompletedSuccess,
        Reuse
    }

    internal sealed class QualificationStateRecord
    {
        public string SourceArchiveSha256 { get; set; }
        public string TiaBuildIdentity { get; set; }
        public string QualificationIdentity { get; set; }
        public string QualifiedArchiveSha256 { get; set; }
        public string QualificationRecipeIdentity { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public QualificationStateKind StateKind { get; set; }
        public string OriginalProvenanceRunId { get; set; }
    }

    internal sealed class QualificationStateManager
    {
        private readonly string _qualificationOutputRoot;
        private readonly string _qualificationIdentity;

        public QualificationStateManager(string qualificationOutputRoot, string qualificationIdentity)
        {
            _qualificationOutputRoot = qualificationOutputRoot;
            _qualificationIdentity = qualificationIdentity;
        }

        private string StateFilePath => Path.Combine(_qualificationOutputRoot, _qualificationIdentity + ".state.json");
        private string StateTempFilePath => Path.Combine(_qualificationOutputRoot, _qualificationIdentity + ".state.json.tmp");

        public QualificationStateRecord Load()
        {
            if (!File.Exists(StateFilePath))
                return null;

            try
            {
                return QualificationStateJson.Deserialize(File.ReadAllText(StateFilePath));
            }
            catch
            {
                return null;
            }
        }

        public void WriteInProgress(string sourceArchiveSha256, string tiaBuildIdentity, string qualificationRecipeIdentity)
        {
            var record = new QualificationStateRecord
            {
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                QualificationIdentity = _qualificationIdentity,
                QualificationRecipeIdentity = qualificationRecipeIdentity,
                StateKind = QualificationStateKind.InProgress,
                CompletedAtUtc = DateTime.MinValue,
                OriginalProvenanceRunId = null
            };
            WriteStateAtomic(record);
        }

        public void WriteCompletedSuccess(string sourceArchiveSha256, string tiaBuildIdentity, string qualificationRecipeIdentity, string qualifiedArchiveSha256, string provenanceRunId)
        {
            var record = new QualificationStateRecord
            {
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                QualificationIdentity = _qualificationIdentity,
                QualifiedArchiveSha256 = qualifiedArchiveSha256,
                QualificationRecipeIdentity = qualificationRecipeIdentity,
                StateKind = QualificationStateKind.CompletedSuccess,
                CompletedAtUtc = DateTime.UtcNow,
                OriginalProvenanceRunId = provenanceRunId
            };
            WriteStateAtomic(record);
        }

        public void WriteReuse(QualificationStateRecord existingRecord)
        {
            var record = new QualificationStateRecord
            {
                SourceArchiveSha256 = existingRecord.SourceArchiveSha256,
                TiaBuildIdentity = existingRecord.TiaBuildIdentity,
                QualificationIdentity = existingRecord.QualificationIdentity,
                QualifiedArchiveSha256 = existingRecord.QualifiedArchiveSha256,
                QualificationRecipeIdentity = existingRecord.QualificationRecipeIdentity,
                StateKind = QualificationStateKind.Reuse,
                CompletedAtUtc = DateTime.UtcNow,
                OriginalProvenanceRunId = existingRecord.OriginalProvenanceRunId ?? existingRecord.QualificationIdentity
            };
            WriteStateAtomic(record);
        }

        private void WriteStateAtomic(QualificationStateRecord record)
        {
            string json = QualificationStateJson.Serialize(record);
            File.WriteAllText(StateTempFilePath, json, new UTF8Encoding(false));
            File.Move(StateTempFilePath, StateFilePath, true);
        }

        public bool TryValidateAndReuse(string sourceArchiveSha256, string tiaBuildIdentity, string qualificationRecipeIdentity, string qualifiedArchiveSha256, out QualificationStateRecord reuseRecord)
        {
            reuseRecord = null;
            var existing = Load();
            if (existing == null)
                return false;

            if (existing.StateKind != QualificationStateKind.CompletedSuccess)
                return false;

            if (!string.Equals(existing.SourceArchiveSha256, sourceArchiveSha256, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(existing.TiaBuildIdentity, tiaBuildIdentity, StringComparison.Ordinal))
                return false;

            if (!string.Equals(existing.QualificationRecipeIdentity, qualificationRecipeIdentity, StringComparison.Ordinal))
                return false;

            if (!string.Equals(existing.QualifiedArchiveSha256, qualifiedArchiveSha256, StringComparison.OrdinalIgnoreCase))
                return false;

            reuseRecord = existing;
            return true;
        }

        public void DeleteState()
        {
            if (File.Exists(StateFilePath))
                File.Delete(StateFilePath);
        }
    }

    internal static class QualificationStateJson
    {
        public static string Serialize(QualificationStateRecord record)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "sourceArchiveSha256", record.SourceArchiveSha256, true, true);
            AppendProperty(builder, "tiaBuildIdentity", record.TiaBuildIdentity, true, true);
            AppendProperty(builder, "qualificationIdentity", record.QualificationIdentity, true, true);
            AppendProperty(builder, "qualifiedArchiveSha256", record.QualifiedArchiveSha256, true, true);
            AppendProperty(builder, "qualificationRecipeIdentity", record.QualificationRecipeIdentity, true, true);
            AppendProperty(builder, "stateKind", record.StateKind.ToString(), true, true);
            AppendProperty(builder, "completedAtUtc", record.CompletedAtUtc.ToString("o"), true, true);
            AppendProperty(builder, "originalProvenanceRunId", record.OriginalProvenanceRunId, true, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        public static QualificationStateRecord Deserialize(string json)
        {
            var record = new QualificationStateRecord();
            var lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"sourceArchiveSha256\":"))
                    record.SourceArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"tiaBuildIdentity\":"))
                    record.TiaBuildIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualificationIdentity\":"))
                    record.QualificationIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualifiedArchiveSha256\":"))
                    record.QualifiedArchiveSha256 = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"qualificationRecipeIdentity\":"))
                    record.QualificationRecipeIdentity = ExtractValue(trimmed);
                else if (trimmed.StartsWith("\"stateKind\":"))
                    record.StateKind = Enum.TryParse<QualificationStateKind>(ExtractValue(trimmed), out var sk) ? sk : QualificationStateKind.None;
                else if (trimmed.StartsWith("\"completedAtUtc\":"))
                    DateTime.TryParse(ExtractValue(trimmed), out var dt) ? record.CompletedAtUtc = dt : record.CompletedAtUtc = DateTime.MinValue;
                else if (trimmed.StartsWith("\"originalProvenanceRunId\":"))
                    record.OriginalProvenanceRunId = ExtractValue(trimmed);
            }
            return record;
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