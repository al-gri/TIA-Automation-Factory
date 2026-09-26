using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public QualificationStateKind Kind { get; set; }
        public string SchemaVersion { get; set; } = "olq-state-v2";
        public QualificationRecipeVersion RecipeVersion { get; set; } = QualificationRecipeVersion.TXN002;
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

        public static QualificationState CreateInProgress(
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity)
        {
            return new QualificationState
            {
                Kind = QualificationStateKind.InProgress,
                QualificationIdentity = qualificationIdentity,
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity
            };
        }

        public static QualificationState CreateCompletedSuccess(
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity,
            string qualifiedArchiveSha256,
            string provenanceRunId,
            string completedAtUtc,
            bool isReuse,
            bool nativeReopenSuccess)
        {
            return new QualificationState
            {
                Kind = QualificationStateKind.CompletedSuccess,
                QualificationIdentity = qualificationIdentity,
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                QualifiedArchiveSha256 = qualifiedArchiveSha256,
                OriginalProvenanceRunId = provenanceRunId,
                OriginalCompletedAtUtc = completedAtUtc,
                IsReuse = isReuse,
                NativeReopenSuccess = nativeReopenSuccess
            };
        }

        public static QualificationState CreateStoredFalseSuccess(
            string qualificationIdentity,
            string sourceArchiveSha256,
            string tiaBuildIdentity,
            string failure,
            string failureDetails)
        {
            return new QualificationState
            {
                Kind = QualificationStateKind.StoredFalseSuccess,
                QualificationIdentity = qualificationIdentity,
                SourceArchiveSha256 = sourceArchiveSha256,
                TiaBuildIdentity = tiaBuildIdentity,
                Failure = failure,
                FailureDetails = failureDetails
            };
        }

        public bool IsCompletedSuccess()
        {
            return Kind == QualificationStateKind.CompletedSuccess;
        }

        public bool IsInProgress()
        {
            return Kind == QualificationStateKind.InProgress;
        }

        public bool IsStoredFalseSuccess()
        {
            return Kind == QualificationStateKind.StoredFalseSuccess;
        }
    }

    internal static class QualificationStateJson
    {
        private static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions DeserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string Serialize(QualificationState state)
        {
            return JsonSerializer.Serialize(state, SerializeOptions);
        }

        public static QualificationState Deserialize(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            ValidateJsonStructure(root);

            var state = JsonSerializer.Deserialize<QualificationState>(json, DeserializeOptions);
            if (state == null)
                throw new QualificationStateException("Deserialization returned null.");

            ValidateStateConsistency(state);
            return state;
        }

        private static void ValidateJsonStructure(JsonElement root)
        {
            var allowedProperties = new HashSet<string>(StringComparer.Ordinal)
            {
                "kind", "schemaVersion", "recipeVersion", "qualificationIdentity",
                "sourceArchiveSha256", "tiaBuildIdentity", "qualifiedArchiveSha256",
                "originalProvenanceRunId", "originalCompletedAtUtc", "isReuse",
                "nativeReopenSuccess", "failure", "failureDetails"
            };

            foreach (var property in root.EnumerateObject())
            {
                if (!allowedProperties.Contains(property.Name))
                {
                    throw new QualificationStateException($"Unknown JSON property: {property.Name}");
                }
            }

            var seenProperties = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!seenProperties.Add(property.Name))
                {
                    throw new QualificationStateException($"Duplicate JSON property: {property.Name}");
                }
            }
        }

        private static void ValidateStateConsistency(QualificationState state)
        {
            if (string.IsNullOrWhiteSpace(state.SchemaVersion))
                throw new QualificationStateException("SchemaVersion is required.");
            if (!string.Equals(state.SchemaVersion, "olq-state-v2", StringComparison.Ordinal))
                throw new QualificationStateException($"Invalid SchemaVersion: {state.SchemaVersion}");

            if (!Enum.IsDefined(typeof(QualificationRecipeVersion), state.RecipeVersion))
                throw new QualificationStateException($"Invalid RecipeVersion: {state.RecipeVersion}");

            if (string.IsNullOrWhiteSpace(state.QualificationIdentity))
                throw new QualificationStateException("QualificationIdentity is required.");
            if (!IsSafeProvenance(state.QualificationIdentity))
                throw new QualificationStateException("QualificationIdentity format invalid.");

            if (string.IsNullOrWhiteSpace(state.SourceArchiveSha256))
                throw new QualificationStateException("SourceArchiveSha256 is required.");
            if (!IsLowerSha256(state.SourceArchiveSha256))
                throw new QualificationStateException("SourceArchiveSha256 must be lowercase hex SHA-256.");

            if (string.IsNullOrWhiteSpace(state.TiaBuildIdentity))
                throw new QualificationStateException("TiaBuildIdentity is required.");
            if (!IsExactTiaBuildIdentity(state.TiaBuildIdentity))
                throw new QualificationStateException("TiaBuildIdentity does not match expected value.");

            switch (state.Kind)
            {
                case QualificationStateKind.InProgress:
                    ValidateInProgress(state);
                    break;
                case QualificationStateKind.CompletedSuccess:
                    ValidateCompletedSuccess(state);
                    break;
                case QualificationStateKind.StoredFalseSuccess:
                    ValidateStoredFalseSuccess(state);
                    break;
                default:
                    throw new QualificationStateException($"Invalid Kind: {state.Kind}");
            }
        }

        private static void ValidateInProgress(QualificationState state)
        {
            if (state.IsReuse)
                throw new QualificationStateException("InProgress state must have IsReuse=false.");
            if (state.NativeReopenSuccess)
                throw new QualificationStateException("InProgress state must have NativeReopenSuccess=false.");
            if (!string.IsNullOrWhiteSpace(state.QualifiedArchiveSha256))
                throw new QualificationStateException("InProgress state must not have QualifiedArchiveSha256.");
            if (!string.IsNullOrWhiteSpace(state.OriginalProvenanceRunId))
                throw new QualificationStateException("InProgress state must not have OriginalProvenanceRunId.");
            if (!string.IsNullOrWhiteSpace(state.OriginalCompletedAtUtc))
                throw new QualificationStateException("InProgress state must not have OriginalCompletedAtUtc.");
            if (!string.IsNullOrWhiteSpace(state.Failure))
                throw new QualificationStateException("InProgress state must not have Failure.");
            if (!string.IsNullOrWhiteSpace(state.FailureDetails))
                throw new QualificationStateException("InProgress state must not have FailureDetails.");
        }

        private static void ValidateCompletedSuccess(QualificationState state)
        {
            if (string.IsNullOrWhiteSpace(state.QualifiedArchiveSha256))
                throw new QualificationStateException("CompletedSuccess state requires QualifiedArchiveSha256.");
            if (!IsLowerSha256(state.QualifiedArchiveSha256))
                throw new QualificationStateException("QualifiedArchiveSha256 must be lowercase hex SHA-256.");

            if (string.IsNullOrWhiteSpace(state.OriginalProvenanceRunId))
                throw new QualificationStateException("CompletedSuccess state requires OriginalProvenanceRunId.");
            if (!IsSafeProvenance(state.OriginalProvenanceRunId))
                throw new QualificationStateException("OriginalProvenanceRunId format invalid.");

            if (string.IsNullOrWhiteSpace(state.OriginalCompletedAtUtc))
                throw new QualificationStateException("CompletedSuccess state requires OriginalCompletedAtUtc.");
            if (!IsCanonicalUtcTimestamp(state.OriginalCompletedAtUtc))
                throw new QualificationStateException("OriginalCompletedAtUtc must be canonical UTC timestamp.");

            if (state.IsReuse)
            {
                if (state.NativeReopenSuccess)
                    throw new QualificationStateException("Reuse CompletedSuccess must have NativeReopenSuccess=false.");
            }
            else
            {
                if (!state.NativeReopenSuccess)
                    throw new QualificationStateException("Fresh CompletedSuccess must have NativeReopenSuccess=true.");
            }

            if (!string.IsNullOrWhiteSpace(state.Failure))
                throw new QualificationStateException("CompletedSuccess state must not have Failure.");
            if (!string.IsNullOrWhiteSpace(state.FailureDetails))
                throw new QualificationStateException("CompletedSuccess state must not have FailureDetails.");
        }

        private static void ValidateStoredFalseSuccess(QualificationState state)
        {
            if (state.IsReuse)
                throw new QualificationStateException("StoredFalseSuccess state must have IsReuse=false.");
            if (state.NativeReopenSuccess)
                throw new QualificationStateException("StoredFalseSuccess state must have NativeReopenSuccess=false.");
            if (!string.IsNullOrWhiteSpace(state.QualifiedArchiveSha256))
                throw new QualificationStateException("StoredFalseSuccess state must not have QualifiedArchiveSha256.");
            if (!string.IsNullOrWhiteSpace(state.OriginalProvenanceRunId))
                throw new QualificationStateException("StoredFalseSuccess state must not have OriginalProvenanceRunId.");
            if (!string.IsNullOrWhiteSpace(state.OriginalCompletedAtUtc))
                throw new QualificationStateException("StoredFalseSuccess state must not have OriginalCompletedAtUtc.");

            if (string.IsNullOrWhiteSpace(state.Failure))
                throw new QualificationStateException("StoredFalseSuccess state requires Failure.");
            if (string.IsNullOrWhiteSpace(state.FailureDetails))
                throw new QualificationStateException("StoredFalseSuccess state requires FailureDetails.");
        }

        private static bool IsLowerSha256(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(value, "^[0-9a-f]{64}$");
        }

        private static bool IsSafeProvenance(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$");
        }

        private static bool IsExactTiaBuildIdentity(string value)
        {
            return string.Equals(value, "Siemens.Engineering v21.0.0.0 (file: 2100.0.121.1)", StringComparison.Ordinal);
        }

        private static bool IsCanonicalUtcTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$"))
                return false;

            var styles = System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal;
            if (!DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, styles, out var parsed))
                return false;

            return parsed.Offset == TimeSpan.Zero;
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
                throw new ArgumentException("Output root cannot be empty.", nameof(outputRoot));
            if (string.IsNullOrWhiteSpace(qualificationIdentity))
                throw new ArgumentException("Qualification identity cannot be empty.", nameof(qualificationIdentity));

            _outputRoot = Path.GetFullPath(outputRoot);
            _qualificationIdentity = qualificationIdentity;
        }

        public string GetCompletedManifestPath()
        {
            return Path.Combine(_outputRoot, _qualificationIdentity + ".manifest.json");
        }

        public string GetStagingManifestPath(string attemptId)
        {
            return Path.Combine(_outputRoot, ".staging", _qualificationIdentity + $".{attemptId}.manifest.json");
        }

        public string GetQuarantinePath(string attemptId)
        {
            return Path.Combine(_outputRoot, ".quarantine", _qualificationIdentity + $".{attemptId}.manifest.json");
        }

        public bool TryLoadCompleted(out QualificationState state)
        {
            state = null;
            string path = GetCompletedManifestPath();
            if (!File.Exists(path))
                return false;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                state = QualificationStateJson.Deserialize(json);
                return state.IsCompletedSuccess();
            }
            catch (QualificationStateException)
            {
                return false;
            }
            catch (JsonException)
            {
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
                string json = File.ReadAllText(path, Encoding.UTF8);
                state = QualificationStateJson.Deserialize(json);
                return state.IsInProgress() || state.IsCompletedSuccess();
            }
            catch (QualificationStateException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public void WriteStaging(string attemptId, QualificationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!state.IsInProgress() && !state.IsCompletedSuccess())
                throw new ArgumentException("Only InProgress or CompletedSuccess states can be staged.", nameof(state));

            string stagingDir = Path.Combine(_outputRoot, ".staging");
            Directory.CreateDirectory(stagingDir);

            string path = GetStagingManifestPath(attemptId);
            string json = QualificationStateJson.Serialize(state);
            File.WriteAllText(path, json, new UTF8Encoding(false));
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

            QualificationState state;
            try
            {
                string json = File.ReadAllText(stagingPath, Encoding.UTF8);
                state = QualificationStateJson.Deserialize(json);
            }
            catch (Exception ex)
            {
                error = "Staging manifest deserialization failed: " + ex.Message;
                return false;
            }

            if (!state.IsCompletedSuccess())
            {
                error = "Staging manifest is not a CompletedSuccess state.";
                return false;
            }

            if (File.Exists(completedPath))
            {
                if (!TryLoadCompleted(out var existing))
                {
                    error = "Existing completed manifest is corrupt or invalid.";
                    return false;
                }

                if (!StatesMatchForReuse(existing, state))
                {
                    error = "Existing completed manifest identity mismatch; cannot overwrite.";
                    return false;
                }
            }

            try
            {
                string json = QualificationStateJson.Serialize(state);
                string tempPath = completedPath + ".tmp";
                File.WriteAllText(tempPath, json, new UTF8Encoding(false));
                File.Move(tempPath, completedPath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                error = "Failed to publish completed manifest: " + ex.Message;
                return false;
            }
        }

        public void QuarantineStaging(string attemptId, string reason)
        {
            string stagingPath = GetStagingManifestPath(attemptId);
            if (!File.Exists(stagingPath))
                return;

            string quarantineDir = Path.Combine(_outputRoot, ".quarantine");
            Directory.CreateDirectory(quarantineDir);

            string quarantinePath = GetQuarantinePath(attemptId);
            try
            {
                File.Move(stagingPath, quarantinePath, overwrite: true);
            }
            catch
            {
            }
        }

        public void CleanupStaging(string attemptId)
        {
            string stagingPath = GetStagingManifestPath(attemptId);
            if (File.Exists(stagingPath))
            {
                try { File.Delete(stagingPath); } catch { }
            }
        }

        public static bool StatesMatchForReuse(QualificationState existing, QualificationState candidate)
        {
            return string.Equals(existing.SchemaVersion, candidate.SchemaVersion, StringComparison.Ordinal) &&
                   existing.RecipeVersion == candidate.RecipeVersion &&
                   string.Equals(existing.QualificationIdentity, candidate.QualificationIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.SourceArchiveSha256, candidate.SourceArchiveSha256, StringComparison.Ordinal) &&
                   string.Equals(existing.TiaBuildIdentity, candidate.TiaBuildIdentity, StringComparison.Ordinal) &&
                   string.Equals(existing.QualifiedArchiveSha256, candidate.QualifiedArchiveSha256, StringComparison.Ordinal) &&
                   existing.Kind == candidate.Kind &&
                   string.Equals(existing.OriginalProvenanceRunId, candidate.OriginalProvenanceRunId, StringComparison.Ordinal) &&
                   string.Equals(existing.OriginalCompletedAtUtc, candidate.OriginalCompletedAtUtc, StringComparison.Ordinal);
        }

        public static string GenerateProvenanceRunId()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return "prov-" + Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public static string GetCurrentUtcTimestamp()
        {
            return DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}