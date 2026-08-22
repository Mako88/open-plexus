using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The vote taken by several holders instead of one — <b>fork 52</b>, and the half of it
/// that can be answered without a socket.
/// </summary>
/// <remarks>
/// <para>
/// <b>The commitment learner runs in one process</b>, so C1 is kept by convention. No
/// node has ever had the OPPORTUNITY to read another's data, which is not the same as a
/// design that forbids it — and a constraint nothing has ever been able to break is a
/// check that cannot fire. <see cref="Population.Weigh"/> is the narrow seam that makes
/// the difference testable: a holder emits expectations and weights, and a merge that
/// only ever sees those cannot cheat.
/// </para>
/// <para>
/// <b>And the first question is agreement-with-itself, which has a known answer.</b>
/// Splitting a population changes no commitment, no accuracy and no dial — only who does
/// the arithmetic. So a split vote that differs from a whole one is a DEFECT rather than a
/// finding, and this file asserts that before it measures anything. The measurements below are only worth
/// reading because that assertion holds.
/// </para>
/// <para>
/// <b>No socket here on purpose</b>, and the limit is stated rather than discovered. What
/// this file exercises is the ARITHMETIC of a distributed vote and none of its transport.
/// A green run says the fold composes; it says nothing whatever about C2, because nothing
/// here is late, and nothing about C3, because nothing here dies. Reading it as <i>the
/// distributed vote works</i> would be a claim about correctness doing duty as a claim
/// about the wire — a trap already on this repo's own list.
/// </para>
/// </remarks>
public sealed class SplitTests(ITestOutputHelper output)
{
    /// <summary>How long the population trains before it is asked anything.</summary>
    /// <remarks>
    /// <b>Enough for the weights to be real and no longer.</b> This file measures whether
    /// an arithmetic composes, and that question does not get a better answer from a
    /// population that has learnt more — but it gets a WORSE one from a population where
    /// every accuracy is still nought, since every weight would then be nought too and
    /// every merge would agree for free.
    /// </remarks>
    private const long Rounds = 8000;

    /// <summary>How many fresh moments the split is asked about.</summary>
    private const int Asked = 4000;

    /// <summary>Address bits, so the world is the six-bit multiplexer step one is judged on.</summary>
    private const int Address = 2;

    /// <summary>
    /// A trained population, and moments it has never been taught on.
    /// </summary>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <b>The moments are drawn after training and nothing is taught on them.</b> Every
    /// call below is one of the three read-only ones — <c>Moment</c>, <c>Firing</c>,
    /// <c>Predict</c> — so asking four thousand questions leaves the population exactly as
    /// the run left it. A comparison that moved a counter would be comparing two
    /// populations rather than two ways of adding up one.
    /// </remarks>
    private static (Population Held, List<IReadOnlySet<Code>> Moments) Trained(
        int seed)
    {
        var brain = new Brain(new CommittingSettings(), seed);

        new MultiplexerRun(new MultiplexerSettings { Address = Address }, brain, seed)
            .Run(Rounds);

        var held = brain.Held;

        // A fresh generator, so these are not the rounds it just saw. Whether the
        // population has met a moment before changes what fires, and a comparison drawn
        // only from familiar moments would be measuring the merge on the easy half.
        //
        // AND THROUGH `IWorld` RATHER THAN THE WORLD'S OWN `Next`, so these moments are
        // coded by exactly the path `Watching` used in training. `Multiplexer.Assignment` carries
        // cues that are already codes, and reading them directly would skip the quantiser
        // -- producing moments the population had never been asked in that alphabet, which
        // is the answer-key-in-the-wrong-alphabet trap wearing a different hat.
        IWorld<IReadOnlyList<int>> world =
            new Multiplexer(new MultiplexerSettings { Address = Address }, seed + 1);

        var sensing = new Bits(Multiplexer.Bit);

        var moments = new List<IReadOnlySet<Code>>(Asked);

        for (var ask = 0; ask < Asked; ask++)
            moments.Add(held.Moment(new HashSet<Code>(sensing.Codify(world.Next().Seen))));

        return (held, moments);
    }

    /// <summary>
    /// Splits what fired between holders the way the ring would — <b>by the identity</b>, so
    /// the same commitment lands on the same holder every time.
    /// </summary>
    /// <param name="firing">What fired, whole.</param>
    /// <param name="held">The population, for the weights.</param>
    /// <param name="holders">How many machines to spread it over.</param>
    /// <param name="missing">A holder that says nothing at all, or -1 for none.</param>
    /// <remarks>
    /// <b>A missing holder is not a silent one</b>, and the parameter is named for the
    /// difference. A holder that fires nothing returns an empty <see cref="Weights"/> and
    /// has been heard from; this one is never asked, which is what a death looks like from
    /// the outside. <see cref="Population.Decide"/> cannot tell them apart and is not
    /// supposed to — the count of who was asked lives with the asker.
    /// </remarks>
    private static List<Weights> Spread(
        ImmutableArray<Commitment> firing, Population held, int holders, int missing = -1)
    {
        var shards = new List<ImmutableArray<Commitment>.Builder>(holders);

        for (var holder = 0; holder < holders; holder++)
            shards.Add(ImmutableArray.CreateBuilder<Commitment>());

        foreach (var commitment in firing)
            shards[(int)(commitment.Identity.Value % (ulong)holders)].Add(commitment);

        var heard = new List<Weights>(holders);

        for (var holder = 0; holder < holders; holder++)
        {
            if (holder == missing) continue;
            heard.Add(held.Weigh(shards[holder].ToImmutable()));
        }

        return heard;
    }

    // ---- the check that can fire -------------------------------------------

    /// <summary>
    /// <b>Scale-free, because the property is the aggregate and not the weight.</b>
    /// </summary>
    /// <remarks>
    /// <b>This once ran over two maximum-shaped rules and the second is deleted.</b> The
    /// base-rate divisor divided before the maximum and the merge never saw it, so this
    /// check was the claim that every holder held the same divisor table. What survives is
    /// the simpler and stronger half: a maximum of maxima is a maximum, at any number of
    /// holders, exactly.
    /// </remarks>
    [Fact]
    public void A_vote_split_between_holders_is_the_same_vote()
    {
        // The aggregate is a maximum under both, and a maximum composes exactly.
        // So this is not a tolerance and must never become one: every field of the vote
        // is bit-identical, including the margin, which is a subtraction of two doubles
        // that were each chosen rather than accumulated.
        var (held, moments) = Trained(seed: 1);

        var compared = 0;
        var contested = 0;

        foreach (var moment in moments)
        {
            var firing = held.Firing(moment);
            if (firing.IsDefaultOrEmpty) continue;

            var whole = held.Predict(firing);

            foreach (var holders in new[] { 2, 3, 5, 12 })
            {
                var split = Population.Decide(Spread(firing, held, holders));

                Assert.Equal(whole.Expects, split.Expects);
                Assert.Equal(whole.By, split.By);
                Assert.Equal(whole.Weight, split.Weight);
                Assert.Equal(whole.Margin, split.Margin);

                compared++;
            }

            // Whether the split was ever load-bearing, which the equalities above cannot
            // say. A moment where one commitment fires is split by every rule alike, so a
            // file full of those would assert nothing and pass. This counts the moments
            // where at least two holders had something to say.
            if (Spread(firing, held, 12).Count(one => !one.Silent) > 1) contested++;
        }

        output.WriteLine($"{compared} comparisons, {contested} moments genuinely spread");

        Assert.True(compared > 1000, $"only {compared} comparisons — the population is too quiet to test");

        Assert.True(contested > 100,
            $"only {contested} moments put advocates on more than one holder, so the merge "
            + "was never asked to combine anything and this file is a tautology");
    }

    [Fact]
    public void What_a_holder_says_survives_being_written_as_bytes_and_read_back()
    {
        // Asked because the other payload built this session could not cross. `Recurrence`
        // was committed as the thing that travels and had never been near a serialiser; it
        // wrote its scope count and dropped both tables, silently and plausibly. Assuming
        // `Weights` is fine because it looks fine is the same reasoning that shipped
        // that one.
        //
        // And the weights are doubles, which is why this is not a formality. `Wire` exists
        // in the shape it does because a value that comes back differing in its last bit
        // codes differently at a band boundary -- and here a weight differing in its last
        // bit reorders a vote whose margin was thin. Fork 12 with the two halves on
        // different machines, where neither end can see that they disagree.
        var (held, moments) = Trained(seed: 1);

        var compared = 0;

        foreach (var moment in moments)
        {
            var firing = held.Firing(moment);
            if (firing.IsDefaultOrEmpty) continue;

            var heard = Spread(firing, held, holders: 12);

            var arrived = heard
                .Select(one => OpenPlexus.Bus.Wire.Read<Weights>(OpenPlexus.Bus.Wire.Write(one)))
                .ToList();

            // On the decision and not only on the fields. Two testimonies comparing equal
            // says the members survived; the same VOTE coming out says the arithmetic did,
            // and that is the thing a machine on the far side actually acts on.
            Assert.Equal(
                Population.Decide(heard),
                Population.Decide(arrived));

            compared++;
        }

        Assert.True(compared > 500, $"only {compared} testimonies crossed");

        // The check that anything was carried. An empty testimony round-trips perfectly and
        // decides identically, so a population too quiet to advocate would pass every line
        // above without the format having been asked anything.
        var spoke = moments
            .Select(moment => held.Firing(moment))
            .Where(firing => !firing.IsDefaultOrEmpty)
            .Sum(firing => Spread(firing, held, 12).Count(one => !one.Silent));

        Assert.True(spoke > 500,
            $"only {spoke} holders had anything to say, so the round trip moved empty sets");
    }

    // ---- what a missing holder costs ---------------------------------------

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_lost_holder_costs_the_vote_against_what_the_margin_predicted()
    {
        // C3 says a cluster vanishing mid-thought is normal, and nothing has ever
        // measured what that does to an answer. In one process nothing can die, so the
        // third outcome has only ever been exercised by unit tests -- an open defect in
        // the plan, and the reason this sweep exists.
        //
        // And the grid is bucketed by the margin because that is the prediction under
        // test. `Vote.Margin` has been computed every round for the life of the branch and
        // called a confidence; if it is one, a wide-margin round should survive a lost
        // holder and a thin-margin round should not. A flat grid would mean the margin is
        // not measuring what its own documentation says.
        //
        // No bar, because a threshold written before the first reading is a prediction
        // dressed as a requirement. It prints a grid and the grid is the finding.
        var (held, moments) = Trained(seed: 1);

        const int Holders = 12;
        var edges = new[] { 0.0, 0.2, 0.4, 0.6, 0.8 };

        var asked = new int[edges.Length];
        var changed = new int[edges.Length];
        var silenced = new int[edges.Length];

        foreach (var moment in moments)
        {
            var firing = held.Firing(moment);
            if (firing.IsDefaultOrEmpty) continue;

            var whole = held.Predict(firing);
            if (whole.Weight <= 0) continue;

            // RELATIVE RATHER THAN ABSOLUTE, matching `Round.Confidence` exactly. A weight
            // is an accuracy and was once an accuracy raised to a power, which made an
            // absolute margin incomparable between two settings; relative survived the dial.
            var confidence = whole.Margin / whole.Weight;

            var bucket = Math.Min(edges.Length - 1, (int)(confidence * edges.Length));

            for (var lost = 0; lost < Holders; lost++)
            {
                var without = Population.Decide(
                    Spread(firing, held, Holders, missing: lost));

                asked[bucket]++;

                if (without.Expects is null) silenced[bucket]++;
                else if (without.Expects != whole.Expects) changed[bucket]++;
            }
        }

        output.WriteLine("confidence | asked | answer changed | silenced");

        for (var bucket = 0; bucket < edges.Length; bucket++)
        {
            if (asked[bucket] == 0)
            {
                output.WriteLine($"{edges[bucket]:F1}-{edges[bucket] + 0.2:F1} |      0 | — | —");
                continue;
            }

            output.WriteLine(
                $"{edges[bucket]:F1}-{edges[bucket] + 0.2:F1} | {asked[bucket],6} "
                + $"| {changed[bucket] / (double)asked[bucket],14:F4} "
                + $"| {silenced[bucket] / (double)asked[bucket],8:F4}");
        }

        // The one assertion, and it is about the instrument rather than the result. A
        // sweep whose every bucket is empty prints five dashes and passes, which is the
        // failure mode this suite exists to refuse.
        Assert.True(asked.Sum() > 1000, $"only {asked.Sum()} holder-losses examined");
    }
}
