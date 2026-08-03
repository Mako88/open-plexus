using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// One question, with the plumbing left attached.
/// </summary>
/// <remarks>
/// <b>Shared by every world that asks rather than acts</b>, in the same way
/// <see cref="Questioned"/> is shared by their results. The two worlds grew
/// identical copies of this — the same six fields, the same read out of a
/// settled thought, the same fold across concurrent votes — differing only in
/// what <see cref="Landed"/> was about.
/// </remarks>
/// <param name="Named">What the walk named, or null if it reached nothing.</param>
/// <param name="Landed">
/// Whether the question was actually answerable by this walk — <b>which is a
/// different question from whether it was answered right, and each world means
/// something specific by it.</b> The binding world means both candidate shapes
/// were in reach, so a coin-flip score really was a forced choice. The
/// composition world means the walk got to the index it was asking about, so a
/// wrong answer can be blamed on the second hop rather than on the reference.
/// </param>
/// <param name="Halted">Routes killed by the horizon rather than by economics.</param>
/// <param name="Balanced">Whether the thought's own accounting closed.</param>
/// <param name="Settled">Whether it finished, rather than running out of patience.</param>
/// <param name="Reached">Everything that arrived, for the chain histogram.</param>
public readonly record struct Answered(
    int? Named,
    bool Landed,
    int Halted,
    bool Balanced,
    bool Settled,
    IReadOnlyList<Arrival> Reached)
{
    /// <summary>
    /// Reads a thought into a report.
    /// </summary>
    /// <remarks>
    /// <b>Call before releasing the thought</b> — everything here is read off it,
    /// and a released thought answers with nothing.
    /// </remarks>
    /// <param name="thought">The settled walk.</param>
    /// <param name="named">What the caller made of what it reached.</param>
    /// <param name="landed">See <see cref="Landed"/>.</param>
    /// <param name="settled">Whether it finished rather than timing out.</param>
    public static Answered From(Thought thought, int? named, bool landed, bool settled)
    {
        ArgumentNullException.ThrowIfNull(thought);

        return new Answered(
            named,
            landed,
            thought.Halted,
            thought.Balanced(),
            settled,
            thought.Best(int.MaxValue));
    }

    /// <summary>
    /// Folds several concurrent answers to one question into a single report.
    /// </summary>
    /// <remarks>
    /// <b>The majority names it, and everything else is pooled</b> — a route that
    /// halted halted whichever vote it belonged to, and the accounting has to
    /// close across all of them or none of them.
    /// </remarks>
    public static Answered Voted(IReadOnlyList<Answered> votes)
    {
        ArgumentNullException.ThrowIfNull(votes);

        return new Answered(
            Majority.Of(votes.Select(one => one.Named)).Chosen,
            votes.Any(one => one.Landed),
            votes.Sum(one => one.Halted),
            votes.All(one => one.Balanced),
            votes.All(one => one.Settled),
            [.. votes.SelectMany(one => one.Reached)]);
    }
}
