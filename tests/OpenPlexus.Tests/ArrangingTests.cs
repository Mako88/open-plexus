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
    [Fact]
    public void The_gap_between_what_it_was_shown_and_what_it_was_not()
    {
        var settings = new ArrangedSettings { Side = 3, Cell = 3, Clutter = 1, Hold = 4 };

        foreach (var rounds in new[] { 10_000, 40_000 })
        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var run = new ArrangedRun(
                settings, new Brain(new CommittingSettings(), seed), looking, seed);

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
    public void Whether_the_gate_that_was_load_bearing_on_photographs_is_one_here()
    {
        // ONE SEED IS NOT A COMPARISON AND WILL HAPPILY INVERT, which this repo has
        // already paid for once -- winnowing beat bands on seed one and lost over five.
        // The single-seed reading says `Unaccounted` starves genesis on this world,
        // which is the OPPOSITE of what five seeds said on CIFAR, so it gets error bars
        // before it gets written down as anything.
        var settings = new ArrangedSettings { Side = 3, Cell = 3, Clutter = 1, Hold = 4 };

        foreach (var looking in new[] { Looking.Whole, Looking.Tiled })
        {
            // HOISTED, BECAUSE THE CEILING IS A FACT ABOUT THE WORLD AND THE FRONT END
            // AND NEITHER MOVES WITH THE SEED. Recomputing it per run would spend most
            // of the grid's time confirming the same number twenty times.
            var could = new ArrangedRun(
                settings, new Brain(new CommittingSettings(), seed: 1), looking, seed: 1)
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
                        settings,
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
