using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The world built so that prediction has somewhere to be measured.
/// </summary>
/// <remarks>
/// <b>Snake dies at seventy-seven steps</b>, and it is the only predictive world
/// here — which is why fork 24's budget hunt was measured never moving and nobody
/// could say whether that was the controller or the sample. This stream is
/// stationary and never ends, so a dial can be swept against it at two run
/// lengths, which the traps section asks for and nothing here could offer.
/// </remarks>
public sealed class RhythmTests
{
    private static RhythmSettings World(
        int symbols = 12, int period = 5, double violations = 0.1) => new()
    {
        Symbols = symbols, Period = period, Violations = violations,
    };

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void The_cycle_repeats_and_the_statistics_never_move()
    {
        // STATIONARY IS THE WHOLE POINT. A dial swept over a long run against a
        // world that drifts is measuring the drift.
        var world = new Rhythm(World(violations: 0.0), seed: 1);

        var first = Enumerable.Range(0, 20).Select(_ => world.Next().Shown).ToList();

        Assert.Equal(first.Take(5), first.Skip(5).Take(5));
        Assert.Equal(first.Take(5), first.Skip(15).Take(5));
    }

    [Fact]
    public void A_violation_is_never_the_symbol_the_cycle_called_for()
    {
        // A "VIOLATION" THAT DREW THE EXPECTED SYMBOL IS NOT ONE, and counting it
        // as one would put predictable moments into the unpredictable column and
        // quietly raise the ceiling.
        var world = new Rhythm(World(violations: 1.0), seed: 3);

        for (var moment = 0; moment < 200; moment++)
        {
            var wanted = world.Wanted(moment);
            var (shown, violated) = world.Next();

            Assert.True(violated);
            Assert.NotEqual(Rhythm.Of(wanted), shown);
        }
    }

    [Fact]
    public void The_ceiling_is_what_a_perfect_model_would_score()
    {
        var world = new Rhythm(World(violations: 0.2), seed: 1);

        Assert.Equal(0.8, world.Ceiling, 6);

        // AND THE MARGINAL BASELINE IS BELOW IT, which is what makes it a control:
        // a system that learnt only which symbols are frequent, and nothing about
        // order, lands there.
        Assert.True(world.Marginal < world.Ceiling);
        Assert.True(world.Marginal > world.Chance);
    }

    // ---- what the graph does with it ---------------------------------------

}
