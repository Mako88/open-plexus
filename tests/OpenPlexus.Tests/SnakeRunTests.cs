using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The loop, closed. A chain of reasoning causes a move in a world.
/// </summary>
public sealed class SnakeRunTests
{
    private static SnakeSettings World(int? sight = 1) => Fixture.Snake(sight);

    private static WalkSettings Dials() => Fixture.Dials(foresight: 2.0, horizon: 6);

    // ---- the headline -----------------------------------------------------

    [Fact]
    public async Task A_chain_causes_a_move()
    {
        using var run = new SnakeRun(World(), Dials(), seed: 1);

        var result = await run.PlayAsync(200);

        // THE NUMBER THIS PROJECT HAS NEVER BEEN ABLE TO REPORT. Not that the
        // snake plays well -- nothing here claims that -- but that a chain of
        // reasoning reached an action code and that action was taken.
        Assert.True(result.ChosenByChain > 0,
            $"no chain ever reached an action: {result.ReachedNothing} thoughts reached " +
            $"nothing, {result.Silent} frames were silent, over {result.Steps} steps");

        Assert.True(result.Nodes > 0, "the graph is empty");
    }

    [Fact]
    public async Task Without_action_codes_in_the_graph_nothing_is_ever_chosen()
    {
        // THE COMPANION, and the one that makes the count above mean something.
        // If the action never joins the occasion it has no edges, so no walk
        // can reach it -- and every step falls back to random. Same world, same
        // dials, one wire cut.
        using var run = new SnakeRun(World(), Dials(), seed: 1);

        var result = await run.PlayAsync(200, cut: true);

        Assert.Equal(0, result.ChosenByChain);
        Assert.True(result.ReachedNothing > 0, "no thought ran at all");
    }

    [Fact]
    public async Task The_action_reaches_the_prediction_and_the_jitter_is_what_proves_it()
    {
        // THE MUTATION THAT SURVIVED THREE ATTEMPTS, KILLED. Removing the action
        // from the prediction broadcast used to turn no test red, because the two
        // available signals both proved nothing: a positive `Differed` is
        // explained by concurrent delivery, and a zero `Differed` is explained by
        // a small graph ranking the same codes whichever action is named.
        //
        // The third arm asks the SAME question twice and measures how far the walk
        // lands from itself. That floor is what the counterfactual has to clear.
        // NAMING ONE CODE, WHICH IS THE CONFIGURATION WHERE THE ACTION BITES.
        // Under the default the walk names as many codes as the frame holds, and
        // the difference an action makes is swamped -- measured inert at every
        // sight radius tried. That is itself the "naming fewer predicted codes"
        // row: coarse ranking informs and fine does not.
        using var wired = new SnakeRun(World(), Dials(), seed: 1, names: 1);
        var result = await wired.PlayAsync(500);

        Assert.True(result.Consequence.Asked > 0, "no consequence was ever scored");

        // THE JITTER FLOOR IS ZERO, AND THAT IS THE HALF NOBODY HAD MEASURED.
        // The old note said a positive `Differed` proved nothing because
        // concurrent delivery makes two identical broadcasts differ. It does not:
        // asking the same question twice lands in exactly the same place, at every
        // sight radius and both naming settings tried. So a difference between the
        // arms cannot be blamed on the bus.
        Assert.Equal(0.0, result.Consequence.Echoed, 6);

        Assert.True(result.Consequence.Moved > 0.0,
            $"naming a different action moved the prediction no further than asking "
            + $"the same one twice: apart {result.Consequence.Apart}, "
            + $"echoed {result.Consequence.Echoed}");

        // AND IT PREDICTS BETTER FOR KNOWING, which is fork 18's own number.
        Assert.True(result.Consequence.Gap > 0.0,
            $"knowing {result.Consequence.Knowing} against "
            + $"counterfactual {result.Consequence.Counterfactual}");
    }

    [Fact]
    public async Task With_the_action_out_of_the_graph_it_moves_nothing()
    {
        // THE COMPANION, AND IT IS THE MUTATION ITSELF. `cut` keeps the action out
        // of the occasion, so the action code has no edges and naming it in a
        // prediction cannot reach anything. Whatever difference is left between
        // the two arms is jitter, and the third arm measures exactly that -- so
        // the two distances should meet.
        //
        // Without this, the test above passes for a harness that reports any
        // positive number.
        using var run = new SnakeRun(World(), Dials(), seed: 1, names: 1);
        var result = await run.PlayAsync(500, cut: true);

        Assert.True(result.Consequence.Asked > 0, "no consequence was ever scored");

        Assert.True(result.Consequence.Moved <= 0.0,
            $"an action with no edges still moved the prediction: "
            + $"apart {result.Consequence.Apart}, echoed {result.Consequence.Echoed}");
    }

    // ---- the run is honest about itself -----------------------------------

    [Fact]
    public async Task Every_step_is_accounted_for()
    {
        using var run = new SnakeRun(World(), Dials(), seed: 3);

        var result = await run.PlayAsync(150);

        // A step either acted on a chain, ran a thought that reached no action,
        // or was silent. Nothing else, and none of it hidden.
        Assert.Equal(result.Steps, result.ChosenByChain + result.ReachedNothing + result.Silent);
    }

    [Fact]
    public async Task A_stable_scene_really_is_silent_sometimes()
    {
        // Persistence produces no message. With a one-cell window in open space
        // the view repeats, and the run should say so rather than inventing
        // work.
        using var run = new SnakeRun(World(), Dials(), seed: 5);

        var result = await run.PlayAsync(150);

        Assert.True(result.Silent > 0, "no frame was ever unchanged");

        // The companion: it was not silent throughout, or nothing was learned.
        Assert.True(result.Silent < result.Steps, "every single frame was silent");
    }

    [Fact]
    public async Task The_run_ends_rather_than_going_forever()
    {
        using var run = new SnakeRun(World(), Dials(), seed: 7);

        var result = await run.PlayAsync(500);

        // Energy depletes and running out ends the run. Something is at stake.
        Assert.True(result.Steps <= 500);
        Assert.True(!result.Alive || result.Steps == 500);
    }

    [Fact]
    public async Task A_run_at_a_fixed_seed_is_not_reproducible_and_that_is_C2()
    {
        // MEASURED, 20 repeats on three seeds: one seed reproduced exactly,
        // one varied only in its internal counts, and one varied in the
        // TRAJECTORY. Delivery is concurrent, so arrivals accumulate in
        // different orders, floating-point sums differ in their last bits and a
        // near-tie in the ranking can flip.
        //
        // <b>This is the architecture, not a defect.</b> C2 says messages are
        // late, jittered and out of order, and a system that produced identical
        // output under that would be one where the concurrency was fake. What
        // it means is that NO SINGLE RUN IS EVIDENCE -- every measurement here
        // is over seeds, with a spread.
        using var first = new SnakeRun(World(), Dials(), seed: 3);
        using var second = new SnakeRun(World(), Dials(), seed: 3);

        var one = await first.PlayAsync(300);
        var other = await second.PlayAsync(300);

        // What must hold every time, however the messages landed: the
        // accounting closes and the run is well formed.
        Assert.Equal(0, one.Unbalanced);
        Assert.Equal(0, other.Unbalanced);
        Assert.True(one.Steps > 0 && other.Steps > 0);
    }

    [Fact]
    public async Task A_different_seed_plays_a_different_run()
    {
        // The companion. Without it the test above passes for a run that always
        // produces the same thing regardless of the world.
        using var first = new SnakeRun(World(), Dials(), seed: 11);
        using var second = new SnakeRun(World(), Dials(), seed: 12);

        Assert.NotEqual(await first.PlayAsync(120), await second.PlayAsync(120));
    }

    // ---- the front end ----------------------------------------------------

    [Fact]
    public void What_the_body_did_joins_the_moment_it_is_sensed_in()
    {
        var view = new SnakeView { Cells = [new Seen(0, 0, Cell.Body)] };
        var sense = new SnakeSense();

        var without = sense.Codify(new SnakeFrame { View = view, Did = null });
        var with = sense.Codify(new SnakeFrame { View = view, Did = SnakeSense.Encode(Turn.Ahead) });

        // An action code only gets edges if it is present alongside what was
        // seen; without that no walk can ever reach one.
        Assert.Equal(without.Count + 1, with.Count);
        Assert.Contains(SnakeSense.Encode(Turn.Ahead), with);
    }

}
