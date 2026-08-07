using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whole against tiled, on the world where the difference can show.
/// </summary>
/// <remarks>
/// <b>THE MEASUREMENT THE PLAN HAS BEEN OWED SINCE STEP FOUR.</b> A pooled embedding
/// has no parts and cannot carry an arrangement, and a whole-picture projection is the
/// same thing by another road. Patch tokens are named as the fix; here is the first
/// world on which the claim can be wrong.
/// </remarks>
public sealed class ArrangingTests(ITestOutputHelper output)
{
    private static readonly ArrangedSettings Small =
        new() { Side = 3, Cell = 3, Clutter = 1, Hold = 4 };

    /// <summary>Five seeds of one configuration, and what the last one left behind.</summary>
    /// <param name="dials">The brain, built once and handed to every seed.</param>
    /// <param name="looking">How the picture is cut up.</param>
    /// <remarks>
    /// <b>ONE BRAIN PER CONFIGURATION AND FIVE SEEDS OF IT, because one seed is not a
    /// comparison and this repo has watched an ordering invert.</b> Written out twice
    /// before `DuplicationTests` refused the second, which is that budget doing its job
    /// on a measurement file rather than on the library.
    /// </remarks>
    private static (List<double> Unseen, Grounded Last) Sweep(
        CommittingSettings dials, Looking looking)
    {
        var unseen = new List<double>();
        var last = default(Grounded);

        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            last = new ArrangedRun(Small, new Brain(dials, seed), looking, seed).Run(20_000);
            unseen.Add(last.Tally.Unseen!.Accuracy);
        }

        return (unseen, last!);
    }

    /// <summary>The standard error of a handful of readings.</summary>
    private static double Spread(List<double> readings)
    {
        var mean = readings.Average();

        return Math.Sqrt(
            readings.Sum(one => (one - mean) * (one - mean)) / (readings.Count - 1))
            / Math.Sqrt(readings.Count);
    }

    [Fact]
    public void The_gap_between_what_it_was_shown_and_what_it_was_not()
    {
        foreach (var rounds in new[] { 10_000, 40_000 })
        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var run = new ArrangedRun(
                Small, new Brain(new CommittingSettings(), seed), looking, seed);

            var got = run.Run(rounds);
            var bar = run.Measure();

            output.WriteLine(
                $"rounds {rounds} {looking,-6} seed {seed} | drawn {got.Tally.Recent:F3} "
                + $"unseen {got.Tally.Unseen!.Accuracy:F3} "
                + $"silence {got.Tally.Unseen.Silence:F3} | "
                + $"codes {got.Tally.Codes:F0} resident {got.Tally.Resident} "
                + $"minted {got.Tally.Minted} repaired {got.Tally.Repaired} "
                + $"named {got.Tally.Named} | "
                + $"sound {got.Rules.Sound} unsound {got.Rules.Unsound} inert {got.Rules.Inert} | "
                + $"tags {got.Tags}/{got.Readings} | "
                + $"probe pixels {bar.OnPixels.Accuracy:F3} codes {bar.OnCodes.Accuracy:F3} "
                + $"over {bar.Features} features, {bar.OnPixels.Trained} fitted "
                + $"{bar.OnPixels.Tested} scored");
        }

        // NO BAR, DELIBERATELY. Nobody knows what either arm scores here yet, and a
        // threshold written before the first run is a prediction dressed as a check --
        // which is how a measurement quietly becomes a thing that must not change.
        Assert.True(true);
    }

    [Fact]
    public void What_the_vote_costs_when_the_population_already_knows_which_rules_are_true()
    {
        // THE ONE ARM WITH A CLEAN TARGET, SWEPT ON THE ONE DIAL THE PLAN NAMED. Tiled
        // under `AnyFailure` holds every sound single-code rule on every seed, the
        // front end loses nothing a linear probe can find, and the language covers the
        // whole world at depth one. Everything is in place and it scores 0.800.
        //
        // AND THE POPULATION IS NOT CONFUSED ABOUT WHICH RULES ARE TRUE -- it believes
        // the sound ones 1.000 and the unsound ones 0.522. So a crowd of rules that
        // LOOK worse is outvoting a handful that look perfect, which is what raising
        // accuracy to a power exists to stop, and five is evidently not enough of it:
        // 0.522^5 is 0.039, and a few dozen of those agreeing beat one weight of 1.
        //
        // SO THIS IS A PREDICTION WITH A NUMBER ON IT. If the vote is the gap, the
        // score climbs toward 1.000 as the power rises. If it does not, the gap is
        // somewhere nobody has looked yet and this rules out the obvious place.
        var could = new ArrangedRun(
            Small, new Brain(new CommittingSettings(), seed: 1), Looking.Tiled, seed: 1)
            .Reachable(depth: 1);

        output.WriteLine(
            $"target {could.CoversUnseen:F3} on the unseen, from {could.Alone.Length} "
            + $"codes sound alone, {could.Least} of them enough");

        // AND THE SCALE-FREE ARM AT THE DEFAULT POWER, WHICH IS THE WHOLE QUESTION. If
        // `Strongest` reaches the target at five, the peak stops moving with the world
        // and there is nothing left to tune. If it needs a power too, the count was
        // never the fault and this rules that out.
        foreach (var weighing in new[] { Weighing.Summing, Weighing.Strongest })
        foreach (var sharpness in weighing == Weighing.Summing
            ? new[] { 1.0, 5.0, 10.0, 20.0, 50.0 }
            : [5.0])
        {
            var (unseen, last) = Sweep(
                new CommittingSettings
                {
                    Surprising = Surprising.AnyFailure,
                    Sharpness = sharpness,
                    Weighing = weighing,
                },
                Looking.Tiled);

            output.WriteLine(
                $"  {weighing,-9} sharpness {sharpness,4} | unseen {unseen.Average():F3} +/- "
                + $"{Spread(unseen):F3} | "
                + $"[{string.Join(" ", unseen.Select(one => one.ToString("F3")))}] | "
                + $"last run: {last.Rules.Sound} sound {last.Rules.Unsound} unsound, "
                + $"believed {last.Rules.Trusted:F3} vs {last.Rules.Doubted:F3}, "
                + $"lead {last.Tally.Confidence:F3}");
        }

        Assert.True(true);
    }

    [Fact]
    public void And_the_other_half_of_the_decoupling_where_the_target_is_known()
    {
        // THE SIDE OF THE PREDICTION THAT MUST NOT MOVE. Repair on this world is not
        // the constraint -- the rules that solve it are one code each and genesis mints
        // them directly -- so letting repair run on more rounds should change nothing
        // and the score should stay at the target. If it FALLS, the extra gate was
        // holding back damage rather than search, and the whole argument inverts.
        foreach (var mending in new[] { Mending.Outvoted, Mending.Uncovered, Mending.Improving })
        foreach (var subsuming in new[] { Subsuming.Weaker, Subsuming.Insignificant })
        {
            var (unseen, last) = Sweep(
                new CommittingSettings
                {
                    Surprising = Surprising.AnyFailure,
                    Weighing = Weighing.Strongest,
                    Mending = mending,
                    Subsuming = subsuming,
                },
                Looking.Tiled);

            output.WriteLine(
                $"{mending,-9} {subsuming,-13} | unseen {unseen.Average():F3} "
                + $"[{string.Join(" ", unseen.Select(one => one.ToString("F3")))}] | "
                + $"{last.Rules.Sound} sound {last.Rules.Unsound} unsound, "
                + $"repaired {last.Tally.Repaired}, lead {last.Tally.Confidence:F3}");
        }

        Assert.True(true);
    }

    [Fact]
    public void Whether_the_gate_that_was_load_bearing_on_photographs_is_one_here()
    {
        // ONE SEED IS NOT A COMPARISON AND WILL HAPPILY INVERT, which this repo has
        // already paid for once -- winnowing beat bands on seed one and lost over five.
        // The single-seed reading says `Unaccounted` starves genesis on this world,
        // which is the OPPOSITE of what five seeds said on CIFAR, so it gets error bars
        // before it gets written down as anything.
        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        {
            // HOISTED, BECAUSE THE CEILING IS A FACT ABOUT THE WORLD AND THE FRONT END
            // AND NEITHER MOVES WITH THE SEED. Recomputing it per run would spend most
            // of the grid's time confirming the same number twenty times.
            var could = new ArrangedRun(
                Small, new Brain(new CommittingSettings(), seed: 1), looking, seed: 1)
                .Reachable(depth: 1);

            output.WriteLine(
                $"{looking}: ceiling {could.CoversUnseen:F3} on the unseen, from "
                + $"{could.Alone.Length} codes sound alone, {could.Least} of them enough");

            foreach (var gate in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
            {
                var unseen = new List<double>();

                foreach (var seed in new[] { 1, 2, 3, 4, 5 })
                {
                    var run = new ArrangedRun(
                        Small,
                        new Brain(new CommittingSettings { Surprising = gate }, seed),
                        looking,
                        seed);

                    var got = run.Run(20_000);

                    var alone = Fixture.Alone(run.Held);

                    unseen.Add(got.Tally.Unseen!.Accuracy);

                    output.WriteLine(
                        $"  {gate,-11} seed {seed} | unseen {got.Tally.Unseen.Accuracy:F3} "
                        + $"drawn {got.Tally.Recent:F3} | "
                        + $"{could.Alone.Count(alone.Contains)}/{could.Alone.Length} sound "
                        + $"singles held, {got.Tally.Resident} resident "
                        + $"({got.Tally.Minted} minted) | "
                        + $"sound {got.Rules.Sound} unsound {got.Rules.Unsound}");
                }

                var mean = unseen.Average();
                var spread = Math.Sqrt(
                    unseen.Sum(one => (one - mean) * (one - mean)) / (unseen.Count - 1));

                output.WriteLine(
                    $"  {gate,-11} MEAN {mean:F3} +/- {spread / Math.Sqrt(unseen.Count):F3}");
            }
        }

        Assert.True(true);
    }
}
