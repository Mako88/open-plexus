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
    private const byte Proprioception = 2;

    private readonly SnakeQuantizer _vision;

    /// <remarks>
    /// <b>NOTHING TO CONFIGURE — John's call, 2026-08-04.</b> Empty cells produce
    /// codes and the action is said to come BEFORE what was then seen. Both were
    /// switches; withholding empty cells left 47% of steps with no onset at all,
    /// and without the order the chain reached an action zero times on every seed.
    /// </remarks>
    public SnakeSense() => _vision = new SnakeQuantizer();

    /// <summary>Where the turn codes start, clear of the four direction codes.</summary>
    private const ulong Turning = 16;

    /// <summary>
    /// The three codes a turn can be. <b>Three, not four</b> — Back does not
    /// exist, so an output machine cannot offer it.
    /// </summary>
    public static IReadOnlyList<Code> Turns { get; } =
        [.. Enum.GetValues<Turn>().Select(Encode)];

    /// <summary>The code for one turn. Kept clear of the direction codes.</summary>
    public static Code Encode(Turn turn) => new(Proprioception, Turning + (ulong)turn);

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

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>The action came first, and the view is what followed it.</b>
    /// <see cref="SnakeFrame.Did"/> is the move already taken and the view is the
    /// world after it, so they are not simultaneous and never were. Written as
    /// one occasion with no order they became a <c>With</c> pair, which says the
    /// view ACCOMPANIED the action — indistinguishable from the view having been
    /// there when the action was chosen — which is why prediction conditional on an
    /// action was blocked until something could say what FOLLOWED what.
    /// </para>
    /// <para>
    /// <b>One way, action to view.</b> The past records the future and the reverse
    /// is not written, so a broadcast carrying an action can walk to what usually
    /// follows it — which is <i>what will the world look like if I do X</i>, and is
    /// the question this project exists to be able to ask.
    /// </para>
    /// <para>
    /// <b>Null when there was no action</b>, which is the first frame and every
    /// frame under the cut control: with nothing to come first, nothing is
    /// ordered and the occasion is the flat set it always was.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Order(SnakeFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Did is not { } did) return null;

        var order = new Dictionary<Code, int> { [did] = 0 };

        foreach (var code in _vision.Codify(frame.View)) order[code] = 1;

        return order;
    }
}
