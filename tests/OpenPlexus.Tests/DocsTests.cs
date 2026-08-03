using System.Reflection;
using System.Text.RegularExpressions;
using OpenPlexus.Codes;

namespace OpenPlexus.Tests;

/// <summary>
/// The docs, checked against the code that is actually there.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S ASK, 2026-08-02: incremental doc updates miss things.</b> They do,
/// and the failure mode is specific — something gets built, the doc it belongs
/// in is not touched, and months later nobody can tell whether the omission
/// means "undocumented" or "deleted". This runs on every <c>dotnet test</c>, so
/// the gap cannot outlive the commit that opened it.
/// </para>
/// <para>
/// <b>It checks both directions, and the second one is the load-bearing half.</b>
/// A type named in the doc but absent from the code is a ghost reference. A type
/// in the code but absent from the doc is the drift this project actually keeps
/// suffering: <c>ArrivalValue.Lift</c> had no test at all, and a disconnected
/// stamina parameter survived a build, 155 tests and three measurements.
/// </para>
/// </remarks>
public sealed class DocsTests
{
    /// <summary>
    /// Walks up from the test binary until it finds the repository.
    /// </summary>
    /// <remarks>
    /// <b>Throws rather than skipping</b> if it cannot find the docs. A doc check
    /// that silently passes when it cannot read the docs is worse than no check,
    /// because it reports green for a question it never asked.
    /// </remarks>
    private static string Repo()
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

    private static string Read(string name) =>
        File.ReadAllText(Path.Combine(Repo(), "docs", name));

    /// <summary>
    /// Every public type the library exposes.
    /// </summary>
    /// <remarks>
    /// <b>Nested types are excluded</b> — they are an implementation detail of
    /// the type that holds them, and requiring a heading for each would train
    /// everyone to add headings that say nothing.
    /// </remarks>
    private static IReadOnlyList<Type> Public() =>
    [
        .. typeof(Code).Assembly
            .GetExportedTypes()
            .Where(type => !type.IsNested)
            .Where(type => type.Name is not ['<', ..])
            .OrderBy(type => type.Name, StringComparer.Ordinal)
    ];

    /// <summary>Strips the arity marker so `Thought` matches `Thought`1`.</summary>
    private static string Plain(Type type) =>
        type.Name.Contains('`', StringComparison.Ordinal)
            ? type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]
            : type.Name;

    [Fact]
    public void Every_public_type_is_described_in_the_design_doc()
    {
        var design = Read("design.md");

        var missing = Public()
            .Select(Plain)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !design.Contains($"`{name}", StringComparison.Ordinal))
            .ToList();

        Assert.True(missing.Count == 0,
            $"built and never written down: {string.Join(", ", missing)}");
    }

    [Fact]
    public void The_design_doc_names_something_that_exists()
    {
        // THE COMPANION. Without it the test above passes for a doc consisting
        // of every type name in one line, or for an assembly with no public
        // types at all.
        var design = Read("design.md");
        var known = Public().Select(Plain).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(known);

        // Headings of the form `### `Something`` are the doc's own index, so a
        // heading naming a type that no longer exists is a ghost reference.
        // The closing backtick is required, so the folder headings -- `Codes/`,
        // `Graph/` and the rest -- are not read as type names.
        var headings = Regex
            .Matches(design, @"^#{2,3} `([A-Za-z]+)(?:<[^`]*>)?`", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(headings);

        var ghosts = headings.Where(name => !known.Contains(name)).ToList();

        Assert.True(ghosts.Count == 0,
            $"documented and not built: {string.Join(", ", ghosts)}");
    }

    [Fact]
    public void Every_fork_the_code_cites_is_in_the_architecture_index()
    {
        // THE GHOST-REFERENCE PROBLEM THAT HAS BITTEN THIS PROJECT BEFORE, which
        // is why forks are deliberately never renumbered. The code cites fork
        // numbers in a dozen places; this asserts each one still resolves.
        var architecture = Read("architecture.md");

        var listed = Regex
            .Matches(architecture, @"\*\*(\d{1,2})\*\*")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(listed);

        var source = Directory
            .EnumerateFiles(Path.Combine(Repo(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(source);

        var cited = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in source)
            foreach (Match match in Regex.Matches(
                File.ReadAllText(path), @"[Ff]ork (\d{1,2})"))
                cited.Add(match.Groups[1].Value);

        Assert.NotEmpty(cited);

        var dangling = cited.Where(number => !listed.Contains(number)).ToList();

        Assert.True(dangling.Count == 0,
            $"the code cites forks the index does not list: {string.Join(", ", dangling)}");
    }
}
