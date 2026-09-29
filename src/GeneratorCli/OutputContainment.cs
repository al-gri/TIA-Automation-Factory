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

        EnsureNoExistingRedirectingComponent(candidateFull);
        return candidateFull;
    }

    private static void EnsureNoExistingRedirectingComponent(string fullPath)
    {
        var pathRoot = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(pathRoot))
            throw new GeneratorCliException(DiagnosticCode);

        var relative = Path.GetRelativePath(pathRoot, fullPath);
        var current = pathRoot;

        if (IsExistingRedirectingPath(current))
            throw new GeneratorCliException(DiagnosticCode);

        if (string.Equals(relative, ".", StringComparison.Ordinal))
            return;

        foreach (var segment in relative.Split(
                     new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);

            if (!TryGetAttributes(current, out var attributes))
                break;

            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new GeneratorCliException(DiagnosticCode);
        }
    }

    private static bool IsExistingRedirectingPath(string path)
    {
        return TryGetAttributes(path, out var attributes)
            && (attributes & FileAttributes.ReparsePoint) != 0;
    }

    private static bool TryGetAttributes(string path, out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            attributes = default;
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            attributes = default;
            return false;
        }
        catch (IOException)
        {
            throw new GeneratorCliException(DiagnosticCode);
        }
        catch (UnauthorizedAccessException)
        {
            throw new GeneratorCliException(DiagnosticCode);
        }
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
