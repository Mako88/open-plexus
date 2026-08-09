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
/// <b>THE PLAN CARRIED THIS AS AN OPEN DEFECT AND BLAMED THE WRONG THING.</b> <i>`Abstain`
/// is unarmed in any run — nothing in one process can die, so C3's third outcome is
/// exercised only by unit tests.</i> Distribution was never the wall.
/// <see cref="Commitment.Settle"/> has always handled the verdict correctly and
/// <see cref="Population.Settle"/> has always taken a nullable code; <c>Cycle.Step</c> took
/// a NON-NULLABLE one, so no caller could produce a single abstain on any number of
/// machines with any number of deaths.
/// </para>
/// <para>
/// <b>SO A CHECK THAT COULD NOT FIRE WAS READING AS A MECHANISM NOBODY HAD NEEDED YET</b>,
/// which is the oldest line on this repo's trap list wearing the plan's own words.
/// </para>
/// <para>
/// <b>AND THE SOURCE IS A WORLD RATHER THAN A NETWORK, WHICH IS THE HONEST PLACE FOR
/// IT.</b> A world that sometimes does not say what followed is not a simulation of a dead
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

    /// <summary>
    /// <b>A ROUND THE WORLD CANNOT SETTLE COSTS A COMMITMENT EXACTLY NOTHING.</b>
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
        var held = new Population(new CommittingSettings(), seed: 1);

        var one = Fixture.C(1);
        var two = Fixture.C(2);

        held.Add(new Commitment([one], Brain.Says(0)));

        var cycle = new Cycle(new Alone(held), rounds: 10, sweep: 1000, target: 0.9, window: 2);

        var moment = new HashSet<Code> { one, two };

        await cycle.StepAsync(moment, Brain.Says(0));

        var mind = held.All.Single();

        var hits = mind.Hits;
        var misses = mind.Misses;
        var accuracy = mind.Accuracy;
        var separations = mind.Separations.Count;

        await cycle.StepAsync(moment, arrived: null);

        Assert.Equal(1, cycle.Abstained);
        Assert.Equal(1, mind.Abstains);

        // NOT ONE OF THESE MOVES, AND THE TABLE IS THE ONE THAT WOULD BE MISSED. Hits and
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
        Assert.Equal(2, cycle.Rounds);

        // AND NOTHING WAS MINTED EITHER. Genesis needs something to have arrived to be
        // surprised by, so an unsettled round cannot cover -- and if it could, a quiet
        // world would fill a population with commitments about nothing.
        Assert.Single(held.All);
    }

    /// <summary>
    /// <b>THE VERDICT FIRES IN A RUN, AT ABOUT THE RATE THE WORLD WAS TOLD TO WITHHOLD.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE RATE IS THE CHECK RATHER THAN THE MERE PRESENCE OF A COUNT.</b> A single
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

        // GENEROUS ON PURPOSE, BECAUSE IT IS A DRAW AND NOT A QUOTA. What is being asserted
        // is that the dial reaches the loop, not that a binomial landed on its mean.
        Assert.InRange(share, 0.07, 0.13);
    }

    /// <summary>
    /// <b>AND THE CONTROL: WITH THE DIAL OFF, NOTHING ABSTAINS AND THE RUN IS THE OLD
    /// RUN.</b>
    /// </summary>
    /// <remarks>
    /// <b>BIT-IDENTICAL AND NOT MERELY SIMILAR, WHICH IS WHAT THE SHORT-CIRCUIT BUYS.</b>
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
    /// <b>THE DESIGN CLAIMS AN UNSETTLED ROUND IS FREE, WHICH IS NOT THE SAME AS
    /// HARMLESS.</b> It costs a commitment nothing, and it costs the RUN an observation —
    /// so a world quiet a third of the time should look like a shorter run rather than a
    /// worse one, and anything beyond that would be the verdict doing damage the primitive
    /// says it cannot.
    /// </para>
    /// <para>
    /// <b>NO BAR, BECAUSE WHAT SILENCE SHOULD COST HAS NEVER BEEN MEASURED</b> and a
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
}
