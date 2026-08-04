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
    /// <para>
    /// <b>RAISED FROM 2,800 — JOHN'S CALL, 2026-08-03, AND ONLY AFTER A FULL
    /// COMPRESSION PASS.</b> The old number was set when there were four worlds;
    /// three more arrived in one session, two of them external and each needing a
    /// line in the standing list, a build box and a refutation row. The pass that
    /// preceded this retired a closed section, a superseded LATER item and about a
    /// hundred and fifty words of prose about the document itself.
    /// <b>The test is meant to force that pass, not to be raised instead of
    /// it</b> — so raising it without one is the failure, and the number moving is
    /// not.
    /// </para>
    /// <para>
    /// <b>RAISED AGAIN TO 4,000 — JOHN'S CALL, 2026-08-03, AND FOR A DIFFERENT
    /// REASON.</b> Not compaction this time but scope: the plan now carries the
    /// three structural limits of a co-occurrence count and the approach to each,
    /// edge kinds, credit over time, variable binding, replay, inhibition and the
    /// scaling order. <b>"I really don't want to lose stuff just because the plan
    /// is too big"</b> — and an idea that never reaches the doc is lost the moment
    /// the session ends, which is a worse failure than a doc that takes longer to
    /// read. The budget still exists, and it still forces a pass when it bites.
    /// </para>
    /// </remarks>
    private const int Budget = 4_000;

    /// <summary>
    /// The budget for PROSE, as against structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>JOHN'S RULE, 2026-08-04: PREFER CUTTING PROSE OVER CUTTING LISTS.</b> A
    /// total word budget is indifferent to what gets retired, so when the doc goes
    /// over, whatever is easiest to delete goes — and that is usually a table row
    /// or a bullet, because a paragraph reads as though it is holding an argument
    /// together. It is the wrong instinct: a bullet is a reminder, and a reminder
    /// is all this doc has to be. <b>The connective tissue can be rederived; the
    /// item cannot.</b>
    /// </para>
    /// <para>
    /// <b>It sits well above the current count and far below the total</b>, so
    /// prose has room to exist where a bullet genuinely will not do, and no room
    /// to creep back into being the default shape.
    /// </para>
    /// </remarks>
    private const int Prose = 400;

    /// <summary>
    /// Whether a line is structure rather than prose.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately syntactic, like the findings rules.</b> The point is not to
    /// judge whether a paragraph is earning its place — it is to make the
    /// distinction mechanical enough that nobody has to argue about it. A heading,
    /// a bullet, a table row, a quote, and the wrapped continuation of a bullet
    /// all count as structure.
    /// </remarks>
    private static bool Structural(string line)
    {
        var trimmed = line.TrimStart();

        if (trimmed.Length == 0) return true;

        if ("-|#>".Contains(trimmed[0], StringComparison.Ordinal)) return true;

        // AN ASTERISK IS A BULLET ONLY WITH A SPACE AFTER IT. `**` opens bold,
        // and this doc leads nearly every sentence with it — counting those as
        // structure would let prose pass the budget by shouting, which is the
        // one way this check could be worth nothing.
        if (trimmed[0] == '*')
            return trimmed.Length > 1 && trimmed[1] == ' ';

        // `1.` and friends — an ordered list is a list.
        if (char.IsAsciiDigit(trimmed[0]) && trimmed.Contains('.', StringComparison.Ordinal))
            return true;

        // A WRAPPED BULLET IS STILL A BULLET. Markdown continues a list item on an
        // indented line, and counting those as prose would make the rule punish
        // line wrapping rather than paragraphs.
        return line.StartsWith("  ", StringComparison.Ordinal);
    }

    private static string Repo() => Tree.Repo();

    private static string Docs() => Tree.Docs();

    private static string Plan() => File.ReadAllText(Path.Combine(Docs(), "plan.md"));

    /// <summary>
    /// What a finding looks like in prose, so the doc can be kept clear of them.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately syntactic rather than clever.</b> The point is not to
    /// detect the idea of a result — it is to make the rule so mechanical that
    /// nobody has to argue about whether a paragraph counts.
    /// </remarks>
    private static readonly (string What, string Pattern)[] Findings =
    [
        ("a measured score", @"\d\.\d{3,}"),
        ("a spread", @"±|\+-"),
        ("a sigma count", @"(?i)\bsigma\b"),
        ("a result marker", @"✅|❌"),
        ("a measured comparison", @"\d[\d,.]* (?:\w+ )*against \d"),
    ];

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
    public void The_doc_is_mostly_structure_and_not_mostly_prose()
    {
        var wordy = Directory
            .EnumerateFiles(Docs(), "*.md")
            .Select(path => (
                Name: Path.GetFileName(path),
                Words: File.ReadAllLines(path)
                    .Where(line => !Structural(line))
                    .Sum(line => line.Split(
                        [' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Length)))
            .Where(doc => doc.Words > Prose)
            .ToList();

        Assert.True(wordy.Count == 0,
            "over the prose budget of " + Prose + " words: "
            + string.Join(", ", wordy.Select(doc => $"{doc.Name} at {doc.Words}"))
            + ". Turn a paragraph into bullets rather than deleting a list item — "
            + "the connective tissue is what can be rederived.");
    }

    [Fact]
    public void The_prose_check_can_still_tell_the_two_apart()
    {
        // THE COMPANION, and without it the check above passes for a predicate
        // that calls everything structural.
        Assert.True(Structural("- a bullet"));
        Assert.True(Structural("| a | table | row |"));
        Assert.True(Structural("## a heading"));
        Assert.True(Structural("  a wrapped bullet"));
        Assert.True(Structural("1. an ordered item"));

        Assert.False(Structural("A sentence that is just a sentence."));
        Assert.False(Structural("**Bold prose is still prose.**"));
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
    public void The_plan_looks_forward_and_records_no_findings()
    {
        // JOHN'S CALL, 2026-08-03: THE PLAN IS WHERE THE PROJECT IS GOING, AND A
        // RESULT IS SOMETHING THAT ALREADY HAPPENED. The two were mixed, and the
        // findings won -- roughly half the doc was scores, and the sections
        // saying what to build next were the ones getting compacted to make room
        // under the word budget.
        //
        // Worse, a finding written here goes stale silently. The commit that
        // produced a number is the honest home for it, the comment beside the
        // mechanism is where anyone touching that mechanism will actually see it,
        // and the test that asserts it is the only copy that cannot drift.
        //
        // THE GUARDS ARE NOT FINDINGS. `DO NOT RE-TRY` and `TRAPS` say what not
        // to do, which is a forward-facing instruction -- so they stay, and this
        // is what keeps their evidence column a reason rather than a readout.
        var plan = Plan();
        var lines = plan.Split('\n');

        var recorded = new List<string>();

        foreach (var (what, pattern) in Findings)
            foreach (var line in lines)
                if (Regex.IsMatch(line, pattern))
                    recorded.Add($"{what}: {line.Trim()}");

        Assert.True(recorded.Count == 0,
            "the plan records findings, and it is meant to be forward-facing. "
            + "Put the number in the commit, in the XML comment beside the "
            + "mechanism, or in the test that asserts it:\n"
            + string.Join("\n", recorded.Take(10)));
    }

    [Fact]
    public void The_forward_facing_check_can_still_fail()
    {
        // THE COMPANION, AND WITHOUT IT THE CHECK ABOVE PASSES FOR A PATTERN SET
        // THAT MATCHES NOTHING. Every rule is asserted against a line that must
        // trip it, so a regex quietly broken by an edit is caught here rather
        // than by the doc slowly refilling with results.
        var findings = new[]
        {
            "Binding — 0.5240, now 0.8798 on the world built to be impossible",
            "0.8077 ± 0.0215 against a chance of 0.0833",
            "12.2 sigma apart, 25.7 clear of chance",
            "| **12** | ✅ CLOSED by 22's fix |",
            "5,000,003 messages against 1,111 on a 12-clique",
        };

        Assert.All(findings, line => Assert.True(
            Findings.Any(rule => Regex.IsMatch(line, rule.Pattern)),
            $"nothing in the rule set notices this is a finding: {line}"));
    }

    [Fact]
    public void Every_fork_the_code_cites_is_in_the_index()
    {
        // THE GHOST-REFERENCE PROBLEM THAT HAS BITTEN THIS PROJECT BEFORE, which
        // is why forks are deliberately never renumbered. The code cites fork
        // numbers in a dozen places; this asserts each one still resolves.
        var plan = Plan();

        var listed = Regex
            .Matches(plan, @"\*\*(\d{1,2})\*\*")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(listed);

        var cited = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src"))
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
            Plan(),
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
        var lines = Plan().Split('\n');

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
