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
    /// The budget, in <b>words</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The number is arbitrary; having one is not.</b> It sits a little above
    /// the current length, so ordinary edits pass and a doc that has started
    /// growing without bound fails. <b>To add something, retire something</b> —
    /// which is the whole mechanism, because nothing else has ever made anyone
    /// delete a stale paragraph.
    /// </para>
    /// <para>
    /// <b>IT WAS LINES, AND LINES WERE THE WRONG UNIT.</b> The thing being
    /// budgeted is context, and a markdown table row is one line however long it
    /// is — so an hour of compacting long table cells moved the count by two and
    /// the doc was no cheaper to load. Words track what is actually being spent.
    /// </para>
    /// </remarks>
    private const int Budget = 2_800;

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
            .Select(path => (
                Name: Path.GetFileName(path),
                Words: File.ReadAllText(path)
                    .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length))
            .Where(doc => doc.Words > Budget)
            .ToList();

        Assert.True(oversized.Count == 0,
            "over the budget of " + Budget + " words: " +
            string.Join(", ", oversized.Select(doc => $"{doc.Name} at {doc.Words}")) +
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
    public void A_ticked_box_means_the_type_exists_and_an_unticked_one_means_it_does_not()
    {
        // JOHN'S ASK, 2026-08-03: KEEP THE PLAN AND THE CODE IN SYNC, IN BOTH
        // DIRECTIONS. Building something forces it out of the plan, because the
        // box stays wrong until someone ticks it; and planning to build
        // something that already exists fails immediately rather than sitting
        // there looking like work.
        var known = typeof(Codes.Code).Assembly
            .GetExportedTypes()
            .Where(type => !type.IsNested)
            .Select(type => type.Name.Contains('`', StringComparison.Ordinal)
                ? type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]
                : type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var boxes = Regex.Matches(
            File.ReadAllText(Path.Combine(Docs(), "plan.md")),
            @"^- \[( |x)\] `([A-Za-z]+)`",
            RegexOptions.Multiline);

        Assert.NotEmpty(boxes);

        var wrong = boxes
            .Select(box => (Ticked: box.Groups[1].Value == "x", Type: box.Groups[2].Value))
            .Where(entry => entry.Ticked != known.Contains(entry.Type))
            .Select(entry => entry.Ticked
                ? $"{entry.Type} is ticked and does not exist"
                : $"{entry.Type} exists and is not ticked")
            .ToList();

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    [Fact]
    public void Every_refuted_row_says_what_would_revive_it()
    {
        // A REFUTATION IS CONDITIONAL ON ITS CONFIGURATION, and this project has
        // already had to revive two arms whose reason for being dead had quietly
        // expired -- the empty-cell workaround and the temporal window. A row
        // without a revival condition is a superstition rather than a finding,
        // so the shape is enforced rather than encouraged.
        var lines = File.ReadAllLines(Path.Combine(Docs(), "plan.md"));

        var start = Array.FindIndex(lines, line =>
            line.StartsWith("## DO NOT RE-TRY", StringComparison.Ordinal));

        Assert.True(start >= 0, "the refuted section is gone");

        var rows = lines
            .Skip(start)
            .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal) || line == lines[start])
            .Where(line => line.StartsWith("| ", StringComparison.Ordinal))
            .Where(line => !line.Contains("---", StringComparison.Ordinal))
            .Skip(1)
            .ToList();

        Assert.NotEmpty(rows);

        var malformed = rows
            .Where(row => row.Split('|', StringSplitOptions.TrimEntries)
                .Where(cell => cell.Length > 0).Count() != 3)
            .ToList();

        Assert.True(malformed.Count == 0,
            "a refuted row must be `what | what refuted it | what would revive it`, " +
            $"all on one line: {string.Join(" // ", malformed)}");
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
