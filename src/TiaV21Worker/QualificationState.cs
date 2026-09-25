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
        CompletedSuccess
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
        public int SchemaVersion { get; set; }
    }

    internal sealed class QualificationStateManager
    {
        private const int CurrentSchemaVersion = 1;

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
                OriginalProvenanceRunId = null,
                SchemaVersion = CurrentSchemaVersion
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
                OriginalProvenanceRunId = provenanceRunId,
                SchemaVersion = CurrentSchemaVersion
            };
            WriteStateAtomic(record);
        }

        private void WriteStateAtomic(QualificationStateRecord record)
        {
            string json = QualificationStateJson.Serialize(record);
            File.WriteAllText(StateTempFilePath, json, new UTF8Encoding(false));
            if (File.Exists(StateFilePath))
                File.Delete(StateFilePath);
            File.Move(StateTempFilePath, StateFilePath);
        }

        public bool TryValidateAndReuse(string sourceArchiveSha256, string tiaBuildIdentity, string qualificationRecipeIdentity, string qualifiedArchiveSha256, out QualificationStateRecord reuseRecord)
        {
            reuseRecord = null;
            var existing = Load();
            if (existing == null)
                return false;

            if (existing.SchemaVersion != CurrentSchemaVersion)
                return false;

            if (existing.StateKind != QualificationStateKind.CompletedSuccess)
                return false;

            if (!string.Equals(existing.QualificationIdentity, _qualificationIdentity, StringComparison.Ordinal))
                return false;

            if (!string.Equals(existing.SourceArchiveSha256, sourceArchiveSha256, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(existing.TiaBuildIdentity, tiaBuildIdentity, StringComparison.Ordinal))
                return false;

            if (!string.Equals(existing.QualificationRecipeIdentity, qualificationRecipeIdentity, StringComparison.Ordinal))
                return false;

            if (!string.Equals(existing.QualifiedArchiveSha256, qualifiedArchiveSha256, StringComparison.OrdinalIgnoreCase))
                return false;

            if (existing.CompletedAtUtc == DateTime.MinValue)
                return false;

            if (string.IsNullOrEmpty(existing.OriginalProvenanceRunId))
                return false;

            reuseRecord = existing;
            return true;
        }

        public void DeleteState()
        {
            if (File.Exists(StateFilePath))
                File.Delete(StateFilePath);
        }

        public bool HasInvalidStateAlongsideArchive(string qualifiedArchivePath)
        {
            if (!File.Exists(qualifiedArchivePath))
                return false;

            var existing = Load();
            if (existing == null)
                return true;

            if (existing.SchemaVersion != CurrentSchemaVersion)
                return true;

            if (existing.StateKind != QualificationStateKind.CompletedSuccess)
                return true;

            if (!string.Equals(existing.QualificationIdentity, _qualificationIdentity, StringComparison.Ordinal))
                return true;

            if (existing.CompletedAtUtc == DateTime.MinValue)
                return true;

            if (string.IsNullOrEmpty(existing.OriginalProvenanceRunId))
                return true;

            return false;
        }

        public void ClearInvalidState()
        {
            DeleteState();
        }
    }

    internal static class QualificationStateJson
    {
        public static string Serialize(QualificationStateRecord record)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "schemaVersion", record.SchemaVersion.ToString(), false, true);
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
            bool hasSchemaVersion = false;
            bool hasSourceArchiveSha256 = false;
            bool hasTiaBuildIdentity = false;
            bool hasQualificationIdentity = false;
            bool hasQualifiedArchiveSha256 = false;
            bool hasQualificationRecipeIdentity = false;
            bool hasStateKind = false;
            bool hasCompletedAtUtc = false;
            bool hasOriginalProvenanceRunId = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"schemaVersion\":"))
                {
                    record.SchemaVersion = int.TryParse(ExtractValue(trimmed), out var v) ? v : 0;
                    hasSchemaVersion = true;
                }
                else if (trimmed.StartsWith("\"sourceArchiveSha256\":"))
                {
                    record.SourceArchiveSha256 = ExtractValue(trimmed);
                    hasSourceArchiveSha256 = true;
                }
                else if (trimmed.StartsWith("\"tiaBuildIdentity\":"))
                {
                    record.TiaBuildIdentity = ExtractValue(trimmed);
                    hasTiaBuildIdentity = true;
                }
                else if (trimmed.StartsWith("\"qualificationIdentity\":"))
                {
                    record.QualificationIdentity = ExtractValue(trimmed);
                    hasQualificationIdentity = true;
                }
                else if (trimmed.StartsWith("\"qualifiedArchiveSha256\":"))
                {
                    record.QualifiedArchiveSha256 = ExtractValue(trimmed);
                    hasQualifiedArchiveSha256 = true;
                }
                else if (trimmed.StartsWith("\"qualificationRecipeIdentity\":"))
                {
                    record.QualificationRecipeIdentity = ExtractValue(trimmed);
                    hasQualificationRecipeIdentity = true;
                }
                else if (trimmed.StartsWith("\"stateKind\":"))
                {
                    record.StateKind = Enum.TryParse<QualificationStateKind>(ExtractValue(trimmed), out var sk) ? sk : QualificationStateKind.None;
                    hasStateKind = true;
                }
                else if (trimmed.StartsWith("\"completedAtUtc\":"))
                {
                    DateTime.TryParse(ExtractValue(trimmed), out var dt);
                    record.CompletedAtUtc = dt;
                    hasCompletedAtUtc = true;
                }
                else if (trimmed.StartsWith("\"originalProvenanceRunId\":"))
                {
                    record.OriginalProvenanceRunId = ExtractValue(trimmed);
                    hasOriginalProvenanceRunId = true;
                }
            }

            if (!hasSchemaVersion || !hasSourceArchiveSha256 || !hasTiaBuildIdentity ||
                !hasQualificationIdentity || !hasQualifiedArchiveSha256 || !hasQualificationRecipeIdentity ||
                !hasStateKind || !hasCompletedAtUtc || !hasOriginalProvenanceRunId)
            {
                throw new InvalidDataException("Qualification state record missing required fields");
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
                .Replace("\\t", "\\t");
        }
    }
}