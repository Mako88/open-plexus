using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether two machines asked one question hear the same question — <b>the thing the
/// split vote assumed and never checked.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>`SplitTests` folded the moment</b> once and handed the same set to every holder, which
/// is not what a deployment does. That file split a population's COMMITMENTS and left
/// its <see cref="Naming"/> whole, so every shard fired against an identical moment by
/// construction. The bit-identical result it reports is therefore a fact about the
/// arithmetic and not about two machines — and reading it as the latter would be a
/// simulated constraint being gentler than the real one, which is the trap this repo's own
/// list already carries, running in the other direction.
/// </para>
/// <para>
/// <b>Because a moment is folded before anything fires</b>, and folding needs the names.
/// <see cref="Naming.Fold"/> adds every minted name whose members are all present, and
/// runs to a fixed point because a name may stand for a set containing names. A holder
/// folds with the names IT has minted. Two holders that abstracted on different evidence
/// hold different names, so they do not merely disagree about the answer — they were asked
/// different questions.
/// </para>
/// <para>
/// <b>And the design says this should be fine, which is exactly why it is worth
/// checking.</b> A name's identity is a hash of its members, so two machines that notice
/// the same redundancy mint the same code without speaking. That is the claim. What it
/// does not say is whether they notice the same redundancies at all — fork 29's divergent
/// siblings, arriving at rung five instead of at repair.
/// </para>
/// </remarks>
public sealed class FoldingTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    private const int Asked = 2000;

    /// <summary>
    /// Eleven bits, <b>because six mints no names at all and would pass for free.</b>
    /// </summary>
    /// <remarks>
    /// <b>Fork 34 is why this number is not two.</b> Rung five names nothing at six bits
    /// and both names and STACKS at eleven — so a version of this file run on the world
    /// step one is judged on would compare two empty naming tables, find them identical,
    /// and report agreement it had never tested.
    /// </remarks>
    private const int Address = 3;

    /// <param name="world">The world's generator — which observations are seen.</param>
    /// <param name="brain">The brain's own generator, used by the control arm.</param>
    /// <remarks>
    /// <b>Two seeds and not one</b>, because otherwise the arm below cannot be built.
    /// Holding the world fixed and moving the brain asks whether the SAME evidence mints
    /// the same names; moving both asks whether two machines that saw different things
    /// end up anywhere near each other. One number without the other is unreadable —
    /// disagreement could be a fact about abstraction or a fact about the draw, and this
    /// project has a trap line about exactly that confusion.
    /// </remarks>
    private static Population Trained(int world, int brain)
    {
        var thinking = new Brain(new CommittingSettings(), brain);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, thinking, world)
            .Run(Rounds);

        return thinking.Held;
    }

    /// <summary>How far apart two machines fold the same fresh moments.</summary>
    /// <param name="mine">One machine's population.</param>
    /// <param name="yours">The other's.</param>
    internal static (int Folded, int Apart) Compared(Population mine, Population yours)
    {
        IWorld<IReadOnlyList<int>> world =
            new Multiplexer(new MultiplexerSettings { Address = Address }, seed: 99);

        var sensing = new Bits(Multiplexer.Bit);

        var apart = 0;
        var folded = 0;

        for (var ask = 0; ask < Asked; ask++)
        {
            var raw = new HashSet<Code>(sensing.Codify(world.Next().Seen));

            var here = mine.Moment(raw);
            var there = yours.Moment(raw);

            if (here.Count > raw.Count || there.Count > raw.Count) folded++;
            if (!here.SetEquals(there)) apart++;
        }

        return (folded, apart);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Two_machines_on_one_world_and_whether_they_hear_the_same_question()
    {
        // TWO SEEDS IS TWO MACHINES, and it is the honest simulation of the case rather
        // than a harsh one. Real holders see the same stream and abstract on their own
        // share of it; these see different streams entirely, so any disagreement here is
        // an upper bound on the disagreement a deployment would show. An upper bound that
        // came back at nought would settle the question; one that came back large would
        // only open it.
        var mine = Trained(world: 1, brain: 1);
        var yours = Trained(world: 2, brain: 2);

        var ours = mine.Names.Means.Select(one => one.Key).ToHashSet();
        var theirs = yours.Names.Means.Select(one => one.Key).ToHashSet();

        var shared = ours.Intersect(theirs).Count();

        output.WriteLine($"names: {ours.Count} and {theirs.Count}, {shared} in common");

        // The check that this file is asking anything at all. Two empty tables fold every
        // moment identically and would report perfect agreement for the one reason that
        // makes the number worthless.
        Assert.True(ours.Count > 0 && theirs.Count > 0,
            $"{ours.Count} and {theirs.Count} names minted — rung five did not fire, so "
            + "this file compared two empty tables and learnt nothing");

        var (folded_at_all, apart) = Compared(mine, yours);

        output.WriteLine(
            $"different streams | {folded_at_all} folded by at least one | {apart} folded apart "
            + $"({apart / (double)Asked:P1})");

        // The arm, and without it the number above is unreadable. Same world, same
        // stream, different brain generator: if the same evidence still mints different
        // names then abstraction is not convergent and the wire cannot fix it. If it
        // mints the SAME names, then everything above is a fact about seeing different
        // observations and says nothing about the mechanism.
        var alike = Trained(world: 1, brain: 7);

        var also = alike.Names.Means.Select(one => one.Key).ToHashSet();

        var (folded_alike, apart_alike) = Compared(mine, alike);

        output.WriteLine(
            $"one stream, two brains | names {ours.Count} and {also.Count}, "
            + $"{ours.Intersect(also).Count()} in common | {folded_alike} folded "
            + $"| {apart_alike} folded apart ({apart_alike / (double)Asked:P1})");

        // And the second check that this file can fail. A naming table that never
        // completes on any live moment is a table that exists and does nothing, and the
        // disagreement count below it would then be nought for that reason instead of
        // because two machines agreed.
        Assert.True(folded_at_all > 0,
            "no moment was folded by either machine, so the comparison below is between "
            + "two unchanged sets and says nothing about naming");

        // No bar on the disagreement itself, because what it should be has never been
        // measured and a threshold written first would be a prediction dressed as a
        // requirement. The number is the finding.
    }
}
