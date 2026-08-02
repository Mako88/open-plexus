namespace OpenPlexus.Worlds;

/// <summary>What a cell in the view contains.</summary>
/// <remarks>
/// <b>Categorical, and quantised one-hot rather than by hyperplane.</b> These
/// are 0 1 2 3 and a hyperplane over those numbers would make wall-and-body
/// near and empty-and-food far, which is arithmetic nobody meant.
/// </remarks>
public enum Cell { Empty, Wall, Body, Food }

/// <summary>Where the snake goes next.</summary>
public enum SnakeAction { North, South, East, West }

/// <summary>One cell of the view, offset from the head.</summary>
public readonly record struct Seen(int Dx, int Dy, Cell Content);

/// <summary>
/// What the snake can see. <b>Head-centred and local.</b>
/// </summary>
/// <remarks>
/// <b>Centred</b> so the same situation in two places is one observation, which
/// is what makes anything recur at all — 233 distinct views over 2,000 steps.
/// <b>Local</b> so the food is usually unseen, which is what gives <i>act to
/// disambiguate</i> something to disambiguate.
/// </remarks>
public sealed record SnakeView
{
    public required IReadOnlyList<Seen> Cells { get; init; }
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
    private readonly int _width, _height, _sight;
    private readonly Random _rng;

    public Snake(int width, int height, int sight, int seed) =>
        throw new NotImplementedException();

    /// <summary>Advances one tick.</summary>
    public void Step(SnakeAction action) => throw new NotImplementedException();

    /// <inheritdoc cref="SnakeView"/>
    public SnakeView View() => throw new NotImplementedException();

    /// <summary>
    /// Something to lose.
    /// </summary>
    /// <remarks>
    /// Depletes, food restores it, running out <b>ends</b> the run rather than
    /// resetting. <b>Nothing declares food good.</b> A policy that does not eat
    /// gets fewer steps of experience — selection without a reward, and the
    /// first source of preference this design has had.
    /// </remarks>
    public double Energy => throw new NotImplementedException();

    /// <summary>Whether the run is still going.</summary>
    public bool Alive => throw new NotImplementedException();
}
