using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The third outcome, reachable from a run at last.
/// </summary>
/// <remarks>
/// <para>
/// <b>The plan carried this as an open defect and blamed the wrong thing.</b> <i>`Abstain`
/// is unarmed in any run — nothing in one process can die, so C3's third outcome is
/// exercised only by unit tests.</i> Distribution was never the wall.
/// <see cref="Commitment.Settle"/> has always handled the verdict correctly and
/// <see cref="Population.Settle"/> has always taken a nullable code; <c>Round.Step</c> took
/// a NON-NULLABLE one, so no caller could produce a single abstain on any number of
/// machines with any number of deaths.
/// </para>
/// <para>
/// <b>So a check that could not fire was reading as a mechanism nobody had needed yet</b>,
/// which is the oldest line on this repo's trap list wearing the plan's own words.
/// </para>
/// <para>
/// <b>And the source is a world rather than a network, which is the honest place for
/// it.</b> A world that sometimes does not say what followed is not a simulation of a dead
/// machine — most moments in any real stream are followed by nothing anybody observes, and
/// a world saying so is a world saying what it is looking at. The distributed case arrives
/// on top of this rather than instead of it.
/// </para>
/// </remarks>
public sealed class AbstainTests(ITestOutputHelper output)
{
    private const long Rounds = 8000;

    private static Learned Run(double unsettled, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = 2, Unsettled = unsettled },
            new Brain(new CommittingSettings(), seed),
            seed).Run(Rounds);

    /// <summary>One moment from one source, at a stamp the caller chooses.</summary>
    /// <param name="sequence">Which moment it is from that source.</param>
    /// <param name="codes">What is live.</param>
    /// <param name="followed">What the source says followed, or nothing.</param>
    /// <remarks>
    /// <b>The stamp is the argument</b>, because two of the tests below turn on it. A
    /// helper that counted for its caller could not express a repeat, and a repeat is the
    /// other thing a brain refuses to settle on.
    /// </remarks>
    private static Pushed Push(long sequence, IReadOnlySet<Code> codes, Code? followed) =>
        new()
        {
            From = new Stamp { Source = Stamp.First, Sequence = sequence },
            Codes = codes,
            Followed = followed,
        };

    /// <summary>
    /// <b>A round the world cannot settle costs a commitment exactly nothing.</b>
    /// </summary>
    /// <remarks>
    /// The design's claim, asserted on the primitive rather than inferred from a score. A
    /// monotone counter cannot retract a slur, so a settlement that could not say must
    /// leave the hits, the misses, the local estimate AND the separation table untouched —
    /// the last of those being the one that would otherwise make a run's repair depend on
    /// how often the world was quiet.
    /// </remarks>
    [Fact]
    public async Task An_unsettled_round_moves_nothing_but_the_abstain_count()
    {
        var brain = new Brain(new CommittingSettings(), seed: 1);
        var held = brain.Held;

        var one = Fixture.C(1);
        var two = Fixture.C(2);

        held.Add(new Commitment([one], Brain.Says(0)));

        var loop = new Round(brain, rounds: 10, sweep: 1000, target: 0.9, window: 2);

        var moment = new HashSet<Code> { one, two };

        await loop.StepAsync(Push(1, moment, Brain.Says(0)));

        var mind = held.All.Single();

        var hits = mind.Hits;
        var misses = mind.Misses;
        var accuracy = mind.Accuracy;
        var separations = mind.Separations.Count;

        await loop.StepAsync(Push(2, moment, followed: null));

        Assert.Equal(1, loop.Abstained);
        Assert.Equal(1, mind.Abstains);

        // Not one of these moves, and the table is the one that would be missed. Hits and
        // misses are the obvious pair; `Separations` is what repair reads, and letting an
        // unsettled round into it would make which condition wins a fact about the
        // world's silence.
        Assert.Equal(hits, mind.Hits);
        Assert.Equal(misses, mind.Misses);
        Assert.Equal(accuracy, mind.Accuracy);
        Assert.Equal(separations, mind.Separations.Count);

        // AND THE ROUND HAPPENED, which is the half that separates this from the loop
        // simply skipping the call. A round nobody counted would leave every number above
        // equal for the wrong reason.
        Assert.Equal(2, loop.Rounds);

        // AND NOTHING WAS MINTED EITHER. Genesis needs something to have arrived to be
        // surprised by, so an unsettled round cannot cover -- and if it could, a quiet
        // world would fill a population with commitments about nothing.
        Assert.Single(held.All);
    }

    /// <summary>
    /// <b>The verdict fires in a run, at about the rate the world was told to withhold.</b>
    /// </summary>
    /// <remarks>
    /// <b>The rate is the check rather than the mere presence of a count.</b> A single
    /// abstain anywhere would satisfy <i>it can fire</i> and would not say the dial was
    /// connected to the thing it names — this repo has a line about a dial declared,
    /// documented, passed everywhere and connected to nothing.
    /// </remarks>
    [Fact]
    public void The_third_outcome_fires_in_a_run_and_at_the_rate_the_world_withheld()
    {
        var quiet = Run(unsettled: 0.1, seed: 1);

        Assert.True(quiet.Abstained > 0, "no round abstained, so the verdict is still unarmed");

        var share = quiet.Abstained / (double)quiet.Rounds;

        output.WriteLine(
            $"{quiet.Abstained} of {quiet.Rounds} rounds unsettled ({share:F3}) | "
            + $"recent {quiet.Recent:F3} | sound {quiet.Sound} | resident {quiet.Resident}");

        // Generous on purpose, because it is a draw and not a quota. What is being asserted
        // is that the dial reaches the loop, not that a binomial landed on its mean.
        Assert.InRange(share, 0.07, 0.13);
    }

    /// <summary>
    /// <b>And the control: with the dial off, nothing abstains and the run is the old
    /// run.</b>
    /// </summary>
    /// <remarks>
    /// <b>Bit-identical and not merely similar, which is what the short-circuit buys.</b>
    /// The draw is skipped entirely at zero, so the world's generator is never touched and
    /// every figure this world has ever reported is reproduced. A dial that consumed one
    /// number a round even when off would shift the stream and move every existing
    /// measurement — fork 12 arriving dressed as a feature.
    /// </remarks>
    [Fact]
    public void With_the_dial_off_nothing_abstains_and_the_run_is_what_it_always_was()
    {
        var plain = Run(unsettled: 0.0, seed: 1);

        Assert.Equal(0, plain.Abstained);

        var again = new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new Brain(new CommittingSettings(), 1),
            1).Run(Rounds);

        Assert.Equal(again.Recent, plain.Recent);
        Assert.Equal(again.Sound, plain.Sound);
        Assert.Equal(again.Resident, plain.Resident);
        Assert.Equal(again.Repaired, plain.Repaired);
    }

    /// <summary>
    /// What a world that goes quiet costs the learner — <b>a grid, and no bar.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The design claims an unsettled round is free, which is not the same as
    /// harmless.</b> It costs a commitment nothing, and it costs the RUN an observation —
    /// so a world quiet a third of the time should look like a shorter run rather than a
    /// worse one, and anything beyond that would be the verdict doing damage the primitive
    /// says it cannot.
    /// </para>
    /// <para>
    /// <b>No bar, because what silence should cost has never been measured</b> and a
    /// threshold written before the first reading is a prediction dressed as a
    /// requirement. The grid is the finding.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_world_that_goes_quiet_costs()
    {
        output.WriteLine("unsettled | abstained | recent | sound | resident | reached");

        foreach (var unsettled in new[] { 0.0, 0.1, 0.3, 0.5 })
        {
            var recent = new List<double>();
            var sound = new List<double>();
            var abstained = new List<double>();
            var reached = new List<double>();
            var resident = new List<double>();

            foreach (var seed in new[] { 1, 2, 3 })
            {
                var learned = Run(unsettled, seed);

                recent.Add(learned.Recent);
                sound.Add(learned.Sound);
                abstained.Add(learned.Abstained);
                reached.Add(learned.Reached);
                resident.Add(learned.Resident);
            }

            output.WriteLine(
                $"{unsettled,9:F1} | {abstained.Average(),9:F0} | {recent.Average(),6:F3} "
                + $"| {sound.Average(),5:F1} | {resident.Average(),8:F0} "
                + $"| {reached.Average(),7:F0}");
        }
    }

    /// <summary>
    /// <b>A moment whose stamp does not advance its source is refused.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other way nothing moves, and it is a different absence from the one above. An
    /// abstain is the world unable to say what followed; this is the brain declining to
    /// read a moment as an answer at all — C2 permits a message to be late, duplicated or
    /// out of order, and a settlement taken from one of those would be an answer counted
    /// against a question it was never about.
    /// </para>
    /// <para>
    /// <b>And it is not a round</b>, which is the half a single counter would lose. A
    /// repeat leaves <see cref="Round.Rounds"/> alone and moves <see cref="Round.Refused"/>
    /// instead, so a source that pushed everything twice reads as half the rounds it
    /// claimed rather than as a population that stopped learning.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task A_repeated_stamp_is_refused_and_is_not_a_round()
    {
        var brain = new Brain(new CommittingSettings(), seed: 1);

        var one = Fixture.C(1);

        brain.Held.Add(new Commitment([one], Brain.Says(0)));

        var loop = new Round(brain, rounds: 10, sweep: 1000, target: 0.9, window: 2);

        var moment = new HashSet<Code> { one };

        await loop.StepAsync(Push(1, moment, Brain.Says(0)));

        var mind = brain.Held.All.Single();

        var hits = mind.Hits;

        await loop.StepAsync(Push(1, moment, Brain.Says(0)));

        // Settled once, for the one moment there was. A second hit here would be the
        // duplicate counted as evidence, which is the failure the stamp exists to stop.
        Assert.Equal(hits, mind.Hits);

        Assert.Equal(1, loop.Rounds);
        Assert.Equal(1, loop.Refused);

        // And an older stamp goes the same way, because late and duplicated are one thing
        // to a receiver that may only ask whether a moment advances its source.
        await loop.StepAsync(Push(0, moment, Brain.Says(0)));

        Assert.Equal(hits, mind.Hits);
        Assert.Equal(2, loop.Refused);

        output.WriteLine(
            $"{loop.Rounds} rounds | {loop.Refused} refused | {mind.Hits} hits");
    }

    /// <summary>
    /// <b>A second run on one bench keeps counting where the first stopped.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The budget for a failure class this session created and CI caught. The first build of
    /// the seam stamped a moment with the loop's round number, so a bench run twice pushed
    /// sequences 0 to 3,999 both times — and the second run was refused entirely, round for
    /// round, while reporting the rounds it had been asked for. Two fleet tests went red on a
    /// count that had not moved, and nothing local looked at them.
    /// </para>
    /// <para>
    /// <b>The class is a stamp taken from something that restarts</b>, and the fix is that the
    /// sequence belongs to the source. <see cref="Watching{TSeen}"/> holds it, so it survives
    /// however many times a bench is run — and this is the check that says so rather than the
    /// comment beside the field.
    /// </para>
    /// <para>
    /// <b>Read on <see cref="Tally.Refused"/> rather than on a score</b>, because a refused run
    /// scores whatever the first one left behind. That is why the original went unnoticed for a
    /// commit: the population was still there, still accurate, and had simply stopped learning.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_bench_run_twice_takes_every_moment_of_the_second_run()
    {
        var brain = new Brain(new CommittingSettings(), seed: 1);

        var bench = new Bench(
            new Watching<IReadOnlyList<int>>(
                new Multiplexer(new MultiplexerSettings { Address = 2 }, seed: 1),
                new Bits(Multiplexer.Bit)),
            brain);

        const long Half = 500;

        var first = bench.Run(Half, sweep: 1000, target: 0.9, window: 100);
        var second = bench.Run(Half, sweep: 1000, target: 0.9, window: 100);

        Assert.Equal(0, first.Refused);
        Assert.Equal(0, second.Refused);

        Assert.Equal(Half, first.Rounds);
        Assert.Equal(Half, second.Rounds);

        output.WriteLine(
            $"{first.Rounds} + {second.Rounds} rounds | "
            + $"{first.Refused} + {second.Refused} refused");
    }
}
