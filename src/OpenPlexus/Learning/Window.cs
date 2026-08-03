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
    /// <summary>Code to the moment it stopped.</summary>
    private readonly Dictionary<Code, long> _departed = [];

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
        [.. _departed.Where(entry => now - entry.Value < _span).Select(entry => entry.Key)];

    /// <summary>
    /// Takes what just stopped, and drops what has been carried long enough.
    /// </summary>
    /// <remarks>
    /// <b>A code that comes back is no longer departed.</b> Leaving it in would
    /// let it be joined twice for one moment — once as live and once as recent.
    /// </remarks>
    public void Carry(IReadOnlyCollection<Code> stopped, IReadOnlyCollection<Code> started, long now)
    {
        ArgumentNullException.ThrowIfNull(stopped);
        ArgumentNullException.ThrowIfNull(started);

        foreach (var code in started) _departed.Remove(code);
        foreach (var code in stopped) _departed[code] = now;

        foreach (var code in _departed.Where(e => now - e.Value >= _span).Select(e => e.Key).ToArray())
            _departed.Remove(code);
    }
}
