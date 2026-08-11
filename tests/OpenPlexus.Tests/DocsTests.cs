using System.Text;
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
/// <b>AND IT WAS ON FOR THE LIBRARY ONLY, WHICH MEANT THE TESTS ROTTED FREELY.</b> The
/// sentence above was written about one project and read as being about the tree, so a test
/// citing a deleted type compiled quietly — five files went on explaining themselves in
/// terms of two vote arms that had been removed. It is on for this project now, with
/// CS1591 muted so the rule is <i>what a comment SAYS must resolve</i> rather than <i>every
/// member must have one</i>. Switching it on found twenty-six: six crefs into things that
/// no longer exist, and twenty param tags that had drifted off their signatures.
/// </para>
/// <para>
/// <b>So this file is down to what a compiler cannot check: is the one remaining
/// doc still small, and do the fork numbers the code cites still resolve.</b>
/// </para>
/// </remarks>
public sealed class DocsTests
{
    /// <summary>
    /// The most words ONE item may spend. <b>The cap that replaced the doc-wide
    /// one</b> — see the test that reads it.
    /// </summary>
    /// <remarks>
    /// <b>RAISED FROM 45 ON 2026-08-11, AND BY WHAT IT COST RATHER THAN BY FEEL.</b> John's
    /// rule for this budget is that it must stop a doc growing without ever stopping
    /// information getting written down, and that day it did the second thing: a fork row
    /// carrying a closed finding and a new design decision would not fit, so <i>R scales
    /// with load</i> — John's own, from the fork he wrote — was deleted to afford the
    /// sentence beside it. A cap that is paid for by deleting content is a cap set wrong.
    /// Sixty still refuses the essay the rule was written against; eleven items were 38% of
    /// the doc when it was, and none of them was near this.
    /// </remarks>
    private const int Item = 60;

    /// <summary>
    /// Every section the plan is allowed to have, in order.
    /// </summary>
    /// <remarks>
    /// <b>John's shape, 2026-08-04</b>: the goal, the constraints, what is not yet
    /// done, what was tried and what would revive it, the traps, and the fork index
    /// the code cites. <b>Anything built and decided is not on this list</b>,
    /// because it is in the code.
    /// <para>
    /// <b>AND ONE SECTION ARRIVES BETWEEN THE GOAL AND THE CONSTRAINTS, which is where it
    /// belongs rather than where it was convenient.</b> The first north star is the thing
    /// being built TOWARD — twenty phones and a body — and it sits after the goal because it
    /// is not the goal, and before the constraints because several constraints only make
    /// sense once you know what is going to be running on what.
    /// </para>
    /// </remarks>
    private static readonly string[] Sections =
    [
        "The goal",
        "The first north star",
        "The constraints",
        "TO BUILD",
        "DO NOT RE-TRY",
        "TRAPS",
        "OPEN DEFECTS",
        "FORK NUMBERS THE CODE CITES",
    ];

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

    /// <summary>
    /// What a claim looks like when it is dated rather than stated — <b>the doc's own rule
    /// about citing a TIME, made mechanical.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE RULE WAS WRITTEN AND THEN BROKEN THREE TIMES IN ONE FILE.</b> The plan already
    /// says never to cite a time, because <i>one session ago</i> is true when written and
    /// false forever after. What it did not say is that a DEFAULT is a time: <i>the shipped
    /// timing</i> names whatever ships at the moment of reading, so a row comparing
    /// <i>the shipped timing</i> against a named arm reverses the day that arm ships. One
    /// such row ended up asserting the opposite of the mechanism it described.
    /// </para>
    /// <para>
    /// <b>SO A ROW NAMES THE ARM AND NEVER THE SEAT.</b> <c>AfterFailure</c> means the same
    /// thing forever; <i>the shipped timing</i> does not. Introducing this rule turned up
    /// three rows already rotted — two comparing a default against the arm that had since
    /// become it, and one calling a check unarmed that has been armed and load-bearing since.
    /// </para>
    /// </remarks>
    private static readonly (string What, string Pattern)[] Dated =
    [
        ("a default named as a seat rather than an arm", @"(?i)\bthe shipped \w+"),
        ("a claim dated by the session", @"(?i)\b(this|last|next) (session|morning|afternoon|week)\b"),
        ("a claim dated by the day", @"(?i)\b(today|yesterday|tomorrow)\b(?!'s)"),
    ];

    [Fact]
    public void No_single_item_outgrows_a_line()
    {
        // JOHN'S CALL, 2026-08-04: CAP THE ITEM, NOT THE DOC. A doc-wide ceiling
        // punishes having twelve ideas, which is the wrong thing to discourage. It
        // made several sessions trim good sentences to afford new ones, and the
        // trimming produced worse prose than one pass would have.
        //
        // WHAT ACTUALLY GOES WRONG IS AN ITEM BECOMING AN ESSAY. Eleven had, and
        // between them they were 38% of the doc while saying what the XML comments
        // beside the code already said better. So the rule is per item: name the
        // thing, say enough to recognise it on return, stop. TWELVE NEW IDEAS NOW
        // COST TWELVE LINES and nothing has to be retired to make room.
        var swollen = new List<string>();

        foreach (var path in Directory.EnumerateFiles(Docs(), "*.md"))
            foreach (var item in Items(File.ReadAllText(path)))
            {
                var words = item
                    .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Length;

                if (words > Item)
                    swollen.Add($"{Path.GetFileName(path)}: {words} words — {Opening(item)}");
            }

        Assert.True(swollen.Count == 0,
            $"{swollen.Count} item(s) over the per-item cap of {Item} words. Say what "
            + "the thing is and enough to recognise it later; the reasoning belongs in "
            + "the XML comment beside the mechanism:\n  "
            + string.Join("\n  ", swollen.Take(10)));
    }

    /// <summary>
    /// The doc as a list of ITEMS — bullets and table rows, each with its wrapped
    /// continuation lines folded back in.
    /// </summary>
    /// <remarks>
    /// <b>A wrapped bullet is ONE item</b>, or the cap would be punishing line width
    /// rather than length. Headings and paragraphs are not items; the prose budget
    /// governs those.
    /// </remarks>
    private static IEnumerable<string> Items(string doc)
    {
        var item = new StringBuilder();

        foreach (var line in doc.Split('\n'))
        {
            var trimmed = line.TrimStart();

            var starts = trimmed.StartsWith("- ", StringComparison.Ordinal)
                || (trimmed.StartsWith('|')
                    && !trimmed.StartsWith("|---", StringComparison.Ordinal));

            if (starts)
            {
                if (item.Length > 0) yield return item.ToString();
                item.Clear();
                item.Append(trimmed);
            }
            else if (item.Length > 0 && line.StartsWith("  ", StringComparison.Ordinal))
            {
                item.Append(' ').Append(trimmed);
            }
            else if (item.Length > 0)
            {
                yield return item.ToString();
                item.Clear();
            }
        }

        if (item.Length > 0) yield return item.ToString();
    }

    /// <summary>Enough of an item to find it by.</summary>
    private static string Opening(string item) =>
        item.Length <= 60 ? item : string.Concat(item.AsSpan(0, 60), "...");

    [Fact]
    public void The_item_cap_can_tell_a_wrapped_bullet_from_a_long_one()
    {
        // THE COMPANION, and without it the cap above passes for a reader that
        // splits every bullet at its line breaks and therefore never sees a long
        // one. Two lines of one item must count as one item of both.
        var wrapped = Items("- one two three\n  four five six\n").Single();

        Assert.Equal("- one two three four five six", wrapped);

        Assert.Equal(2, Items("- one\n- two\n").Count());
        Assert.Equal(2, Items("| a | b |\n|---|---|\n| c | d |\n").Count());
    }

    [Fact]
    public void The_doc_holds_these_sections_and_no_others()
    {
        // THE OTHER HALF OF CAPPING THE ITEM RATHER THAN THE DOC. Nothing else stops
        // a new prose section appearing beside the lists — which is exactly how
        // "WHAT A WHOLE SESSION OF THIS SAYS" arrived, 289 words of findings in the
        // one doc whose own first rule is that findings live in the commit.
        //
        // ADDING A SECTION IS A DECISION and should cost a deliberate edit here.
        var found = File.ReadLines(Path.Combine(Docs(), "plan.md"))
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line[3..].Trim())
            .ToList();

        Assert.Equal(Sections, found);
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
    public void No_row_is_dated_rather_than_stated()
    {
        // A ROW THAT NAMES A SEAT INSTEAD OF AN ARM REVERSES ITSELF WHEN THE SEAT CHANGES
        // HANDS, and it does so silently -- nothing goes red, the sentence still parses, and
        // it now says the opposite of what it was written to say. That is worse than a stale
        // number, which at least looks like a number nobody has re-taken.
        var lines = Plan().Split('\n');

        var dated = new List<string>();

        foreach (var (what, pattern) in Dated)
            foreach (var line in lines)
                if (Regex.IsMatch(line, pattern))
                    dated.Add($"{what}: {line.Trim()}");

        Assert.True(dated.Count == 0,
            "the plan dates a claim instead of stating it. Name the arm, not the seat it "
            + "currently holds -- `AfterFailure` means the same thing forever and `the "
            + "shipped timing` does not:\n" + string.Join("\n", dated.Take(10)));
    }

    [Fact]
    public void The_dating_check_can_still_fail()
    {
        // THE COMPANION, FOR THE SAME REASON THE ONE BELOW HAS ONE. A pattern set that
        // matches nothing passes forever and reads exactly like a doc with no dated claims
        // in it.
        var dated = new[]
        {
            "Free under the shipped timing carries no hard round at all",
            "which is what this session's grid settled",
            "`Surprise` is one, today",
        };

        Assert.All(dated, line => Assert.True(
            Dated.Any(rule => Regex.IsMatch(line, rule.Pattern)),
            $"nothing in the rule set notices this is dated: {line}"));

        // AND A ROW THAT MERELY MENTIONS A DAY IS NOT DATED, which is what the lookahead is
        // for. Asserted, because a rule that reddens on ordinary prose gets deleted rather
        // than obeyed.
        Assert.DoesNotContain(Dated, rule =>
            Regex.IsMatch("the depth cap is why it is not today's problem", rule.Pattern));
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

    /// <summary>The test files, by the name a comment would call them.</summary>
    private static HashSet<string> Suites() =>
        Tree.Sources("tests")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && name.EndsWith("Tests", StringComparison.Ordinal))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void Every_suite_the_library_names_in_a_comment_still_exists()
    {
        // THE SAME GHOST REFERENCE AS THE FORK CHECK, THROUGH THE ONE DOOR IT LEAVES OPEN.
        // A `<see cref="..."/>` is enforced by the compiler and a fork number by the check
        // above; a suite named in PROSE is enforced by nothing at all. So a library comment
        // saying *measured in `SomeTests`* keeps compiling forever after that file is
        // renamed, and the reader who goes looking finds nothing and cannot tell whether
        // the measurement moved or never existed.
        //
        // AND IT IS A REAL PATH RATHER THAN A HYPOTHETICAL ONE. Comments in this library
        // now cite suites for their numbers -- that is deliberate, since the plan forbids
        // findings living in the doc, so the citation is how a mechanism points at its own
        // evidence. Which makes the citation load-bearing and therefore worth a budget.
        var suites = Suites();

        Assert.NotEmpty(suites);

        var cited = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src"))
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"\b(\w+Tests)\b"))
                cited.Add(match.Groups[1].Value);

        // NOT `Assert.NotEmpty` ON THE CITATIONS, because a library that names no suite is
        // a perfectly good library and this check has nothing to say about it. The fork
        // check can demand citations exist; this one may only demand they resolve.
        var dangling = cited.Where(name => !suites.Contains(name)).ToList();

        Assert.True(dangling.Count == 0,
            "the library names suites that do not exist — either the file was renamed and "
            + "the comment was not, or the measurement was never written: "
            + string.Join(", ", dangling));
    }

    [Fact]
    public void The_suite_check_can_still_fail()
    {
        // THE COMPANION, BECAUSE A LOOKUP OVER AN EMPTY SET PASSES EVERYTHING. If `Suites`
        // returned nothing the check above would find every citation dangling and fail
        // loudly, which is the safe direction -- but if its matching were loose enough to
        // accept anything, it would pass in silence. This pins both ends.
        var suites = Suites();

        Assert.Contains(nameof(DocsTests), suites);
        Assert.DoesNotContain("AWeatherBalloonTests", suites);
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
