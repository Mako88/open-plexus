using System.Text.RegularExpressions;

namespace OpenPlexus.Tests;

/// <summary>
/// The plan's claims that something is NOT WIRED, checked against whether it is —
/// <b>John's call, the drift that has cost this project three sessions.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The doc rots in the dangerous direction and nothing was watching.</b> A stale
/// claim that something IS built gets found the moment somebody looks for it. A stale
/// claim that something is NOT built is never looked for at all — it is read as a note
/// about work outstanding, so the mechanism sits there running while the plan says it
/// has never run. <c>Surprise</c> and <c>Winnow</c> were both mounted and both still
/// described as mounted nowhere, in a doc whose whole purpose is to say what is left.
/// </para>
/// <para>
/// <b>And it is the same fault the plan already names from the other end.</b> <i>A gate
/// named in the plan and mounted nowhere makes the word it gates mean something else</i>
/// — this is that trap's dual, and it is worse, because a reader acts on it by building
/// something that already exists.
/// </para>
/// <para>
/// <b>What this cannot do is read English, so it is deliberately narrow.</b> It finds
/// items that make a not-wired claim, takes the backticked identifiers in them, and asks
/// whether the source CALLS any of them. Being merely defined is not enough — a type can
/// exist and never be invoked, which is exactly the state these claims describe. A claim
/// naming nothing in backticks is left alone rather than guessed at.
/// </para>
/// </remarks>
public sealed class StaleClaimTests
{
    /// <summary>
    /// What a not-wired claim looks like in this doc's voice.
    /// </summary>
    /// <remarks>
    /// <b>Taken from the claims that were actually stale rather than invented.</b>
    /// <i>mounted nowhere</i>, <i>has never run</i> and <i>ran nowhere</i> are the exact
    /// phrasings this plan reached for, and a pattern list grown from real drift is worth
    /// more than one grown from imagination — it can be extended the next time a new
    /// phrasing slips through, which is the only way it stays honest.
    /// </remarks>
    private static readonly string[] Claims =
    [
        "mounted nowhere",
        "mounted in nothing",
        "has never run",
        "have never run",
        "ran nowhere",
        "runs nowhere",
        "is not mounted",
        "never been mounted",
        "read by nothing",
        "connected to nothing",
        "no caller",
    ];

    /// <summary>
    /// The sections that describe what is TRUE NOW, as against what was once learnt.
    /// </summary>
    /// <remarks>
    /// <b>`Traps` and `do not re-try` are historical by design and must be left alone.</b>
    /// <i>A guard mounted on one caller is not mounted</i> is a lesson about a fault that
    /// was fixed, and it names the type it was found on — so a check reading it as a
    /// claim about today would demand that a trap be deleted the moment its subject
    /// started working, which is the opposite of what a trap is for. What has to stay
    /// true is the sections a reader consults to decide what to BUILD.
    /// <para>
    /// <b>And `the route` replaced the fork index here, which is not a rename.</b> The index
    /// was a flat table and this list held it because that is where an <i>unwired</i> claim
    /// used to live. The route now holds them — <i>nothing tracks a source through a
    /// change</i>, <i>the walk still learns nowhere but at home</i> — and it holds them
    /// against a REQUIREMENT rather than against a number, so a claim going stale there is
    /// a requirement that has quietly been met and nobody noticed.
    /// </para>
    /// </remarks>
    private static readonly string[] Current =
    [
        // And it is one section now, `to build` and `open defects` having folded into the
        // route. That is a narrowing of the list and not of the check: everything those two
        // sections held that spoke about the present is a leaf in here, so the same claims
        // are read -- against the requirement they block rather than in a list of their own.
        "THE ROUTE",
    ];

    /// <summary>
    /// <b>A claim that something is unwired has to still be true.</b>
    /// </summary>
    [Fact]
    public void The_plan_may_not_say_a_mounted_mechanism_is_unmounted()
    {
        var plan = Sections(File.ReadAllText(Path.Combine(Tree.Docs(), "plan.md")));

        // The source only, never the tests. A mechanism exercised solely by a test IS
        // unmounted in every sense the plan means -- an arm that no run reaches is
        // exactly the thing these claims are for, and counting its test as a caller
        // would make this check certify the fault it exists to find.
        var source = string.Join(
            "\n", Tree.Sources("src").Select(File.ReadAllText));

        var stale = new List<string>();

        foreach (var item in Items(plan))
        {
            if (!Claims.Any(claim =>
                item.Contains(claim, StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (var named in Backticked(item))
            {
                // CALLED RATHER THAN MERELY PRESENT. `Winnow` appears in its own file
                // whether anything reaches it or not, so a substring search over the
                // source would call every claim stale the moment the type was written.
                if (!Regex.IsMatch(source, $@"\b{Regex.Escape(named)}\s*[(.]"))
                    continue;

                stale.Add($"the plan says `{named}` is unwired — \"{Opening(item)}\"");
            }
        }

        Assert.True(
            stale.Count == 0,
            "the plan claims something is unwired that the source calls. Either the "
            + "claim is stale and the item should say what it does now, or the call is "
            + "not a mount and the claim should say so in words this check can read:\n  "
            + string.Join("\n  ", stale));
    }

    /// <summary>Only the parts of the plan that speak about the present.</summary>
    /// <remarks>
    /// <b>By heading rather than by line number</b>, so reordering the doc cannot quietly
    /// take a section out of scope. A heading this does not know is left OUT, which is the
    /// safe direction only because <c>DocsTests</c> already fails the build when the
    /// section list changes — the two checks hold each other up.
    /// </remarks>
    private static string Sections(string doc)
    {
        var kept = new List<string>();
        var taking = false;

        foreach (var line in doc.Split('\n'))
        {
            if (line.StartsWith("##", StringComparison.Ordinal))
                taking = Current.Any(section =>
                    line.Contains(section, StringComparison.Ordinal));

            if (taking) kept.Add(line);
        }

        return string.Join("\n", kept);
    }

    /// <summary>Every backticked identifier in an item.</summary>
    /// <remarks>
    /// <b>IDENTIFIERS ONLY, so a backticked phrase is not searched for as a symbol.</b>
    /// The plan quotes English in backticks as often as it quotes code, and asking the
    /// source whether it calls <c>together / seen</c> would find nothing forever and
    /// certify every claim containing it.
    /// </remarks>
    private static IEnumerable<string> Backticked(string item) =>
        Regex.Matches(item, @"`([A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)*)`")
            .Select(match => match.Groups[1].Value)

            // THE LAST SEGMENT, because the source calls `Absent`, never
            // `Conditions.Absent`, and a dotted claim is about the same mechanism.
            .Select(named => named.Split('.')[^1])

            // A one or two letter name is noise and would match half the source.
            .Where(named => named.Length > 2)
            .Distinct(StringComparer.Ordinal);

    /// <summary>One bullet or table row of the plan, joined across its wrapped lines.</summary>
    private static IEnumerable<string> Items(string doc)
    {
        foreach (var block in doc.Split('\n'))
        {
            var line = block.TrimEnd('\r');

            if (line.TrimStart().StartsWith('-') || line.TrimStart().StartsWith('|'))
                yield return line.Trim();
            else if (line.StartsWith("  ", StringComparison.Ordinal) && line.Trim().Length > 0)
                yield return line.Trim();
        }
    }

    /// <summary>Enough of an item to find it by.</summary>
    private static string Opening(string item) =>
        item.Length <= 70 ? item : string.Concat(item.AsSpan(0, 70), "...");
}
