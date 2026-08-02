using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The world. What matters here is that it has something to lose and that the
/// view recurs — the two properties every later measurement rests on.
/// </summary>
public sealed class SnakeTests
{
    private static SnakeSettings Board(int? sight = 1, double energy = 100.0, double perFood = 50.0) => new()
    {
        Width = 21,
        Height = 21,
        Sight = sight,

        // These tests are about absolute movement and an unrotated view, so
        // they say so rather than riding on whatever the default happens to be.
        Relative = false,
        StartingEnergy = energy,
        EnergyPerStep = 1.0,
        EnergyPerFood = perFood,
    };

    private static Cell SeenAt(SnakeView view, int dx, int dy) =>
        view.Cells.Single(c => c.Dx == dx && c.Dy == dy).Content;

    // ---- something to lose ------------------------------------------------

    [Fact]
    public void Energy_depletes_with_every_step()
    {
        var snake = new Snake(Board(), seed: 1);
        var before = snake.Energy;

        snake.Step(SnakeAction.East);

        Assert.Equal(before - 1.0, snake.Energy);
    }

    [Fact]
    public void Running_out_of_energy_ends_the_run_rather_than_resetting()
    {
        var snake = new Snake(Board(energy: 2.0), seed: 1);

        snake.Step(SnakeAction.East);
        Assert.True(snake.Alive);

        snake.Step(SnakeAction.East);

        // ENDS, not resets. A run that restarts on failure has nothing at
        // stake, and nothing at stake is no source of preference.
        Assert.False(snake.Alive);
        Assert.Throws<InvalidOperationException>(() => snake.Step(SnakeAction.East));
    }

    [Fact]
    public void Walking_into_a_wall_ends_the_run_and_open_space_does_not()
    {
        var into = new Snake(Board(), seed: 1);
        for (var i = 0; i < 11 && into.Alive; i++) into.Step(SnakeAction.East);
        Assert.False(into.Alive);

        // The companion. Without it this passes even if Alive were always
        // false after any step at all.
        var across = new Snake(Board(), seed: 1);
        across.Step(SnakeAction.East);
        Assert.True(across.Alive);
    }

    [Fact]
    public void Walking_into_its_own_body_ends_the_run()
    {
        var snake = new Snake(Board(), seed: 1);

        // The body runs west from the head, so reversing collides with the neck.
        // NOTE a length-3 snake cannot self-intersect any other way: the tail
        // vacates a cell in the same step the head would reach it, so a tight
        // four-step square is legal and this is the only collision available.
        snake.Step(SnakeAction.West);

        Assert.False(snake.Alive);

        // The companion. Without it this passes even if every step killed it.
        var onward = new Snake(Board(), seed: 1);
        onward.Step(SnakeAction.North);
        onward.Step(SnakeAction.West);
        onward.Step(SnakeAction.South);
        onward.Step(SnakeAction.East);
        Assert.True(onward.Alive);
    }

    [Fact]
    public void Eating_restores_energy_and_lengthens_the_snake()
    {
        // Walk until a fruit is taken, whenever the seeded board puts one in
        // reach. Nothing declares food good; this only asserts what eating DOES.
        var snake = new Snake(Board(sight: null), seed: 7);
        var length = snake.Length;

        var ate = false;
        for (var i = 0; i < 200 && snake.Alive && !ate; i++)
        {
            var before = snake.Energy;
            var food = snake.View().Cells.FirstOrDefault(c => c.Content == Cell.Food);
            snake.Step(Toward(food));
            if (snake.Alive && snake.Energy > before) ate = true;
        }

        Assert.True(ate, "no fruit was reached in 200 steps");
        Assert.True(snake.Length > length);
    }

    private static SnakeAction Toward(Seen food) =>
        Math.Abs(food.Dx) >= Math.Abs(food.Dy)
            ? food.Dx >= 0 ? SnakeAction.East : SnakeAction.West
            : food.Dy >= 0 ? SnakeAction.South : SnakeAction.North;

    // ---- the view recurs --------------------------------------------------

    [Fact]
    public void The_view_is_expressed_as_offsets_from_the_head()
    {
        var snake = new Snake(Board(), seed: 1);
        var before = snake.View().Cells.Select(c => (c.Dx, c.Dy)).Order().ToArray();

        snake.Step(SnakeAction.East);

        // CENTRING IS THE WHOLE POINT: the frame travels with the head, so the
        // same situation in two places is one observation. 233 distinct views
        // over 2,000 steps is what that bought on the Python side.
        var after = snake.View().Cells.Select(c => (c.Dx, c.Dy)).Order().ToArray();
        Assert.Equal(before, after);
    }

    [Fact]
    public void What_is_in_those_offsets_still_changes()
    {
        // The companion to the test above. An identical offset set proves
        // nothing if the contents never move — that would be a view that
        // cannot observe anything.
        var snake = new Snake(Board(), seed: 1);

        // The head starts at x=10 on a board 21 wide, so the wall enters the
        // one-cell window only on the eleventh reading.
        var seen = new HashSet<Cell>();
        for (var i = 0; i < 12 && snake.Alive; i++)
        {
            seen.Add(SeenAt(snake.View(), 1, 0));
            snake.Step(SnakeAction.East);
        }

        Assert.Contains(Cell.Empty, seen);
        Assert.Contains(Cell.Wall, seen);
    }

    [Fact]
    public void A_wall_shows_up_at_the_offset_it_actually_occupies()
    {
        var snake = new Snake(Board(), seed: 1);
        while (SeenAt(snake.View(), 1, 0) != Cell.Wall) snake.Step(SnakeAction.East);

        // Adjacent east means the wall is at +1, and stepping there ends the run.
        snake.Step(SnakeAction.East);
        Assert.False(snake.Alive);
    }

    [Fact]
    public void Sight_bounds_what_is_visible_and_the_full_board_does_not()
    {
        var local = new Snake(Board(sight: 1), seed: 1);
        Assert.Equal(9, local.View().Cells.Count);

        // Local so the food is usually unseen, which is what gives "act to
        // disambiguate" something to disambiguate.
        Assert.DoesNotContain(local.View().Cells, c => c.Content == Cell.Food);

        var whole = new Snake(Board(sight: null), seed: 1);
        Assert.Equal(21 * 21, whole.View().Cells.Count);
        Assert.Contains(whole.View().Cells, c => c.Content == Cell.Food);
    }

    [Fact]
    public void The_head_is_at_the_origin_of_its_own_view()
    {
        var snake = new Snake(Board(), seed: 1);

        Assert.Equal(Cell.Body, SeenAt(snake.View(), 0, 0));
    }
}
