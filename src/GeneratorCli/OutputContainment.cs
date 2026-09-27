namespace TiaAutomationFactory.GeneratorCli;

public static class OutputContainment
{
    public const string DiagnosticCode = "GEN-CLI-CONTAINMENT";

    public static string ResolveContainedPath(string outputRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(outputRoot) || string.IsNullOrWhiteSpace(relativePath))
            throw new GeneratorCliException(DiagnosticCode);

        if (LooksRooted(relativePath))
            throw new GeneratorCliException(DiagnosticCode);

        var normalizedSegments = relativePath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (normalizedSegments.Length == 0
            || normalizedSegments.Any(segment => string.Equals(segment, "..", StringComparison.Ordinal)))
        {
            throw new GeneratorCliException(DiagnosticCode);
        }

        var normalizedRelative = string.Join(Path.DirectorySeparatorChar, normalizedSegments);
        var rootFull = Path.GetFullPath(outputRoot);
        var candidateFull = Path.GetFullPath(Path.Combine(rootFull, normalizedRelative));
        var relativeFromRoot = Path.GetRelativePath(rootFull, candidateFull);

        if (Path.IsPathRooted(relativeFromRoot)
            || string.Equals(relativeFromRoot, "..", StringComparison.Ordinal)
            || relativeFromRoot.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relativeFromRoot.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new GeneratorCliException(DiagnosticCode);
        }

        return candidateFull;
    }

    private static bool LooksRooted(string path)
    {
        if (Path.IsPathRooted(path)
            || path.StartsWith("/", StringComparison.Ordinal)
            || path.StartsWith("\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return path.Length >= 3
            && char.IsLetter(path[0])
            && path[1] == ':'
            && (path[2] == '\\' || path[2] == '/');
    }
}
