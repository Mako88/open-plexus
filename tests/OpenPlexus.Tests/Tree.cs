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
    /// A fetched corpus, or a named reason it is not there.
    /// </summary>
    /// <param name="named">The directory <c>corpora/fetch.sh</c> extracts it to.</param>
    /// <remarks>
    /// <b>Throws with the command that fixes it, and never skips.</b> A corpus test
    /// that quietly passes when the corpus is absent reports green for a question it
    /// never asked — which is the same failure as a check wired so it cannot fire.
    /// </remarks>
    public static string Corpus(string named)
    {
        var corpus = Path.Combine(Repo(), "corpora", named);

        return Directory.Exists(corpus)
            ? corpus
            : throw new DirectoryNotFoundException(
                $"{named} is not at {corpus}. Fetch it with:\n    bash corpora/fetch.sh");
    }

    /// <summary>
    /// The bAbI task files, which two worlds now read and once said so twice.
    /// </summary>
    /// <remarks>
    /// <b>ONE PLACE BECAUSE THERE ARE TWO READERS OF IT.</b> <see cref="OpenPlexus.Worlds.Babi"/>
    /// feeds the walk and <see cref="OpenPlexus.Worlds.Recalled"/> feeds the commitment
    /// learner, and the corpus is the same eleven megabytes on disk either way.
    /// </remarks>
    public static string Babi() => Path.Combine(Corpus("tasks_1-20_v1-2"), "en");

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
