using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// Carries recently departed codes forward, so a thing that has just stopped
/// can still join with what starts next.
/// </summary>
/// <remarks>
/// <para>
/// <b>Without this the graph holds no temporal edges at all.</b> A join pairs an
/// onset with what is live <i>at that moment</i>, so everything recorded is
/// simultaneity. A code that stopped before the next one started is never
/// linked to it, and the graph is left being asked what comes next by a
/// structure that has only recorded what happens together — measured as
/// predicting worse than a blind guess.
/// </para>
/// <para>
/// <b><c>Span = 0</c> is the old behaviour exactly</b>, which is what makes this
/// an arm rather than a replacement.
/// </para>
/// </remarks>
public sealed class Window
{
    /// <summary>
    /// Code to the moment it stopped, and whether it was ASSIGNED rather than
    /// selected when it happened.
    /// </summary>
    /// <remarks>
    /// <b>THE FLAG HAS TO TRAVEL WITH THE CODE OR THE ONE CELL THAT ANSWERS THE
    /// PROJECT'S QUESTION CAN NEVER BE WRITTEN.</b> An act and its outcome are
    /// never in one moment — that is the whole reason this class exists — so by the
    /// time the outcome arrives, the fact that nothing about the state chose the
    /// act is a moment old. Carrying the code and dropping the flag would leave
    /// <see cref="Graph.Kind.Meddled"/> reachable only where cause and effect
    /// coincide, which is nowhere. See <see cref="Occasion.Forced"/>.
    /// </remarks>
    private readonly Dictionary<Code, (long When, bool Forced)> _departed = [];

    private readonly int _span;

    /// <param name="span">
    /// How many moments a departed code is carried for. <b>A dial with no
    /// default</b>: too short and nothing is ever carried, too long and
    /// everything is eventually adjacent to everything, which is the density
    /// that makes a graph unwalkable.
    /// </param>
    public Window(int span)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(span);
        _span = span;
    }

    /// <summary>What is still being carried, as of now.</summary>
    /// <remarks>
    /// <b>STRICTLY INSIDE THE SPAN, AND THE STRICTNESS IS WHAT KEEPS ZERO
    /// MEANING OFF.</b> A code is carried into the very moment it stopped, because
    /// the input machine now carries before it reads, so <c>now - departed</c> is
    /// zero for whatever just left. Admitting that at <c>span = 0</c> would form edges
    /// in the arm that exists to form none, and every measurement taken under it
    /// would silently change meaning. With this strict, <c>span = 0</c> carries
    /// nothing and <c>span = 1</c> carries exactly the previous frame.
    /// </remarks>
    public IReadOnlyCollection<Code> Recent(long now) =>
        [.. _departed.Where(entry => now - entry.Value.When < _span).Select(entry => entry.Key)];

    /// <summary>
    /// Which of the carried codes were ASSIGNED rather than selected.
    /// </summary>
    /// <remarks>
    /// <b>A subset of <see cref="Recent"/>, and empty until a body says
    /// otherwise.</b> What it is for is <see cref="Graph.Kind.Meddled"/>: a
    /// carried code that nothing chose records what followed it in its own cell,
    /// and that cell is interventional where the ordinary one is merely observed.
    /// </remarks>
    public IReadOnlySet<Code> Forced(long now) =>
        _departed
            .Where(entry => entry.Value.Forced && now - entry.Value.When < _span)
            .Select(entry => entry.Key)
            .ToHashSet();

    /// <summary>
    /// Takes what just stopped, and drops what has been carried long enough.
    /// </summary>
    /// <remarks>
    /// <b>A code that comes back is no longer departed.</b> Leaving it in would
    /// let it be joined twice for one moment — once as live and once as recent.
    /// </remarks>
    /// <param name="stopped">What has just departed.</param>
    /// <param name="started">What has just begun, and so is no longer departed.</param>
    /// <param name="now">The observing machine's clock.</param>
    /// <param name="forced">
    /// Which of <paramref name="stopped"/> nothing about the state selected.
    /// <b>Null is every call ever made</b> — see <see cref="Forced"/>.
    /// </param>
    public void Carry(
        IReadOnlyCollection<Code> stopped,
        IReadOnlyCollection<Code> started,
        long now,
        IReadOnlySet<Code>? forced = null)
    {
        ArgumentNullException.ThrowIfNull(stopped);
        ArgumentNullException.ThrowIfNull(started);

        foreach (var code in started) _departed.Remove(code);
        foreach (var code in stopped)
            _departed[code] = (now, forced?.Contains(code) == true);

        foreach (var code in _departed
            .Where(e => now - e.Value.When >= _span).Select(e => e.Key).ToArray())
            _departed.Remove(code);
    }
}
