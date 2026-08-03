namespace OpenPlexus.Tests;

/// <summary>
/// Where the source is, for the tests that read it rather than run it.
/// </summary>
/// <remarks>
/// <b>Throws rather than skipping.</b> A check that silently passes when it
/// cannot find the files reports green for a question it never asked, which is
/// the failure mode every test in this suite exists to avoid.
/// </remarks>
public static class Tree
{
    /// <summary>The repository root, found by walking up from the test binary.</summary>
    public static string Repo()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);

        while (here is not null)
        {
            if (Directory.Exists(Path.Combine(here.FullName, "docs"))) return here.FullName;
            here = here.Parent;
        }

        throw new DirectoryNotFoundException(
            $"no docs/ directory above {AppContext.BaseDirectory}");
    }

    /// <summary>The one directory the docs live in.</summary>
    public static string Docs() => Path.Combine(Repo(), "docs");

    /// <summary>
    /// Every hand-written C# file under a directory of the repo.
    /// </summary>
    /// <remarks>
    /// <b>Generated files are excluded, and they have to be.</b> <c>obj/</c> holds
    /// assembly attributes and global usings that nobody wrote and nobody can
    /// edit, so anything asserted about the source has to mean the source.
    /// </remarks>
    /// <param name="under">A directory name directly beneath the repo root.</param>
    public static IReadOnlyList<string> Sources(string under)
    {
        var files = Directory
            .EnumerateFiles(Path.Combine(Repo(), under), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0)
            throw new DirectoryNotFoundException($"no source files under {under}/");

        return files;
    }
}
