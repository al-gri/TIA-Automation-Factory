using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal enum QualificationStateKind
    {
        InProgress = 0,
        CompletedSuccess = 1,
        StoredFalseSuccess = 2
    }

    internal enum QualificationRecipeVersion
    {
        TXN002 = 2
    }

    internal sealed class QualificationState
    {
        public const string CurrentSchemaVersion = "olq-state-v2";
        public const string CurrentRecipeIdentity = "olq-txn-002-v1";
        public const string ExpectedTiaBuildIdentity = "Siemens.Engineering v21.0.0.0 (file: 2100.0.121.1)";

        public QualificationStateKind Kind { get; set; }
        public string SchemaVersion { get; set; }
        public QualificationRecipeVersion RecipeVersion { get; set; }
        public string RecipeIdentity { get; set; }
        public string QualificationIdentity { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string TiaBuildIdentity { get; set; }
        public string QualifiedArchiveSha256 { get; set; }
        public string OriginalProvenanceRunId { get; set; }
        public string OriginalCompletedAtUtc { get; set; }
        public bool IsReuse { get; set; }
        public bool NativeReopenSuccess { get; set; }
        public string Failure { get; set; }
        public string FailureDetails { get; set; }

        private static QualificationState NewBase(
            QualificationStateKind kind,
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity)
        {
            return new QualificationState
            {
                Kind = kind,
                SchemaVersion = CurrentSchemaVersion,
                RecipeVersion = QualificationRecipeVersion.TXN002,
                RecipeIdentity = CurrentRecipeIdentity,
                QualificationIdentity = qualificationIdentity,
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                IsReuse = false,
                NativeReopenSuccess = false
            };
        }

        public static QualificationState CreateInProgress(
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity)
        {
            return NewBase(
                QualificationStateKind.InProgress,
                qualificationIdentity,
                sourceArchiveSha256,
                tiaBuildIdentity);
        }

        public static QualificationState CreateCompletedSuccess(
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity,
            string qualifiedArchiveSha256,
            string provenanceRunId,
            string completedAtUtc)
        {
            var state = NewBase(
                QualificationStateKind.CompletedSuccess,
                qualificationIdentity,
                sourceArchiveSha256,
                tiaBuildIdentity);
            state.QualifiedArchiveSha256 = qualifiedArchiveSha256;
            state.OriginalProvenanceRunId = provenanceRunId;
            state.OriginalCompletedAtUtc = completedAtUtc;
            state.NativeReopenSuccess = true;
            return state;
        }

        public static QualificationState CreateStoredFalseSuccess(
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity,
            string failure,
            string failureDetails)
        {
            var state = NewBase(
                QualificationStateKind.StoredFalseSuccess,
                qualificationIdentity,
                sourceArchiveSha256,
                tiaBuildIdentity);
            state.Failure = failure;
            state.FailureDetails = failureDetails;
            return state;
        }

        public bool IsVerifiedCompletedSuccess()
        {
            return Kind == QualificationStateKind.CompletedSuccess &&
                   !IsReuse &&
                   NativeReopenSuccess;
        }
    }

    internal static class QualificationStateJson
    {
        private enum JsonValueKind
        {
            String,
            Boolean,
            Null,
            Number
        }

        private sealed class JsonValue
        {
            public JsonValueKind Kind;
            public string Text;
            public bool Boolean;
        }

        public static string Serialize(QualificationState state)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            ValidateStateConsistency(state);

            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendString(builder, "kind", state.Kind.ToString(), true);
            AppendString(builder, "schemaVersion", state.SchemaVersion, true);
            AppendString(builder, "recipeVersion", state.RecipeVersion.ToString(), true);
            AppendString(builder, "recipeIdentity", state.RecipeIdentity, true);
            AppendString(builder, "qualificationIdentity", state.QualificationIdentity, true);
            AppendString(builder, "sourceArchiveSha256", state.SourceArchiveSha256, true);
            AppendString(builder, "tiaBuildIdentity", state.TiaBuildIdentity, true);

            if (state.QualifiedArchiveSha256 != null)
                AppendString(builder, "qualifiedArchiveSha256", state.QualifiedArchiveSha256, true);
            if (state.OriginalProvenanceRunId != null)
                AppendString(builder, "originalProvenanceRunId", state.OriginalProvenanceRunId, true);
            if (state.OriginalCompletedAtUtc != null)
                AppendString(builder, "originalCompletedAtUtc", state.OriginalCompletedAtUtc, true);

            AppendBoolean(builder, "isReuse", state.IsReuse, true);
            AppendBoolean(builder, "nativeReopenSuccess", state.NativeReopenSuccess,
                state.Failure != null || state.FailureDetails != null);

            if (state.Failure != null)
                AppendString(builder, "failure", state.Failure, state.FailureDetails != null);
            if (state.FailureDetails != null)
                AppendString(builder, "failureDetails", state.FailureDetails, false);

            builder.AppendLine("}");
            return builder.ToString();
        }

        public static QualificationState Deserialize(string json)
        {
            var properties = ParseFlatObject(json);

            RequireAllowedOnly(properties, new[]
            {
                "kind", "schemaVersion", "recipeVersion", "recipeIdentity",
                "qualificationIdentity", "sourceArchiveSha256", "tiaBuildIdentity",
                "qualifiedArchiveSha256", "originalProvenanceRunId", "originalCompletedAtUtc",
                "isReuse", "nativeReopenSuccess", "failure", "failureDetails"
            });

            var state = new QualificationState
            {
                Kind = ParseKind(RequireString(properties, "kind")),
                SchemaVersion = RequireString(properties, "schemaVersion"),
                RecipeVersion = ParseRecipeVersion(RequireString(properties, "recipeVersion")),
                RecipeIdentity = RequireString(properties, "recipeIdentity"),
                QualificationIdentity = RequireString(properties, "qualificationIdentity"),
                SourceArchiveSha256 = RequireString(properties, "sourceArchiveSha256"),
                TiaBuildIdentity = RequireString(properties, "tiaBuildIdentity"),
                QualifiedArchiveSha256 = OptionalString(properties, "qualifiedArchiveSha256"),
                OriginalProvenanceRunId = OptionalString(properties, "originalProvenanceRunId"),
                OriginalCompletedAtUtc = OptionalString(properties, "originalCompletedAtUtc"),
                IsReuse = RequireBoolean(properties, "isReuse"),
                NativeReopenSuccess = RequireBoolean(properties, "nativeReopenSuccess"),
                Failure = OptionalString(properties, "failure"),
                FailureDetails = OptionalString(properties, "failureDetails")
            };

            ValidateStateConsistency(state);
            return state;
        }

        private static QualificationStateKind ParseKind(string value)
        {
            if (string.Equals(value, "InProgress", StringComparison.Ordinal))
                return QualificationStateKind.InProgress;
            if (string.Equals(value, "CompletedSuccess", StringComparison.Ordinal))
                return QualificationStateKind.CompletedSuccess;
            if (string.Equals(value, "StoredFalseSuccess", StringComparison.Ordinal))
                return QualificationStateKind.StoredFalseSuccess;

            throw new QualificationStateException("Invalid kind.");
        }

        private static QualificationRecipeVersion ParseRecipeVersion(string value)
        {
            if (string.Equals(value, "TXN002", StringComparison.Ordinal))
                return QualificationRecipeVersion.TXN002;

            throw new QualificationStateException("Invalid recipeVersion.");
        }

        private static void ValidateStateConsistency(QualificationState state)
        {
            if (!string.Equals(state.SchemaVersion, QualificationState.CurrentSchemaVersion, StringComparison.Ordinal))
                throw new QualificationStateException("Invalid schemaVersion.");
            if (state.RecipeVersion != QualificationRecipeVersion.TXN002)
                throw new QualificationStateException("Invalid recipeVersion.");
            if (!string.Equals(state.RecipeIdentity, QualificationState.CurrentRecipeIdentity, StringComparison.Ordinal))
                throw new QualificationStateException("Invalid recipeIdentity.");
            if (!IsSafeToken(state.QualificationIdentity))
                throw new QualificationStateException("Invalid qualificationIdentity.");
            if (!IsLowerSha256(state.SourceArchiveSha256))
                throw new QualificationStateException("Invalid sourceArchiveSha256.");
            if (!string.Equals(state.TiaBuildIdentity, QualificationState.ExpectedTiaBuildIdentity, StringComparison.Ordinal))
                throw new QualificationStateException("Invalid tiaBuildIdentity.");

            if (state.Kind == QualificationStateKind.InProgress)
            {
                RequireAbsent(state.QualifiedArchiveSha256, "qualifiedArchiveSha256");
                RequireAbsent(state.OriginalProvenanceRunId, "originalProvenanceRunId");
                RequireAbsent(state.OriginalCompletedAtUtc, "originalCompletedAtUtc");
                RequireAbsent(state.Failure, "failure");
                RequireAbsent(state.FailureDetails, "failureDetails");
                if (state.IsReuse || state.NativeReopenSuccess)
                    throw new QualificationStateException("InProgress state has invalid success flags.");
                return;
            }

            if (state.Kind == QualificationStateKind.CompletedSuccess)
            {
                if (!IsLowerSha256(state.QualifiedArchiveSha256))
                    throw new QualificationStateException("CompletedSuccess requires qualifiedArchiveSha256.");
                if (!IsSafeToken(state.OriginalProvenanceRunId))
                    throw new QualificationStateException("CompletedSuccess requires originalProvenanceRunId.");
                if (!IsCanonicalUtcTimestamp(state.OriginalCompletedAtUtc))
                    throw new QualificationStateException("CompletedSuccess requires canonical originalCompletedAtUtc.");
                if (state.IsReuse || !state.NativeReopenSuccess)
                    throw new QualificationStateException("Durable CompletedSuccess must represent a fresh verified reopen.");
                RequireAbsent(state.Failure, "failure");
                RequireAbsent(state.FailureDetails, "failureDetails");
                return;
            }

            if (state.Kind == QualificationStateKind.StoredFalseSuccess)
            {
                RequireAbsent(state.QualifiedArchiveSha256, "qualifiedArchiveSha256");
                RequireAbsent(state.OriginalProvenanceRunId, "originalProvenanceRunId");
                RequireAbsent(state.OriginalCompletedAtUtc, "originalCompletedAtUtc");
                if (state.IsReuse || state.NativeReopenSuccess)
                    throw new QualificationStateException("StoredFalseSuccess state has invalid success flags.");
                if (string.IsNullOrWhiteSpace(state.Failure))
                    throw new QualificationStateException("StoredFalseSuccess requires failure.");
                return;
            }

            throw new QualificationStateException("Invalid state kind.");
        }

        private static void RequireAbsent(string value, string name)
        {
            if (value != null)
                throw new QualificationStateException(name + " is not allowed for this state.");
        }

        private static bool IsLowerSha256(string value)
        {
            return value != null &&
                   value.Length == 64 &&
                   Regex.IsMatch(value, "^[0-9a-f]{64}$");
        }

        private static bool IsSafeToken(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length <= 128 &&
                   Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$");
        }

        private static bool IsCanonicalUtcTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$"))
            {
                return false;
            }

            DateTimeOffset parsed;
            var styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
            return DateTimeOffset.TryParse(
                       value,
                       CultureInfo.InvariantCulture,
                       styles,
                       out parsed) &&
                   parsed.Offset == TimeSpan.Zero;
        }

        private static void RequireAllowedOnly(
            IDictionary<string, JsonValue> properties,
            IEnumerable<string> allowed)
        {
            var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal);
            foreach (var key in properties.Keys)
            {
                if (!allowedSet.Contains(key))
                    throw new QualificationStateException("Unknown JSON property: " + key);
            }
        }

        private static string RequireString(IDictionary<string, JsonValue> properties, string name)
        {
            JsonValue value;
            if (!properties.TryGetValue(name, out value))
                throw new QualificationStateException("Missing JSON property: " + name);
            if (value.Kind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.Text))
                throw new QualificationStateException("JSON property must be a non-empty string: " + name);
            return value.Text;
        }

        private static string OptionalString(IDictionary<string, JsonValue> properties, string name)
        {
            JsonValue value;
            if (!properties.TryGetValue(name, out value))
                return null;
            if (value.Kind == JsonValueKind.Null)
                return null;
            if (value.Kind != JsonValueKind.String)
                throw new QualificationStateException("JSON property must be a string or null: " + name);
            return value.Text;
        }

        private static bool RequireBoolean(IDictionary<string, JsonValue> properties, string name)
        {
            JsonValue value;
            if (!properties.TryGetValue(name, out value))
                throw new QualificationStateException("Missing JSON property: " + name);
            if (value.Kind != JsonValueKind.Boolean)
                throw new QualificationStateException("JSON property must be boolean: " + name);
            return value.Boolean;
        }

        private static Dictionary<string, JsonValue> ParseFlatObject(string json)
        {
            if (json == null)
                throw new QualificationStateException("JSON input is null.");

            int index = 0;
            SkipWhitespace(json, ref index);
            Expect(json, ref index, '{');
            SkipWhitespace(json, ref index);

            var result = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            if (TryConsume(json, ref index, '}'))
            {
                SkipWhitespace(json, ref index);
                if (index != json.Length)
                    throw new QualificationStateException("Trailing JSON content.");
                return result;
            }

            while (true)
            {
                SkipWhitespace(json, ref index);
                string key = ParseJsonString(json, ref index);
                if (result.ContainsKey(key))
                    throw new QualificationStateException("Duplicate JSON property: " + key);

                SkipWhitespace(json, ref index);
                Expect(json, ref index, ':');
                SkipWhitespace(json, ref index);
                JsonValue value = ParseJsonValue(json, ref index);
                result.Add(key, value);

                SkipWhitespace(json, ref index);
                if (TryConsume(json, ref index, '}'))
                    break;
                Expect(json, ref index, ',');
            }

            SkipWhitespace(json, ref index);
            if (index != json.Length)
                throw new QualificationStateException("Trailing JSON content.");
            return result;
        }

        private static JsonValue ParseJsonValue(string json, ref int index)
        {
            if (index >= json.Length)
                throw new QualificationStateException("Unexpected end of JSON.");

            if (json[index] == '"')
                return new JsonValue { Kind = JsonValueKind.String, Text = ParseJsonString(json, ref index) };

            if (StartsWith(json, index, "true"))
            {
                index += 4;
                return new JsonValue { Kind = JsonValueKind.Boolean, Boolean = true };
            }
            if (StartsWith(json, index, "false"))
            {
                index += 5;
                return new JsonValue { Kind = JsonValueKind.Boolean, Boolean = false };
            }
            if (StartsWith(json, index, "null"))
            {
                index += 4;
                return new JsonValue { Kind = JsonValueKind.Null };
            }

            int start = index;
            if (json[index] == '-')
                index++;
            bool hasDigit = false;
            while (index < json.Length && char.IsDigit(json[index]))
            {
                hasDigit = true;
                index++;
            }
            if (index < json.Length && json[index] == '.')
            {
                index++;
                while (index < json.Length && char.IsDigit(json[index]))
                {
                    hasDigit = true;
                    index++;
                }
            }
            if (hasDigit)
                return new JsonValue { Kind = JsonValueKind.Number, Text = json.Substring(start, index - start) };

            throw new QualificationStateException("Unsupported JSON value.");
        }

        private static string ParseJsonString(string json, ref int index)
        {
            Expect(json, ref index, '"');
            var builder = new StringBuilder();

            while (index < json.Length)
            {
                char ch = json[index++];
                if (ch == '"')
                    return builder.ToString();
                if (ch < 0x20)
                    throw new QualificationStateException("Control character in JSON string.");
                if (ch != '\\')
                {
                    builder.Append(ch);
                    continue;
                }

                if (index >= json.Length)
                    throw new QualificationStateException("Invalid JSON escape.");

                char escape = json[index++];
                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length)
                            throw new QualificationStateException("Invalid unicode escape.");
                        int code;
                        if (!int.TryParse(
                            json.Substring(index, 4),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture,
                            out code))
                        {
                            throw new QualificationStateException("Invalid unicode escape.");
                        }
                        builder.Append((char)code);
                        index += 4;
                        break;
                    default:
                        throw new QualificationStateException("Invalid JSON escape.");
                }
            }

            throw new QualificationStateException("Unterminated JSON string.");
        }

        private static void AppendString(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append("  \"").Append(Escape(name)).Append("\": ");
            if (value == null)
                builder.Append("null");
            else
                builder.Append('"').Append(Escape(value)).Append('"');
            if (comma)
                builder.Append(',');
            builder.AppendLine();
        }

        private static void AppendBoolean(StringBuilder builder, string name, bool value, bool comma)
        {
            builder.Append("  \"").Append(Escape(name)).Append("\": ")
                .Append(value ? "true" : "false");
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
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
        }

        private static void Expect(string json, ref int index, char expected)
        {
            if (index >= json.Length || json[index] != expected)
                throw new QualificationStateException("Malformed JSON.");
            index++;
        }

        private static bool TryConsume(string json, ref int index, char value)
        {
            if (index < json.Length && json[index] == value)
            {
                index++;
                return true;
            }
            return false;
        }

        private static bool StartsWith(string value, int index, string token)
        {
            if (index + token.Length > value.Length)
                return false;
            return string.CompareOrdinal(value, index, token, 0, token.Length) == 0;
        }
    }

    internal static class QualificationPublicDiagnostics
    {
        public static string SanitizeFailureDetails(string phaseToken, string rawDetails)
        {
            if (string.IsNullOrWhiteSpace(phaseToken) ||
                phaseToken.Length > 64 ||
                !Regex.IsMatch(phaseToken, "^[a-z0-9-]+$"))
            {
                throw new ArgumentException("Invalid public diagnostic phase token.", "phaseToken");
            }

            string fingerprint = "none";
            if (!string.IsNullOrEmpty(rawDetails))
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawDetails));
                    fingerprint = BitConverter.ToString(hash)
                        .Replace("-", "")
                        .ToLowerInvariant()
                        .Substring(0, 16);
                }
            }

            return "details:redacted phase:" + phaseToken + " rawfp:" + fingerprint;
        }

        public static string SourceArchiveNotFoundMessage()
        {
            return "Source archive not found.";
        }
    }

    internal sealed class QualificationStateException : Exception
    {
        public QualificationStateException(string message) : base(message) { }
    }

    internal sealed class QualificationStateStore
    {
        private readonly string _outputRoot;
        private readonly string _qualificationIdentity;

        public QualificationStateStore(string outputRoot, string qualificationIdentity)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
                throw new ArgumentException("Output root cannot be empty.", "outputRoot");
            if (string.IsNullOrWhiteSpace(qualificationIdentity))
                throw new ArgumentException("Qualification identity cannot be empty.", "qualificationIdentity");

            _outputRoot = Path.GetFullPath(outputRoot);
            _qualificationIdentity = qualificationIdentity;
        }

        public string GetCompletedManifestPath()
        {
            return Path.Combine(_outputRoot, _qualificationIdentity + ".manifest.json");
        }

        public string GetStagingManifestPath(string attemptId)
        {
            return Path.Combine(
                _outputRoot,
                ".staging",
                _qualificationIdentity + "." + attemptId + ".manifest.json");
        }

        public bool CompletedManifestExists()
        {
            return File.Exists(GetCompletedManifestPath());
        }

        public bool TryLoadCompleted(out QualificationState state)
        {
            state = null;
            string path = GetCompletedManifestPath();
            if (!File.Exists(path))
                return false;

            try
            {
                state = QualificationStateJson.Deserialize(File.ReadAllText(path, Encoding.UTF8));
                return state.IsVerifiedCompletedSuccess();
            }
            catch
            {
                state = null;
                return false;
            }
        }

        public bool TryLoadStaging(string attemptId, out QualificationState state)
        {
            state = null;
            string path = GetStagingManifestPath(attemptId);
            if (!File.Exists(path))
                return false;

            try
            {
                state = QualificationStateJson.Deserialize(File.ReadAllText(path, Encoding.UTF8));
                return true;
            }
            catch
            {
                state = null;
                return false;
            }
        }

        public void WriteStaging(string attemptId, QualificationState state)
        {
            if (string.IsNullOrWhiteSpace(attemptId))
                throw new ArgumentException("Attempt id cannot be empty.", "attemptId");
            if (state == null)
                throw new ArgumentNullException("state");

            string stagingDir = Path.Combine(_outputRoot, ".staging");
            Directory.CreateDirectory(stagingDir);
            File.WriteAllText(
                GetStagingManifestPath(attemptId),
                QualificationStateJson.Serialize(state),
                new UTF8Encoding(false));
        }

        public bool TryPromoteStagingToCompleted(string attemptId, out string error)
        {
            error = null;
            string stagingPath = GetStagingManifestPath(attemptId);
            string completedPath = GetCompletedManifestPath();

            if (!File.Exists(stagingPath))
            {
                error = "Staging manifest does not exist.";
                return false;
            }
            if (File.Exists(completedPath))
            {
                error = "Completed manifest already exists and is immutable.";
                return false;
            }

            QualificationState state;
            try
            {
                state = QualificationStateJson.Deserialize(File.ReadAllText(stagingPath, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                error = "Staging manifest validation failed: " + ex.GetType().Name;
                return false;
            }

            if (!state.IsVerifiedCompletedSuccess())
            {
                error = "Staging manifest is not a fresh verified CompletedSuccess record.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(_outputRoot);
                File.Move(stagingPath, completedPath);
                return true;
            }
            catch (Exception ex)
            {
                error = "Completed manifest publication failed: " + ex.GetType().Name;
                return false;
            }
        }

        public bool TryQuarantineInvalidCompleted(string attemptId, out string error)
        {
            error = null;
            string completedPath = GetCompletedManifestPath();
            if (!File.Exists(completedPath))
                return true;

            QualificationState existing;
            if (TryLoadCompleted(out existing))
            {
                error = "Completed manifest is valid and must not be quarantined.";
                return false;
            }

            return TryMoveToQuarantine(completedPath, attemptId + ".invalid-state", out error);
        }

        public bool TryQuarantineOrphanFile(string path, string attemptId, out string error)
        {
            error = null;
            if (!File.Exists(path))
                return true;
            return TryMoveToQuarantine(path, attemptId + ".orphan", out error);
        }

        public void QuarantineStaging(string attemptId, string reason)
        {
            string stagingPath = GetStagingManifestPath(attemptId);
            if (!File.Exists(stagingPath))
                return;

            string ignored;
            TryMoveToQuarantine(stagingPath, attemptId + ".attempt", out ignored);
        }

        public void CleanupStaging(string attemptId)
        {
            string stagingPath = GetStagingManifestPath(attemptId);
            if (File.Exists(stagingPath))
                File.Delete(stagingPath);
        }

        private bool TryMoveToQuarantine(string sourcePath, string suffix, out string error)
        {
            error = null;
            try
            {
                string quarantineDir = Path.Combine(_outputRoot, ".quarantine");
                Directory.CreateDirectory(quarantineDir);
                string destination = Path.Combine(
                    quarantineDir,
                    Path.GetFileName(sourcePath) + "." + suffix);
                if (File.Exists(destination))
                {
                    destination += "." + Guid.NewGuid().ToString("N");
                }
                File.Move(sourcePath, destination);
                return true;
            }
            catch (Exception ex)
            {
                error = "Quarantine move failed: " + ex.GetType().Name;
                return false;
            }
        }

        public static bool StatesMatchForReuse(QualificationState existing, QualificationState candidate)
        {
            if (existing == null || candidate == null)
                return false;
            return existing.IsVerifiedCompletedSuccess() &&
                   candidate.IsVerifiedCompletedSuccess() &&
                   string.Equals(existing.SchemaVersion, candidate.SchemaVersion, StringComparison.Ordinal) &&
                   existing.RecipeVersion == candidate.RecipeVersion &&
                   string.Equals(existing.RecipeIdentity, candidate.RecipeIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.QualificationIdentity, candidate.QualificationIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.SourceArchiveSha256, candidate.SourceArchiveSha256, StringComparison.Ordinal) &&
                   string.Equals(existing.TiaBuildIdentity, candidate.TiaBuildIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.QualifiedArchiveSha256, candidate.QualifiedArchiveSha256, StringComparison.Ordinal) &&
                   string.Equals(existing.OriginalProvenanceRunId, candidate.OriginalProvenanceRunId, StringComparison.Ordinal) &&
                   string.Equals(existing.OriginalCompletedAtUtc, candidate.OriginalCompletedAtUtc, StringComparison.Ordinal);
        }

        public static bool MatchesReuseIdentity(
            QualificationState existing,
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity,
            string recipeIdentity,
            string qualifiedArchiveSha256)
        {
            return existing != null &&
                   existing.IsVerifiedCompletedSuccess() &&
                   existing.RecipeVersion == QualificationRecipeVersion.TXN002 &&
                   string.Equals(existing.RecipeIdentity, recipeIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.QualificationIdentity, qualificationIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.SourceArchiveSha256, sourceArchiveSha256, StringComparison.Ordinal) &&
                   string.Equals(existing.TiaBuildIdentity, tiaBuildIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.QualifiedArchiveSha256, qualifiedArchiveSha256, StringComparison.Ordinal);
        }

        public static string GenerateProvenanceRunId()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return "prov-" + Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        public static string GetCurrentUtcTimestamp()
        {
            return DateTimeOffset.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ss.fffffffZ",
                CultureInfo.InvariantCulture);
        }
    }
}
