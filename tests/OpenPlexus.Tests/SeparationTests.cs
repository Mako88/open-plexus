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
/// </remarks>
public sealed class SeparationTests
{
    /// <summary>Everything a world is not allowed to have heard of.</summary>
    /// <remarks>
    /// <b>The NEW brain only.</b> `csharp`'s walk types are named all over the old
    /// worlds and go when they go; naming them here would make this check a report on
    /// work already scheduled rather than a guard on work being done.
    /// </remarks>
    private static IEnumerable<string> Brainish() =>
        typeof(Commitment).Assembly
            .GetTypes()
            .Where(one => one.IsPublic)
            .Where(one =>
                one.Namespace == "OpenPlexus.Commitments"
                || one.Namespace == "OpenPlexus.Machines")
            .Select(one => one.Name.Split('`')[0])
            .Distinct(StringComparer.Ordinal);

    private static IEnumerable<KeyValuePair<string, string>> Worlds() =>
        Tree.Sources("src")
            .Where(path => path.Contains(
                $"{Path.DirectorySeparatorChar}Worlds{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Select(path => new KeyValuePair<string, string>(path, File.ReadAllText(path)));

    [Fact]
    public void No_world_has_heard_of_the_brain()
    {
        var brainish = Brainish().ToList();

        Assert.NotEmpty(brainish);

        var trespass = new List<string>();

        foreach (var (path, text) in Worlds())
        {
            // The walk-era worlds are exempt and say so. They name `csharp`'s brain
            // everywhere and go when it goes -- this guards what is being built now.
            if (!Path.GetFileName(path).StartsWith("Multiplexer", StringComparison.Ordinal)
                && !Path.GetFileName(path).StartsWith("Graded", StringComparison.Ordinal)
                && !Path.GetFileName(path).StartsWith("Cifar", StringComparison.Ordinal)
                && !Path.GetFileName(path).StartsWith("Arranged", StringComparison.Ordinal)
                && !Path.GetFileName(path).StartsWith("IWorld", StringComparison.Ordinal))
                continue;

            foreach (var name in brainish)
                if (Regex.IsMatch(text, @"\b" + Regex.Escape(name) + @"\b"))
                    trespass.Add($"{Path.GetFileName(path)} names {name}");
        }

        Assert.True(trespass.Count == 0,
            $"{trespass.Count} place(s) where a world reaches into the brain. Move the "
            + "decision to the join, or turn it into something the world outputs:\n  "
            + string.Join("\n  ", trespass));
    }

    [Fact]
    public void And_the_check_can_still_fail()
    {
        // The companion, and without it this passes for a type list that came back
        // empty -- which is exactly how a separation rule rots into a comment.
        var brainish = Brainish().ToList();

        Assert.Contains("Population", brainish);
        Assert.Contains("CommittingSettings", brainish);
        Assert.Contains("Brain", brainish);
        Assert.Contains("Bench", brainish);

        Assert.DoesNotContain("Multiplexer", brainish);
        Assert.DoesNotContain("Graded", brainish);
    }

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
}
