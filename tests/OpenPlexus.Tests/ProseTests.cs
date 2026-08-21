using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// How the prose in this repo is written, made mechanical where it can be — John's,
/// 2026-08-13.
/// </summary>
/// <remarks>
/// <para>
/// CLAUDE.md says to say the thing rather than build up to it, and that the existing prose is
/// the problem and not just the next commit. A rule written only in prose is a rule the next
/// session reads once and then matches its surroundings instead, which is how this register
/// got here: a session takes the repo's own writing as the style to hit, so every pass
/// amplifies the last one.
/// </para>
/// <para>
/// This is the check that breaks that loop for the part of it a regex can see. Two mechanisms
/// carry emphasis here and both are countable: a sentence written in capitals, and a bold span
/// long enough to be a sentence rather than a lead clause. Neither may grow.
/// </para>
/// <para>
/// <b>It catches the typography and none of the structure.</b> The reveal — <i>and here is
/// what actually happened</i> — the stinger restating a point with more force, and the
/// corrective turn that invents a misconception in order to overturn it all survive a
/// lowercasing untouched. Those stay a written rule, and this file MUST NOT be read as
/// covering them.
/// </para>
/// </remarks>
public sealed class ProseTests(ITestOutputHelper output)
{
    /// <summary>
    /// How many capitalised words in a row make a shout rather than a label.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A label names a thing and a shout is a sentence, so the two separate on length. The
    /// route's branches are the longest real labels here — <c>WHAT THE MACHINE MUST SURVIVE</c>
    /// is five words — and six is the first length nothing legitimate reaches.
    /// </para>
    /// <para>
    /// Measured rather than guessed: at the commit that introduced this, every six-word run in
    /// the tree was a fragment of a shouted sentence and no heading, status token or branch
    /// label was among them. Six or more acronyms in an unbroken row would read as a shout and
    /// none exists; if one is ever written, that is the false positive to expect.
    /// </para>
    /// </remarks>
    private const int Words = 6;

    /// <summary>
    /// The most shouted sentences the tree may hold. <b>It is at nought and stays there.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// It began at 2,489 across 158 files, which was most of the repo, and it was written as a
    /// ratchet in the shape <see cref="DocsTests"/> uses for the doc budget. That turned out to
    /// be unnecessary: the transformation is mechanical, so one pass took it to nought rather
    /// than the many that shape assumes.
    /// </para>
    /// <para>
    /// So this is no longer a budget being worked down. It is a rule, and it is one-way — there
    /// is no condition under which this repo wants a sentence written in capitals, so raising
    /// this constant is not a thing a commit may do.
    /// </para>
    /// </remarks>
    private const int Shouting = 0;

    /// <summary>
    /// The most words a bold span may hold before it is a sentence rather than a lead.
    /// </summary>
    /// <remarks>
    /// Bold earns its keep marking the claim a reader scans for. A bold sentence is the same
    /// emphasis-as-volume the capitals are, in a form that survives lowercasing — so it gets
    /// its own count rather than riding on the one above.
    /// </remarks>
    private const int Lead = 12;

    /// <summary>
    /// The most bold sentences the tree may hold. <b>A ratchet, and the target is nought.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the half that stayed a ratchet, because it is the half a script cannot do. A
    /// shouted sentence has one correct rewrite and a bold sentence does not: where the lead
    /// clause ends is a judgement about which part of the claim a reader scans for, and there
    /// are 799 of those judgements left.
    /// </para>
    /// <para>
    /// And the pass that took it to 799 measured the same ratio the entry above claims. Of
    /// 36 taken by hand, the comma cut was right for 31 — the claim already came second and
    /// the bold only had to close earlier. Five needed the clause reordered because the lead
    /// a reader scans for was the second half, which is the judgement no script makes.
    /// </para>
    /// <para>
    /// And 783 is one file rather than a sweep, which is the shape that fits a session with
    /// research in it. Sixteen came off <c>CensusTests</c> alone, every one of them by
    /// closing the bold early, and the ratio held again — the claim came second in fourteen
    /// and the lead had to be shortened rather than moved in the other two.
    /// </para>
    /// <para>
    /// And 650 is five files, taken to clear the schedule ahead of a conversion rather than as
    /// the session's work. The ratio held a third time: of 80 cut, 66 closed at a punctuation
    /// mark the claim was already sitting behind, 14 had none and closed at a clause boundary,
    /// and 3 of those needed the claim moved because the lead a reader scans for was the
    /// second half.
    /// </para>
    /// <para>
    /// And 568 is six more files taken the same way, in the same session and after the work
    /// rather than before it. Of 80, 57 closed at punctuation, 23 at a clause boundary, and 4
    /// of those needed the claim moved or the shout lowercased. Two passes of eighty in one
    /// session is what says the rate is a judgement cost rather than a search cost — finding
    /// them is instant and deciding where each lead ends is the whole of the work.
    /// </para>
    /// <para>
    /// And 168 is the schedule's five and nothing more, which is the smallest a pass gets. All
    /// five came out of <see cref="UnifyingCostTests"/>: three closed at a comma the claim was
    /// already sitting behind, and two at the clause boundary in front of <i>rather than</i>,
    /// where the cut point is a word rather than a mark. None needed the claim moved.
    /// </para>
    /// <para>
    /// And 148 is three commits' worth taken at once, which is what a session doing other work
    /// owes rather than what a pass achieves. Of 21, 12 closed at punctuation and 9 at a clause
    /// boundary; 2 of those needed the claim moved, both of them a <i>so X, and Y</i> where the
    /// lead a reader scans for was the second half. The ratio holds a fifth time.
    /// </para>
    /// <para>
    /// And 128 is five files taken ahead of the schedule rather than up to it, which is what a
    /// session with a sweep in flight can do with the wait. Of 20, 13 closed at punctuation
    /// and 7 at a clause boundary; none needed the claim moved, and three left a shouted lead
    /// as the whole of a bold, so <see cref="Opened"/> falls with it for the fourth time.
    /// </para>
    /// <para>
    /// And 113 is five more files taken the same way, fifteen ahead of the schedule rather
    /// than up to it. Of 16, 11 closed at punctuation and 5 at a clause boundary, and none
    /// needed the claim moved. Two left a shout standing as the whole of a bold, so
    /// <see cref="Opened"/> falls again — five passes now and none of them looking.
    /// </para>
    /// <para>
    /// And 97, which is the first pass under a hundred and the first taken across the whole
    /// tree rather than out of the worst files. Of 16, 12 closed at punctuation and 4 at a
    /// clause boundary; two wanted the claim shortened rather than moved. Spreading the pass
    /// thin costs nothing extra — the judgement is per span and the file it sits in does not
    /// change it, which is what the per-file passes could not show.
    /// </para>
    /// <para>
    /// And 85, twelve taken across four files while a grid ran. Of 12, 8 closed at
    /// punctuation and 4 at a clause boundary; two left a shout as the whole of a bold. The
    /// rate is steady enough now that a pass is a number rather than a finding, which is what
    /// the entry below <see cref="Opened"/> already says about its own half.
    /// </para>
    /// <para>
    /// And 73, twelve more across four files while a control ran. The rate has held at
    /// roughly two-thirds closing at punctuation across seven passes and forty files, so a
    /// pass is a number now: what remains to say about this debt is what it costs a session,
    /// which is about twenty minutes of the wait beside a grid.
    /// </para>
    /// <para>
    /// And 63, ten taken across six files, none of which was among the worst. Below a
    /// hundred the pass is no longer a sweep of a file — the spans are scattered one and two
    /// to a file, so a pass is a walk of the whole tree's list and the per-file shape this
    /// entry describes above has stopped applying.
    /// </para>
    /// <para>
    /// And 53, ten taken alphabetically off the whole-tree list rather than by picking files.
    /// Of 10, 7 closed at punctuation and 3 at a clause boundary. Taking them in the order
    /// the list prints costs nothing over choosing, which is what says the judgement really
    /// is per span.
    /// </para>
    /// <para>
    /// Every pass lowers this to what that pass achieved. It is one of two ceilings now and it
    /// is the tight one — <see cref="Scheduled"/> is what stops it sitting still, and this is
    /// what stops the slack that schedule leaves being spent on new bold sentences.
    /// </para>
    /// </remarks>
    private const int Shouted = 53;

    /// <summary>
    /// The most bold spans that may open in capitals. <b>A ratchet, and the target is nought.</b>
    /// </summary>
    /// <remarks>
    /// It began at 77, found by reading rather than by checking: thirteen turned up in three
    /// files during one pass, which is what says the class is not incidental. No schedule sits
    /// on this one — <see cref="Falls"/> paces the bold half and a second clock would be two
    /// deadlines on one session for the same debt.
    /// <para>
    /// It falls to 68 as a side effect rather than as a pass, which is worth saying because
    /// the two debts are not independent. A bold sentence being cut back to its lead often
    /// leaves the shouted words as the whole of the bold, and a label is allowed to be
    /// capitals — so three came off here without anybody looking for them.
    /// </para>
    /// <para>
    /// And it falls to 64 the same way, two of them having been the whole of a bold that a cut
    /// left standing in capitals. Neither was looked for. It reached 63 when `Trial` was
    /// rewritten as `Bench`, which is the same road: prose that goes takes its debt with it.
    /// </para>
    /// <para>
    /// And it falls to 56 the same way again, on a pass that cut twenty-four bold sentences
    /// back to their lead and was not looking for these at all. Three passes now, same
    /// direction, which is what says the two debts come off together.
    /// </para>
    /// <para>
    /// And 54, on the fourth such pass and by the same road. Two of twenty cuts left a shout
    /// as the whole of a bold and had to be lowercased to read as a label. Four passes now
    /// and none of them looking, so the coupling is a property of the cut rather than of any
    /// one file.
    /// </para>
    /// <para>
    /// And 52 on the fifth, which is where this stops being worth a paragraph each time. The
    /// rate is about one shout per ten bold sentences cut, steady across five passes and five
    /// different sets of files, so the next pass should be recorded as a number rather than
    /// as a finding.
    /// </para>
    /// </remarks>
    private const int Opened = 48;

    /// <summary>
    /// The commit the decay schedule counts from. <b>John's, 2026-08-13.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Shouted"/> alone says nothing about the count ever falling, and John asked
    /// for a rule that some prose work happens as the repo moves. The straight form of that was
    /// <i>one commit in five must do prose work</i>, refused because it would block a one-line
    /// fix on a tone debt it has nothing to do with. This is what was offered instead: the
    /// ceiling falls with the commit count, so the debt is owed by the branch rather than by
    /// any one commit, and a session pays it whenever it likes.
    /// </para>
    /// <para>
    /// <b>A commit's verdict here does not change once it is made.</b> The clock is how many
    /// commits are on the first-parent path from this SHA, which is as fixed a property of a
    /// commit as the files in it — so an old commit re-run gives the answer it always gave, and
    /// a bisect is still readable. That was the objection to the per-commit form and it is
    /// answered rather than inherited.
    /// </para>
    /// <para>
    /// It is not on `master`, so this SHA must survive the merge. Rebasing the branch would
    /// take it out of the history and `git rev-list` would then fail rather than pass, which is
    /// the right way round for a clock that has lost its zero.
    /// </para>
    /// </remarks>
    private const string Baseline = "8c68253e8a38f43ee79f4715cc93949f39ed8cd7";

    /// <summary>How many bold sentences stood at <see cref="Baseline"/>.</summary>
    /// <remarks>
    /// <b>It was written as 1,164 and that number was wrong</b>, because the count that
    /// produced it was reading <c>///</c> as a word — see <see cref="Bolds"/>. Re-taken on the
    /// baseline tree with the fixed count rather than adjusted by the difference, so the
    /// schedule's zero is a measurement and not an arithmetic correction to one.
    /// </remarks>
    private const int Started = 1_128;

    /// <summary>
    /// How many bold sentences a commit costs the ceiling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Measured rather than guessed. The pass at <see cref="Baseline"/> did 45 by hand in one
    /// sitting, and John's proposal was one commit in five doing prose work — 45 spread over
    /// five commits is nine, and five is that with room for the commits where the judgement is
    /// harder than it was in the two documents.
    /// </para>
    /// <para>
    /// It reaches nought in 226 commits from the baseline. Raise it if a pass keeps landing far
    /// under the ceiling, and lower it if the schedule starts deciding what a session works on
    /// — the tax is meant to be payable alongside the research rather than instead of it.
    /// </para>
    /// </remarks>
    private const int Rate = 5;

    /// <summary>The ceiling the schedule has reached after a given number of commits.</summary>
    /// <param name="commits">Commits on the first-parent path since <see cref="Baseline"/>.</param>
    /// <remarks>
    /// <b>The first commit is not billed</b>, and that is arithmetic rather than a fudge. The
    /// commit that installs a schedule is the one that writes this file, so billing it would
    /// make the schedule red the moment it was committed and the only way to land it would be
    /// to bundle prose work with it — which is the one thing CLAUDE.md says the rewrite must
    /// not do.
    /// </remarks>
    internal static int Falls(int commits) =>
        Math.Max(0, Started - (Rate * Math.Max(0, commits - 1)));

    /// <summary>
    /// How many commits have landed since <see cref="Baseline"/>, on the first parent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>First-parent, so a merge counts once.</b> Counting everything reachable would let a
    /// long branch merged in jump the clock by its whole length, and the schedule would then
    /// demand hundreds of sentences for work that was already done somewhere else.
    /// </para>
    /// <para>
    /// <b>Throws rather than skipping</b>, in the shape <see cref="Tree"/> uses. A clock that
    /// silently reads nought would park the ceiling at <see cref="Started"/> forever and read
    /// exactly like a branch that is keeping up.
    /// </para>
    /// </remarks>
    private static int Count(string range)
    {
        using var git = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            ArgumentList = { "rev-list", "--count", "--first-parent", range },
            WorkingDirectory = Tree.Repo(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("`git` did not start");

        var said = git.StandardOutput.ReadToEnd();
        var complained = git.StandardError.ReadToEnd();

        git.WaitForExit();

        if (git.ExitCode == 0 && int.TryParse(said.Trim(), out var commits)) return commits;

        throw new InvalidOperationException(
            $"could not count `{range}`. A shallow clone is the likely cause — CI checks out "
            + "with `fetch-depth: 0` for this. If the SHA is genuinely gone, the branch was "
            + $"rebased and the schedule needs a new zero.\n{complained.Trim()}");
    }

    /// <summary>How far the clock has run.</summary>
    private static int Since() => Count($"{Baseline}..HEAD");

    /// <summary>Where the decay schedule has reached on this commit.</summary>
    private static int Scheduled() => Falls(Since());

    /// <summary>
    /// Every file whose prose this repo is responsible for.
    /// </summary>
    /// <remarks>
    /// CLAUDE.md is here explicitly because it lives at the root rather than under
    /// <see cref="Tree.Docs"/>, and it is the file a session reads as instruction — leaving it
    /// out would exempt the one document that sets the register for everything else.
    /// </remarks>
    private static IEnumerable<string> Written()
    {
        yield return Path.Combine(Tree.Repo(), "CLAUDE.md");

        foreach (var path in Directory.EnumerateFiles(Tree.Docs(), "*.md")) yield return path;

        foreach (var path in Tree.Sources("src")) yield return path;

        foreach (var path in Tree.Sources("tests")) yield return path;
    }

    /// <summary>
    /// The prose of one file, which for source means its comments and nothing else.
    /// </summary>
    /// <remarks>
    /// Identifiers are capitalised for reasons that have nothing to do with emphasis, and a
    /// constant named <c>MAX_DEPTH</c> beside a <c>const int N</c> would read as shouting. So
    /// the check never sees code: a markdown file is prose whole, and a source file is the
    /// lines whose first non-space characters are <c>//</c>.
    /// </remarks>
    private static string Prose(string path) =>
        path.EndsWith(".md", StringComparison.Ordinal)
            ? File.ReadAllText(path)
            : string.Join(
                '\n',
                File.ReadLines(path)
                    .Where(line => line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    /// <summary>Strip what marks prose up, so none of it breaks a run.</summary>
    /// <remarks>
    /// A comment leader, a markdown bold marker and an XML tag all sit between words without
    /// being words. Leaving them in would end a run at every <c>&lt;/b&gt;</c> and undercount
    /// exactly the sentences this is looking for.
    /// </remarks>
    private static string Bare(string prose) =>
        Regex.Replace(
            Regex.Replace(prose, @"(?m)^\s*///?", " ").Replace("**", " ", StringComparison.Ordinal),
            @"<[^>]+>",
            " ");

    /// <summary>Every run of <see cref="Words"/> or more capitalised words in one piece of prose.</summary>
    /// <remarks>
    /// Numbers and punctuation are transparent rather than breaking: <i>JOHN'S CALL, 2026-08-04:
    /// CAP THE ITEM</i> is one shout, and a date in the middle of it does not make it two
    /// fragments that each fall under the threshold.
    /// </remarks>
    private static List<string> Shouts(string prose)
    {
        var found = new List<string>();
        var run = new List<string>();

        foreach (Match token in Regex.Matches(Bare(prose), @"[A-Za-z][A-Za-z'’-]*"))
        {
            var word = token.Value;

            if (word.Length >= 2 && !word.Any(char.IsLower))
            {
                run.Add(word);
                continue;
            }

            if (run.Count >= Words) found.Add(string.Join(' ', run));

            run.Clear();
        }

        if (run.Count >= Words) found.Add(string.Join(' ', run));

        return found;
    }

    /// <summary>Every bold span of more than <see cref="Lead"/> words in one piece of prose.</summary>
    /// <remarks>
    /// <b>The comment leader is stripped first</b>, in two passes rather than one. <c>///</c> is
    /// not a word, and a bold span wrapped across three comment lines was being counted as two
    /// words longer than it reads — 36 spans in the tree were over the cap on their markup
    /// alone. <see cref="Shouts"/> had been going through <see cref="Bare"/> since it was
    /// written and this had not, which is the whole of the fault.
    /// </remarks>
    private static List<string> Bolds(string prose) =>
        Said(prose)
            .Where(said => said.Length > Lead)
            .Select(said => string.Join(' ', said))
            .ToList();

    /// <summary>Every bold span in one piece of prose, as its words.</summary>
    /// <remarks>
    /// Shared by <see cref="Bolds"/> and <see cref="Leads"/> so the two cannot disagree about
    /// what a bold span is or where one ends. They ask different questions of the same list.
    /// </remarks>
    private static IEnumerable<string[]> Said(string prose) =>
        Regex
            .Matches(prose, @"\*\*(?<said>[^*]+)\*\*|<b>(?<said>.+?)</b>", RegexOptions.Singleline)
            .Select(match => Regex.Replace(match.Groups["said"].Value, @"(?m)^\s*///?", " "))
            .Select(said => Regex.Replace(said, @"<[^>]+>|\s+", " ").Trim())
            .Select(said => said.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Every bold span that opens in capitals and then continues in lower case.</summary>
    /// <remarks>
    /// <para>
    /// <b>The shout the six-word threshold cannot see.</b> <see cref="Words"/> was measured
    /// against a tree where a shouted sentence ran six words or more in the clear, and two
    /// things get under it: a run broken in the middle by an inline <c>&lt;c&gt;</c> or
    /// <c>&lt;see&gt;</c>, whose mixed-case content ends the run; and a shouted lead of four or
    /// five words, which is shorter than the longest legitimate label.
    /// </para>
    /// <para>
    /// <b>So it separates them by position rather than by length</b>, and that is what makes it
    /// safe at two words where a plain threshold would not be. A label is the WHOLE of its bold
    /// — <c>**WHAT THE MACHINE MUST SURVIVE**</c> ends where the capitals do. A shout is a
    /// prefix of one, with the sentence carrying on in lower case after it.
    /// </para>
    /// <para>
    /// Measured rather than guessed, in the same shape <see cref="Words"/> was: at the commit
    /// that introduced this, 77 spans matched and every one read as a shout. No heading, status
    /// token or route branch was among them, because none of those has lower case after it.
    /// </para>
    /// <para>
    /// <b>One capitalised word is left alone on purpose.</b> <i>a SET, never a variable</i>
    /// reads closer to italics than to shouting, so the run has to reach two before this looks
    /// at it — which is why <c>UNDER &lt;see cref="Subsuming.Weaker"/&gt;</c> is not caught.
    /// </para>
    /// </remarks>
    private static List<string> Leads(string prose)
    {
        var found = new List<string>();

        foreach (var said in Said(prose))
        {
            var run = said
                .TakeWhile(word => Regex.IsMatch(word.Trim('.', ',', ':', ';', '—', '-'),
                    @"^[A-Z][A-Z'’-]+$"))
                .ToList();

            if (run.Count >= 2 && run.Count < said.Length) found.Add(string.Join(' ', run));
        }

        return found;
    }

    /// <summary>
    /// How many bold sentences the tree holds, for <see cref="OutstandingTests"/> to read.
    /// </summary>
    /// <remarks>
    /// Exposed rather than reimplemented there, so the outstanding entry and the ratchet cannot
    /// disagree about what they are counting. One of this repo's own traps is a statistic whose
    /// two readers each got whichever definition they assumed.
    /// </remarks>
    internal static int BoldSentences() => Written().Sum(path => Bolds(Prose(path)).Count);

    /// <summary>How many bold spans open in capitals, for <see cref="OutstandingTests"/>.</summary>
    internal static int ShoutedLeads() => Written().Sum(path => Leads(Prose(path)).Count);

    /// <summary>How many of something each file holds, worst first, for the message.</summary>
    private static string Worst(Func<string, List<string>> count)
    {
        var rows = Written()
            .Select(path => (Path.GetRelativePath(Tree.Repo(), path), Found: count(Prose(path)).Count))
            .Where(row => row.Found > 0)
            .OrderByDescending(row => row.Found)
            .Take(10);

        return string.Join("\n  ", rows.Select(row => $"{row.Found,5}  {row.Item1}"));
    }

    [Fact]
    public void No_more_of_the_prose_is_shouted_than_was()
    {
        var found = Written().Sum(path => Shouts(Prose(path)).Count);

        output.WriteLine($"{found} shouted sentences against a ratchet of {Shouting}");

        Assert.True(found <= Shouting,
            $"{found} runs of {Words} or more capitalised words, against {Shouting}. Capitals "
            + "are carrying emphasis that MUST/SHOULD/MAY carries instead, and the register "
            + "spreads because a session takes the surrounding prose as the one to match. "
            + $"Lowercase the sentence and keep the bold:\n  {Worst(Shouts)}");
    }

    [Fact]
    public void No_more_of_the_prose_is_bolded_by_the_sentence_than_was()
    {
        var found = Written().Sum(path => Bolds(Prose(path)).Count);
        var scheduled = Scheduled();
        var ceiling = Math.Min(Shouted, scheduled);

        output.WriteLine(
            $"{found} bold sentences against a ratchet of {Shouted} and a schedule of "
            + $"{scheduled}, {Since()} commits on from {Baseline[..7]}");

        // Which of the two bit changes what the next commit has to do, so the message says.
        // The ratchet going red is a commit that ADDED bold sentences and the fix is in that
        // commit; the schedule going red is prose work the branch owes and the fix is anywhere.
        Assert.True(found <= ceiling,
            found > Shouted
                ? $"{found} bold spans over {Lead} words, against a ratchet of {Shouted}. Bold "
                    + "marks the claim a reader scans for, and a bold sentence is the same "
                    + $"emphasis-as-volume the capitals are:\n  {Worst(Bolds)}"
                : $"{found} bold spans over {Lead} words, against a schedule of {scheduled}. "
                    + $"The ceiling falls by {Rate} a commit and this branch is "
                    + $"{found - scheduled} behind it. Cut that many bold spans back to the "
                    + $"lead clause anywhere in the tree, then lower `Shouted`:\n  {Worst(Bolds)}");
    }

    [Fact]
    public void No_more_of_the_prose_opens_a_lead_in_capitals()
    {
        var found = Written().Sum(path => Leads(Prose(path)).Count);

        output.WriteLine($"{found} bold spans opening in capitals against a ratchet of {Opened}");

        Assert.True(found <= Opened,
            $"{found} bold spans open in capitals and carry on in lower case, against {Opened}. "
            + "A label is the whole of its bold and a shout is a prefix of one, so this is the "
            + $"shout that gets under the {Words}-word threshold. Lowercase the lead and keep "
            + $"the bold:\n  {Worst(Leads)}");
    }

    [Fact]
    public void A_label_is_the_whole_of_its_bold_and_a_shout_is_the_start_of_one()
    {
        // The companion, and this one is the reason the rule can be safe at two words where a
        // length threshold cannot. Every label below is real and none may ever trip it.
        Assert.Empty(Leads("- **WHAT IT MUST DO** — one entry a line of THE ARCHITECTURE"));
        Assert.Empty(Leads("- **WHAT THE MACHINE MUST SURVIVE** — C1 to C4 do not move"));
        Assert.Empty(Leads("**NOW** — a commitment fires when its scope is a subset"));

        // And one word is left alone on purpose, which is what `UNDER <see .../>` relies on.
        Assert.Empty(Leads("<b>UNDER <see cref=\"Weaker\"/> a hair of advantage saves it.</b>"));

        // The two shapes that were getting under the threshold, both real and both found by
        // reading. The first is broken into runs of five and two by the tag in the middle of
        // it; the second is four words, shorter than the longest label in the tree.
        Assert.Single(Leads(
            "<b>AND IT IS INERT WITHOUT <c>Population.Sorts</c>, so it is a control.</b>"));

        Assert.Single(Leads("**NAMES RATHER THAN A COUNT, so a parent can be asked.**"));
    }

    [Fact]
    public void The_schedule_falls_to_nought_and_stops()
    {
        // The companion, and the schedule needs one more than most: it is the only ceiling here
        // that no commit edits, so an arithmetic slip in it would move silently and read as the
        // branch keeping up or as the branch being hopeless, with nothing to compare against.
        Assert.Equal(Started, Falls(0));
        Assert.Equal(Started, Falls(1));
        Assert.Equal(Started - Rate, Falls(2));

        // It floors rather than going negative, which matters because the ratchet is the other
        // half of a `Math.Min`. A negative ceiling would demand the tree hold fewer than no
        // bold sentences and could never be satisfied.
        Assert.Equal(0, Falls(Started));
        Assert.Equal(0, Falls(int.MaxValue / Rate));

        // And the clock's zero has to be on this history rather than merely be a SHA that
        // parses. Nought commits back the other way is what makes the baseline an ancestor of
        // HEAD; a rebase that dropped it would leave `Since` counting from a commit on nobody's
        // branch, and the ceiling would then sit at `Started` and read as a branch keeping up.
        Assert.Equal(0, Count($"HEAD..{Baseline}"));
    }

    /// <summary>
    /// What UTF-8 looks like after being read as Windows-1252 and written back out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written as escapes rather than as the characters themselves, which is not a style
    /// choice. A literal here would put the damage in this file, and the check would fail on
    /// the one file that defines it.
    /// </para>
    /// <para>
    /// An em-dash is three bytes in UTF-8 and this repo's prose is full of them, so the
    /// round-trip turns every one into a visible pair. None of these sequences occurs in
    /// English, which is what makes the test exact rather than a heuristic.
    /// </para>
    /// </remarks>
    private static readonly string[] Mangled = ["\u00e2\u20ac", "\u00c2", "\u00c3"];

    [Fact]
    public void No_file_has_been_round_tripped_through_the_wrong_encoding()
    {
        // A real failure, and it happened while writing this file. `Get-Content` and
        // `Set-Content` in Windows PowerShell 5.1 default to the system ANSI codepage for a
        // file with no byte-order mark, so reading a UTF-8 source and writing it straight back
        // rewrites every em-dash as mojibake -- and the edit that does it reports success.
        //
        // IT IS A WHOLE-FILE READ RATHER THAN A PROSE ONE, unlike everything else here. The
        // damage lands wherever the non-ASCII characters are, and a string literal is as good
        // a home for one as a comment.
        var damaged = Written()
            .Where(path => Mangled.Any(bad =>
                File.ReadAllText(path).Contains(bad, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(Tree.Repo(), path))
            .ToList();

        Assert.True(damaged.Count == 0,
            $"{damaged.Count} file(s) have been read as ANSI and written back as UTF-8, so "
            + "their em-dashes are mojibake. Restore them from git rather than repairing by "
            + "hand, and use `[System.IO.File]::ReadAllText`/`WriteAllText` with an explicit "
            + $"UTF-8 encoding:\n  {string.Join("\n  ", damaged)}");
    }

    [Fact]
    public void The_encoding_check_can_still_fire()
    {
        // THE COMPANION, and this one earns it twice over: the signatures are escapes, so a
        // typo in one would produce a check that matches nothing and passes forever while
        // reading exactly like a tree with no damage in it.
        var wrecked = "the plan\u00e2\u20ac\u2122s own rule";

        Assert.Contains(Mangled, bad => wrecked.Contains(bad, StringComparison.Ordinal));

        Assert.DoesNotContain(Mangled, bad =>
            "the plan's own rule — said once".Contains(bad, StringComparison.Ordinal));
    }

    [Fact]
    public void The_check_can_tell_a_shouted_sentence_from_a_label()
    {
        // The companion every guard in this suite has, and it is the reason the two above are
        // worth anything: a detector that matches nothing passes forever and reads exactly like
        // prose that is in order. This comment is left shouting on purpose -- it is inside the
        // one file that would notice, and the assertions below are what notices it.
        Assert.Single(Shouts("**THESE ARE SUPPOSED TO BE FAILING AND THAT IS THE POINT.**"));
        Assert.Single(Shouts("/// <b>JOHN'S CALL, 2026-08-04: CAP THE ITEM, NOT THE DOC.</b>"));

        // A label is shorter than a sentence, which is the whole basis of the threshold. Every
        // one of these is real: two are route branches, one is a section heading, one is a leaf
        // status. None may ever trip this check.
        Assert.Empty(Shouts("- **WHAT IT MUST DO** — one entry a line of THE ARCHITECTURE"));
        Assert.Empty(Shouts("- **WHAT THE MACHINE MUST SURVIVE** — C1 to C4 do not move"));
        Assert.Empty(Shouts("## DO NOT RE-TRY"));
        Assert.Empty(Shouts("**NOW** — a commitment fires when its scope is a subset"));

        // And an acronym cluster is not a shout, at any length this repo writes.
        Assert.Empty(Shouts("held under TCP, and ILP, MDL and LSH say the same"));
    }

    [Fact]
    public void The_bold_check_can_tell_a_lead_from_a_sentence()
    {
        // The same companion for the same reason. A lead clause is what bold is for and a bold
        // sentence is what it is not, so both directions are pinned.
        Assert.Empty(Bolds("**A control beats an argument.**"));
        Assert.Empty(Bolds("<b>Throws rather than skipping.</b>"));

        Assert.Single(Bolds(
            "**Nothing ships switched off, and preserving recorded numbers is never a reason "
            + "to keep anything or to not change it.**"));

        // And a lead clause does not become a sentence by being wrapped, which is the property
        // the leader strip is for. Eleven words either way; across three comment lines it
        // counted as thirteen while `///` was going in as a word, so the check was reading the
        // markup and calling it prose.
        Assert.Empty(Bolds("/// <b>Nothing ships switched off, and a dial is a new ability.</b>"));

        Assert.Empty(Bolds(
            "    /// <b>Nothing ships switched off, and a dial\n"
            + "    /// is a new\n"
            + "    /// ability.</b>"));
    }

    [Fact]
    public void Only_comments_are_read_out_of_a_source_file()
    {
        // The third companion, and it pins the one thing that would make this check absurd.
        // Reading a whole `.cs` file would count `SearchOption.AllDirectories` and every
        // `SCREAMING_CASE` constant as prose, and the ratchet would then be measuring the
        // codebase rather than the writing.
        var source = Path.Combine(Tree.Repo(), "tests", "OpenPlexus.Tests", "Tree.cs");

        var prose = Prose(source);

        Assert.DoesNotContain("public static string Repo()", prose, StringComparison.Ordinal);
        Assert.Contains("Throws rather than skipping", prose, StringComparison.Ordinal);
    }
}
