using System;
using System.IO;
using Xunit;
using TiaAutomationFactory.TiaV21Worker;

namespace SiemensBackend.Tests
{
    public sealed class QualificationStateTests
    {
        private const string QualificationIdentity = "olq-0123456789abcdef0123456789abcdef";
        private const string OtherQualificationIdentity = "olq-fedcba9876543210fedcba9876543210";
        private const string SourceSha = "ed8fe3f52e90399475b321e40f7ce842d86e414324f06f1b078f44fcb666eed3";
        private const string OtherSourceSha = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private const string ArchiveSha = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private const string OtherArchiveSha = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        private const string TiaBuild = "Siemens.Engineering v21.0.0.0 (file: 2100.0.121.1)";
        private const string OtherTiaBuild = "Siemens.Engineering v21.0.0.0 (file: 2100.0.121.2)";
        private const string Provenance = "prov-0123456789abcdef";
        private const string CompletedAt = "2026-09-26T09:00:00.0000000Z";

        [Fact]
        public void InProgress_round_trips()
        {
            var state = QualificationState.CreateInProgress(QualificationIdentity, SourceSha, TiaBuild);
            var parsed = QualificationStateJson.Deserialize(QualificationStateJson.Serialize(state));

            Assert.Equal(QualificationStateKind.InProgress, parsed.Kind);
            Assert.Equal(QualificationState.CurrentSchemaVersion, parsed.SchemaVersion);
            Assert.Equal(QualificationRecipeVersion.TXN002, parsed.RecipeVersion);
            Assert.Equal(QualificationState.CurrentRecipeIdentity, parsed.RecipeIdentity);
            Assert.Equal(QualificationIdentity, parsed.QualificationIdentity);
            Assert.Equal(SourceSha, parsed.SourceArchiveSha256);
            Assert.Equal(TiaBuild, parsed.TiaBuildIdentity);
            Assert.False(parsed.IsReuse);
            Assert.False(parsed.NativeReopenSuccess);
        }

        [Fact]
        public void Fresh_completed_success_round_trips()
        {
            var state = Completed(QualificationIdentity, ArchiveSha);
            var parsed = QualificationStateJson.Deserialize(QualificationStateJson.Serialize(state));

            Assert.True(parsed.IsVerifiedCompletedSuccess());
            Assert.Equal(ArchiveSha, parsed.QualifiedArchiveSha256);
            Assert.Equal(Provenance, parsed.OriginalProvenanceRunId);
            Assert.Equal(CompletedAt, parsed.OriginalCompletedAtUtc);
            Assert.False(parsed.IsReuse);
            Assert.True(parsed.NativeReopenSuccess);
        }

        [Fact]
        public void Stored_false_success_round_trips_and_is_stageable()
        {
            using (var root = new TempDirectory())
            {
                var store = new QualificationStateStore(root.Path, QualificationIdentity);
                var state = QualificationState.CreateStoredFalseSuccess(
                    QualificationIdentity,
                    SourceSha,
                    TiaBuild,
                    "phase:test type:InvalidOperationException hresult:0x80131509",
                    "bounded-test-detail");

                store.WriteStaging("attempt1", state);

                QualificationState parsed;
                Assert.True(store.TryLoadStaging("attempt1", out parsed));
                Assert.Equal(QualificationStateKind.StoredFalseSuccess, parsed.Kind);
                Assert.Equal(state.Failure, parsed.Failure);
                store.QuarantineStaging("attempt1", "test");
                Assert.False(File.Exists(store.GetStagingManifestPath("attempt1")));
            }
        }

        [Fact]
        public void Strict_parser_rejects_malformed_unknown_and_duplicate_fields()
        {
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize("[]"));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize("{\"unknown\":\"x\"}"));

            string json = QualificationStateJson.Serialize(
                QualificationState.CreateInProgress(QualificationIdentity, SourceSha, TiaBuild));
            string duplicate = json.Replace(
                "\"kind\": \"InProgress\",",
                "\"kind\": \"InProgress\",\n  \"kind\": \"InProgress\",");
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(duplicate));
        }

        [Theory]
        [InlineData("kind")]
        [InlineData("schemaVersion")]
        [InlineData("recipeVersion")]
        [InlineData("recipeIdentity")]
        [InlineData("qualificationIdentity")]
        [InlineData("sourceArchiveSha256")]
        [InlineData("tiaBuildIdentity")]
        [InlineData("isReuse")]
        [InlineData("nativeReopenSuccess")]
        public void Strict_parser_rejects_missing_required_properties(string property)
        {
            string json = QualificationStateJson.Serialize(
                QualificationState.CreateInProgress(QualificationIdentity, SourceSha, TiaBuild));
            string mutated = RemoveProperty(json, property);
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(mutated));
        }

        [Fact]
        public void Strict_parser_rejects_wrong_json_types_and_null_required_values()
        {
            string json = QualificationStateJson.Serialize(
                QualificationState.CreateInProgress(QualificationIdentity, SourceSha, TiaBuild));

            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace("\"isReuse\": false", "\"isReuse\": \"false\"")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace("\"nativeReopenSuccess\": false", "\"nativeReopenSuccess\": null")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace("\"kind\": \"InProgress\"", "\"kind\": 0")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace(
                        "\"" + QualificationState.CurrentSchemaVersion + "\"",
                        "null")));
        }

        [Fact]
        public void Strict_parser_rejects_schema_recipe_identity_hash_and_timestamp_mutations()
        {
            string json = QualificationStateJson.Serialize(Completed(QualificationIdentity, ArchiveSha));

            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace(QualificationState.CurrentSchemaVersion, "olq-state-v1")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace(QualificationState.CurrentRecipeIdentity, "olq-txn-001-v1")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace("\"recipeVersion\": \"TXN002\"", "\"recipeVersion\": \"999\"")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace(SourceSha, SourceSha.ToUpperInvariant())));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace(TiaBuild, "Siemens.Engineering v20.0.0.0")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    json.Replace(CompletedAt, "2026-09-26T11:00:00+02:00")));
        }

        [Fact]
        public void Strict_parser_rejects_numeric_enum_aliases()
        {
            string inProgressJson = QualificationStateJson.Serialize(
                QualificationState.CreateInProgress(QualificationIdentity, SourceSha, TiaBuild));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    inProgressJson.Replace("\"kind\": \"InProgress\"", "\"kind\": \"0\"")));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    inProgressJson.Replace("\"recipeVersion\": \"TXN002\"", "\"recipeVersion\": \"2\"")));

            string completedJson = QualificationStateJson.Serialize(
                Completed(QualificationIdentity, ArchiveSha));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    completedJson.Replace("\"kind\": \"CompletedSuccess\"", "\"kind\": \"1\"")));

            string falseSuccessJson = QualificationStateJson.Serialize(
                QualificationState.CreateStoredFalseSuccess(
                    QualificationIdentity,
                    SourceSha,
                    TiaBuild,
                    "phase:test type:InvalidOperationException hresult:0x80131509",
                    "bounded-test-detail"));
            Assert.Throws<QualificationStateException>(
                () => QualificationStateJson.Deserialize(
                    falseSuccessJson.Replace("\"kind\": \"StoredFalseSuccess\"", "\"kind\": \"2\"")));
        }

        [Fact]
        public void Public_diagnostics_redact_raw_path_username_and_vendor_text()
        {
            const string sentinelPath = @"C:\Users\sentinel-user\VendorSecret\private-library.zal19";
            const string sentinelUser = "sentinel-user";
            const string sentinelVendor = "VendorSecret";
            string rawDetails = sentinelPath + " " + sentinelUser + " " + sentinelVendor +
                " arbitrary vendor exception payload";

            string sanitized = QualificationPublicDiagnostics.SanitizeFailureDetails(
                "retrieve-with-upgrade",
                rawDetails);

            Assert.StartsWith(
                "details:redacted phase:retrieve-with-upgrade rawfp:",
                sanitized);
            Assert.True(sanitized.Length <= 96);
            Assert.DoesNotContain(sentinelPath, sanitized);
            Assert.DoesNotContain(sentinelUser, sanitized);
            Assert.DoesNotContain(sentinelVendor, sanitized);
            Assert.DoesNotContain("arbitrary vendor exception payload", sanitized);
            Assert.Equal(
                sanitized,
                QualificationPublicDiagnostics.SanitizeFailureDetails(
                    "retrieve-with-upgrade",
                    rawDetails));
            Assert.Equal(
                "Source archive not found.",
                QualificationPublicDiagnostics.SourceArchiveNotFoundMessage());
        }

        [Fact]
        public void Qualification_cli_boundary_redacts_unexpected_filesystem_exception()
        {
            const string sentinelPath = @"C:\Users\boundary-user\VendorSecret\manifest.json";
            const string sentinelUser = "boundary-user";
            const string sentinelVendor = "VendorSecret";
            var stderr = new StringWriter();

            int exitCode = QualificationPublicDiagnostics.ExecuteBoundary(
                "cli-boundary",
                delegate
                {
                    throw new IOException(
                        "Cannot write " + sentinelPath + " for " + sentinelUser + " " + sentinelVendor);
                },
                stderr);

            string publicError = stderr.ToString();
            Assert.Equal(1, exitCode);
            Assert.StartsWith(
                "details:redacted phase:cli-boundary rawfp:",
                publicError.Trim());
            Assert.True(publicError.Trim().Length <= 80);
            Assert.DoesNotContain(sentinelPath, publicError);
            Assert.DoesNotContain(sentinelUser, publicError);
            Assert.DoesNotContain(sentinelVendor, publicError);
            Assert.DoesNotContain("Cannot write", publicError);
        }

        [Fact]
        public void Qualification_cli_boundary_preserves_success_exit_code_without_stderr()
        {
            var stderr = new StringWriter();

            int exitCode = QualificationPublicDiagnostics.ExecuteBoundary(
                "cli-boundary",
                delegate { return 7; },
                stderr);

            Assert.Equal(7, exitCode);
            Assert.Equal(string.Empty, stderr.ToString());
        }

        [Fact]
        public void Public_diagnostics_reject_noncanonical_phase_tokens()
        {
            Assert.Throws<ArgumentException>(
                () => QualificationPublicDiagnostics.SanitizeFailureDetails(
                    "retrieve with upgrade",
                    "raw"));
            Assert.Throws<ArgumentException>(
                () => QualificationPublicDiagnostics.SanitizeFailureDetails(
                    "UPPERCASE",
                    "raw"));
        }

        [Fact]
        public void Durable_completed_record_rejects_reuse_shape_or_false_reopen()
        {
            var reuse = Completed(QualificationIdentity, ArchiveSha);
            reuse.IsReuse = true;
            Assert.Throws<QualificationStateException>(() => QualificationStateJson.Serialize(reuse));

            var falseReopen = Completed(QualificationIdentity, ArchiveSha);
            falseReopen.NativeReopenSuccess = false;
            Assert.Throws<QualificationStateException>(() => QualificationStateJson.Serialize(falseReopen));
        }

        [Fact]
        public void Completed_record_is_published_once_and_never_overwritten()
        {
            using (var root = new TempDirectory())
            {
                var store = new QualificationStateStore(root.Path, QualificationIdentity);
                store.WriteStaging("first", Completed(QualificationIdentity, ArchiveSha));
                string error;
                Assert.True(store.TryPromoteStagingToCompleted("first", out error), error);

                string completedPath = store.GetCompletedManifestPath();
                string original = File.ReadAllText(completedPath);

                store.WriteStaging("second", Completed(QualificationIdentity, ArchiveSha));
                Assert.False(store.TryPromoteStagingToCompleted("second", out error));
                Assert.Equal(original, File.ReadAllText(completedPath));
            }
        }

        [Fact]
        public void Valid_completed_record_cannot_be_quarantined_but_invalid_record_can()
        {
            using (var root = new TempDirectory())
            {
                var validStore = new QualificationStateStore(root.Path, QualificationIdentity);
                validStore.WriteStaging("valid", Completed(QualificationIdentity, ArchiveSha));
                string error;
                Assert.True(validStore.TryPromoteStagingToCompleted("valid", out error), error);
                Assert.False(validStore.TryQuarantineInvalidCompleted("x", out error));
                Assert.True(File.Exists(validStore.GetCompletedManifestPath()));

                var invalidStore = new QualificationStateStore(root.Path, OtherQualificationIdentity);
                File.WriteAllText(invalidStore.GetCompletedManifestPath(), "{\"kind\":\"broken\"}");
                Assert.True(invalidStore.TryQuarantineInvalidCompleted("repair", out error), error);
                Assert.False(File.Exists(invalidStore.GetCompletedManifestPath()));
            }
        }

        [Fact]
        public void Orphan_archive_is_quarantined_without_touching_unrelated_completed_evidence()
        {
            using (var root = new TempDirectory())
            {
                var protectedStore = new QualificationStateStore(root.Path, OtherQualificationIdentity);
                protectedStore.WriteStaging(
                    "protected",
                    Completed(OtherQualificationIdentity, OtherArchiveSha));
                string error;
                Assert.True(
                    protectedStore.TryPromoteStagingToCompleted("protected", out error),
                    error);
                string protectedBytes = File.ReadAllText(protectedStore.GetCompletedManifestPath());

                string orphan = System.IO.Path.Combine(root.Path, QualificationIdentity + ".zal21");
                File.WriteAllText(orphan, "unverified");
                var store = new QualificationStateStore(root.Path, QualificationIdentity);
                Assert.True(store.TryQuarantineOrphanFile(orphan, "attempt", out error), error);
                Assert.False(File.Exists(orphan));
                Assert.Equal(protectedBytes, File.ReadAllText(protectedStore.GetCompletedManifestPath()));
            }
        }

        [Fact]
        public void Interrupted_staging_never_becomes_completed_and_retry_can_succeed()
        {
            using (var root = new TempDirectory())
            {
                var store = new QualificationStateStore(root.Path, QualificationIdentity);
                store.WriteStaging(
                    "interrupted",
                    QualificationState.CreateInProgress(QualificationIdentity, SourceSha, TiaBuild));

                Assert.False(store.CompletedManifestExists());
                QualificationState interrupted;
                Assert.True(store.TryLoadStaging("interrupted", out interrupted));
                Assert.Equal(QualificationStateKind.InProgress, interrupted.Kind);

                store.QuarantineStaging("interrupted", "retry");
                store.WriteStaging("retry", Completed(QualificationIdentity, ArchiveSha));
                string error;
                Assert.True(store.TryPromoteStagingToCompleted("retry", out error), error);

                QualificationState completed;
                Assert.True(store.TryLoadCompleted(out completed));
                Assert.True(completed.IsVerifiedCompletedSuccess());
            }
        }

        [Fact]
        public void Reuse_identity_requires_every_bound_identity_and_preserves_provenance()
        {
            var state = Completed(QualificationIdentity, ArchiveSha);

            Assert.True(QualificationStateStore.MatchesReuseIdentity(
                state,
                QualificationIdentity,
                SourceSha,
                TiaBuild,
                QualificationState.CurrentRecipeIdentity,
                ArchiveSha));
            Assert.False(QualificationStateStore.MatchesReuseIdentity(
                state,
                OtherQualificationIdentity,
                SourceSha,
                TiaBuild,
                QualificationState.CurrentRecipeIdentity,
                ArchiveSha));
            Assert.False(QualificationStateStore.MatchesReuseIdentity(
                state,
                QualificationIdentity,
                OtherSourceSha,
                TiaBuild,
                QualificationState.CurrentRecipeIdentity,
                ArchiveSha));
            Assert.False(QualificationStateStore.MatchesReuseIdentity(
                state,
                QualificationIdentity,
                SourceSha,
                OtherTiaBuild,
                QualificationState.CurrentRecipeIdentity,
                ArchiveSha));
            Assert.False(QualificationStateStore.MatchesReuseIdentity(
                state,
                QualificationIdentity,
                SourceSha,
                TiaBuild,
                "olq-txn-001-v1",
                ArchiveSha));
            Assert.False(QualificationStateStore.MatchesReuseIdentity(
                state,
                QualificationIdentity,
                SourceSha,
                TiaBuild,
                QualificationState.CurrentRecipeIdentity,
                OtherArchiveSha));
            Assert.Equal(Provenance, state.OriginalProvenanceRunId);
            Assert.Equal(CompletedAt, state.OriginalCompletedAtUtc);
        }

        [Fact]
        public void Repeated_reuse_validation_preserves_completed_manifest_and_original_provenance()
        {
            using (var root = new TempDirectory())
            {
                var store = new QualificationStateStore(root.Path, QualificationIdentity);
                store.WriteStaging("fresh", Completed(QualificationIdentity, ArchiveSha));
                string error;
                Assert.True(store.TryPromoteStagingToCompleted("fresh", out error), error);

                string completedPath = store.GetCompletedManifestPath();
                string originalManifest = File.ReadAllText(completedPath);

                QualificationState firstReuse;
                Assert.True(store.TryLoadCompleted(out firstReuse));
                Assert.True(QualificationStateStore.MatchesReuseIdentity(
                    firstReuse,
                    QualificationIdentity,
                    SourceSha,
                    TiaBuild,
                    QualificationState.CurrentRecipeIdentity,
                    ArchiveSha));

                QualificationState secondReuse;
                Assert.True(store.TryLoadCompleted(out secondReuse));
                Assert.True(QualificationStateStore.MatchesReuseIdentity(
                    secondReuse,
                    QualificationIdentity,
                    SourceSha,
                    TiaBuild,
                    QualificationState.CurrentRecipeIdentity,
                    ArchiveSha));

                Assert.Equal(Provenance, firstReuse.OriginalProvenanceRunId);
                Assert.Equal(CompletedAt, firstReuse.OriginalCompletedAtUtc);
                Assert.Equal(firstReuse.OriginalProvenanceRunId, secondReuse.OriginalProvenanceRunId);
                Assert.Equal(firstReuse.OriginalCompletedAtUtc, secondReuse.OriginalCompletedAtUtc);
                Assert.Equal(originalManifest, File.ReadAllText(completedPath));
            }
        }

        [Fact]
        public void Generated_provenance_and_timestamp_are_parseable_by_strict_state_contract()
        {
            var state = QualificationState.CreateCompletedSuccess(
                QualificationIdentity,
                SourceSha,
                TiaBuild,
                ArchiveSha,
                QualificationStateStore.GenerateProvenanceRunId(),
                QualificationStateStore.GetCurrentUtcTimestamp());

            var parsed = QualificationStateJson.Deserialize(QualificationStateJson.Serialize(state));
            Assert.True(parsed.IsVerifiedCompletedSuccess());
            Assert.StartsWith("prov-", parsed.OriginalProvenanceRunId);
            Assert.EndsWith("Z", parsed.OriginalCompletedAtUtc);
        }

        private static QualificationState Completed(string identity, string archiveSha)
        {
            return QualificationState.CreateCompletedSuccess(
                identity,
                SourceSha,
                TiaBuild,
                archiveSha,
                Provenance,
                CompletedAt);
        }

        private static string RemoveProperty(string json, string property)
        {
            string[] lines = json.Replace("\r", "").Split('\n');
            var output = new System.Collections.Generic.List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("\"" + property + "\":", StringComparison.Ordinal))
                    continue;
                output.Add(lines[i]);
            }

            for (int i = 0; i < output.Count; i++)
            {
                int next = i + 1;
                while (next < output.Count && string.IsNullOrWhiteSpace(output[next]))
                    next++;
                if (next < output.Count && output[next].Trim() == "}" && output[i].TrimEnd().EndsWith(",", StringComparison.Ordinal))
                    output[i] = output[i].TrimEnd().TrimEnd(',');
            }
            return string.Join(Environment.NewLine, output.ToArray());
        }

        private sealed class TempDirectory : IDisposable
        {
            public TempDirectory()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "olq-state-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; private set; }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
            }
        }
    }
}
