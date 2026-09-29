using System;
using System.Collections.Generic;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal static class QualificationLexicalHints
    {
        private static readonly HashSet<string> AllowedHints = new HashSet<string>(StringComparer.Ordinal)
        {
            "context-support-package",
            "context-software-product",
            "context-license",
            "context-library-element",
            "context-version",
            "context-dependency",
            "semantic-required",
            "semantic-missing",
            "semantic-install",
            "semantic-available",
            "semantic-unsupported",
            "semantic-compatible",
            "semantic-release",
            "semantic-upgrade",
            "semantic-cannot-use"
        };

        public static List<string> DeriveHints(string messageText, IEnumerable<string> detailTexts)
        {
            var hints = new SortedSet<string>(StringComparer.Ordinal);

            AddHintsFromText(messageText, hints);
            if (detailTexts != null)
            {
                foreach (string detailText in detailTexts)
                {
                    AddHintsFromText(detailText, hints);
                }
            }

            return new List<string>(hints);
        }

        public static bool IsValidHint(string hint)
        {
            return hint != null && AllowedHints.Contains(hint);
        }

        private static void AddHintsFromText(string text, SortedSet<string> hints)
        {
            if (string.IsNullOrEmpty(text))
                return;

            string lower = text.ToLowerInvariant();

            AddContextHints(lower, hints);
            AddSemanticHints(lower, hints);
        }

        private static void AddContextHints(string lower, SortedSet<string> hints)
        {
            if (lower.Contains("support") && lower.Contains("package"))
                hints.Add("context-support-package");

            if (lower.Contains("software") && lower.Contains("product"))
                hints.Add("context-software-product");

            if (lower.Contains("library") &&
                (lower.Contains("element") || lower.Contains("type") || lower.Contains("object")))
            {
                hints.Add("context-library-element");
            }

            if (ContainsAny(lower, "license", "licence", "licensing"))
                hints.Add("context-license");

            if (ContainsAny(lower, "version"))
                hints.Add("context-version");

            if (ContainsAny(lower, "dependency", "dependencies"))
                hints.Add("context-dependency");
        }

        private static void AddSemanticHints(string lower, SortedSet<string> hints)
        {
            if (ContainsAny(lower, "required", "requires", "require", "requirement", "mandatory", "must have"))
                hints.Add("semantic-required");

            if (ContainsAny(lower, "missing", "not found", "absent", "lacking"))
                hints.Add("semantic-missing");

            if (ContainsAny(lower, "install"))
                hints.Add("semantic-install");

            if (ContainsAny(lower, "available"))
                hints.Add("semantic-available");

            if (ContainsAny(lower, "unsupported", "not support"))
                hints.Add("semantic-unsupported");

            if (ContainsAny(lower, "compatible"))
                hints.Add("semantic-compatible");

            if (ContainsAny(lower, "release"))
                hints.Add("semantic-release");

            if (ContainsAny(lower, "upgrade", "migration", "migrate"))
                hints.Add("semantic-upgrade");

            if (ContainsAny(
                lower,
                "cannot be used",
                "can't be used",
                "cannot use",
                "can't use",
                "unable to use",
                "not usable",
                "not be used"))
            {
                hints.Add("semantic-cannot-use");
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
