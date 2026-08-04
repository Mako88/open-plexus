using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Step 4's third factor — <b>does weighting what is learnt by whether things got
/// better make association beat random?</b>
/// </summary>
/// <remarks>
/// <b>THE BAR IS RANDOM, NOT IDLING.</b> Choosing by association already scores
/// BELOW drawing at random on this world, which is not a tuning failure: a state
/// and the act taken in it join one occasion, so walking from that state reaches
/// whatever was done there last time, helpful or ruinous. Beating idling would
/// only say the arithmetic still works.
/// </remarks>
public sealed class DrivesTests(ITestOutputHelper output)
{
    private static HomeostatSettings World() => new();

    private static WalkSettings Dials => Fixture.Dials(stamina: 4.0);

    private const int Steps = 400;

    // ---- the factor itself -------------------------------------------------

    [Fact]
    public void Nothing_is_credited_until_the_body_has_been_felt_twice()
    {
        // The first occasion of a run is written exactly as it would have been
        // without any of this, which is what keeps it additive.
        var drives = new Drives(reach: 0.04);

        Assert.Equal(1.0, drives.Credit);

        drives.Feel([0.9, 0.8, 0.7]);
        Assert.Equal(1.0, drives.Credit);
    }

    [Fact]
    public void Getting_better_is_worth_more_than_getting_worse_and_neither_is_worth_nothing()
    {
        var drives = new Drives(reach: 0.04);

        drives.Feel([0.9, 0.8, 0.5]);
        drives.Feel([0.9, 0.8, 0.6]);
        var better = drives.Credit;

        drives.Feel([0.9, 0.8, 0.4]);
        var worse = drives.Credit;

        Assert.True(better > 1.0, $"an improvement earned {better}");
        Assert.True(worse < 1.0, $"a decline earned {worse}");

        // NEITHER END REACHES ZERO, and that is the CRDT property rather than a
        // taste. A count that could stop increasing is not a G-Counter, and an
        // occasion worth nothing is refused outright by `Node.Observe`.
        Assert.True(worse > 0.0, $"a decline cancelled the occasion: {worse}");
    }

    [Fact]
    public void The_most_at_risk_variable_is_what_counts_and_not_the_mean()
    {
        // A BODY IS OUT OF BOUNDS WHEN ANY VARIABLE IS, so a mean can rise while
        // the one about to fail keeps falling — the same shape as a mean fan-out
        // over rows that grew without bound.
        var drives = new Drives(reach: 0.04);

        drives.Feel([0.5, 0.9, 0.9]);

        // The mean rises sharply; the worst falls.
        drives.Feel([0.4, 1.0, 1.0]);

        Assert.True(drives.Credit < 1.0,
            $"a mean that rose while the worst fell earned {drives.Credit}");
    }

    [Fact]
    public void The_audited_signal_is_exactly_the_share_of_the_three_raw_counts()
    {
        // `Improving` PASSED `SignalTests` AND ITS PARTS WERE READ BY NOTHING, which
        // is how a signal comes to mean something other than what its name claims.
        // The gate says the number separates a good policy from a bad one; it
        // cannot say the number is the share of transitions that improved things,
        // because any monotone function of that share would pass the gate
        // identically. This is the half the world-level audit structurally cannot
        // reach.
        var drives = new Drives(reach: 0.04);

        drives.Feel([0.9, 0.8, 0.5]);

        drives.Feel([0.9, 0.8, 0.6]);   // better
        drives.Feel([0.9, 0.8, 0.4]);   // worse
        drives.Feel([0.9, 0.8, 0.4]);   // same
        drives.Feel([0.9, 0.8, 0.7]);   // better

        output.WriteLine(
            $"better={drives.Better} worse={drives.Worse} same={drives.Same} "
            + $"improving={drives.Improving:F4}");

        // EVERY TRANSITION LANDS IN EXACTLY ONE OF THE THREE. Without this the
        // denominator could quietly drop transitions and the share would read high
        // for a body that mostly did nothing measurable.
        Assert.Equal(2, drives.Better);
        Assert.Equal(1, drives.Worse);
        Assert.Equal(1, drives.Same);

        Assert.Equal(
            drives.Better / (double)(drives.Better + drives.Worse + drives.Same),
            drives.Improving,
            precision: 10);
    }

    [Fact]
    public void And_a_body_that_has_felt_nothing_twice_reports_no_share_rather_than_a_wrong_one()
    {
        // AN EMPTY DENOMINATOR IS NOT A READING, which is the distinction the
        // `Overreach` audit turned on. Nought here means no transition has been
        // seen, and a controller told "nothing is improving" would drive on it.
        var drives = new Drives(reach: 0.04);

        Assert.Equal(0, drives.Better + drives.Worse + drives.Same);
        Assert.Equal(0.0, drives.Improving);
    }
}
