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
            {
                if (File.Exists(StateTempFilePath))
                {
                    try
                    {
                        var record = QualificationStateJson.Deserialize(File.ReadAllText(StateTempFilePath));
                        File.Move(StateTempFilePath, StateFilePath);
                        return record;
                    }
                    catch
                    {
                        File.Delete(StateTempFilePath);
                        return null;
                    }
                }
                return null;
            }

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
            if (File.Exists(StateTempFilePath))
                File.Delete(StateTempFilePath);
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
        private static readonly HashSet<string> KnownFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "schemaVersion",
            "sourceArchiveSha256",
            "tiaBuildIdentity",
            "qualificationIdentity",
            "qualifiedArchiveSha256",
            "qualificationRecipeIdentity",
            "stateKind",
            "completedAtUtc",
            "originalProvenanceRunId"
        };

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
            var seenFields = new HashSet<string>(StringComparer.Ordinal);
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
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex < 0)
                    throw new InvalidDataException("Malformed JSON line: " + trimmed);

                string fieldName = trimmed.Substring(0, colonIndex).Trim().Trim('"');
                if (!KnownFields.Contains(fieldName))
                    throw new InvalidDataException("Unknown field in qualification state: " + fieldName);

                if (!seenFields.Add(fieldName))
                    throw new InvalidDataException("Duplicate field in qualification state: " + fieldName);

                string value = ExtractValueStrict(trimmed, fieldName);

                switch (fieldName)
                {
                    case "schemaVersion":
                        if (!int.TryParse(value, out var v))
                            throw new InvalidDataException("Invalid schemaVersion: " + value);
                        record.SchemaVersion = v;
                        hasSchemaVersion = true;
                        break;
                    case "sourceArchiveSha256":
                        record.SourceArchiveSha256 = ValidateNonEmptyString(value, "sourceArchiveSha256");
                        hasSourceArchiveSha256 = true;
                        break;
                    case "tiaBuildIdentity":
                        record.TiaBuildIdentity = ValidateNonEmptyString(value, "tiaBuildIdentity");
                        hasTiaBuildIdentity = true;
                        break;
                    case "qualificationIdentity":
                        record.QualificationIdentity = ValidateNonEmptyString(value, "qualificationIdentity");
                        hasQualificationIdentity = true;
                        break;
                    case "qualifiedArchiveSha256":
                        record.QualifiedArchiveSha256 = ValidateNonEmptyString(value, "qualifiedArchiveSha256");
                        hasQualifiedArchiveSha256 = true;
                        break;
                    case "qualificationRecipeIdentity":
                        record.QualificationRecipeIdentity = ValidateNonEmptyString(value, "qualificationRecipeIdentity");
                        hasQualificationRecipeIdentity = true;
                        break;
                    case "stateKind":
                        if (!Enum.TryParse<QualificationStateKind>(value, out var sk))
                            throw new InvalidDataException("Invalid stateKind: " + value);
                        record.StateKind = sk;
                        hasStateKind = true;
                        break;
                    case "completedAtUtc":
                        if (!DateTime.TryParseExact(value, "o", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                            throw new InvalidDataException("completedAtUtc must be UTC round-trip format (o): " + value);
                        if (dt.Kind != DateTimeKind.Utc)
                            throw new InvalidDataException("completedAtUtc must be UTC: " + value);
                        record.CompletedAtUtc = dt;
                        hasCompletedAtUtc = true;
                        break;
                    case "originalProvenanceRunId":
                        record.OriginalProvenanceRunId = ValidateNonEmptyString(value, "originalProvenanceRunId");
                        hasOriginalProvenanceRunId = true;
                        break;
                }
            }

            if (!hasSchemaVersion || !hasSourceArchiveSha256 || !hasTiaBuildIdentity ||
                !hasQualificationIdentity || !hasQualifiedArchiveSha256 || !hasQualificationRecipeIdentity ||
                !hasStateKind || !hasCompletedAtUtc || !hasOriginalProvenanceRunId)
            {
                throw new InvalidDataException("Qualification state record missing required fields");
            }

            if (seenFields.Count != KnownFields.Count)
            {
                var missing = new List<string>();
                foreach (var kf in KnownFields)
                {
                    if (!seenFields.Contains(kf))
                        missing.Add(kf);
                }
                throw new InvalidDataException("Qualification state record missing required fields: " + string.Join(", ", missing));
            }

            return record;
        }

        private static string ExtractValueStrict(string line, string fieldName)
        {
            int colonIndex = line.IndexOf(':');
            string value = line.Substring(colonIndex + 1).Trim();
            if (value.EndsWith(","))
                value = value.Substring(0, value.Length - 1).Trim();

            if (value == "null")
                throw new InvalidDataException("Field " + fieldName + " cannot be null");

            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
                return Unescape(value);
            }

            return value;
        }

        private static string ValidateNonEmptyString(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidDataException("Field " + fieldName + " cannot be empty");
            return value;
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