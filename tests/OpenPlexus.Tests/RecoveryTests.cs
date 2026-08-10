using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What it costs to recover when the world moves under the learner — <b>fork 27's direct
/// test, and the one step-one requirement whose world was built and never measured.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE PLAN NAMES THIS INSTRUMENT AND THE REPO HAS ONLY THE WORLD.</b>
/// <c>MultiplexerSettings.Switch</c> moves the target mid-run and <c>MultiplexerTests</c>
/// asserts that it moves it correctly — the key travels with it, the first mapping is the
/// identity so a switching run and a standard one are one world until the first flip. What
/// nobody built is the reading: <i>flip the target mid-run, report steps to recover</i>.
/// </para>
/// <para>
/// <b>AND IT IS THE ONLY THING THAT CAN SETTLE FORK 27.</b> Hits, misses and abstains are
/// G-Counters and give a LIFETIME average for free; beside them each node keeps a
/// recency-weighted estimate of what IT saw, which never merges and is what decides. The
/// second estimate is justified by C4 — no episode boundary, so a lifetime average cannot
/// track — and on a stationary world it is predicted to buy nothing. A world that moves is
/// where that prediction is falsifiable, and the dial that turns the local estimate back
/// into the lifetime one is <see cref="CommittingSettings.Recency"/> at near zero.
/// </para>
/// <para>
/// <b>SO THE GRID IS TWO WORLDS BY TWO ARMS, AND THE STATIONARY HALF IS NOT DECORATION.</b>
/// A difference on the switching world alone is the finding; the same difference on both is
/// the dial doing something else entirely, and this repo has paid for reading one cell of a
/// two-by-two as though it were the whole of it. Measure one mechanism ON from a known
/// baseline, never one OFF from all-on.
/// </para>
/// </remarks>
public sealed class RecoveryTests(ITestOutputHelper output)
{
    /// <summary>How long the world holds still before it moves.</summary>
    /// <remarks>
    /// <b>LONG ENOUGH THAT THE TARGET IS HELD BEFORE THE FLIP, or this measures learning
    /// rather than recovery.</b> Six bits reaches the target well inside this on every seed
    /// the scaling grid reads, so what happens after the flip is a fall from a height the
    /// run had actually reached.
    /// </remarks>
    private const int Settled = 20_000;

    /// <summary>Matched to the other multiplexer grids, so the rows are comparable.</summary>
    private const int Seeds = 6;

    /// <summary>The local estimate turned off, as near as a rate can be turned off.</summary>
    /// <remarks>
    /// <b>NOT ZERO, BECAUSE ZERO IS A DIFFERENT MECHANISM AND NOT A SLOWER ONE.</b> At
    /// exactly nought the estimate never moves off whatever it was initialised to, which is
    /// an arm about initialisation. Near zero it is the lifetime average the G-Counters
    /// already carry, which is the arm fork 27 actually names.
    /// </remarks>
    private const double Lifetime = 0.001;

    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_moving_target_costs_and_whether_the_local_estimate_pays_for_it()
    {
        // READ AS A CURVE PAST THE FLIP RATHER THAN AS A CROSSING, because there is no
        // machinery for a second crossing and inventing one would be a mechanism built to
        // serve a measurement. `Tally.Reached` reports the FIRST time the trailing window
        // held the target and nothing re-arms it, so a run that flips at twenty thousand
        // reports the same crossing it would have reported without flipping at all.
        //
        // SO THE READING IS THE TRAILING ACCURACY AT A KNOWN DISTANCE PAST THE FLIP, taken
        // over separate runs of the same seed. Same world, same brain, same draw order up to
        // the flip -- only how long the run continues afterwards differs, which is what makes
        // the row a recovery curve rather than four unrelated numbers.
        output.WriteLine($"{Seeds} seeds, target moves once at {Settled} rounds");
        output.WriteLine("world       | recency | rounds past the flip: 250 | 1000 | 5000");

        foreach (var (world, flip) in new (string World, int Flip)[]
        {
            // THE CONTROL FIRST. A stationary world's row is the same three runs at three
            // lengths, so anything that moves along it is the run getting longer and not the
            // world moving -- and if the two arms differ HERE, they differ for a reason this
            // grid is not about.
            ("stationary", 0),
            ("switching", Settled),
        })
        {
            foreach (var (arm, recency) in new (string Arm, double Recency)[]
            {
                ("0.1", new CommittingSettings().Recency),
                ("~0", Lifetime),
            })
            {
                var read = new List<double>();

                foreach (var past in new[] { 250, 1_000, 5_000 })
                {
                    var recent = new List<double>();

                    for (var seed = 1; seed <= Seeds; seed++)
                        recent.Add(new MultiplexerRun(
                            new MultiplexerSettings { Address = 2, Switch = flip },
                            new Brain(new CommittingSettings { Recency = recency }, seed),
                            seed).Run(Settled + past).Recent);

                    read.Add(recent.Average());
                }

                output.WriteLine(
                    $"{world,-11} | {arm,7} | "
                    + string.Join(" | ", read.Select(one => $"{one,24:F3}")));
            }
        }

        // NO BAR. Whether the local estimate earns its keep is what this reports, and a
        // threshold written before the first reading would be the answer rather than the
        // finding. What a bar would also do is hide the case the plan predicts: no difference
        // on either world, which would mean the second estimate is unearned everywhere and
        // the G-Counters are enough.
    }

    [Fact]
    public void The_flip_is_reached_by_the_learner_and_not_only_by_the_world()
    {
        // A WORLD CAN MOVE AND THE LEARNER NEVER NOTICE, WHICH READS EXACTLY LIKE A LEARNER
        // THAT RECOVERED INSTANTLY. `MultiplexerTests` asserts the target moves and the key
        // moves with it; that is a fact about the world and says nothing about whether any
        // run was disturbed. So the grid above is unreadable until something shows the flip
        // costs accuracy at all.
        //
        // AND IT IS ASSERTED AS A DIFFERENCE RATHER THAN AS A DEPTH. How far a run falls is
        // the grid's question; that it falls is this one's, and a bar on the size of the fall
        // would be a number chosen to sit just under what was measured.
        var settings = new MultiplexerSettings { Address = 2 };

        var still = new MultiplexerRun(
            settings, new Brain(new CommittingSettings(), 1), seed: 1).Run(Settled + 250);

        var moved = new MultiplexerRun(
            settings with { Switch = Settled },
            new Brain(new CommittingSettings(), 1),
            seed: 1).Run(Settled + 250);

        output.WriteLine(
            $"250 rounds past the flip: still {still.Recent:F3}, moved {moved.Recent:F3}");

        Assert.True(moved.Recent < still.Recent,
            $"the flipped run scored {moved.Recent:F3} against {still.Recent:F3} standing "
            + "still, so moving the target cost it nothing and the recovery grid beside "
            + "this is measuring two identical worlds");

        // AND THE TWO ARE ONE WORLD UNTIL THE FLIP, so the difference above is the flip and
        // not two runs that diverged from the first round. `Switch` leaves the first mapping
        // as the identity for exactly this reason.
        var before = new MultiplexerRun(
            settings with { Switch = Settled },
            new Brain(new CommittingSettings(), 1),
            seed: 1).Run(Settled - 1_000);

        var alone = new MultiplexerRun(
            settings, new Brain(new CommittingSettings(), 1), seed: 1).Run(Settled - 1_000);

        Assert.Equal(alone.Recent, before.Recent, precision: 10);
        Assert.Equal(alone.Sound, before.Sound);
    }
}
