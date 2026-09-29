using System;
using System.Collections.Generic;
using Xunit;
using TiaAutomationFactory.TiaV21Worker;

namespace SiemensBackend.Tests
{
    public sealed class QualificationLexicalHintsTests
    {
        [Fact]
        public void Context_support_package_requires_support_and_package_in_the_same_string()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Required support package is not installed.",
                null!);

            Assert.Contains("context-support-package", hints);
            Assert.Contains("semantic-required", hints);
            Assert.Contains("semantic-install", hints);
        }

        [Fact]
        public void Context_support_package_requires_both_lexical_signals()
        {
            Assert.DoesNotContain(
                "context-support-package",
                QualificationLexicalHints.DeriveHints("Support is available for this feature.", null!));
            Assert.DoesNotContain(
                "context-support-package",
                QualificationLexicalHints.DeriveHints("The package is available.", null!));
        }

        [Fact]
        public void Context_support_package_coupling_does_not_span_separate_strings()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Support is required.",
                new[] { "The package is available." });

            Assert.DoesNotContain("context-support-package", hints);
        }

        [Fact]
        public void Context_support_package_matches_package_family_variant()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Support package-family missing.",
                null!);

            Assert.Contains("context-support-package", hints);
            Assert.Contains("semantic-missing", hints);
        }

        [Fact]
        public void Context_software_product_requires_software_and_product_in_the_same_string()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Required software product is not installed.",
                null!);

            Assert.Contains("context-software-product", hints);
            Assert.Contains("semantic-required", hints);
            Assert.Contains("semantic-install", hints);
        }

        [Fact]
        public void Context_software_product_requires_both_lexical_signals()
        {
            Assert.DoesNotContain(
                "context-software-product",
                QualificationLexicalHints.DeriveHints("Software is available.", null!));
            Assert.DoesNotContain(
                "context-software-product",
                QualificationLexicalHints.DeriveHints("The product is available.", null!));
        }

        [Fact]
        public void Context_software_product_coupling_does_not_span_separate_strings()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Software is required.",
                new[] { "The product is available." });

            Assert.DoesNotContain("context-software-product", hints);
        }

        [Theory]
        [InlineData("Unsupported library element detected.")]
        [InlineData("The library type cannot be used.")]
        [InlineData("The library object is not supported.")]
        public void Context_library_element_requires_library_and_element_type_or_object(string text)
        {
            Assert.Contains(
                "context-library-element",
                QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Fact]
        public void Context_library_element_requires_coupled_lexical_signals()
        {
            Assert.DoesNotContain(
                "context-library-element",
                QualificationLexicalHints.DeriveHints("The library was inspected.", null!));
            Assert.DoesNotContain(
                "context-library-element",
                QualificationLexicalHints.DeriveHints("The element was inspected.", null!));
        }

        [Fact]
        public void Context_library_element_coupling_does_not_span_separate_strings()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "The library was inspected.",
                new[] { "The object was inspected." });

            Assert.DoesNotContain("context-library-element", hints);
        }

        [Fact]
        public void Context_license_generic_vocabulary_signal()
        {
            var hints = QualificationLexicalHints.DeriveHints("License missing.", null!);

            Assert.Contains("context-license", hints);
            Assert.Contains("semantic-missing", hints);
        }

        [Fact]
        public void Context_version_generic_vocabulary_signal()
        {
            var hints = QualificationLexicalHints.DeriveHints("Incompatible version detected.", null!);

            Assert.Contains("context-version", hints);
            Assert.Contains("semantic-compatible", hints);
        }

        [Fact]
        public void Context_dependency_generic_vocabulary_signal()
        {
            var hints = QualificationLexicalHints.DeriveHints("Missing dependency.", null!);

            Assert.Contains("context-dependency", hints);
            Assert.Contains("semantic-missing", hints);
        }

        [Theory]
        [InlineData("This feature is required.")]
        [InlineData("The product requires a package.")]
        [InlineData("The requirement is not satisfied.")]
        [InlineData("This feature is mandatory.")]
        public void Semantic_required_predicate_family(string text)
        {
            Assert.Contains("semantic-required", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("The component is missing.")]
        [InlineData("The component was not found.")]
        [InlineData("The entry is absent.")]
        [InlineData("The data is lacking.")]
        public void Semantic_missing_predicate_family(string text)
        {
            Assert.Contains("semantic-missing", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("Package not installed.")]
        [InlineData("Installation required for this product.")]
        [InlineData("Please install the prerequisite.")]
        public void Semantic_install_predicate_family(string text)
        {
            Assert.Contains("semantic-install", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("The feature is available.")]
        [InlineData("The feature is unavailable.")]
        public void Semantic_available_predicate_family(string text)
        {
            Assert.Contains("semantic-available", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("Unsupported library element detected.")]
        [InlineData("The library type is not supported.")]
        public void Semantic_unsupported_predicate_family(string text)
        {
            Assert.Contains("semantic-unsupported", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("The version is compatible.")]
        [InlineData("Incompatible version detected.")]
        public void Semantic_compatible_predicate_family(string text)
        {
            Assert.Contains("semantic-compatible", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("Released content found.")]
        [InlineData("Unreleased content found.")]
        public void Semantic_release_predicate_family(string text)
        {
            Assert.Contains("semantic-release", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("Upgrade required for this library.")]
        [InlineData("Migration required for this library.")]
        [InlineData("The content must be migrated.")]
        public void Semantic_upgrade_predicate_family(string text)
        {
            Assert.Contains("semantic-upgrade", QualificationLexicalHints.DeriveHints(text, null!));
        }

        [Theory]
        [InlineData("The library object cannot be used.")]
        [InlineData("You can't use this element.")]
        [InlineData("Unable to use the object.")]
        [InlineData("The element is not usable.")]
        public void Semantic_cannot_use_predicate_family(string text)
        {
            var hints = QualificationLexicalHints.DeriveHints(text, null!);

            Assert.Contains("semantic-cannot-use", hints);
            Assert.DoesNotContain("semantic-unsupported", hints);
        }

        [Fact]
        public void Matching_is_case_insensitive()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "REQUIRED SUPPORT PACKAGE IS NOT INSTALLED.",
                null!);

            Assert.Contains("context-support-package", hints);
            Assert.Contains("semantic-required", hints);
            Assert.Contains("semantic-install", hints);
        }

        [Fact]
        public void Detail_texts_contribute_hints()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Main error message.",
                new[]
                {
                    "Required support package is not installed.",
                    "License missing."
                });

            Assert.Contains("context-support-package", hints);
            Assert.Contains("context-license", hints);
            Assert.Contains("semantic-required", hints);
            Assert.Contains("semantic-install", hints);
            Assert.Contains("semantic-missing", hints);
        }

        [Fact]
        public void Duplicate_text_inputs_do_not_duplicate_hints()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Required support package is not installed.",
                new[]
                {
                    "Required support package is not installed.",
                    "Required support package is not installed."
                });

            Assert.Equal(
                new[]
                {
                    "context-support-package",
                    "semantic-install",
                    "semantic-required"
                },
                hints);
        }

        [Fact]
        public void Multiple_hints_are_sorted_and_deduplicated()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Required support package is not installed. Software product missing. "
                    + "Library element not supported. License unavailable. Dependency version.",
                null!);

            Assert.Equal(
                new[]
                {
                    "context-dependency",
                    "context-library-element",
                    "context-license",
                    "context-software-product",
                    "context-support-package",
                    "context-version",
                    "semantic-available",
                    "semantic-install",
                    "semantic-missing",
                    "semantic-required",
                    "semantic-unsupported"
                },
                hints);
        }

        [Fact]
        public void Empty_or_null_inputs_return_no_hints()
        {
            Assert.Empty(QualificationLexicalHints.DeriveHints(null, null!));
            Assert.Empty(QualificationLexicalHints.DeriveHints(string.Empty, new List<string>()));
            Assert.Empty(QualificationLexicalHints.DeriveHints(
                "A target-side engineering failure occurred.",
                new List<string> { null, string.Empty, "Additional reason without a supported predicate." }));
        }

        [Fact]
        public void Sentinel_private_text_never_appears_in_hints_output()
        {
            const string sentinelPath = @"C:\Users\sentinel-user\VendorSecret\private.zal19";
            var hints = QualificationLexicalHints.DeriveHints(
                sentinelPath + " required support package is not installed",
                new[] { "VendorSecret sentinel-user" });

            string publicValue = string.Join(",", hints);
            Assert.Contains("context-support-package", hints);
            Assert.Contains("semantic-required", hints);
            Assert.Contains("semantic-install", hints);
            Assert.DoesNotContain(sentinelPath, publicValue);
            Assert.DoesNotContain("sentinel-user", publicValue);
            Assert.DoesNotContain("VendorSecret", publicValue);
            Assert.DoesNotContain("Users", publicValue);
            Assert.DoesNotContain("private.zal19", publicValue);
        }

        [Fact]
        public void Every_emitted_hint_is_exactly_allowlisted()
        {
            var hints = QualificationLexicalHints.DeriveHints(
                "Required support package is not installed. Software product missing. "
                    + "Library element not supported. License missing. Dependency version incompatible. "
                    + "Released upgrade cannot be used. Unavailable.",
                null!);

            Assert.NotEmpty(hints);
            foreach (string hint in hints)
            {
                Assert.True(QualificationLexicalHints.IsValidHint(hint), "Unexpected hint: " + hint);
            }
        }
    }
}
