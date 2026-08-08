using OpenPlexus.Commitments;
using OpenPlexus.Graph;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// A fixed seed reproduces a run exactly — <b>fork 12, closed 2026-08-03 as a
/// side effect of fork 22, and confirmed against its own control.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE OLD CHARACTERISATION UNDERSTATED IT.</b> Fork 12 was recorded as
/// "every reported quantity is stable except <c>Halted</c>, which varies by a few
/// percent". That was measured at a horizon where the backstop never fires, so
/// <c>Halted</c> was the only place the damage showed. At a horizon where it does
/// fire, the pre-fix code destabilised <b>whole runs</b> at a fixed seed —
/// trajectory, choices, graph size and final energy all moved.
/// </para>
/// <para>
/// <b>Measured, 12 repeats × 3 seeds × 3 horizons.</b> With the pre-fix line
/// restored — untracking a thought the instant its live count hit zero — seed 1
/// at horizon 3 produced steps in {14, 37, 96, 103, 105, 106, 107} and halts
/// spanning 755 to 5,726. With it removed, every quantity is identical across
/// every repeat. <b>The control was run rather than assumed</b>, so this is the
/// fix causing the change rather than merely preceding it.
/// </para>
/// <para>
/// <b>Why it follows from fork 22.</b> A route killed by the horizon is counted
/// in the report that carries it; a thought untracked during a transient zero
/// dropped that report along with its halt count, and how many were dropped
/// depended on delivery order. Folding late reports instead of dropping them
/// removes the only source of run-to-run variation there was.
/// </para>
/// </remarks>
public sealed class DeterminismTests
{
    private static SnakeSettings World => Fixture.Snake(sight: 2, energy: 200, perFood: 50);

    /// <summary>
    /// <b>Horizon 2, so the backstop actually fires.</b> At the default of 50 it
    /// never does under inverse cost, <c>Halted</c> is zero everywhere, and this
    /// test would pass without asking anything.
    /// </summary>
    /// <remarks>
    /// <b>IT WAS 3 AND 3 STOPPED FIRING, WHICH IS A FACT ABOUT THE WALK AND NOT
    /// ABOUT THIS TEST.</b> A horizon only bites where routes try to go past it,
    /// and on this world they no longer do — measured at stamina 8, and the depth
    /// does not move when the budget does:
    /// <code>
    ///   horizon   halted   deepest   chains
    ///         1      302         0   (none complete)
    ///         2      138         2   2:88
    ///         3        0         2   2:88
    ///         6        0         2   2:88
    /// </code>
    /// <b>Every chain is length two — one hop — at every budget from 8 to 64.</b>
    /// So 3 sits above anything the walk reaches and can never fire; 1 kills every
    /// route before a chain completes and leaves nothing to compare. 2 is the only
    /// setting where the backstop fires AND chains still finish, which is what this
    /// file needs. The bar below is unchanged and reads 138 against it.
    /// </remarks>
    private static WalkSettings Dials =>
        Fixture.Dials(stamina: 8.0, foresight: 2.0, horizon: 2);

    private static async Task<RunResult> PlayAsync(int seed)
    {
        using var run = new SnakeRun(World, Dials, seed);
        return (await run.ReportAsync(120)).Result;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task The_same_seed_produces_the_same_run(int seed)
    {
        var first = await PlayAsync(seed);

        for (var repeat = 0; repeat < 4; repeat++)
        {
            var again = await PlayAsync(seed);

            Assert.Equal(first.Steps, again.Steps);
            Assert.Equal(first.ChosenByChain, again.ChosenByChain);
            Assert.Equal(first.Nodes, again.Nodes);
            Assert.Equal(first.FinalEnergy, again.FinalEnergy);
            Assert.Equal(first.Ate, again.Ate);

            // THE ONE THAT USED TO MOVE, and the reason the horizon is set low
            // enough for it to be a real number rather than zero.
            Assert.Equal(first.Halted, again.Halted);
        }
    }

    [Fact]
    public async Task And_the_backstop_really_is_firing()
    {
        // THE COMPANION. Every assertion above passes trivially for a
        // configuration where nothing is ever halted, which is exactly the
        // configuration fork 12 was originally measured at.
        var run = await PlayAsync(seed: 1);

        Assert.True(run.Halted > 100, $"only {run.Halted} routes hit the horizon");
    }

    /// <summary>
    /// A short multiplexer run, for the tests below that want a <see cref="Tally"/> and
    /// do not care what is in it.
    /// </summary>
    private static Tally Counted() =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = 2 },
            new Brain(new CommittingSettings(), seed: 1),
            seed: 1).Run(rounds: 200).Tally;

    /// <summary>
    /// <b>A WALL CLOCK IS NOT PART OF A RUN'S IDENTITY, AND FOR TWO DAYS IT WAS.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE THREE <i>a fixed seed reproduces a run exactly</i> TESTS WENT RED ON A
    /// CORRECT MACHINE</b> the moment <see cref="Spent"/> joined <see cref="Tally"/>,
    /// because a record compares every field it has and milliseconds do not repeat.
    /// Every other number in those reports was identical to the digit. See
    /// <see cref="Spent.Equals(Spent)"/> for why the fix is there rather than here.
    /// </remarks>
    [Fact]
    public void Two_runs_differing_only_in_how_long_they_took_are_the_same_run()
    {
        var tally = Counted();

        Assert.Equal(
            tally,
            tally with { Spent = tally.Spent with { Firing = tally.Spent.Firing + 1000.0 } });
    }

    /// <summary>
    /// <b>THE COMPANION, AND IT IS THE HALF THAT WAS ACTUALLY DANGEROUS.</b>
    /// </summary>
    /// <remarks>
    /// A clock inside the report did not merely turn three tests red. It made every
    /// <c>Assert.NotEqual</c> over a <see cref="Tally"/> pass for free — the clocks
    /// always differ, so the controls sitting beside those three tests could not fail
    /// whatever the learner did. <b>Excluding the clock is what ARMS them</b>, and a
    /// rule saying two reports are always equal would disarm them again just as
    /// thoroughly, so it is asserted rather than assumed.
    /// </remarks>
    [Fact]
    public void And_a_report_that_differs_anywhere_else_is_still_a_different_run()
    {
        var tally = Counted();

        Assert.NotEqual(tally, tally with { Rounds = tally.Rounds + 1 });
        Assert.NotEqual(tally, tally with { Right = tally.Right + 1 });
        Assert.NotEqual(tally, tally with { Separations = tally.Separations + 1 });
    }
}
