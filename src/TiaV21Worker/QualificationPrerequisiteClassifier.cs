using System;
using System.Collections.Generic;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal static class QualificationPrerequisiteClassifier
    {
        public static List<string> Classify(string messageText, IEnumerable<string> detailTexts)
        {
            var tags = new SortedSet<string>(StringComparer.Ordinal);

            AddTags(messageText, tags);
            if (detailTexts != null)
            {
                foreach (string detailText in detailTexts)
                {
                    AddTags(detailText, tags);
                }
            }

            if (tags.Count == 0)
                return new List<string> { "unknown" };

            return new List<string>(tags);
        }

        private static void AddTags(string text, ISet<string> tags)
        {
            if (string.IsNullOrEmpty(text))
                return;

            string lower = text.ToLowerInvariant();

            // Preserve OLQ-DIAG-002 legacy classifications exactly.
            if (ContainsAny(lower, "missing product", "product not found", "product missing"))
                tags.Add("missing-product");
            if (ContainsAny(lower, "unreleased content", "not released", "pre-release version"))
                tags.Add("unreleased-content");
            if (ContainsAny(lower, "unsupported version", "version not supported", "incompatible version"))
                tags.Add("unsupported-version");
            if (ContainsAny(lower, "invalid archive", "corrupt archive", "archive corrupt", "not a valid archive"))
                tags.Add("invalid-archive");
            if (ContainsAny(lower, "access denied", "permission denied", "unauthorized access", "no access"))
                tags.Add("access-denied");
            if (ContainsAny(lower, "user abort", "cancelled by user", "aborted by user"))
                tags.Add("user-abort");
            if (ContainsAny(lower, "target conflict", "conflict with target"))
                tags.Add("target-conflict");
            if (ContainsAny(lower, "license missing", "license not found", "no license"))
                tags.Add("license-missing");

            // New prerequisite categories use coupled phrase families. A generic mention
            // of the noun alone is never enough to classify a missing prerequisite.
            if (ContainsAny(
                lower,
                "missing support package",
                "support package missing",
                "support package is missing",
                "support package not installed",
                "support package is not installed",
                "support package not available",
                "support package is not available",
                "support package unavailable",
                "support package is unavailable",
                "support packages missing",
                "support packages are missing",
                "support packages not installed",
                "support packages are not installed",
                "support packages not available",
                "support packages are not available",
                "support packages unavailable",
                "support packages are unavailable"))
            {
                tags.Add("support-package-missing");
            }

            if (ContainsAny(
                lower,
                "missing software product",
                "software product missing",
                "software product is missing",
                "software product not installed",
                "software product is not installed",
                "software product not available",
                "software product is not available",
                "software product unavailable",
                "software product is unavailable",
                "missing product",
                "product missing",
                "product not installed",
                "product is not installed",
                "product not available",
                "product is not available",
                "product unavailable",
                "product is unavailable"))
            {
                tags.Add("software-product-missing");
            }

            if (ContainsAny(
                lower,
                "unsupported library element",
                "library element unsupported",
                "library element is unsupported",
                "library element not supported",
                "library element is not supported",
                "library element cannot be used",
                "library element can't be used",
                "unsupported library type",
                "library type unsupported",
                "library type is unsupported",
                "library type not supported",
                "library type is not supported",
                "library type cannot be used",
                "library type can't be used",
                "unsupported library object",
                "library object unsupported",
                "library object is unsupported",
                "library object not supported",
                "library object is not supported",
                "library object cannot be used",
                "library object can't be used"))
            {
                tags.Add("unsupported-library-element");
            }
        }

        private static bool ContainsAny(string text, params string[] phrases)
        {
            foreach (string phrase in phrases)
            {
                if (text.Contains(phrase))
                    return true;
            }

            return false;
        }
    }
}
