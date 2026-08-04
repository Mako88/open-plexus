using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Every public member is either CALLED, or named here with the reason it is
/// not — <b>a budget, like the dials and the doc and the clones and the row.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S CALL, 2026-08-04: DEAD CODE IS THE WORST.</b> Nothing in this build
/// fails when a public member loses its last caller, so a mechanism can be
/// written, documented, cited in the plan, and never once run — and it reads
/// exactly like a mechanism that works. <b>Two were found by hand the day this
/// was written</b>: `Question.Conjoining`, the conjunction question, which had
/// never had a caller since the day it was written; and `Drives.Improving`, an
/// internal signal computed every step and read by nothing, which had been
/// described to John as one of three signals the system has.
/// </para>
/// <para>
/// <b>THE SHAPE IS <c>DialTests</c>'s, ON PURPOSE.</b> Either something uses it,
/// or somebody has written down why not — and "nobody has got to it yet" is a
/// perfectly good reason as long as it is written where it can be counted. What
/// is not allowed is silence.
/// </para>
/// <para>
/// <b>USE IS TEXTUAL AND COMMENTS DO NOT COUNT.</b> A <c>cref</c> naming a type
/// keeps its documentation honest and is exactly how a dead mechanism stays
/// looking alive, so the scan strips comments before it looks. <b>That is the
/// whole trick</b>: `Code.Prefix` is cited by fork 3 in prose and called by
/// nothing.
/// </para>
/// </remarks>
public sealed class DeadCodeTests(ITestOutputHelper output)
{
    /// <summary>
    /// Public members with no caller, each with the reason it survives.
    /// </summary>
    /// <remarks>
    /// <b>A REASON, NOT AN EXCUSE.</b> Several of these say outright that the
    /// thing should be wired or deleted and nobody has done it — which is the
    /// point of writing it down rather than the failure of it.
    /// </remarks>
    private static readonly Dictionary<string, string> Unused = new(StringComparer.Ordinal)
    {
        ["Code.Prefix"] =
            "OPEN FORK 3 — cluster placement by prefix locality. Cited in prose "
            + "and called by nothing since the day it was written. Step 8's grains "
            + "are the same idea arriving by another road, so this is either that "
            + "mechanism's home or it goes",

        ["Drives.Improving"] =
            "AN INTERNAL SIGNAL WITH NO CONSUMER, and the project has exactly "
            + "three. Read by nothing, which makes the honest count of signals the "
            + "SYSTEM can act on zero. Wire it or drop it",

        ["Drives.Better"] = "the counts `Improving` is derived from",
        ["Drives.Worse"] = "the counts `Improving` is derived from",
        ["Drives.Same"] = "the counts `Improving` is derived from",

        ["Chunk.Noticed"] =
            "the denominator that says whether minting was SELECTIVE — a detector "
            + "that mints nearly everything has found no structure. Reported by "
            + "`ToString` and asserted by nothing",

        ["Foresight.Foresaw"] =
            "the share of predictions that named something real, beside "
            + "`Precision` which is used. Kept as the pair reads together",

        // ---- PRE-DATING THIS FILE, AND LISTING THEM IS A HOLDING POSITION ----
        //
        // Every one of these is a world's own vocabulary that its run does not
        // read, so the honest options are the same two: wire it to something that
        // asserts, or delete it. They are NOT deleted here only because a long
        // session is the wrong time to cut into four worlds nobody was touching,
        // and a list somebody has to look at beats a silence nobody does.

        ["Clevr.Material"] = "a scene attribute the run never asks about",
        ["Clevr.Thing"] = "a scene attribute the run never asks about",
        ["Motif.Cue"] = "the motif's own cue code, never read back",
        ["MotifRun.Chunks"] = "reported by `ToString` and asserted by nothing",
        ["Rhythm.Cycle"] = "the stream's period, never read back",
        ["Rhythm.Symbol"] = "the stream's alphabet, never read back",
        ["RunReport.NoveltyGap"] = "reported and asserted by nothing",
        ["RunReport.Silence"] = "reported and asserted by nothing",
        ["Senses.Aside"] = "clutter's own modality, never read back",
        ["SnakeSense.Proprioception"] = "the body's own modality, never read back",
    };

    /// <summary>What a record or a runtime generates and nobody writes.</summary>
    private static readonly HashSet<string> Generated = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "ToString", "Deconstruct", "PrintMembers",
        "CompareTo", "Dispose", "GetEnumerator", "Clone",
    };

    [Fact]
    public void Every_public_member_is_called_or_has_a_written_reason_it_is_not()
    {
        var source = Sources();

        var orphans = new List<string>();

        foreach (var type in typeof(Code).Assembly.GetTypes().Where(one => one.IsPublic))
        {
            foreach (var member in Members(type))
            {
                var name = $"{type.Name}.{member}";

                // ITS OWN DECLARATION IS NOT A USE. Every other file counts, which
                // includes the tests -- a member exercised only by a test is doing
                // something, even if only holding a claim in place.
                var used = source
                    .Where(file => !file.Key.EndsWith($"{type.Name}.cs", StringComparison.Ordinal))
                    .Any(file => Regex.IsMatch(file.Value, Calls(member)));

                if (used || Unused.ContainsKey(name)) continue;

                orphans.Add(name);
            }
        }

        foreach (var one in orphans.Order(StringComparer.Ordinal)) output.WriteLine(one);

        Assert.True(orphans.Count == 0,
            $"{orphans.Count} public member(s) nothing calls and nobody has "
            + "explained. Wire it, delete it, or write down why it stays:\n  "
            + string.Join("\n  ", orphans.Order(StringComparer.Ordinal).Take(20)));
    }

    [Fact]
    public void And_the_list_does_not_rot_into_a_record_of_what_used_to_be_dead()
    {
        // THE OTHER DIRECTION, and without it the list becomes a graveyard of
        // members that have since been wired up — which is the exact failure the
        // doc's ticked boxes and the fork index are both checked for.
        var source = Sources();

        var revived = new List<string>();

        foreach (var (name, _) in Unused)
        {
            var member = name.Split('.')[1];
            var owner = name.Split('.')[0];

            if (source
                .Where(file => !file.Key.EndsWith($"{owner}.cs", StringComparison.Ordinal))
                .Any(file => Regex.IsMatch(file.Value, Calls(member))))
                revived.Add(name);
        }

        Assert.True(revived.Count == 0,
            $"named here as unused and now called: {string.Join(", ", revived)}. "
            + "Take it off the list.");
    }

    [Fact]
    public void The_budget_is_visible_and_does_not_grow()
    {
        // THE POINT OF THE FILE. The number is what it is today; having one is what
        // stops the next unwired mechanism arriving unnoticed beside these. IT
        // SHOULD ONLY EVER FALL — every entry is something to wire or delete.
        Assert.Equal(17, Unused.Count);
    }

    /// <summary>
    /// What using a member actually looks like, as against merely spelling it.
    /// </summary>
    /// <remarks>
    /// <b>A BARE WORD IS NOT A USE, AND THE COMPANION CHECK CAUGHT ME ASSUMING IT
    /// WAS.</b> <c>Better</c>, <c>Same</c>, <c>Symbol</c> and <c>Thing</c> are
    /// ordinary words appearing as unrelated identifiers all over the tree, so
    /// matching the name alone reports a dead member as live — <b>a check that
    /// produces false PASSES</b>, which is the one failure this file exists to
    /// prevent. A use is a member access, a named argument, or an initialiser.
    /// </remarks>
    private static string Calls(string member) =>
        @"\." + Regex.Escape(member) + @"\b|\b" + Regex.Escape(member) + @"\s*[:=][^=]";

    /// <summary>Every source file, with comments stripped.</summary>
    /// <remarks>
    /// <b>A <c>cref</c> IS NOT A CALL, and that is the whole trick.</b> Doc
    /// comments are how a dead mechanism goes on looking alive — the compiler
    /// checks that they RESOLVE, which is a guarantee about spelling and not about
    /// use.
    /// </remarks>
    private static Dictionary<string, string> Sources()
    {
        var root = Tree.Repo();

        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))

            // AND NOT THIS FILE, which names every member on the list and would
            // otherwise report all of them as used — the list keeping itself
            // alive, which is the funniest way this check could be worth nothing.
            .Where(path => !path.EndsWith("DeadCodeTests.cs", StringComparison.Ordinal))
            .ToDictionary(
                path => path,
                path => Regex.Replace(File.ReadAllText(path), @"^\s*///.*$", "", RegexOptions.Multiline));
    }

    /// <summary>The public members of one type that a caller could name.</summary>
    private static IEnumerable<string> Members(Type type)
    {
        const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        foreach (var one in type.GetMembers(Declared))
        {
            if (one is MethodBase { IsSpecialName: true }) continue;
            if (one.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;
            if (Generated.Contains(one.Name)) continue;
            if (one.Name.StartsWith('<') || one.Name.StartsWith("op_", StringComparison.Ordinal))
                continue;

            // CONSTRUCTORS ARE NAMED BY THEIR TYPE, and the type's own use is a
            // different question from a member's.
            if (one is ConstructorInfo) continue;

            // AN ENUM MEMBER IS A VALUE RATHER THAN A CALL, and a refuted arm being
            // deleted is what `Attending` and `Gardening` are already policed by.
            if (type.IsEnum) continue;

            yield return one.Name;
        }
    }
}
