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
    /// are 1,209 of those judgements left.
    /// </para>
    /// <para>
    /// Every pass lowers this to what that pass achieved.
    /// <see cref="OutstandingTests.The_prose_is_out_of_the_engagement_register"/> is what stops
    /// it sitting here, since a ratchet nobody turns is a budget rather than a target.
    /// </para>
    /// </remarks>
    private const int Shouted = 1_209;

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
    private static List<string> Bolds(string prose) =>
        Regex
            .Matches(prose, @"\*\*(?<said>[^*]+)\*\*|<b>(?<said>.+?)</b>", RegexOptions.Singleline)
            .Select(match => Regex.Replace(match.Groups["said"].Value, @"<[^>]+>|\s+", " ").Trim())
            .Where(said => said.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > Lead)
            .ToList();

    /// <summary>
    /// How many bold sentences the tree holds, for <see cref="OutstandingTests"/> to read.
    /// </summary>
    /// <remarks>
    /// Exposed rather than reimplemented there, so the outstanding entry and the ratchet cannot
    /// disagree about what they are counting. One of this repo's own traps is a statistic whose
    /// two readers each got whichever definition they assumed.
    /// </remarks>
    internal static int BoldSentences() => Written().Sum(path => Bolds(Prose(path)).Count);

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

        output.WriteLine($"{found} bold sentences against a ratchet of {Shouted}");

        Assert.True(found <= Shouted,
            $"{found} bold spans over {Lead} words, against {Shouted}. Bold marks the claim a "
            + "reader scans for, and a bold sentence is the same emphasis-as-volume the "
            + $"capitals are:\n  {Worst(Bolds)}");
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
