using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The loop, closed. A chain of reasoning causes a move in a world.
/// </summary>
public sealed class SnakeRunTests
{
    private static SnakeSettings World(int? sight = 1) => new()
    {
        Width = 15,
        Height = 15,
        Sight = sight,
        StartingEnergy = 60.0,
        EnergyPerStep = 1.0,
        EnergyPerFood = 30.0,
    };

    private static WalkSettings Dials() => new()
    {
        Stamina = 4.0,
        Cost = StepCost.Best,
        Refuel = Refuel.Strength,
        Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum,
            Horizon = 6,
    };

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

        var result = await run.PlayAsync(200, blind: true);

        Assert.Equal(0, result.ChosenByChain);
        Assert.True(result.ReachedNothing > 0, "no thought ran at all");
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
    public async Task The_same_seed_plays_the_same_run()
    {
        using var first = new SnakeRun(World(), Dials(), seed: 11);
        using var second = new SnakeRun(World(), Dials(), seed: 11);

        var one = await first.PlayAsync(120);
        var other = await second.PlayAsync(120);

        // Concurrent delivery must not reach the answer. If it does, no result
        // from this harness can be compared with any other.
        Assert.Equal(one, other);
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
    public void An_action_code_and_a_cell_code_can_never_collide()
    {
        var cells = new SnakeQuantizer(includeEmpty: true)
            .Codify(new SnakeView { Cells = [new Seen(0, 0, Cell.Body), new Seen(1, 0, Cell.Food)] });

        Assert.All(SnakeSense.Actions, action => Assert.DoesNotContain(action, cells));
        Assert.All(SnakeSense.Actions, action => Assert.Equal(SnakeSense.Proprioception, action.Modality));
    }

    [Fact]
    public void Every_action_encodes_and_decodes_back_to_itself()
    {
        foreach (var action in Enum.GetValues<SnakeAction>())
            Assert.Equal(action, SnakeSense.Decode(SnakeSense.Encode(action)));

        // And a cell code is not mistaken for an action.
        Assert.Null(SnakeSense.Decode(new Code(SnakeQuantizer.Vision, 0)));
    }

    [Fact]
    public void What_the_body_did_joins_the_moment_it_is_sensed_in()
    {
        var view = new SnakeView { Cells = [new Seen(0, 0, Cell.Body)] };
        var sense = new SnakeSense(includeEmpty: true);

        var without = sense.Codify(new SnakeFrame { View = view, Did = null });
        var with = sense.Codify(new SnakeFrame { View = view, Did = SnakeAction.East });

        // An action code only gets edges if it is present alongside what was
        // seen; without that no walk can ever reach one.
        Assert.Equal(without.Count + 1, with.Count);
        Assert.Contains(SnakeSense.Encode(SnakeAction.East), with);
    }
}
