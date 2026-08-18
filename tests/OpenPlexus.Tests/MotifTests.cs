using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The world where rung five's redundancy is manufactured on purpose.
/// </summary>
/// <remarks>
/// <para>
/// <b>A set of codes that recurs should earn a code</b>, and that is what lets the
/// alphabet grow where a quantiser fixes it forever, and this stream is built so that the
/// recurrence is there to be found rather than hoped for.
/// </para>
/// <para>
/// <b>The world carries its own control</b>, which is the whole reason it is a bench and
/// not a demonstration. <see cref="MotifSettings.Motifs"/> at nought is the same stream
/// with nothing in it worth a name, so a gate that mints there is minting on coincidence.
/// </para>
/// </remarks>
public sealed class MotifTests(ITestOutputHelper output)
{
    private static MotifSettings World(int motifs = 6, int size = 4, double density = 0.5) => new()
    {
        Symbols = 60, Motifs = motifs, Size = size, Density = density,
    };

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void The_sets_are_disjoint_and_a_noise_moment_is_the_same_size()
    {
        // Overlapping sets would make a completion ambiguous for a reason that has
        // nothing to do with chunking, and a noise moment of a different size
        // would make the task counting rather than recognising.
        var world = new Motif(World(), seed: 1);

        var seen = new HashSet<OpenPlexus.Codes.Code>();

        foreach (var motif in world.Motifs)
        {
            Assert.Equal(4, motif.Length);
            foreach (var code in motif) Assert.True(seen.Add(code), "the sets overlap");
        }

        for (var moment = 0; moment < 200; moment++)
            Assert.Equal(4, world.Draw().Shown.Length);
    }

    [Fact]
    public void A_question_shows_half_a_set_and_wants_the_rest()
    {
        var world = new Motif(World(), seed: 1);
        var (asked, wanted) = world.Ask(0);

        Assert.Equal(2, asked.Length);
        Assert.Equal(2, wanted.Count);
        Assert.Empty(asked.Where(wanted.Contains));

        // And the whole set is the two together, so nothing was dropped.
        Assert.Equal(world.Motifs[0].Order(), asked.Concat(wanted).Order());
    }

    [Fact]
    public void The_compression_target_is_arithmetic_and_not_a_measurement()
    {
        // A SET OF SIZE S WRITTEN AS CO-OCCURRENCE IS S(S-1) DIRECTED ENTRIES; a
        // node standing for the set is S. That gap is what step 3 buys, and it is
        // computed rather than measured so the graph can be compared against it.
        var world = new Motif(World(motifs: 6, size: 4), seed: 1);

        Assert.Equal(6 * 4, world.Compressed);
        Assert.Equal(6 * 4 * 3, world.Uncompressed);
    }

    // ---- what the graph does with it ---------------------------------------

    /// <summary>One run of the stream through the population, and what it held afterwards.</summary>
    /// <param name="settings">The shape of the stream.</param>
    /// <param name="seed">What draws the sets, the moments and the brain.</param>
    /// <param name="rounds">How many moments.</param>
    /// <remarks>
    /// <see cref="Passthrough"/>, because a moment here is already codes and rendering it as
    /// a signal first would put a second thing between the mechanism and the reading.
    /// </remarks>
    private static (Motif World, Tally Tally) Learnt(
        MotifSettings settings, int seed, long rounds = 20_000)
    {
        var world = new Motif(settings, seed);
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed);

        var tally = new Bench(new Watching<Coded>(world, new Passthrough()), brain)
            .Run(rounds, sweep: 1000, target: 0.9, window: 2000);

        return (world, tally);
    }

    /// <summary>
    /// The stream reaches the commitment learner, between the two bars the world computes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first run of this world against a population of commitments. What it had before
    /// was the walk, whose runner went — and a world nothing runs asserts what the world is
    /// and nothing about what is learnt.
    /// </para>
    /// <para>
    /// <b>Neither bar is an aspiration and both are arithmetic.</b>
    /// <see cref="Motif.Marginal"/> is what naming the commonest outcome forever scores and
    /// <see cref="Motif.Ceiling"/> is what knowing the whole generative process scores, the
    /// gap between them being the sets and nothing else.
    /// </para>
    /// <para>
    /// <b>A score above the ceiling means the bar is wrong</b>, which is the assertion
    /// underneath. A bar computed with no learning is only worth having while something
    /// would notice it being exceeded.
    /// </para>
    /// </remarks>
    [Fact]
    public void Motif_reaches_the_commitment_learner()
    {
        var (world, tally) = Learnt(World(size: 6), seed: 1);

        output.WriteLine($"drawn   : {tally.Recent:F3} over the last tenth");
        output.WriteLine(
            $"bars    : ceiling {world.Ceiling:F3}, marginal {world.Marginal:F3}, "
            + $"blind draw {1.0 / world.Outcomes:F3}");
        output.WriteLine(
            $"held    : {tally.Resident} commitments, {tally.Repaired} repairs, "
            + $"{tally.Named} names over {tally.Eligible} eligible scopes, "
            + $"{tally.Stackable} of them stackable, {tally.Stacked} stacked");
        output.WriteLine($"wanting : {tally.Wanting:F3} of blamed rounds nothing separated");

        Assert.True(tally.Recent > world.Marginal,
            $"{tally.Recent:F3} is under the {world.Marginal:F3} naming the commonest "
            + "outcome forever scores, so the sets are not reaching the learner");

        Assert.True(tally.Recent < world.Ceiling + 0.02,
            $"{tally.Recent:F3} is over the {world.Ceiling:F3} the world allows, so the bar "
            + "is wrong rather than the learner good");
    }

    /// <summary>
    /// What the naming gate does with manufactured redundancy, and what it does without it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This repo's oldest refutation, re-asked of the current gate.</b> On <c>csharp</c>'s
    /// <c>Motif</c> a minimum-description-length gate alone minted 715 names on a pure-noise
    /// control, which is why the plan's table says never to run one without a bar it has to
    /// beat as well. Whether the corrected gate still does that here was unmeasured, and
    /// <see cref="MotifSettings.Motifs"/> at nought is the control the world already carried.
    /// </para>
    /// <para>
    /// <b>The two arms are the same stream in every other respect.</b> Same alphabet, same
    /// moment size, same draw without replacement — the control simply has nothing in it
    /// that recurs, so a name minted there stands for a coincidence.
    /// </para>
    /// <para>
    /// <b>The column is names per eligible scope</b>, rather than a count of names. An
    /// absolute count is bounded by the sweep calendar and moves with how much repair
    /// happened, and the two arms repair at different rates by construction — so a
    /// numerator on its own would be reporting the denominator.
    /// </para>
    /// <para>
    /// <b>What would drop the arm, written before the run.</b> If the control's names per
    /// eligible scope is not below the motif arm's by more than the motif arm's own seed
    /// spread, then the gate is not reading recurrence and this world prices nothing about
    /// rung five.
    /// </para>
    /// <para>
    /// <b>The cue is three codes rather than two</b>, because a scope of two may be named and
    /// can never carry a name. Stacking is the reading that needs the length, and
    /// <see cref="Motif.Cue"/> is what supplies it.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_naming_gate_answers_to_recurrence_and_not_to_the_stream()
    {
        var recurring = new List<double>();
        var noise = new List<double>();

        foreach (var seed in new[] { 1, 2, 3 })
        {
            var (world, sets) = Learnt(World(size: 6), seed);
            var (_, none) = Learnt(World(motifs: 0, size: 6), seed);

            recurring.Add(sets.PerEligible);
            noise.Add(none.PerEligible);

            Assert.Equal(3, world.Cue);

            output.WriteLine(
                $"seed {seed} | sets  | named {sets.Named,3} over {sets.Eligible,4} "
                + $"eligible ({sets.PerEligible:F3}), {sets.Stackable,4} stackable, "
                + $"{sets.Stacked,3} stacked | held {sets.Resident,4} | "
                + $"{sets.Repaired} repairs");

            output.WriteLine(
                $"seed {seed} | noise | named {none.Named,3} over {none.Eligible,4} "
                + $"eligible ({none.PerEligible:F3}), {none.Stackable,4} stackable, "
                + $"{none.Stacked,3} stacked | held {none.Resident,4} | "
                + $"{none.Repaired} repairs");
        }

        var spread = recurring.Max() - recurring.Min();
        var withSets = recurring.Average();
        var without = noise.Average();

        output.WriteLine(
            $"sets {withSets:F3} over a spread of {spread:F3}, noise {without:F3}");

        Assert.True(withSets - without > spread,
            $"the noise control named {without:F3} per eligible scope against {withSets:F3} "
            + $"with the sets in, a gap of {withSets - without:F3} against a seed spread of "
            + $"{spread:F3} — so the gate is not reading recurrence");
    }

    // ---- step 3, built ------------------------------------------------------

    // ---- What chunking bought, and why the measurement is gone ------------
    //
    // two tests stood here and both compared chunking against its own absence:
    // `Minting_a_node_buys_the_traffic_it_was_supposed_to` and
    // `What_the_minting_costs_and_what_it_buys_over_seeds`. Step 3 became
    // unconditional on 2026-08-04 -- John's rule, you build it and it is ON -- so
    // both arms are now the same run and the comparison is not expressible.
    //
    // WHAT THEY FOUND, so it is not lost with them: minting cut traffic and cost
    // a little accuracy over six seeds, and the unverified reading of the
    // accuracy cost was that a minted node is a hub by construction and
    // `Pricing.Receiver` refuses hubs. `Toll.Traffic` is the arm that should
    // invert that, and it is a named alternative rather than a switch, so THAT
    // comparison is still expressible and is the one worth taking.
    //
    // The complaint list is what guards the mechanism now: `MotifResult` fails a
    // run that minted nothing at all, and one that minted more names than the
    // world has recurring sets.
}
