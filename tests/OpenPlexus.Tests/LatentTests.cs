using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A world whose best explanation is never shown — <b>what a posited hub would be
/// minted over.</b>
/// </summary>
/// <remarks>
/// <b>Every channel reports one hidden state</b>, so they co-occur constantly and none
/// of them causes any other. The thing that would explain them has no code and
/// no walk can reach it. That is the shape `Thought.Grouped` was built for and
/// which no other world here has.
/// </remarks>
public sealed class LatentTests(ITestOutputHelper output)
{
    private static LatentSettings World(
        int channels = 6, int causes = 12, double noise = 0.1) =>
        new() { Channels = channels, Causes = causes, Noise = noise };

    private const int Moments = 400;

    [Fact]
    public void A_channel_never_shows_another_channels_code()
    {
        // The world, asserted rather than described. If two channels could emit one
        // code the group would be an artefact of the coding rather than of the
        // hidden cause.
        var codes = Enumerable.Range(0, 6)
            .SelectMany(channel => Enumerable.Range(0, 12)
                .Select(cause => Latent.Shows(channel, cause)))
            .ToList();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void The_cause_itself_is_never_emitted()
    {
        // THE POINT OF THE WORLD. What explains the moment is not in the moment. Taken at
        // the control, where every channel agrees, so the assertion is exact rather than a
        // rate -- what noise does to it is the next test's question.
        var world = new Latent(World(noise: 0), seed: 1);

        for (var moment = 0; moment < 50; moment++)
        {
            var (cause, shown) = world.Moment();

            Assert.Equal(6, shown.Length);
            Assert.All(shown, code => Assert.Equal(Latent.Seen, code.Modality));

            // Every channel agrees on the state, which is what makes them a group.
            for (var channel = 0; channel < shown.Length; channel++)
                Assert.Equal(Latent.Shows(channel, cause), shown[channel]);
        }
    }

    [Fact]
    public void A_lying_channel_shows_another_state_and_lies_at_the_rate_asked_for()
    {
        // A lie the learner could see would not be a lie: a channel that went silent, or
        // that emitted a code no truthful channel ever emits, would be a fourth thing in
        // the moment and the group would not be needed to see through it.
        var world = new Latent(World(noise: 0.25), seed: 1);

        var reports = 0;
        var lies = 0;

        for (var moment = 0; moment < 4_000; moment++)
        {
            var (cause, reported) = world.Draw();

            foreach (var state in reported)
            {
                reports++;
                if (state == cause) continue;

                lies++;
                Assert.InRange(state, 0, 11);
            }
        }

        // Three standard errors of a quarter over twenty-four thousand reports is well
        // under a point, so the band is loose rather than tight.
        Assert.InRange(lies / (double)reports, 0.24, 0.26);
    }

    // ---- what the learner does with it -------------------------------------

    /// <summary>One run of the stream through the population.</summary>
    /// <param name="settings">How many causes, and how many channels report them.</param>
    /// <param name="seed">What draws the causes and the brain.</param>
    /// <param name="rounds">How many moments.</param>
    private static (Latent World, Tally Tally, Brain Brain) Learnt(
        LatentSettings settings, int seed, long rounds = 20_000)
    {
        var world = new Latent(settings, seed);
        var brain = new Brain(new CommittingSettings { Capacity = 4000 }, seed);

        var tally = new Bench(new Watching<Coded>(world, new Passthrough()), brain)
            .Run(rounds, sweep: 1000, target: 0.9, window: 2000);

        return (world, tally, brain);
    }

    /// <summary>How many resident commitments hold a scope of each length.</summary>
    /// <remarks>
    /// <b>What <see cref="Tally.Stackable"/> cannot say on its own.</b> That column is
    /// eligible scopes of three codes or more, so nought at it has two completely different
    /// readings: repair stopping at two, or deep children existing and never reaching the
    /// experience floor a name is offered over. Those want opposite work.
    /// </remarks>
    private static string Lengths(Brain brain)
    {
        var all = brain.Held.All;

        return string.Join(", ", all
            .GroupBy(one => one.Scope.Length)
            .OrderBy(group => group.Key)
            .Select(group =>
                $"{group.Key}:{group.Count()} held, "
                + $"{group.Count(one => one.Seen >= brain.Dials.Floor)} past the floor, "
                + $"{group.Average(one => one.Seen):F1} firings each"));
    }

    /// <summary>
    /// The stream reaches the commitment learner, between the bars the world computes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first run of this world against a population of commitments. What it had before
    /// was the walk, whose runner went, and a world nothing runs asserts what the world is
    /// and nothing about what is learnt.
    /// </para>
    /// <para>
    /// <b>Neither bar is an aspiration and both are arithmetic.</b>
    /// <see cref="Latent.Marginal"/> is what naming one state forever scores, and
    /// <see cref="Latent.Ceiling"/> is one because every channel names the cause outright.
    /// A run under the first says the channels are not reaching the learner at all.
    /// </para>
    /// </remarks>
    [Fact]
    public void Latent_reaches_the_commitment_learner()
    {
        foreach (var noise in new[] { 0.0, 0.1 })
        {
            var (world, tally, brain) = Learnt(World(noise: noise), seed: 1);

            output.WriteLine(
                $"noise {noise:F2} | drawn {tally.Recent:F3} | ceiling {world.Ceiling:F3}, "
                + $"marginal {world.Marginal:F3} | held {tally.Resident}, "
                + $"{tally.Repaired} repairs | {tally.Named} names over {tally.Eligible} "
                + $"eligible, {tally.Stackable} stackable, {tally.Stacked} stacked | "
                + $"wanting {tally.Wanting:F3}");

            output.WriteLine($"noise {noise:F2} | scopes {Lengths(brain)}");

            Assert.True(tally.Recent > world.Marginal,
                $"{tally.Recent:F3} is under the {world.Marginal:F3} naming one state "
                + "forever scores, so the channels are not reaching the learner");

            Assert.True(tally.Recent < world.Ceiling + 0.02,
                $"{tally.Recent:F3} is over the {world.Ceiling:F3} the world allows, so "
                + "the bar is wrong rather than the learner good");
        }
    }

    /// <summary>
    /// Whether this world offers rung five anything to name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The measurement a standing objection asked for</b>, and it settles that objection
    /// in two directions at once. <c>PushbackTests</c> said this world reaches nothing rung
    /// five needs, because its redundancy is in the MOMENT while rung five's trigger is
    /// redundancy across SCOPES — every channel reporting the cause deterministically means
    /// genesis answers at one code, repair is never asked, and no scope longer than one code
    /// exists to hold a pair. The control arm here is that world, and the argument is exactly
    /// right about it: nought eligible, nought repairs, nought names.
    /// </para>
    /// <para>
    /// <b>What it was wrong about is the world rather than the build.</b> A channel that can
    /// lie is what makes the group necessary, which is what a latent cause was always
    /// supposed to be for — with deterministic channels there is no reason to group them,
    /// since any one of them suffices. Under noise no single channel settles the answer,
    /// repair grows scopes over channels that always co-occur, and eligible scopes exist in
    /// hundreds.
    /// </para>
    /// <para>
    /// <b>What would drop the arm, written before the run.</b> If the noisy arm's eligible
    /// count is not above the control's on every seed, then a lying channel bought nothing
    /// rung five can use and the world goes with a revival row.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_channel_that_can_lie_is_what_gives_rung_five_a_scope_to_read()
    {
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var (_, control, _) = Learnt(World(noise: 0), seed);
            var (_, noisy, _) = Learnt(World(noise: 0.1), seed);

            output.WriteLine(
                $"seed {seed} | control | {control.Repaired,5} repairs, "
                + $"{control.Eligible,4} eligible, {control.Stackable,3} stackable | "
                + $"noisy | {noisy.Repaired,5} repairs, {noisy.Eligible,4} eligible, "
                + $"{noisy.Stackable,3} stackable");

            Assert.Equal(0, control.Eligible);

            Assert.True(noisy.Eligible > control.Eligible,
                $"a lying channel left {noisy.Eligible} eligible scopes against the "
                + $"control's {control.Eligible}, so it bought rung five nothing to read "
                + "and this world reaches nothing it needs");
        }
    }

    /// <summary>
    /// Depth exists here and is never tested, which is a different fault from no depth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Tally.Stackable"/> is nought on both arms and for different reasons</b>,
    /// which is why the scope lengths are read rather than the column. On the control there
    /// is no scope past one code at all. On the noisy arm there are three-code scopes in the
    /// hundreds and not one of them has fired often enough to be offered to the naming gate.
    /// </para>
    /// <para>
    /// <b>The third code is a coincidence rather than a channel</b>, and the firing count is
    /// what says so. Three channels agreeing on one state fire together about six rounds in
    /// a hundred, which is over a thousand firings in this run; the three-code scopes repair
    /// actually mints fire a fraction of once. So repair, having exhausted what separates,
    /// is separating on codes that co-occur with the misses by chance — the plan's own
    /// warning that repair amplifies irreducible noise, on a world where the noise is the
    /// point rather than an artifact of a band edge.
    /// </para>
    /// <para>
    /// <b>So what blocks recursion here is repair and not naming</b>, which is the opposite
    /// of <see cref="Worlds.Motif"/>'s reading. There a name shortens the scope that carried
    /// it, so the redundancy consumes its own trigger; here the trigger never arrives,
    /// because a fresh child starts blind and a three-code child never lives long enough to
    /// stop being blind.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_three_code_scopes_exist_and_never_reach_the_floor()
    {
        var (_, tally, brain) = Learnt(World(noise: 0.1), seed: 1);

        output.WriteLine($"scopes  : {Lengths(brain)}");
        output.WriteLine(
            $"named   : {tally.Named} over {tally.Eligible} eligible, "
            + $"{tally.Stackable} stackable");

        var deep = brain.Held.All.Where(one => one.Scope.Length >= 3).ToList();

        Assert.NotEmpty(deep);

        Assert.True(deep.TrueForAll(one => one.Seen < brain.Dials.Floor),
            $"{deep.Count(one => one.Seen >= brain.Dials.Floor)} of {deep.Count} scopes of "
            + "three codes or more are past the experience floor, so depth IS being tested "
            + "here and this reading has changed");

        Assert.Equal(0, tally.Stackable);
    }

    /// <summary>
    /// Which bar refuses the naming gate here, given that the material exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The gate is offered hundreds of scopes and almost never speaks</b>, which the arm
    /// grid in <c>NamingYieldTests</c> turned up as a side effect: both ranking arms come
    /// back identical on this world to every decimal, because there is nearly nothing to
    /// rank. A count of silences says which of five completely different things happened
    /// only if it is partitioned, and on this world it never has been.
    /// </para>
    /// <para>
    /// <b>It is a different question from the one above</b> and shares its run. That one
    /// asks why the scopes are too short-lived to stack; this asks why the pairs they do
    /// contribute are refused. Both can be true and they want opposite work — the first is
    /// repair's, the second is the statistic's.
    /// </para>
    /// <para>
    /// <b>The bar is that it is not <see cref="Refused.Scarce"/></b>, which is the one
    /// refusal that would make the reading uninteresting. Scarce means fewer than three
    /// eligible scopes existed, and this world holds hundreds — so a run landing there would
    /// say the material never reached the gate and every column beside it is about that.
    /// </para>
    /// </remarks>
    [Fact]
    public void Which_bar_refuses_the_gate_where_the_material_is_there()
    {
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var (_, tally, brain) = Learnt(World(noise: 0.1), seed);

            var held = brain.Held;

            output.WriteLine(
                $"seed {seed} | {tally.Eligible,4} eligible | {held.Asked,3} asked, "
                + $"{held.Spoke,2} spoke | scarce {held.AtScarce,3}, unpaired "
                + $"{held.AtUnpaired,3}, rare {held.AtRare,3}, independent "
                + $"{held.AtIndependent,3}, uncertain {held.AtUncertain,3}");

            if (held.Lately is { } lately)
                output.WriteLine(
                    $"         last ask: {lately.Scopes} scopes, {lately.Candidates} "
                    + $"candidates, {lately.Repaying} repaying, peak z {lately.Strongest:F2}");

            Assert.Equal(0, held.AtScarce);
        }
    }
}
