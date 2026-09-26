using System;
using System.Collections.Generic;
using Xunit;
using TiaAutomationFactory.TiaV21Worker;

namespace SiemensBackend.Tests
{
    public sealed class QualificationPrerequisiteClassifierTests
    {
        [Theory]
        [InlineData("Required support package is not installed.")]
        [InlineData("Missing support package for this library.")]
        [InlineData("The required support packages are not available.")]
        public void Support_package_missing_requires_explicit_absence(string text)
        {
            Assert.Contains(
                "support-package-missing",
                QualificationPrerequisiteClassifier.Classify(text, null));
        }

        [Theory]
        [InlineData("The support package is listed in the library properties.")]
        [InlineData("Support packages were inspected.")]
        public void Support_package_plain_mention_stays_unknown(string text)
        {
            Assert.Equal(
                new[] { "unknown" },
                QualificationPrerequisiteClassifier.Classify(text, null));
        }

        [Theory]
        [InlineData("Required software product is not installed.")]
        [InlineData("The software product is unavailable.")]
        [InlineData("Product not available for this project.")]
        public void Software_product_missing_requires_explicit_absence(string text)
        {
            Assert.Contains(
                "software-product-missing",
                QualificationPrerequisiteClassifier.Classify(text, null));
        }

        [Theory]
        [InlineData("The software product appears in the library properties.")]
        [InlineData("This product is installed and available.")]
        public void Software_product_plain_mention_does_not_claim_missing(string text)
        {
            Assert.Equal(
                new[] { "unknown" },
                QualificationPrerequisiteClassifier.Classify(text, null));
        }

        [Theory]
        [InlineData("Unsupported library element detected.")]
        [InlineData("The library type cannot be used.")]
        [InlineData("The library object is not supported.")]
        public void Unsupported_library_element_requires_library_context_and_failure_semantics(string text)
        {
            Assert.Contains(
                "unsupported-library-element",
                QualificationPrerequisiteClassifier.Classify(text, null));
        }

        [Theory]
        [InlineData("The library element was inspected.")]
        [InlineData("Unsupported version of the project.")]
        [InlineData("This object cannot be used.")]
        public void Generic_library_or_unsupported_text_does_not_claim_unsupported_library_element(string text)
        {
            var tags = QualificationPrerequisiteClassifier.Classify(text, null);
            Assert.DoesNotContain("unsupported-library-element", tags);
        }

        [Fact]
        public void Existing_diagnostic_tags_are_preserved()
        {
            var tags = QualificationPrerequisiteClassifier.Classify(
                "Missing product; license missing; invalid archive; access denied; target conflict.",
                null);

            Assert.Contains("missing-product", tags);
            Assert.Contains("software-product-missing", tags);
            Assert.Contains("license-missing", tags);
            Assert.Contains("invalid-archive", tags);
            Assert.Contains("access-denied", tags);
            Assert.Contains("target-conflict", tags);
        }

        [Fact]
        public void Message_and_detail_texts_contribute_and_output_is_sorted_deduplicated()
        {
            var tags = QualificationPrerequisiteClassifier.Classify(
                "Required support package is not installed. Product missing.",
                new[]
                {
                    "The library object cannot be used.",
                    "Required support package is not installed.",
                    "License missing."
                });

            Assert.Equal(
                new[]
                {
                    "license-missing",
                    "missing-product",
                    "software-product-missing",
                    "support-package-missing",
                    "unsupported-library-element"
                },
                tags);
        }

        [Fact]
        public void Matching_is_case_insensitive()
        {
            Assert.Contains(
                "support-package-missing",
                QualificationPrerequisiteClassifier.Classify(
                    "REQUIRED SUPPORT PACKAGE IS NOT INSTALLED.",
                    null));
        }

        [Fact]
        public void No_supported_category_returns_unknown()
        {
            Assert.Equal(
                new[] { "unknown" },
                QualificationPrerequisiteClassifier.Classify(
                    "A target-side engineering failure occurred.",
                    new[] { "Additional reason without a supported category." }));
        }

        [Fact]
        public void Sentinel_private_text_never_appears_in_classifier_output()
        {
            const string sentinelPath = @"C:\Users\sentinel-user\VendorSecret\private.zal19";
            var tags = QualificationPrerequisiteClassifier.Classify(
                sentinelPath + " required support package is not installed",
                new[] { "VendorSecret sentinel-user" });

            string publicValue = string.Join(",", tags);
            Assert.Contains("support-package-missing", tags);
            Assert.DoesNotContain(sentinelPath, publicValue);
            Assert.DoesNotContain("sentinel-user", publicValue);
            Assert.DoesNotContain("VendorSecret", publicValue);
        }

        [Fact]
        public void Empty_or_null_inputs_are_fail_closed_to_unknown()
        {
            Assert.Equal(
                new[] { "unknown" },
                QualificationPrerequisiteClassifier.Classify(null, null));
            Assert.Equal(
                new[] { "unknown" },
                QualificationPrerequisiteClassifier.Classify(string.Empty, new List<string> { null, string.Empty }));
        }
    }
}
