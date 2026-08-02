using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// One moment of snake: what is visible, and what was just done.
/// </summary>
/// <remarks>
/// <b>The action is part of the observation, and it has to be.</b> An action
/// code can only be reached by a walk if it has edges, and edges come from
/// co-occurrence — so unless what the snake did is present in the moment
/// alongside what it saw, no chain can ever arrive at an action and the output
/// machine has nothing to choose from.
/// </remarks>
public sealed record SnakeFrame
{
    public required SnakeView View { get; init; }

    /// <summary>
    /// The code for what was done to get here. Null on the very first frame.
    /// </summary>
    /// <remarks>
    /// A <see cref="Code"/> rather than an action, so the same front end serves
    /// both action vocabularies — absolute directions and relative turns —
    /// without knowing which is in use.
    /// </remarks>
    public required Code? Did { get; init; }
}

/// <summary>
/// Snake's whole front end: vision, plus what the body just did.
/// </summary>
/// <remarks>
/// <b>Two modalities, and they never collide</b> — a cell code and an action
/// code differ in their modality byte before anything else. That is the same
/// separation that would keep a picture and a sound apart, exercised here on
/// the smallest pair that needs it.
/// </remarks>
public sealed class SnakeSense : IQuantizer<SnakeFrame>
{
    /// <summary>What the body did. A sense like any other.</summary>
    public const byte Proprioception = 2;

    private readonly SnakeQuantizer _vision;

    public SnakeSense(bool includeEmpty) => _vision = new SnakeQuantizer(includeEmpty);

    /// <summary>Where the turn codes start, clear of the four direction codes.</summary>
    private const ulong Turning = 16;

    /// <summary>The four codes an absolute action can be.</summary>
    public static IReadOnlyList<Code> Actions { get; } =
        [.. Enum.GetValues<SnakeAction>().Select(Encode)];

    /// <summary>
    /// The three codes a turn can be. <b>Three, not four</b> — Back does not
    /// exist, so an output machine cannot offer it.
    /// </summary>
    public static IReadOnlyList<Code> Turns { get; } =
        [.. Enum.GetValues<Turn>().Select(Encode)];

    /// <summary>The code for one action. A fixed transform, so every machine agrees.</summary>
    public static Code Encode(SnakeAction action) => new(Proprioception, (ulong)action);

    /// <summary>The code for one turn. Kept clear of the direction codes.</summary>
    public static Code Encode(Turn turn) => new(Proprioception, Turning + (ulong)turn);

    /// <summary>The action a code means, if it means one at all.</summary>
    public static SnakeAction? Decode(Code code) =>
        code.Modality == Proprioception && code.Value <= (ulong)SnakeAction.West
            ? (SnakeAction)code.Value
            : null;

    /// <summary>The turn a code means, if it means one at all.</summary>
    public static Turn? Turned(Code code) =>
        code.Modality == Proprioception
        && code.Value >= Turning
        && code.Value <= Turning + (ulong)Turn.Right
            ? (Turn)(code.Value - Turning)
            : null;

    /// <inheritdoc/>
    public byte Modality => SnakeQuantizer.Vision;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(SnakeFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var codes = new List<Code>(_vision.Codify(frame.View));
        if (frame.Did is { } did) codes.Add(did);
        return codes;
    }
}
