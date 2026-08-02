namespace OpenPlexus.Worlds;

/// <summary>What a cell in the view contains.</summary>
/// <remarks>
/// <b>Categorical, and quantised one-hot rather than by hyperplane.</b> These
/// are 0 1 2 3 and a hyperplane over those numbers would make wall-and-body
/// near and empty-and-food far, which is arithmetic nobody meant.
/// </remarks>
public enum Cell { Empty, Wall, Body, Food }

/// <summary>Where the snake goes next, in board terms.</summary>
public enum SnakeAction { North, South, East, West }

/// <summary>
/// Where the snake goes next, in its own terms.
/// </summary>
/// <remarks>
/// <b>There is no Back, and that is the point.</b> Reversing into the neck is
/// instantly fatal, so under absolute actions one move in four kills the snake
/// immediately and every run ends in a handful of steps. Here it is not an
/// action that exists — it falls out of the coordinate system rather than being
/// a rule bolted on.
/// </remarks>
public enum Turn { Ahead, Left, Right }

/// <summary>One cell of the view, offset from the head.</summary>
public readonly record struct Seen(int Dx, int Dy, Cell Content);

/// <summary>
/// What the snake can see. <b>Head-centred and local.</b>
/// </summary>
/// <remarks>
/// <b>Centred</b> so the same situation in two places is one observation, which
/// is what makes anything recur at all. <b>Local</b> so the food is usually
/// unseen, which is what gives <i>act to disambiguate</i> something to
/// disambiguate.
/// </remarks>
public sealed record SnakeView
{
    public required IReadOnlyList<Seen> Cells { get; init; }
}

/// <summary>
/// How the world is set up. Every constant named and none defaulted.
/// </summary>
/// <remarks>
/// <b>A constant that never changes looks like the background.</b> Requiring
/// each one is how a number gets set on purpose rather than inherited.
/// </remarks>
public sealed record SnakeSettings
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>
    /// How far the snake sees, as a radius. <b><c>null</c> is the whole board,
    /// still centred</b> — the two are arms of one experiment, not a feature
    /// and its disabled state.
    /// </summary>
    public int? Sight { get; init; }

    /// <summary>
    /// Whether the view rotates with the snake's heading.
    /// </summary>
    /// <remarks>
    /// <b>Centring made the same situation in two PLACES one observation; this
    /// extends that to two ORIENTATIONS.</b> A wall directly ahead is one code
    /// whichever way the snake happens to be pointing, where an unrotated view
    /// gives four. Both arms exist because the trade is real: rotation loses
    /// absolute direction, so food-to-the-north and food-to-the-east become the
    /// same observation.
    /// </remarks>
    /// <remarks>
    /// <b>Defaults to true — measured, 200 seeds.</b> Runs last 51.3 steps
    /// against 6.5 unrotated, because reversing into the neck stops being an
    /// action that exists. New codes per step fall from 0.98 to 0.19, which is
    /// the recurrence the rotation buys.
    /// </remarks>
    public bool Relative { get; init; } = true;

    public required double StartingEnergy { get; init; }
    public required double EnergyPerStep { get; init; }
    public required double EnergyPerFood { get; init; }
}

/// <summary>
/// An interactive environment, not a recorded corpus.
/// </summary>
/// <remarks>
/// The only source supplying an error signal for free. Chosen over ARC-AGI-3 to
/// start with, so that a bad result cannot be blamed on the environment.
/// </remarks>
public sealed class Snake
{
    private readonly record struct Point(int X, int Y);

    private readonly SnakeSettings _settings;
    private readonly Random _rng;

    /// <summary>Head first. The tail is the last entry.</summary>
    private readonly List<Point> _body = [];

    private readonly HashSet<Point> _occupied = [];

    private Point _food;
    private double _energy;
    private bool _alive = true;

    /// <summary>Which way the snake is pointing. The body runs west, so it starts east.</summary>
    private SnakeAction _heading = SnakeAction.East;

    public Snake(SnakeSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Width, 5);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Height, 5);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.StartingEnergy);

        _settings = settings;
        _rng = new Random(seed);
        _energy = settings.StartingEnergy;

        // Placed at the centre rather than at random: a start position nobody
        // chose is a constant that would sit under every number.
        var head = new Point(settings.Width / 2, settings.Height / 2);
        foreach (var offset in (int[])[0, 1, 2])
        {
            var part = head with { X = head.X - offset };
            _body.Add(part);
            _occupied.Add(part);
        }

        _food = PlaceFood();
    }

    /// <summary>Whether the run is still going.</summary>
    public bool Alive => _alive;

    /// <summary>
    /// Something to lose.
    /// </summary>
    /// <remarks>
    /// Depletes, food restores it, running out <b>ends</b> the run rather than
    /// resetting. <b>Nothing declares food good.</b> A policy that does not eat
    /// gets fewer steps of experience — selection without a reward, and the
    /// first source of preference this design has had.
    /// </remarks>
    public double Energy => _energy;

    /// <summary>How long the snake is. Grows by one per fruit.</summary>
    public int Length => _body.Count;

    /// <summary>Which way the snake is pointing.</summary>
    public SnakeAction Heading => _heading;

    /// <summary>
    /// Advances one tick in the snake's own terms.
    /// </summary>
    /// <remarks>
    /// <b>A turn cannot be a reversal</b>, because Back is not one of the three.
    /// </remarks>
    public void Steer(Turn turn) => Step(Absolute(turn));

    /// <summary>What a turn means from the current heading.</summary>
    public SnakeAction Absolute(Turn turn) => (turn, _heading) switch
    {
        (Turn.Ahead, var heading) => heading,

        (Turn.Left, SnakeAction.North) => SnakeAction.West,
        (Turn.Left, SnakeAction.West) => SnakeAction.South,
        (Turn.Left, SnakeAction.South) => SnakeAction.East,
        (Turn.Left, SnakeAction.East) => SnakeAction.North,

        (Turn.Right, SnakeAction.North) => SnakeAction.East,
        (Turn.Right, SnakeAction.East) => SnakeAction.South,
        (Turn.Right, SnakeAction.South) => SnakeAction.West,
        (Turn.Right, SnakeAction.West) => SnakeAction.North,

        _ => throw new ArgumentOutOfRangeException(nameof(turn)),
    };

    /// <summary>Advances one tick.</summary>
    /// <remarks>
    /// <b>A dead run does not step.</b> Running out of energy ends it; nothing
    /// here resets, because a run that restarts on failure has nothing at stake.
    /// </remarks>
    public void Step(SnakeAction action)
    {
        if (!_alive) throw new InvalidOperationException("the run is over");

        var (dx, dy) = action switch
        {
            SnakeAction.North => (0, -1),
            SnakeAction.South => (0, 1),
            SnakeAction.East => (1, 0),
            SnakeAction.West => (-1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        var head = _body[0];
        var next = new Point(head.X + dx, head.Y + dy);

        if (OutOfBounds(next) || _occupied.Contains(next))
        {
            _alive = false;
            return;
        }

        _energy -= _settings.EnergyPerStep;

        var ate = next == _food;
        _body.Insert(0, next);
        _occupied.Add(next);

        if (ate)
        {
            _energy += _settings.EnergyPerFood;
            _food = PlaceFood();
        }
        else
        {
            var tail = _body[^1];
            _body.RemoveAt(_body.Count - 1);
            _occupied.Remove(tail);
        }

        _heading = action;

        if (_energy <= 0.0) _alive = false;
    }

    /// <summary>
    /// Turns a board offset into one in the snake's own frame: <c>dx</c> ahead,
    /// <c>dy</c> to the right.
    /// </summary>
    private (int Dx, int Dy) Facing(int dx, int dy) => _heading switch
    {
        SnakeAction.East => (dx, dy),
        SnakeAction.South => (dy, -dx),
        SnakeAction.West => (-dx, -dy),
        SnakeAction.North => (-dy, dx),
        _ => throw new InvalidOperationException("no heading"),
    };

    /// <inheritdoc cref="SnakeView"/>
    public SnakeView View()
    {
        var head = _body[0];
        var cells = new List<Seen>();

        void Look(int dx, int dy)
        {
            var content = At(head.X + dx, head.Y + dy);
            var (ax, ay) = _settings.Relative ? Facing(dx, dy) : (dx, dy);
            cells.Add(new Seen(ax, ay, content));
        }

        if (_settings.Sight is int sight)
        {
            for (var dy = -sight; dy <= sight; dy++)
                for (var dx = -sight; dx <= sight; dx++)
                    Look(dx, dy);
        }
        else
        {
            // Still centred. The whole board and a local window are arms of one
            // experiment, so they must produce the same KIND of thing.
            for (var y = 0; y < _settings.Height; y++)
                for (var x = 0; x < _settings.Width; x++)
                    Look(x - head.X, y - head.Y);
        }

        return new SnakeView { Cells = cells };
    }

    private Cell At(int x, int y)
    {
        var point = new Point(x, y);
        if (OutOfBounds(point)) return Cell.Wall;
        if (_occupied.Contains(point)) return Cell.Body;
        return point == _food ? Cell.Food : Cell.Empty;
    }

    private bool OutOfBounds(Point point) =>
        point.X < 0 || point.Y < 0 ||
        point.X >= _settings.Width || point.Y >= _settings.Height;

    private Point PlaceFood()
    {
        var free = _settings.Width * _settings.Height - _occupied.Count;
        if (free <= 0) throw new InvalidOperationException("the board is full");

        while (true)
        {
            var candidate = new Point(_rng.Next(_settings.Width), _rng.Next(_settings.Height));
            if (!_occupied.Contains(candidate)) return candidate;
        }
    }
}
