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
    private static SnakeSettings World() => Fixture.Snake();

    private static WalkSettings Dials() => Fixture.Dials();

    private static Cell At(SnakeView view, int dx, int dy) =>
        view.Cells.Single(c => c.Dx == dx && c.Dy == dy).Content;

    [Fact]
    public void A_wall_ahead_looks_the_same_whichever_way_the_snake_points()
    {
        // THE WHOLE POINT. Unrotated, this situation produces four different
        // sets of codes depending on the compass. Rotated, it produces one.
        var east = new Snake(World(), seed: 1);
        while (At(east.View(), 1, 0) != Cell.Wall) east.Steer(Turn.Ahead);

        var south = new Snake(World(), seed: 1);
        south.Steer(Turn.Right);
        while (At(south.View(), 1, 0) != Cell.Wall) south.Steer(Turn.Ahead);

        Assert.NotEqual(east.Heading, south.Heading);

        // COMPARED AS SETS, because an occasion is a set -- everything in one
        // moment met everything else and nothing came first. Rotation permutes
        // the scan order and that is harmless: nothing downstream reads it.
        var seen = new SnakeQuantizer();
        Assert.Equal(
            seen.Codify(east.View()).ToHashSet(),
            seen.Codify(south.View()).ToHashSet());
    }

    [Fact]
    public void Ahead_is_always_the_way_the_snake_is_going()
    {
        var snake = new Snake(World(), seed: 1);

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
        var snake = new Snake(World(), seed: 1);
        var was = snake.Heading;

        // Three turns exist and none of them is a reversal, so a random policy
        // can no longer kill itself on the first move.
        Assert.Equal(3, Enum.GetValues<Turn>().Length);
        Assert.Equal(3, SnakeSense.Turns.Count);

        foreach (var turn in Enum.GetValues<Turn>())
            Assert.NotEqual(Opposite(was), snake.Absolute(turn));
    }

    [Fact]
    public void A_turn_code_is_never_mistaken_for_a_direction_code()
    {
        // A vision code is never mistaken for something the body did.
        Assert.Null(SnakeSense.Turned(new Code(SnakeQuantizer.Vision, 16)));

        foreach (var turn in Enum.GetValues<Turn>())
            Assert.Equal(turn, SnakeSense.Turned(SnakeSense.Encode(turn)));
    }

    private static SnakeAction Opposite(SnakeAction action) => action switch
    {
        SnakeAction.North => SnakeAction.South,
        SnakeAction.South => SnakeAction.North,
        SnakeAction.East => SnakeAction.West,
        _ => SnakeAction.East,
    };
}
