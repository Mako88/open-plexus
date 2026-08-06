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
        using var run = new RhythmRun(World(), Fixture.Dials(stamina: 4.0) with { Span = 0 }, seed: 1);
        var result = await run.RunAsync(300);

        output.WriteLine(result.ToString());

        Assert.Equal(0, result.Edges);
        Assert.Equal(0, result.Right);

        // AND THE TEMPORAL COUNTER AGREES, which is what ARMS it. `Plumbing.Temporal`
        // reads nought on `Homeostat` and that is the whole diagnosis of an open
        // defect there -- so it has to be shown reading nought for the right reason
        // here and above nought on the arm below, or it is a counter wired to
        // nothing reporting what everybody hoped.
        Assert.Equal(0, result.Plumbing.Temporal);
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
        using var run = new RhythmRun(World(), Fixture.Dials(stamina: 4.0) with { Span = 1 }, seed: 1);
        var result = await run.RunAsync(600);

        output.WriteLine(result.ToString());

        Assert.True(result.Expected > result.Chance * 5,
            $"the next symbol was not learnt: {result.Expected} against "
            + $"chance {result.Chance}");

        // THE OTHER HALF OF ARMING THE TEMPORAL COUNTER -- see the span-nought test
        // above. A world that carries writes these cells in quantity, so a nought
        // anywhere else is a fact about that world and not about the counter.
        Assert.True(result.Plumbing.Temporal > 0,
            "a world whose every edge is a carried one wrote no temporal cells, "
            + "so the counter is measuring nothing");

        // AND NOTHING HERE IS AN INTERVENTION. No body acts in this world, so the
        // interventional cell must be empty -- the control for `Kind.Meddled`.
        Assert.Equal(0, result.Plumbing.Meddled);

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
    public async Task Only_the_surprise_propagates_and_the_traffic_collapses()
    {
        // STEP 2. Rao & Ballard: what travels is the residual, and a perfectly
        // predicted input is silent. The claim is that traffic falls a long way
        // while the score does not -- if the score fell with it, the system would
        // just be thinking less.
        //
        // THE COLLAPSE IS MEASURED WITHIN ONE RUN NOW, AND IT HAS TO BE. This test
        // used to build `loud` and `quiet` and compare them -- but the two were
        // constructed with IDENTICAL arguments and the same seed, because the
        // `Surprising` boolean that separated them was deleted when everything went
        // on. The off-arm went with it and the two runs became one run compared to
        // itself, which is why it read 15922 against 15922. A tie to the last digit
        // is never a measurement.
        //
        // AND THERE IS NO OFF-ARM TO PUT BACK, BY DESIGN -- you build it and it is
        // on. So the claim has to be stated without one, which it can be: Rao &
        // Ballard say a PREDICTED input is silent, so traffic per step must FALL as
        // the world becomes predicted. That is a claim about one run over time, and
        // it is the stronger statement of the two.
        var dials = Fixture.Dials(stamina: 4.0);

        const int Opening = 200;
        const int Whole = 900;

        using var early = new RhythmRun(World(), dials with { Span = 1 }, seed: 1);
        using var entire = new RhythmRun(World(), dials with { Span = 1 }, seed: 1);

        var opening = await early.RunAsync(Opening);
        var whole = await entire.RunAsync(Whole);

        output.WriteLine($"opening {opening}");
        output.WriteLine($"whole   {whole}");

        // THE SUBTRACTION IS SOUND BECAUSE THE SEED IS FIXED -- fork 12, and the
        // determinism suite holds it. The long run's first two hundred steps ARE the
        // short run, so the difference is what the remaining steps cost.
        var opened = opening.Messages / (double)Opening;
        var later = (whole.Messages - opening.Messages) / (double)(Whole - Opening);

        output.WriteLine($"traffic per step: {opened:F1} opening, {later:F1} later");

        var hushEarly = opening.Unspoken / (double)Opening;
        var hushLate = (whole.Unspoken - opening.Unspoken) / (double)(Whole - Opening);

        output.WriteLine($"silent share: {hushEarly:P1} opening, {hushLate:P1} later");

        // THE GATE WORKS AND THE ECONOMIC ARGUMENT DOES NOT, AND THOSE ARE TWO
        // CLAIMS THAT HAVE ALWAYS BEEN READ AS ONE.
        //
        // The gate half holds: the share of moments that stay silent RISES as the
        // world becomes predicted, 40.0% over the opening against 43.3% after it.
        // That is Rao & Ballard's claim proper -- a predicted input is silent -- and
        // it is measured here for the first time, because the arm this test used to
        // compare against was itself.
        Assert.True(hushLate >= hushEarly,
            $"the silent share fell as prediction improved ({hushLate:P1} later "
            + $"against {hushEarly:P1} in the opening), so the gate is not tracking "
            + "the prediction at all");

        // THE ECONOMIC HALF IS REFUTED AT THESE SETTINGS, AND THIS ASSERTS THE
        // REFUTATION SO IT CANNOT DRIFT BACK UNNOTICED. Traffic per step RISES,
        // 20.6 to 30.6, because the widest row goes 6 to 10 over the same stretch
        // and `Fire` emits one message per entry. Row growth swamps a three-point
        // gain in silence, so "predicted input is silent" does not buy "traffic
        // collapses" -- not while the row is still growing on a world whose
        // statistics never move.
        //
        // IF THIS FAILS, THE ARGUMENT HAS BEEN RESCUED and the refuted row wants
        // re-opening rather than this test wants fixing.
        Assert.True(later > opened,
            $"traffic per step no longer rises ({later:F1} against {opened:F1}), so "
            + "step 2's economic argument may have been rescued -- re-open the "
            + "refuted row rather than adjusting this bar");

        // AND THE SYSTEM STILL PREDICTS. Silence bought by a broken predictor is
        // not a saving, it is a system that has stopped working -- and the two
        // look identical in a message count alone.
        Assert.True(whole.Expected > opening.Expected * 0.8,
            $"the score went with the traffic: {whole.Expected} against {opening.Expected}");

        // THE INTERNAL ERROR SIGNAL EXISTS AT ALL, which is the part no dial in
        // this project has ever had. Every error until now was computed by the
        // harness from outside, where no controller could read it.
        Assert.True(whole.Expecting > 0.0, "nothing was ever expected");
        Assert.True(whole.Unspoken > 0, "no moment was ever silent");

        output.WriteLine(
            $"expected {whole.Expecting:F4} of onsets, stayed silent on "
            + $"{whole.Unspoken} moments, and spent "
            + $"{later / opened:P0} of the opening traffic per step");
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
        // FOUR LENGTHS AND A MONOTONE RISE, WHICH IS A STRONGER STATEMENT THAN THE
        // ONE THIS USED TO MAKE. It compared two lengths and required the gap to
        // clear 0.05 -- an arbitrary constant, and one the world outgrew: 200 to 900
        // now reads 0.9150 to 0.9521, a climb of 0.037 that is unmistakably a climb
        // and does not clear the bar. Lowering the bar would be moving a goalpost;
        // asking whether it rises AT EVERY LENGTH asks the actual claim, uses four
        // times the evidence, and rests on no constant at all.
        var dials = Fixture.Dials(stamina: 4.0) with { Span = 1 };

        int[] lengths = [100, 200, 400, 900];
        var scored = new List<RhythmResult>();

        foreach (var length in lengths)
        {
            using var run = new RhythmRun(World(), dials, seed: 1);
            scored.Add(await run.RunAsync(length));

            output.WriteLine($"{length,5} {scored[^1]}");
        }

        // THE WORLD ITSELF DOES NOT MOVE: same cycle, same ceiling, same marginal,
        // at every length. That is what makes the rise below a fact about the
        // LEARNING and not about the world getting easier.
        foreach (var one in scored)
        {
            Assert.Equal(scored[0].Ceiling, one.Ceiling, 6);
            Assert.Equal(scored[0].Marginal, one.Marginal, 6);
        }

        // AND THE SCORE RISES EVERY TIME. A sweep taken at one length here is
        // reading the length -- 0.8919, 0.9150, 0.9387, 0.9521, and still climbing
        // at 1800. This world does not remove the trap; it makes it visible and
        // cheap to respect.
        for (var at = 1; at < scored.Count; at++)
            Assert.True(scored[at].Expected > scored[at - 1].Expected,
                $"the score stopped climbing between {lengths[at - 1]} and "
                + $"{lengths[at]} steps ({scored[at - 1].Expected:F4} to "
                + $"{scored[at].Expected:F4}), so this world has saturated and the "
                + "two-length rule could be relaxed");
    }
}
