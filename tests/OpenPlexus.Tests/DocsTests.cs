using System.Text.RegularExpressions;

namespace OpenPlexus.Tests;

/// <summary>
/// The doc, checked against the code that is actually there — and against a size
/// budget.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S CALL, 2026-08-03: the docs got too big to load, so they stopped
/// being read.</b> `architecture.md` reached 1,646 lines and `design.md` 756, and
/// a doc nobody opens is worse than no doc because it still gets cited. Both were
/// deleted; git holds them.
/// </para>
/// <para>
/// <b>What every piece does now lives in the XML comments beside the code, and
/// the COMPILER enforces those.</b> `GenerateDocumentationFile` is on, so a
/// `param` naming an argument that does not exist (CS1572/1573) or a `cref`
/// pointing at a deleted type (CS1574) fails the build. That check cannot go
/// stale, which no markdown file can promise. It found five ghost references to
/// types deleted weeks earlier on the day it was switched on.
/// </para>
/// <para>
/// <b>So this file is down to what a compiler cannot check: is the one remaining
/// doc still small, and do the fork numbers the code cites still resolve.</b>
/// </para>
/// </remarks>
public sealed class DocsTests
{
    /// <summary>
    /// The budget, in lines.
    /// </summary>
    /// <remarks>
    /// <b>The number is arbitrary; having one is not.</b> It sits a little above
    /// the current length, so ordinary edits pass and a doc that has started
    /// growing without bound fails. <b>To add something, retire something</b> —
    /// which is the whole mechanism, because nothing else has ever made anyone
    /// delete a stale paragraph.
    /// </remarks>
    private const int Budget = 320;

    private static string Repo()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);

        while (here is not null)
        {
            if (Directory.Exists(Path.Combine(here.FullName, "docs"))) return here.FullName;
            here = here.Parent;
        }

        // Throws rather than skipping: a doc check that silently passes when it
        // cannot read the docs reports green for a question it never asked.
        throw new DirectoryNotFoundException(
            $"no docs/ directory above {AppContext.BaseDirectory}");
    }

    private static string Docs() => Path.Combine(Repo(), "docs");

    [Fact]
    public void The_docs_stay_within_their_budget()
    {
        var oversized = Directory
            .EnumerateFiles(Docs(), "*.md")
            .Select(path => (Name: Path.GetFileName(path), Lines: File.ReadAllLines(path).Length))
            .Where(doc => doc.Lines > Budget)
            .ToList();

        Assert.True(oversized.Count == 0,
            "over the budget of " + Budget + " lines: " +
            string.Join(", ", oversized.Select(doc => $"{doc.Name} at {doc.Lines}")) +
            ". Retire something rather than raising this.");
    }

    [Fact]
    public void There_is_still_only_one_doc()
    {
        // THE COMPANION, AND WITHOUT IT THE BUDGET IS TRIVIAL TO DEFEAT: split the
        // doc in two and every file is comfortably under the cap while the total
        // is unchanged. A second doc is a decision, not an accident, so it should
        // cost a deliberate edit here.
        var docs = Directory.EnumerateFiles(Docs(), "*.md").Select(Path.GetFileName).ToList();

        Assert.Equal(["plan.md"], docs);
    }

    [Fact]
    public void Every_fork_the_code_cites_is_in_the_index()
    {
        // THE GHOST-REFERENCE PROBLEM THAT HAS BITTEN THIS PROJECT BEFORE, which
        // is why forks are deliberately never renumbered. The code cites fork
        // numbers in a dozen places; this asserts each one still resolves.
        var plan = File.ReadAllText(Path.Combine(Docs(), "plan.md"));

        var listed = Regex
            .Matches(plan, @"\*\*(\d{1,2})\*\*")
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
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"[Ff]ork (\d{1,2})"))
                cited.Add(match.Groups[1].Value);

        Assert.NotEmpty(cited);

        var dangling = cited.Where(number => !listed.Contains(number)).ToList();

        Assert.True(dangling.Count == 0,
            $"the code cites forks the index does not list: {string.Join(", ", dangling)}");
    }

    [Fact]
    public void The_library_is_built_with_the_doc_contract_switched_on()
    {
        // THE CHECK THAT PROTECTS THE OTHER CHECK. Everything above assumes the
        // compiler is enforcing the XML comments; someone removing
        // GenerateDocumentationFile to quiet a warning would silently take the
        // real doc check with it, and nothing else would notice.
        var project = File.ReadAllText(
            Path.Combine(Repo(), "src", "OpenPlexus", "OpenPlexus.csproj"));

        Assert.Contains("<GenerateDocumentationFile>true", project, StringComparison.Ordinal);

        // And the assembly's own XML file is beside it, which is the same claim
        // made against the build output rather than against the intent.
        Assert.True(
            File.Exists(Path.Combine(AppContext.BaseDirectory, "OpenPlexus.xml")),
            "the library built without its documentation file");
    }
}
