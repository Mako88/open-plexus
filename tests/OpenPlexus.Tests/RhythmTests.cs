using OpenPlexus.Worlds;
using Xunit.Abstractions;

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
public sealed class RhythmTests(ITestOutputHelper output)
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

    [Fact]
    public async Task Without_the_window_this_world_has_no_edges_at_all()
    {
        // THE SHARPEST CONTROL IN THE PROJECT, and it falls out of the world
        // rather than being arranged. One symbol per moment means nothing is ever
        // simultaneous with anything, so an occasion's onsets pair with an empty
        // live set and there is nothing to learn -- unless a departed symbol is
        // carried forward into the next moment.
        using var run = new RhythmRun(World(), Fixture.Dials(stamina: 4.0), seed: 1, span: 0);
        var result = await run.RunAsync(300);

        output.WriteLine(result.ToString());

        Assert.Equal(0, result.Edges);
        Assert.Equal(0, result.Right);
    }

    [Fact]
    public async Task The_window_records_the_immediate_predecessor()
    {
        // THIS WORLD FOUND THE OFFSET BUG AND THIS TEST IS WHAT HOLDS THE FIX.
        //
        // A code that stops in the same moment another starts is not in `Live` --
        // which is what was already there AND STILL IS -- so the window is the
        // only thing that can join it to its successor. The window used to be READ
        // before what just stopped was carried into it, so the immediate
        // predecessor was the one relation it could never record: the graph learnt
        // the step before that instead, and predicted the next symbol at chance
        // while predicting the one AFTER it far above chance.
        //
        // Carrying before reading fixes the phase. The two offsets are both still
        // reported, because an accuracy alone cannot tell "learnt nothing" from
        // "learnt it one step out", and a regression would look like the former.
        using var run = new RhythmRun(World(), Fixture.Dials(stamina: 4.0), seed: 1, span: 1);
        var result = await run.RunAsync(600);

        output.WriteLine(result.ToString());

        Assert.True(result.Expected > result.Chance * 5,
            $"the next symbol was not learnt: {result.Expected} against "
            + $"chance {result.Chance}");

        // AND THE PHASE IS RIGHT WAY ROUND NOW, which is the half that would have
        // caught the original bug. Predicting two ahead is what a walk one step
        // out of phase does, and it should now be the thing at chance.
        Assert.True(result.Expected > result.TwoAhead * 5,
            $"the offset is skewed again: next={result.Expected} after={result.TwoAhead}");

        // AND A VIOLATION IS STILL NEVER FORESEEN, which is the world's own
        // integrity check: it is a draw from everything the cycle did not call for.
        Assert.True(result.Surprised < result.Chance * 3,
            $"violations were foreseen at {result.Surprised}");

        Assert.Empty(result.Complaints);
    }

    [Fact]
    public async Task The_world_is_stationary_and_the_score_is_still_climbing()
    {
        // THE TRAP THIS WORLD EXISTS TO MAKE ANSWERABLE, AND IT ANSWERS IT THE
        // AWKWARD WAY. "A dial swept at one data volume may be measuring the
        // volume" -- and here the world's STATISTICS are stationary by
        // construction while the SCORE is still rising at every run length tried.
        //
        // So this world does not remove the trap; it makes it visible and cheap to
        // respect. Snake could not, because it dies at seventy-seven steps and a
        // longer run is not available at any price. Here a longer run costs
        // nothing, so the rule stands: sweep at two run lengths, and treat any
        // dial measured at one as unread.
        var dials = Fixture.Dials(stamina: 4.0);

        using var brief = new RhythmRun(World(), dials, seed: 1, span: 1);
        using var lengthy = new RhythmRun(World(), dials, seed: 1, span: 1);

        var short_ = await brief.RunAsync(200);
        var extended = await lengthy.RunAsync(900);

        output.WriteLine($"short {short_}");
        output.WriteLine($"long  {extended}");

        // THE WORLD ITSELF DOES NOT MOVE: same cycle, same ceiling, same marginal.
        Assert.Equal(short_.Ceiling, extended.Ceiling, 6);
        Assert.Equal(short_.Marginal, extended.Marginal, 6);

        // THE SCORE DOES, and by more than the margin anybody would call a dial
        // effect. A sweep taken at one length here would be reading the length.
        Assert.True(extended.Expected - short_.Expected > 0.05,
            $"the score no longer climbs with data, so this world has saturated "
            + $"and the two-length rule could be relaxed: "
            + $"{short_.Expected} to {extended.Expected}");
    }
}
