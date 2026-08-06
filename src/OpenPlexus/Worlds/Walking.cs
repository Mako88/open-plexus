using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// What several walks of ONE question add up to.
/// </summary>
/// <remarks>
/// <para>
/// <b>Asking again is several thoughts and one answer</b> — see
/// <see cref="Question.Steps"/>. Each walk contributes to the same five running
/// facts, and <see cref="Absorb"/> is the only place that says how.
/// </para>
/// <para>
/// <b>IT EXISTS BECAUSE TWO WORLDS HAD WRITTEN THE SAME FIVE LINES.</b>
/// <c>BabiRun</c> and <c>ClutrrRun</c> each folded a walk by hand, and
/// <c>DuplicationTests</c> is the budget that noticed the moment a sixth was
/// added to both. Copies drift where nothing fails.
/// </para>
/// </remarks>
internal sealed class Walking
{
    /// <summary>Routes killed by the horizon rather than by economics.</summary>
    public int Halted { get; private set; }

    /// <summary>Whether every walk's own accounting closed.</summary>
    public bool Balanced { get; private set; } = true;

    /// <summary>Whether every walk finished rather than running out of patience.</summary>
    public bool Settled { get; private set; } = true;

    /// <inheritdoc cref="Thought.Divides"/>
    public int Divides { get; private set; }

    /// <summary>
    /// Everything the LAST walk reached.
    /// </summary>
    /// <remarks>
    /// <b>The last and not the union, which is what the hand-written copies both
    /// did.</b> The answer is read from the final walk, so the chain histogram has
    /// to describe the same one — pooling every step's arrivals would count the
    /// intermediate walks' chains against an answer they did not produce.
    /// </remarks>
    public IReadOnlyList<Arrival> Reached { get; private set; } = [];

    /// <summary>
    /// Absorbs one walk.
    /// </summary>
    /// <param name="thought">The walk, read before it is forgotten.</param>
    /// <param name="quiet">Whether it settled rather than timing out.</param>
    public void Absorb(Thought thought, bool quiet)
    {
        ArgumentNullException.ThrowIfNull(thought);

        Halted += thought.Halted;
        Balanced &= thought.Balanced();
        Settled &= quiet;
        Reached = thought.Best(int.MaxValue);
        Divides = Math.Max(Divides, thought.Divides);
    }
}
