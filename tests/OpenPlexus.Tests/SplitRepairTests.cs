using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What sharding does to repair — <b>the same question as rung five's, with the opposite
/// answer, and the difference is the interesting part.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Repair's evidence is per-commitment and survives splitting by construction.</b>
/// <c>Conditions.Discriminator</c> is handed ONE commitment and reads the table that
/// commitment kept of its own hits and misses. A ring places a commitment whole, so
/// nothing about sharding touches that table — which is <i>decide local</i> working
/// exactly as the plan says it should.
/// </para>
/// <para>
/// <b>But the gate in front of it is not per-commitment, and that is the crack.</b>
/// <c>Mending.Uncovered</c> refuses to repair a commitment when any OTHER firing
/// commitment already narrows it, and asks that of <c>firing</c> — which under sharding is
/// only what this holder happens to hold. A holder cannot see that somebody else already
/// covers the case, so it mints a child the whole population would have refused.
/// </para>
/// <para>
/// <b>So the two mechanisms fail in opposite directions from one cause.</b> Rung five
/// loses the power to CERTIFY a redundancy and goes silent; this gate loses the evidence
/// to REFUSE a repair and over-fires. Both are a population-level statistic computed on a
/// shard, and reading either as the general shape of the problem would miss the other.
/// </para>
/// <para>
/// <b>And this one cannot be fixed the way rung five was.</b> <see cref="Recurrence"/>
/// works because a frequency is monotone and adds; <c>Narrows</c> is a structural test
/// between two scopes and there is nothing to add up. What a holder needs is an ANSWER —
/// <i>does anybody hold a strict specialisation of this scope</i> — which is a round trip
/// rather than a merge, and is C1-clean only because what comes back is a boolean and
/// never a population.
/// </para>
/// </remarks>
public sealed class SplitRepairTests(ITestOutputHelper output)
{
    private const long Rounds = 20000;

    private const int Asked = 2000;

    private const int Address = 3;

    /// <summary>A moment the population was never taught on, and what followed it.</summary>
    private readonly record struct Ask
    {
        public required IReadOnlySet<Code> Moment { get; init; }

        public required Code Arrived { get; init; }
    }

    /// <summary>
    /// How many repair candidates there were, and how many a holder could see were
    /// already covered.
    /// </summary>
    /// <param name="held">The population.</param>
    /// <param name="asks">The moments to walk.</param>
    /// <param name="dials">The gate's numbers, for the miss floor.</param>
    /// <param name="place">Which holder a commitment sits on.</param>
    /// <remarks>
    /// <b>The whole population is this same function</b> with every commitment on one holder,
    /// which is why there is no separate baseline path. Two loops differing only in a
    /// placement rule is two places for the filters to drift, and the clone budget refused
    /// the second copy the moment it was written — which is what that budget is for.
    /// </remarks>
    private static (int Candidates, int Covered) Counted(
        Population held,
        IReadOnlyList<Ask> asks,
        CommittingSettings dials,
        Func<Commitment, ulong> place)
    {
        var candidates = 0;
        var covered = 0;

        foreach (var ask in asks)
        {
            var firing = held.Firing(ask.Moment);
            if (firing.IsDefaultOrEmpty) continue;

            foreach (var one in firing)
            {
                // THE FILTERS `Repair` APPLIES BEFORE THE GATE, so what is counted is
                // commitments repair would actually be deciding about. Measuring the gate
                // over everything that fired would report a rate for a question that is
                // never asked of most of them.
                if (one.Expects == ask.Arrived) continue;
                if (one.Misses < dials.Floor) continue;

                candidates++;

                var mine = place(one);

                if (firing.Any(other => place(other) == mine && other.Narrows(one)))
                    covered++;
            }
        }

        return (candidates, covered);
    }

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_holder_cannot_see_it_repairs_anyway()
    {
        var dials = new CommittingSettings();
        var brain = new Brain(dials, seed: 1);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, seed: 1)
            .Run(Rounds);

        var held = brain.Held;

        IWorld<IReadOnlyList<int>> world =
            new Multiplexer(new MultiplexerSettings { Address = Address }, seed: 99);

        var sensing = new Bits(Multiplexer.Bit);

        var asks = new List<Ask>(Asked);

        for (var ask = 0; ask < Asked; ask++)
        {
            var turn = world.Next();

            asks.Add(new Ask
            {
                Moment = held.Moment(new HashSet<Code>(sensing.Codify(turn.Seen))),
                Arrived = Brain.Says(turn.Outcome!.Value),
            });
        }

        // Every commitment on one holder is the whole population, which is what one
        // process computes today and what every row below is measured against.
        var (candidates, whole) = Counted(held, asks, dials, _ => 0UL);

        output.WriteLine($"{candidates} repair candidates, {whole} covered whole");

        // THE INSTRUMENT CHECK. A gate that never fires would print nought in every row
        // and read as sharding costing nothing.
        Assert.True(whole > 0,
            "no firing commitment was ever narrowed by another, so `Mending.Uncovered`'s "
            + "gate never fires on this world and the grid below measures nothing");

        output.WriteLine("holders | covered by identity | excess | covered by scope prefix");

        foreach (var holders in new[] { 1, 2, 3, 5, 12 })
        {
            var (_, by_identity) =
                Counted(held, asks, dials, one => one.Identity.Value % (ulong)holders);

            // A child and its parent are placed independently, and that is why coverage
            // goes. A commitment's identity derives from its SCOPE, and a child's scope is
            // its parent's plus one code, so the two hash to unrelated places -- the ring
            // actively separates exactly the pairs this gate exists to compare.
            //
            // So place by the first code in the scope instead, which is fork 3's prefix
            // locality asked of the one mechanism now known to need it. An arm and not a
            // proposal: prefix placement trades a uniform load for a clustered one, and
            // nothing here measures what that costs -- a family sharing a root piles onto
            // one holder, which is why a uniform hash was chosen in the first place.
            var (_, by_prefix) =
                Counted(held, asks, dials, one => one.Scope[0].Value % (ulong)holders);

            output.WriteLine(
                $"{holders,7} | {by_identity,19} | {whole - by_identity,6} "
                + $"({(whole - by_identity) / (double)candidates,6:P1}) | {by_prefix,23}");

            // At one holder there is nothing to miss, and a row that disagreed would mean
            // the placement rule was reaching something other than placement.
            if (holders == 1) Assert.Equal(whole, by_identity);
        }
    }
}
