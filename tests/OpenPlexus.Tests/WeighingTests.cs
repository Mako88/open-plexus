using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The three vote rules at ONE fixed power — <b>the trigger `DialTests` wrote down for
/// deleting two entries of its own census.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>AND THE THIRD ARM IS SCORED ON THE SAME GRID RATHER THAN ON ITS OWN.</b>
/// <see cref="Weighing.Lifting"/> asks for the answer its evidence most moved from that
/// answer's base rate, which is scale-free exactly as <see cref="Weighing.Strongest"/>
/// is — so it composes with replication and wire deduplication, and the question it has
/// to answer is only whether it SCORES. Its cost is written down in its own remarks
/// before this ran: it reopens <c>Sharpness</c> as a live axis, which the census had
/// closed under the maximum.
/// </para>
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

    /// <summary>Four, because a scene world costs an order more than a bit world.</summary>
    private const int SceneSeeds = 4;

    /// <summary>The last tenth's accuracy on the multiplexer.</summary>
    /// <param name="address">Address bits.</param>
    /// <param name="weighing">Which vote rule.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <param name="noise">How often the world lies.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    private static double Plexed(
        int address, Weighing weighing, int seed, double noise = 0.0, double skew = 0.0) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Noise = noise, Skew = skew },
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

    /// <summary>The last tenth's accuracy on the world of arrangements.</summary>
    /// <param name="weighing">Which vote rule.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <b>THE ONE WORLD WHERE THE POWER IS KNOWN TO MATTER, AND THEREFORE THE ONE THIS
    /// GRID CANNOT LEAVE OUT.</b> <c>Arranged</c> reaches its target at a power of ten and
    /// sits a fifth short at five — which is the per-world peak the design refuses, and
    /// also the case where deleting the sum would cost the most if it were kept.
    /// </remarks>
    private static double Arranged_(Weighing weighing, int seed) =>
        new ArrangedRun(
            new ArrangedSettings { Side = 3, Cell = 3, Clutter = 1, Hold = 4 },
            new Brain(
                new CommittingSettings { Sharpness = Fixed, Weighing = weighing }, seed),
            Looking.Whole,
            seed).Run(20_000).Tally.Recent;

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
        var worlds = new (string World, int Seeds, Func<Weighing, int, double> Run)[]
        {
            ("multiplexer-6", Seeds, (weighing, seed) => Plexed(2, weighing, seed)),
            ("multiplexer-11", Seeds, (weighing, seed) => Plexed(3, weighing, seed)),
            ("multiplexer-6-noisy", Seeds, (weighing, seed) => Plexed(2, weighing, seed, noise: 0.15)),

            // THE ONLY TWO ROWS ON THIS GRID WHERE THE DIVISOR HAS ANYTHING TO DO. Every
            // other world draws its two outcomes to within a percent of even, so a rule
            // that divides by a base rate divides by a constant and cannot move an argmax
            // -- eight rows of it read as a measured verdict and were a fact about the
            // bench. Four to one here, and the rules are the SAME rules: only how often
            // the answer is one differs from the row three above.
            ("multiplexer-6-skewed", Seeds, (weighing, seed) => Plexed(2, weighing, seed, skew: 0.8)),
            ("multiplexer-11-skewed", Seeds, (weighing, seed) => Plexed(3, weighing, seed, skew: 0.8)),
            ("monk-1", Seeds, (weighing, seed) => Monked(Puzzle.One, weighing, seed)),
            ("monk-2", Seeds, (weighing, seed) => Monked(Puzzle.Two, weighing, seed)),
            ("monk-3", Seeds, (weighing, seed) => Monked(Puzzle.Three, weighing, seed)),
            ("graded", Seeds, (weighing, seed) => Graded_(weighing, seed)),
            ("arranged", SceneSeeds, (weighing, seed) => Arranged_(weighing, seed)),
        };

        output.WriteLine($"the last tenth's accuracy, Sharpness fixed at {Fixed} "
            + "— summing, strongest, lifting");

        // THE SKEWED ROWS HAVE A FLOOR OF 0.80 AND THE OTHERS HAVE 0.50, so a number on
        // one is not a number on the other. Always answering the commoner outcome scores
        // four in five there while holding no rule at all -- the arms are still
        // comparable to each other, which is what this grid asks, but the column must
        // never be read down. `LiftingTests` carries the found-of-truths reading, which
        // is the one a majority guess cannot reach.
        output.WriteLine("the skewed rows are 4:1, so guessing the commoner answer scores "
            + "0.80 there against 0.50 everywhere else");

        var led = new Dictionary<string, int>
        {
            ["summing"] = 0,
            ["strongest"] = 0,
            ["lifting"] = 0,
        };

        foreach (var (world, seeds, run) in worlds)
        {
            var arms = await Sweep.AcrossAsync(
                seeds,
                ("summing", seed => Task.FromResult(run(Weighing.Summing, seed))),
                ("strongest", seed => Task.FromResult(run(Weighing.Strongest, seed))),
                ("lifting", seed => Task.FromResult(run(Weighing.Lifting, seed))));

            // TWO STANDARD ERRORS CLEAR OF EVERY OTHER ARM, AND THE MIDDLE IS COUNTED AS
            // NEITHER. A world where the arms are level is not a world any of them wins --
            // counting it for whichever is nominally ahead is how a grid of coin flips
            // becomes a verdict. With three arms the bar is against the RUNNER-UP rather
            // than against one nominated control, or the third arm would be scored on a
            // comparison the other two were not.
            var ranked = arms.OrderByDescending(one => one.Mean).ToList();
            var apart = ranked[0].Separation(ranked[1]);

            if (apart >= 2.0) led[ranked[0].Arm]++;

            output.WriteLine(
                $"{world,-20} | "
                + string.Join(" | ", arms.Select(one =>
                    $"{one.Arm} {one.Mean:F4} +/-{one.StdErr:F4}"))
                + $" | n={arms[0].Seeds} | {apart,5:F1} sigma over the second, "
                + $"{(apart < 2.0 ? "level" : ranked[0].Arm)}");
        }

        output.WriteLine(
            string.Join(", ", led.Select(one => $"{one.Key} leads on {one.Value}"))
            + $" of {worlds.Length} worlds — the rule is that a sum needs more than "
            + "one to keep `Summing` and `Sharpness` in the census, and `Lifting` "
            + "needs to beat `Strongest` somewhere to be worth the axis it reopens");
    }
}
