using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// What a run's IDENTITY is, which is fork 12 from the side nothing else covers.
/// </summary>
/// <remarks>
/// <para>
/// <b>The run half of this file went with the walk, and the property did not.</b> Fork 12
/// asked whether a fixed seed reproduces a run exactly, and it is answered on this side by
/// <c>GradedTests.A_fixed_seed_reproduces_a_graded_run_exactly</c> — which asks MORE than
/// the deleted version did, running its two copies side by side so a learner that agreed
/// with itself through anything ambient could not pass.
/// </para>
/// <para>
/// <b>What is left here is the equality itself, and it is a different question.</b> Every
/// one of those reproducibility tests compares two reports with <c>Assert.Equal</c>, so
/// what a report counts as part of itself decides what they are asserting. A wall clock in
/// there turns every one of them red on a correct machine; a field MISSING from there makes
/// every one of them pass for free. Both faults are invisible from inside the tests that
/// depend on them, which is why they are asserted here instead.
/// </para>
/// </remarks>
public sealed class DeterminismTests
{

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
    /// <b>A wall clock is not part of a run's identity, and for two days it was.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE THREE <i>a fixed seed reproduces a run exactly</i> tests went red on a
    /// correct machine</b> the moment <see cref="Spent"/> joined <see cref="Tally"/>,
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
    /// <b>The companion, and it is the half that was actually dangerous.</b>
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
