using System.Reflection;
using System.Text.RegularExpressions;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// A world is a problem. The brain is the thing being tuned. Neither reaches into the
/// other.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's rule, and it is about what went wrong last time.</b> On `csharp` the
/// worlds grew dials that reached into the brain — `Ranking` was `Sum` on bAbI and
/// `Agreement` on CLEVR, so a WORLD decided how the brain thought. Every score was
/// then a comparison between two brains as much as between two problems, and nobody
/// could say which.
/// </para>
/// <para>
/// <b>A rule nobody checks</b> is a rule that lasts until the next world. This one
/// arrived in the code within an hour of being agreed and had already been broken:
/// the graded world's runner owned the choice of front end, the band count and the
/// projection geometry — three brain-side decisions living inside a world.
/// </para>
/// <para>
/// <b>And it is the compiler's rule now.</b> <c>OpenPlexus.Worlds</c> references
/// <c>OpenPlexus.Brain</c> and nothing else; the learner, the wire and the join are
/// <c>internal</c> behind a friend list that does not name it. A world naming
/// <c>Population</c>, <c>Brain</c> or <c>Bench</c> does not compile, whatever it is called
/// and whenever it was written.
/// </para>
/// <para>
/// <b>The textual half is gone and it had stopped covering.</b> It walked filenames, and the
/// list was written on 2026-08-06 when <c>Multiplexer</c>, <c>Graded</c> and <c>IWorld</c>
/// were the only worlds on this branch. Two names were added to it afterwards and the other
/// twenty-two files never were, so it was reading five of twenty-seven and reporting on all
/// of them. That is this repo's own trap: a check that cannot fire reads exactly like a
/// check that passes.
/// </para>
/// </remarks>
public sealed class SeparationTests
{
    [Fact]
    public void Every_world_says_its_outcome_in_the_same_alphabet()
    {
        // A brain that learnt a different alphabet per world would not be one brain,
        // and a commitment about an outcome would mean different things depending on
        // who was asking. The multiplexer keeps its own name for this because its
        // answer key is written in it; the two must agree or they will drift.
        for (var outcome = 0; outcome < 2; outcome++)
            Assert.Equal(Brain.Says(outcome), Multiplexer.Says(outcome));

        Assert.Equal(Brain.Followed, Multiplexer.Said);

        // And the entry that pins NOTHING, which is the same problem one rung out. A scope
        // naming a variable once is satisfied by any moment holding a code of that kind, so
        // the rule claims exactly what the same rule without it claims -- and a key that
        // refused the modality would mark every rule rung four ever builds unsound by
        // construction. `Monk`'s key read nought for the identical fault.
        Assert.True(Unifying.Names(new Code(Multiplexer.Whatever, 0)));

        // AND `Monk`, whose key was written in its own alphabet first and read zero for
        // it. Every rule the enumeration called true expected a code on a modality the
        // population can never hold, so the soundness count was nought on all three
        // puzzles and the `Found` count with it -- a blind instrument reporting a
        // flawless-looking absence. This is the check that could have caught it.
        Assert.Equal(Brain.Says(0), Monk.Says(holds: false));
        Assert.Equal(Brain.Says(1), Monk.Says(holds: true));

        Assert.Equal(Brain.Followed, Monk.Answered);
    }

    [Fact]
    public void One_brain_can_be_handed_two_different_worlds()
    {
        // The point of the whole arrangement, asserted rather than described. The
        // same brain object, configured once, learns a symbolic world and a graded
        // one -- so switching world cannot switch brain, because there is only one.
        var brain = new Brain(new CommittingSettings(), seed: 1);

        var symbolic = new Bench(
            new Watching<IReadOnlyList<int>>(
                new Multiplexer(new MultiplexerSettings { Address = 2 }, seed: 1),
                new Bits(Multiplexer.Bit)),
            brain);

        // A second SOURCE, because two worlds are two streams and a brain settles by the
        // stamp. Sharing one would have the second trial pushing sequences the brain has
        // already answered, and it refuses those rather than settling twice -- which is the
        // seam's own rule catching the arrangement this test exists to assert.
        var graded = new Bench(
            new Watching<IReadOnlyList<double>>(
                new Graded(new GradedSettings { Address = 3, Crowding = 0.9 }, seed: 1),
                new Winnowing(Multiplexer.Bit, 11),
                source: Stamp.First + 1),
            brain);

        var first = symbolic.Run(4000);
        var second = graded.Run(4000);

        Assert.True(first.Rounds == 4000 && second.Rounds == 4000);

        // And nothing was refused on either, which is the half that says the two streams
        // stayed apart. A shared source reads as a run that did nothing and this is what
        // tells the two apart.
        Assert.True(first.Refused == 0 && second.Refused == 0);

        // And it carries what it learnt across, because the population is the brain's
        // and not the trial's. Whether that HELPS is a separate question nobody has
        // measured; that it is possible at all is what the seam buys.
        Assert.True(second.Resident > first.Resident);
    }

    /// <summary>
    /// <b>Every name the brain reaches across the boundary for is live.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What this replaces is the compiler.</b> Until the split, a brain comment citing a
    /// world was a <c>cref</c> and CS1574 failed the build the day that world was renamed or
    /// deleted — which is where this repo keeps its findings, so the enforcement was load
    /// bearing. <c>OpenPlexus.Brain</c> cannot see a world any more, so those thirty-six
    /// stopped compiling, and softening them to plain prose would have traded a check for a
    /// comment nobody reads again.
    /// </para>
    /// <para>
    /// <b>So the reference stays and the check moves here</b>, to the one project that sees
    /// both assemblies. The convention is the namespace: <c>Worlds.Arranged</c> rather than
    /// <c>Arranged</c>, which is what makes a reference tellable from a word.
    /// </para>
    /// <para>
    /// <b>And a bare mention is not caught, which is the honest limit.</b> A textual rule over
    /// unqualified names would demand the wrong repair on the ones that collide —
    /// <c>Codes.Coded</c> names a deleted nested class <c>Looking</c> and
    /// <c>Machines.ArrangedRun</c> declares an enum of the same spelling. That is the
    /// two-ideas-one-name trap arriving inside the instrument, which is why <c>DrivenTests</c>
    /// reads compiled code and this reads only what says it is a reference.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_name_the_brain_reaches_across_the_boundary_for_still_exists()
    {
        var reference = new Regex(@"<c>((?:Worlds|Machines)\.[A-Za-z0-9_.]+)</c>");

        var dangling = new SortedSet<string>(StringComparer.Ordinal);
        var seen = 0;

        foreach (var path in Tree.Sources("src"))
        {
            if (!path.Contains(
                    $"{Path.DirectorySeparatorChar}OpenPlexus.Brain{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                continue;

            foreach (Match found in reference.Matches(File.ReadAllText(path)))
            {
                seen++;

                if (!Resolves(found.Groups[1].Value))
                    dangling.Add($"{Path.GetFileName(path)} names {found.Groups[1].Value}");
            }
        }

        // The companion. Without it this passes for a regex that stopped matching, which is
        // how a rule turns into a comment.
        Assert.True(seen >= 40, $"only {seen} qualified reference(s) found under the brain");

        Assert.True(dangling.Count == 0,
            $"{dangling.Count} name(s) the brain's comments reach for and nothing answers to:"
            + Environment.NewLine + "  "
            + string.Join(Environment.NewLine + "  ", dangling)
            + Environment.NewLine
            + "The reference is a `cref` the compiler cannot see, so this is where it is "
            + "checked. Repair the name or rewrite the claim.");
    }

    /// <summary>Whether a namespace-qualified name still names something.</summary>
    /// <param name="qualified">A name under <c>OpenPlexus</c>, such as <c>Worlds.Seeds.Apart</c>.</param>
    /// <remarks>
    /// <para>
    /// <b>Types first and then members</b>, longest type prefix winning, because a name may end
    /// at a type or carry one member past it. Generic arity is taken off the type's own name
    /// rather than written into the reference, so <c>Worlds.IActed</c> resolves the interface a
    /// <c>cref</c> had to spell <c>IActed{TSeen}</c>.
    /// </para>
    /// <para>
    /// <b>And both assemblies</b>, because the brain's own <c>OpenPlexus.Machines</c> types are
    /// named this way in four places and the question here is whether a name still answers to
    /// something, which is what CS1574 asked.
    /// </para>
    /// </remarks>
    private static bool Resolves(string qualified)
    {
        var parts = ("OpenPlexus." + qualified).Split('.');

        for (var take = parts.Length; take >= 2; take--)
        {
            var named = string.Join(".", parts.Take(take));

            var type = new[]
                {
                    typeof(Bench).Assembly,
                    typeof(Commitment).Assembly,
                    typeof(Multiplexer).Assembly,
                }
                .SelectMany(one => one.GetTypes())
                .FirstOrDefault(one => one.FullName?.Split('`')[0] == named);

            if (type is null) continue;

            const BindingFlags Every =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var member in parts.Skip(take))
            {
                var found = type.GetMember(member, Every);

                if (found.Length == 0) return false;

                type = found[0] as Type ?? type;
            }

            return true;
        }

        return false;
    }
}
