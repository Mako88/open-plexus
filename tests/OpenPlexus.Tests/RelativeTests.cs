using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The view rotates with the snake. Centring made the same situation in two
/// places one observation; this extends it to two orientations.
/// </summary>
public sealed class RelativeTests
{
    private static SnakeSettings World(bool relative) => new()
    {
        Width = 15, Height = 15, Sight = 1, Relative = relative,
        StartingEnergy = 60.0, EnergyPerStep = 1.0, EnergyPerFood = 30.0,
    };

    private static WalkSettings Dials() => new()
    {
        Stamina = 4.0, Cost = StepCost.Inverse, Refuel = Refuel.Strength,
        Value = ArrivalValue.Strength, Accumulate = Accumulate.Sum, Horizon = 50,
    };

    private static Cell At(SnakeView view, int dx, int dy) =>
        view.Cells.Single(c => c.Dx == dx && c.Dy == dy).Content;

    [Fact]
    public void A_wall_ahead_looks_the_same_whichever_way_the_snake_points()
    {
        // THE WHOLE POINT. Unrotated, this situation produces four different
        // sets of codes depending on the compass. Rotated, it produces one.
        var east = new Snake(World(relative: true), seed: 1);
        while (At(east.View(), 1, 0) != Cell.Wall) east.Steer(Turn.Ahead);

        var south = new Snake(World(relative: true), seed: 1);
        south.Steer(Turn.Right);
        while (At(south.View(), 1, 0) != Cell.Wall) south.Steer(Turn.Ahead);

        Assert.NotEqual(east.Heading, south.Heading);

        // COMPARED AS SETS, because an occasion is a set -- everything in one
        // moment met everything else and nothing came first. Rotation permutes
        // the scan order and that is harmless: nothing downstream reads it.
        var seen = new SnakeQuantizer(includeEmpty: true);
        Assert.Equal(
            seen.Codify(east.View()).ToHashSet(),
            seen.Codify(south.View()).ToHashSet());
    }

    [Fact]
    public void Unrotated_the_same_situation_gives_different_codes()
    {
        // The companion, and the reason the test above is worth anything.
        var east = new Snake(World(relative: false), seed: 1);
        while (At(east.View(), 1, 0) != Cell.Wall) east.Step(SnakeAction.East);

        var south = new Snake(World(relative: false), seed: 1);
        south.Step(SnakeAction.South);
        while (At(south.View(), 0, 1) != Cell.Wall) south.Step(SnakeAction.South);

        var seen = new SnakeQuantizer(includeEmpty: true);
        Assert.NotEqual(
            seen.Codify(east.View()).ToHashSet(),
            seen.Codify(south.View()).ToHashSet());
    }

    [Fact]
    public void Ahead_is_always_the_way_the_snake_is_going()
    {
        var snake = new Snake(World(relative: true), seed: 1);

        foreach (var turn in (Turn[])[Turn.Ahead, Turn.Left, Turn.Left, Turn.Right])
        {
            snake.Steer(turn);
            var body = snake.View().Cells.Where(c => c.Content == Cell.Body).ToArray();

            // The head is at the origin and the neck is always directly behind.
            Assert.Contains(body, c => c is { Dx: 0, Dy: 0 });
            Assert.Contains(body, c => c is { Dx: -1, Dy: 0 });
        }
    }

    [Fact]
    public void There_is_no_way_to_turn_back()
    {
        var snake = new Snake(World(relative: true), seed: 1);
        var was = snake.Heading;

        // Three turns exist and none of them is a reversal, so a random policy
        // can no longer kill itself on the first move.
        Assert.Equal(3, Enum.GetValues<Turn>().Length);
        Assert.Equal(3, SnakeSense.Turns.Count);

        foreach (var turn in Enum.GetValues<Turn>())
            Assert.NotEqual(Opposite(was), snake.Absolute(turn));
    }

    [Fact]
    public void An_absolute_run_can_still_reverse_and_die_instantly()
    {
        // The companion: reversal is fatal, which is what made it worth
        // removing rather than a hypothetical.
        var snake = new Snake(World(relative: false), seed: 1);
        snake.Step(SnakeAction.West);
        Assert.False(snake.Alive);
    }

    [Fact]
    public void A_turn_code_is_never_mistaken_for_a_direction_code()
    {
        Assert.All(SnakeSense.Turns, code => Assert.Null(SnakeSense.Decode(code)));
        Assert.All(SnakeSense.Actions, code => Assert.Null(SnakeSense.Turned(code)));

        foreach (var turn in Enum.GetValues<Turn>())
            Assert.Equal(turn, SnakeSense.Turned(SnakeSense.Encode(turn)));
    }

    [Fact]
    public async Task Runs_last_far_longer_when_the_snake_cannot_reverse()
    {
        // MEASURED at 200 seeds: 51.260 +/- 1.009 relative against
        // 6.530 +/- 0.416 unrotated. This checks a slice with room to spare.
        static async Task<double> Mean(bool relative)
        {
            var steps = new List<int>();
            for (var seed = 1; seed <= 20; seed++)
            {
                using var run = new SnakeRun(World(relative), Dials(), seed);
                steps.Add((await run.PlayAsync(300)).Steps);
            }

            return steps.Average();
        }

        var rotated = await Mean(relative: true);
        var flat = await Mean(relative: false);

        Assert.True(rotated > flat * 3, $"relative {rotated:F2} against absolute {flat:F2}");
    }

    private static SnakeAction Opposite(SnakeAction action) => action switch
    {
        SnakeAction.North => SnakeAction.South,
        SnakeAction.South => SnakeAction.North,
        SnakeAction.East => SnakeAction.West,
        _ => SnakeAction.East,
    };
}
