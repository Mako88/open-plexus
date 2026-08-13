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
/// <b>And it was on for the library only, which meant the tests rotted freely.</b> The
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
    /// <b>Raised from 45 on 2026-08-11, and by what it cost rather than by feel.</b> John's
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
    /// The most words the WHOLE doc may spend. <b>A ratchet with one exception, and John
    /// wrote the exception.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's test, 2026-08-12, and it is the one that decides: if it is long enough that
    /// you would hesitate to load all of it, it is too long.</b> The whole point of collapsing
    /// several docs into one was that a session could read the plan WHOLE before starting. A
    /// doc read in pages is the pile of docs it replaced, wearing one filename.
    /// </para>
    /// <para>
    /// <b>And the per-item cap is why this was needed, which is a correction to the comment
    /// above it.</b> Capping the item and not the doc fixed the right fault — an item becoming
    /// an essay — and gave up the only thing that bounded the total. Both budgets were green
    /// at nearly twenty-five thousand words, because twelve new ideas cost twelve lines and
    /// nothing ever said stop. The session that measured this had read the doc in pages all
    /// day without once noticing it was the failure.
    /// </para>
    /// <para>
    /// <b>And it costs no information, which is the objection that retired the old doc-wide
    /// cap.</b> That objection was written when there was nowhere else to put things. There is
    /// now: a finding belongs to the commit that produced it and the test that asserts it, a
    /// mechanism to the XML comment the compiler enforces, a trap with a check to the check.
    /// A doc-wide cap does not delete information — it evicts it to the home that keeps it
    /// honest. What it must never do is delete an item to afford another.
    /// </para>
    /// <para>
    /// <b>THE TARGET IS SIX THOUSAND</b>, roughly eight thousand tokens, which is loadable
    /// without thinking about it. Every pass lowers this constant to what it achieved, so the
    /// doc can never grow back past its own best by ACCIDENT.
    /// </para>
    /// <para>
    /// <b>And it may be raised for something genuinely new — John, 2026-08-13, in those
    /// words.</b> A ratchet that only ever falls says a new idea can only be afforded by
    /// deleting an old one, which is a doc-wide cap deciding what the project may think
    /// about. That is not what it is for. The three conditions are his: <b>the existing
    /// items are reasonable, the new one duplicates nothing, and the doc is still in a
    /// state you would load whole.</b>
    /// </para>
    /// <para>
    /// <b>The third condition is the only one that is not a judgement call, and it is also
    /// the one this check cannot make.</b> So a raise is a deliberate edit to this constant
    /// and reads as one in a diff — the escape hatch is a NUMBER somebody had to type,
    /// rather than prose. Compaction still lowers it every pass; what is refused is a raise
    /// that pays for a rewording.
    /// </para>
    /// </remarks>
    // And 9,809 is the first deliberate raise, which is the rule above being used rather
    // than an exception to it. It buys the rule itself: two lines saying the budget may
    // rise for something genuinely new. A cap that could only fall would have had to be
    // paid for by deleting an item, which is the failure the paragraph describes.
    //
    // 9,831 is the second, and it buys John's own direction: which world is the spine. The
    // doc named no world at all before it, so a session asking which one to grow had the
    // handoff and nothing that outlives a handoff. The three conditions are met -- nothing
    // else in the route says it, no item was deleted, and forty-five words is not the
    // difference between loading this and not.
    //
    // And 9,911 is the third, for two ideas John raised in conversation that the doc had no
    // home for and that a reply loses at the next context window. Text as an IMAGE, which is
    // the only perceptual world whose ground truth stays enumerable; and whether a word should
    // be one hash at all, given that `walked` and `walking` are currently as unrelated as
    // `walked` and `kitchen`. Both duplicate nothing here, and eighty words is not the
    // difference between loading this and not.
    private const int Whole = 9_911;

    /// <summary>
    /// Every section the plan is allowed to have, in order.
    /// </summary>
    /// <remarks>
    /// <b>Anything built and decided is not on this list</b>, because it is in the code.
    /// <para>
    /// <b>John's shape, 2026-08-12, and it went from nine sections to four.</b> The goal, the
    /// architecture, the constraints and the first north star were four headings saying one
    /// thing — here is what must be true when this is finished — and `TO BUILD` was a fifth
    /// listing what is not built yet, which is what a route leaf already says. So the doc
    /// splits on the only line that matters to a reader: what is FIXED against what MOVES.
    /// </para>
    /// <para>
    /// <b>And `open defects` went with them.</b> It was added when whole areas were being
    /// deferred mid-rabbit-hole and a defect could sit unowned for weeks. Handoffs and a CI
    /// that goes green every session carry that now, and a defect that outlives a session is
    /// a `BROKEN` leaf against the requirement it blocks.
    /// </para>
    /// <para>
    /// <b>And the fork index went earlier, which is what the route becoming a tree bought.</b>
    /// Ninety-four flat rows that had to be read whole to find the one bearing on your work.
    /// The numbers still resolve, because
    /// <see cref="Every_fork_the_code_cites_is_in_the_index"/> reads the whole doc rather
    /// than one section of it.
    /// </para>
    /// </remarks>
    private static readonly string[] Sections =
    [
        // Four, and the split is the fixed against the moving. `The destination` holds what
        // must be true when this is finished -- the bet, the requirements, the machine's
        // invariants, the first target, and what the field already knows. None of it moves.
        // `THE ROUTE` is where everything moves, and it is the only section a normal session
        // edits.
        "THE DESTINATION",
        "THE ROUTE",

        // And the two that are cross-cutting by nature. A refutation belongs to the ARM it
        // killed and a trap to the failure CLASS, neither of which is a requirement -- so
        // folding them into the route would scatter them past finding.
        "DO NOT RE-TRY",
        "TRAPS",
    ];

    /// <summary>
    /// The budget for PROSE, as against structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's rule, 2026-08-04: prefer cutting prose over cutting lists.</b> A
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

        // An asterisk is a bullet only with a space after it. `**` opens bold,
        // and this doc leads nearly every sentence with it — counting those as
        // structure would let prose pass the budget by shouting, which is the
        // one way this check could be worth nothing.
        if (trimmed[0] == '*')
            return trimmed.Length > 1 && trimmed[1] == ' ';

        // `1.` and friends — an ordered list is a list.
        if (char.IsAsciiDigit(trimmed[0]) && trimmed.Contains('.', StringComparison.Ordinal))
            return true;

        // A wrapped bullet is still a bullet. Markdown continues a list item on an
        // indented line, and counting those as prose would make the rule punish
        // line wrapping rather than paragraphs.
        return line.StartsWith("  ", StringComparison.Ordinal);
    }

    private static string Repo() => Tree.Repo();

    private static string Docs() => Tree.Docs();

    private static string Plan() => File.ReadAllText(Path.Combine(Docs(), "plan.md"));

    /// <summary>The lines under one heading, at any depth, the heading excluded.</summary>
    /// <remarks>
    /// <b>BY DEPTH RATHER THAN BY `##`</b>, because collapsing nine sections into four made
    /// `THE ARCHITECTURE` a subsection — and a reader hard-coded to one level would have
    /// silently returned nothing for it, which is a correspondence check passing on an empty
    /// list.
    /// </remarks>
    private static string[] Section(string heading)
    {
        var lines = Plan().Split('\n');

        var start = Array.FindIndex(lines, line =>
            line.TrimEnd().EndsWith(' ' + heading, StringComparison.Ordinal)
            && line.StartsWith('#'));

        Assert.True(start >= 0, $"the plan has no `{heading}` heading");

        var depth = lines[start].TakeWhile(character => character == '#').Count();

        return lines
            .Skip(start + 1)
            .TakeWhile(line => !line.StartsWith('#')
                || line.TakeWhile(character => character == '#').Count() > depth)
            .ToArray();
    }

    /// <summary>
    /// The bullets of a section, each with how deep it is nested.
    /// </summary>
    /// <remarks>
    /// <b>Two spaces a level, which is what markdown nests by</b> — so a branch is depth
    /// nought, the requirement under it is depth one, and anything at two or deeper is a
    /// leaf. The route's shape is load-bearing rather than cosmetic, which is why three
    /// checks read it this way instead of matching prose.
    /// <para>
    /// <b>And a wrapped bullet is one bullet, which is not a detail.</b> The first version of
    /// this read single lines, so a leaf's revival clause was invisible whenever it fell past
    /// the line break — and two dead leaves passed the revival check purely because of where
    /// their text happened to wrap. A guard whose verdict moves with line width is worse than
    /// no guard, because it reads as green.
    /// </para>
    /// </remarks>
    private static List<(int Depth, string Text)> Nested(IEnumerable<string> lines)
    {
        var bullets = new List<(int Depth, string Text)>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                bullets.Add((indent / 2, trimmed[2..].Trim()));
            else if (bullets.Count > 0 && trimmed.Length > 0 && indent > 0)
                bullets[^1] = (bullets[^1].Depth, $"{bullets[^1].Text} {trimmed.Trim()}");
        }

        return bullets;
    }

    /// <summary>
    /// Words too common to mean an entry is talking about the same thing as the
    /// architecture line above it.
    /// </summary>
    /// <remarks>
    /// <b>Short enough to be obviously incomplete, which is deliberate.</b> A missing
    /// stopword only WEAKENS the correspondence check, since a spurious match lets a row
    /// pass; it can never redden a doc that is right. So the list is grown when a real edit
    /// slips through rather than guessed at up front.
    /// </remarks>
    private static readonly HashSet<string> Common =
    [
        "that", "this", "what", "with", "from", "have", "must", "never", "always",
        "which", "when", "then", "than", "into", "about", "also", "does", "over",
        "under", "every", "each", "their", "there", "here", "been", "were", "will",
        "would", "could", "rather", "itself", "part", "held", "only",
    ];

    /// <summary>The words of a line that could carry a subject.</summary>
    private static HashSet<string> Significant(string text) =>
        Regex
            .Matches(text, "[A-Za-z]{4,}")
            .Select(match => match.Value.ToLowerInvariant())
            .Where(word => !Common.Contains(word))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// What a route leaf may open with. <b>A closed set, because the point of a status
    /// token is that a reader can sort by it without reading the clause.</b>
    /// </summary>
    /// <remarks>
    /// <b>`Broken` arrived when `open defects` went.</b> That section was retired because
    /// handoffs and a green CI carry what it used to, but a defect and an open question are
    /// different urgencies and `OPEN` flattens them — one wants investigating and the other
    /// wants fixing. The distinction cost one token rather than a section.
    /// </remarks>
    private static readonly string[] Statuses =
        ["NOW", "OPEN", "DEAD", "BLOCKED", "BROKEN", "SETTLED"];

    /// <summary>Whether a line says what would bring an arm back.</summary>
    /// <remarks>
    /// <b>Syntactic like the findings rules, and for the same reason</b> — the check is
    /// worth having only if nobody has to argue about whether a sentence counts. Naming the
    /// `DO NOT RE-TRY` row that holds the condition is allowed, because that row is itself
    /// guarded by <see cref="Every_refuted_row_says_what_would_revive_it"/>.
    /// </remarks>
    private static bool Revivable(string text) =>
        Regex.IsMatch(text, @"(?i)\brevive[sd]?\b|\brevival\b")
        || text.Contains("DO NOT RE-TRY", StringComparison.Ordinal);

    /// <summary>The route's leaves — the fork-bearing lines, whatever branch they hang off.</summary>
    private static List<string> Leaves() =>
        Nested(Section("THE ROUTE"))
            .Where(bullet => bullet.Depth >= 2)
            .Select(bullet => bullet.Text)
            .ToList();

    [Fact]
    public void The_route_tracks_the_architecture_one_for_one()
    {
        // The check that makes keeping the two sections apart safe rather than merely tidy.
        // `The architecture` is the one section that forbids mechanisms, so the route may
        // not be nested inside it -- an edit to a child would drag an edit into the parent
        // and the property would die quietly. Holding them one for one is what buys the
        // separation, and nothing but this says so.
        //
        // It already had something to catch. The route claimed a row per architecture line
        // in its own opening sentence and had twelve against thirteen: *what it is told
        // must be something it can be wrong about* had no row, its obstacle folded into the
        // line above instead. The prose rule had been there for weeks and read as true.
        var must = Nested(Section("THE ARCHITECTURE"))
            .Where(bullet => bullet.Depth == 0)
            .Select(bullet => bullet.Text)
            .ToList();

        Assert.NotEmpty(must);

        var route = Nested(Section("THE ROUTE"));

        var branch = route.FindIndex(bullet =>
            bullet.Depth == 0
            && bullet.Text.Contains("WHAT IT MUST DO", StringComparison.Ordinal));

        Assert.True(branch >= 0, "`THE ROUTE` has no `WHAT IT MUST DO` branch");

        var entries = route
            .Skip(branch + 1)
            .TakeWhile(bullet => bullet.Depth > 0)
            .Where(bullet => bullet.Depth == 1)
            .Select(bullet => bullet.Text)
            .ToList();

        Assert.True(must.Count == entries.Count,
            $"`THE ARCHITECTURE` has {must.Count} lines and `WHAT IT MUST DO` has "
            + $"{entries.Count} entries. Every architecture line gets one, in the same "
            + "order -- an architecture line with no entry is a requirement nothing is "
            + "carrying, and that is the state this check was written to end.");

        // And order, by a word the two share. Matching prose against prose would make the
        // check a style rule; requiring one significant word in common says the entry is
        // about that line without dictating how it is worded.
        //
        // WHAT IT DOES NOT CATCH, said out loud so nobody trusts it further than it goes:
        // two adjacent lines sharing vocabulary can be swapped and still pass. *Told, never
        // architected* and *what it is told must be settleable* are exactly that pair.
        var adrift = entries
            .Select((entry, index) => (entry, index))
            .Where(row => !Significant(row.entry).Overlaps(Significant(must[row.index])))
            .Select(row => $"entry {row.index + 1} `{row.entry}` shares no word with "
                + $"`{Opening(must[row.index])}`")
            .ToList();

        Assert.True(adrift.Count == 0,
            "an entry has drifted off the architecture line it is meant to carry:\n  "
            + string.Join("\n  ", adrift));
    }

    [Fact]
    public void Every_leaf_carries_exactly_one_status()
    {
        // A leaf is a line a reader should be able to sort without reading. That only works
        // if the token is at the front and there is exactly one of it -- a leaf saying both
        // OPEN and SETTLED is a row that has been half-updated, which is the failure this
        // doc's fork table had in a dozen places and no check could see.
        var leaves = Leaves();

        Assert.NotEmpty(leaves);

        var wrong = leaves
            .Where(leaf =>
                Statuses.Count(status =>
                    leaf.Contains($"**{status}**", StringComparison.Ordinal)) != 1
                || !Statuses.Any(status =>
                    leaf.StartsWith($"**{status}**", StringComparison.Ordinal)))
            .ToList();

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} route leaf/leaves must OPEN with exactly one of "
            + string.Join(", ", Statuses) + " in bold:\n  "
            + string.Join("\n  ", wrong.Take(10).Select(Opening)));
    }

    [Fact]
    public void Every_dead_leaf_carries_a_revival_condition()
    {
        // The same rule `do not re-try` is held to, through the door the tree opens. A
        // refutation is conditional on its configuration, and a leaf saying only that
        // something failed is a superstition -- this repo has already had to revive two
        // arms whose reason for being dead had quietly expired.
        var mute = Leaves()
            .Where(leaf => leaf.StartsWith("**DEAD**", StringComparison.Ordinal))
            .Where(leaf => !Revivable(leaf))
            .ToList();

        Assert.True(mute.Count == 0,
            $"{mute.Count} dead leaf/leaves say what failed and not what would bring it "
            + "back. Say what revives it, or name the `DO NOT RE-TRY` row that does:\n  "
            + string.Join("\n  ", mute.Take(10).Select(Opening)));
    }

    [Fact]
    public void The_route_checks_can_still_fail()
    {
        // The companion the three above need, and for the reason every other companion in
        // this file exists: a predicate that accepts everything passes in silence and reads
        // exactly like a doc that is in order.
        Assert.Equal(
            [(0, "a branch"), (1, "an entry"), (2, "a leaf")],
            Nested(["- a branch", "  - an entry", "    - a leaf", "not a bullet"]));

        // The one that was missing, and its absence let two dead leaves pass the revival
        // check on where their text wrapped. A leaf is a bullet, not a line.
        Assert.Equal(
            [(2, "a leaf that revives when the wrapped half is read")],
            Nested(["    - a leaf that revives when", "      the wrapped half is read"]));

        Assert.False(Significant("Malleability is the record").Overlaps(
            Significant("AND IT LEARNS BY BEING WRONG AND FINDING OUT")));

        Assert.True(Significant("Malleability is the record").Overlaps(
            Significant("AND HOW HARD A BELIEF IS TO SHIFT IS ITS OWN RECORD")));

        // And the stopwords must still bite, or the overlap test above passes on any two
        // English sentences and the order half of the correspondence check is worth nothing.
        Assert.False(Significant("What it must never do").Overlaps(
            Significant("EVERY INPUT IS AN ATTRIBUTE, WHICH MUST NEVER BE THE THING")));

        Assert.True(Revivable("**DEAD** — refuted, and it revives when a world holds still"));
        Assert.True(Revivable("**DEAD** — refuted; the row is in DO NOT RE-TRY"));
        Assert.False(Revivable("**DEAD** — built, measured and deleted"));
    }

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
    /// <b>The rule was written and then broken three times in one file.</b> The plan already
    /// says never to cite a time, because <i>one session ago</i> is true when written and
    /// false forever after. What it did not say is that a DEFAULT is a time: <i>the shipped
    /// timing</i> names whatever ships at the moment of reading, so a row comparing
    /// <i>the shipped timing</i> against a named arm reverses the day that arm ships. One
    /// such row ended up asserting the opposite of the mechanism it described.
    /// </para>
    /// <para>
    /// <b>So a row names the arm and never the seat.</b> <c>AfterFailure</c> means the same
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
        // John's call, 2026-08-04: cap the item, not the doc. A doc-wide ceiling
        // punishes having twelve ideas, which is the wrong thing to discourage. It
        // made several sessions trim good sentences to afford new ones, and the
        // trimming produced worse prose than one pass would have.
        //
        // What actually goes wrong is an item becoming an essay. Eleven had, and
        // between them they were 38% of the doc while saying what the XML comments
        // beside the code already said better. So the rule is per item: name the
        // thing, say enough to recognise it on return, stop. Twelve new ideas now
        // cost twelve lines and nothing has to be retired to make room.
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
        // The other half of capping the item rather than the doc. Nothing else stops
        // a new prose section appearing beside the lists — which is exactly how
        // "What a whole session of this says" arrived, 289 words of findings in the
        // one doc whose own first rule is that findings live in the commit.
        //
        // Adding a section is a decision and should cost a deliberate edit here.
        var found = File.ReadLines(Path.Combine(Docs(), "plan.md"))
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line[3..].Trim())
            .ToList();

        Assert.Equal(Sections, found);
    }

    [Fact]
    public void The_whole_doc_still_fits_in_one_reading()
    {
        // The budget that was missing, and its absence is why this doc reached twenty-five
        // thousand words with every other check green. Read the constant's remarks: the rule
        // is John's and it is about whether a session LOADS the thing, not about tidiness.
        var words = Directory
            .EnumerateFiles(Docs(), "*.md")
            .Sum(path => File.ReadAllText(path)
                .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
                .Length);

        Assert.True(words <= Whole,
            $"the plan is {words} words against a budget of {Whole}. It is meant to be read "
            + "WHOLE at the start of a session, so this is the check that says it still can "
            + "be. Move a finding to its commit and its test, a mechanism to the XML comment "
            + "beside it, a trap to the check that catches it -- and never delete one item "
            + "to afford another.");
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
        // The companion, and without it the budget is trivial to defeat: split the
        // doc in two and every file is comfortably under the cap while the total
        // is unchanged. A second doc is a decision, not an accident, so it should
        // cost a deliberate edit here.
        var docs = Directory.EnumerateFiles(Docs(), "*.md").Select(Path.GetFileName).ToList();

        Assert.Equal(["plan.md"], docs);
    }

    [Fact]
    public void The_plan_looks_forward_and_records_no_findings()
    {
        // John's call, 2026-08-03: the plan is where the project is going, and a
        // result is something that already happened. The two were mixed, and the
        // findings won -- roughly half the doc was scores, and the sections
        // saying what to build next were the ones getting compacted to make room
        // under the word budget.
        //
        // Worse, a finding written here goes stale silently. The commit that
        // produced a number is the honest home for it, the comment beside the
        // mechanism is where anyone touching that mechanism will actually see it,
        // and the test that asserts it is the only copy that cannot drift.
        //
        // The guards are not findings. `Do not re-try` and `TRAPS` say what not
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
        // A row that names a seat instead of an arm reverses itself when the seat changes
        // hands, and it does so silently -- nothing goes red, the sentence still parses, and
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
        // The companion, for the same reason the one below has one. A pattern set that
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

        // And a row that merely mentions a day is not dated, which is what the lookahead is
        // for. Asserted, because a rule that reddens on ordinary prose gets deleted rather
        // than obeyed.
        Assert.DoesNotContain(Dated, rule =>
            Regex.IsMatch("the depth cap is why it is not today's problem", rule.Pattern));
    }

    [Fact]
    public void The_forward_facing_check_can_still_fail()
    {
        // The companion, and without it the check above passes for a pattern set
        // that matches nothing. Every rule is asserted against a line that must
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
        // The ghost-reference problem that has bitten this project before, which
        // is why forks are deliberately never renumbered. The code cites fork
        // numbers in a dozen places; this asserts each one still resolves.
        var plan = Plan();

        var listed = Regex
            .Matches(plan, @"\*\*(\d{1,3})\*\*")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(listed);

        var cited = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src"))
            // Three digits and anchored, because two silently truncated the first fork past
            // ninety-nine. `fork 106` matched as `10`, so the check reported a dangling
            // citation of a fork nobody had written about while the one actually cited went
            // unchecked -- a guard describing a repo that is not there, which is the fault
            // this file exists to catch in other people's work.
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"[Ff]ork (\d{1,3})\b"))
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
        // The same ghost reference as the fork check, through the one door it leaves open.
        // A `<see cref="..."/>` is enforced by the compiler and a fork number by the check
        // above; a suite named in PROSE is enforced by nothing at all. So a library comment
        // saying *measured in `SomeTests`* keeps compiling forever after that file is
        // renamed, and the reader who goes looking finds nothing and cannot tell whether
        // the measurement moved or never existed.
        //
        // And it is a real path rather than a hypothetical one. Comments in this library
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
        // The companion, because a lookup over an empty set passes everything. If `Suites`
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
        // A refutation is conditional on its configuration, and this project has
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
        // The check that protects the other check. Everything above assumes the
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
