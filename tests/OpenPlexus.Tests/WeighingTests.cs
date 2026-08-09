using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The two vote rules at ONE fixed power — <b>the trigger `DialTests` wrote down for
/// deleting two entries of its own census.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE RULE IS DECIDED IN ADVANCE, WHICH IS WHAT MAKES THIS A TEST AND NOT A HUNT.</b>
/// <c>Sharpness</c> decides nothing under <see cref="Weighing.Strongest"/> — measured, and
/// algebraic: raising to a power is monotone, so the argmax never moves. It is a parameter
/// of <see cref="Weighing.Summing"/> and it has a per-world peak, which is a world reaching
/// into the brain. So a sum that only wins with the power tuned per world is disqualified
/// by the design's own rule WHATEVER it scores, and both entries go; a sum that wins at one
/// fixed power on more than one world keeps them both.
/// </para>
/// <para>
/// <b>ONE POWER, AND IT IS THE ONE THAT SHIPS.</b> Five is the value
/// <see cref="CommittingSettings.Sharpness"/> has carried since it was written and the peak
/// on the multiplexer; picking the best power per world is exactly the move being tested
/// for, so choosing it here would answer the question by asking a different one.
/// </para>
/// <para>
/// <b>AND THE WORLDS ARE THREE KINDS RATHER THAN THREE WIDTHS.</b> A finding on one world
/// is a finding about one world, and this repo has a trap on its list for a dial wired to
/// one world in ten and cashed in as general. The multiplexer at two widths is one world
/// twice; <see cref="Monk"/> is a published symbolic benchmark whose rules are counting
/// concepts rather than selections, and <see cref="Graded"/> is the one with a front end
/// between the world and the learner.
/// </para>
/// </remarks>
public sealed class WeighingTests(ITestOutputHelper output)
{
    /// <summary>
    /// The shipped power, held fixed across every world here.
    /// </summary>
    private const double Fixed = 5.0;

    private const int Seeds = 8;

    /// <summary>The last tenth's accuracy on the multiplexer.</summary>
    /// <param name="address">Address bits.</param>
    /// <param name="weighing">Which vote rule.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <param name="noise">How often the world lies.</param>
    private static double Plexed(int address, Weighing weighing, int seed, double noise = 0.0) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Noise = noise },
            new Brain(
                new CommittingSettings { Sharpness = Fixed, Weighing = weighing }, seed),
            seed).Run(20_000).Recent;

    /// <summary>The last tenth's accuracy on one of the Monk's problems.</summary>
    /// <param name="puzzle">Which of the three.</param>
    /// <param name="weighing">Which vote rule.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static double Monked(Puzzle puzzle, Weighing weighing, int seed) =>
        new MonkRun(
            new MonkSettings { Puzzle = puzzle, Withheld = 132 },
            new Brain(
                new CommittingSettings { Sharpness = Fixed, Weighing = weighing }, seed),
            seed).Run(20_000).Recent;

    /// <summary>The last tenth's accuracy on the world with a front end in the way.</summary>
    /// <param name="weighing">Which vote rule.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static double Graded_(Weighing weighing, int seed) =>
        new GradedRun(
            new GradedSettings { Address = 2 },
            new Brain(
                new CommittingSettings { Sharpness = Fixed, Weighing = weighing }, seed),
            Fronting.Banded,
            seed).Run(20_000).Recent;

    /// <summary>
    /// <b>WHETHER A SUM AT ONE FIXED POWER BEATS ITS BEST ADVOCATE — a grid, and the
    /// decision rule is above rather than below.</b>
    /// </summary>
    /// <remarks>
    /// <b>PEAK TO PEAK IS NOT WHAT THIS IS, AND THAT IS THE WHOLE DESIGN.</b> Comparing
    /// each arm at its own best power is exactly the comparison the design forbids
    /// cashing in — the peak of <c>Sharpness</c> moves between worlds, so a per-world peak
    /// is the world choosing how the brain thinks. What is compared here is two rules with
    /// every number the brain has held constant, which is the only comparison a fixed brain
    /// is entitled to make.
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_a_sum_at_one_fixed_power_beats_its_best_advocate()
    {
        var worlds = new (string World, Func<Weighing, int, double> Run)[]
        {
            ("multiplexer-6", (weighing, seed) => Plexed(2, weighing, seed)),
            ("multiplexer-11", (weighing, seed) => Plexed(3, weighing, seed)),
            ("multiplexer-6-noisy", (weighing, seed) => Plexed(2, weighing, seed, noise: 0.15)),
            ("monk-1", (weighing, seed) => Monked(Puzzle.One, weighing, seed)),
            ("monk-2", (weighing, seed) => Monked(Puzzle.Two, weighing, seed)),
            ("monk-3", (weighing, seed) => Monked(Puzzle.Three, weighing, seed)),
            ("graded", (weighing, seed) => Graded_(weighing, seed)),
        };

        output.WriteLine($"the last tenth's accuracy, Sharpness fixed at {Fixed}, "
            + $"{Seeds} seeds — summing first, strongest second");

        var summing = 0;
        var strongest = 0;

        foreach (var (world, run) in worlds)
        {
            var arms = await Sweep.AcrossAsync(
                Seeds,
                ("summing", seed => Task.FromResult(run(Weighing.Summing, seed))),
                ("strongest", seed => Task.FromResult(run(Weighing.Strongest, seed))));

            var apart = arms[1].Separation(arms[0]);

            // TWO STANDARD ERRORS EITHER WAY, AND THE MIDDLE IS COUNTED AS NEITHER. A world
            // where the arms are level is not a world where the sum wins, and it is not a
            // world where it loses -- counting it for whichever is nominally ahead is how a
            // grid of coin flips becomes a verdict.
            if (apart >= 2.0 && arms[0].Mean > arms[1].Mean) summing++;
            if (apart >= 2.0 && arms[1].Mean > arms[0].Mean) strongest++;

            output.WriteLine(
                $"{world,-20} | summing {arms[0].Mean:F4} +/-{arms[0].StdErr:F4} "
                + $"| strongest {arms[1].Mean:F4} +/-{arms[1].StdErr:F4} "
                + $"| {apart,5:F1} sigma, "
                + $"{(apart < 2.0 ? "level" : arms[0].Mean > arms[1].Mean ? "summing" : "strongest")}");
        }

        output.WriteLine(
            $"summing leads on {summing} of {worlds.Length} worlds, strongest on "
            + $"{strongest} — the rule is that a sum needs more than one to keep "
            + "`Summing` and `Sharpness` in the census");
    }
}
