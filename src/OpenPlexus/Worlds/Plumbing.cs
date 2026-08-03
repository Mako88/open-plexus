using OpenPlexus.Graph;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// How long the chains that came back were, by length.
/// </summary>
/// <remarks>
/// <b>The single most revealing line any of these worlds prints.</b> A walk that
/// only ever produces chains of length two is a one-hop lookup wearing the name
/// of a flood, and nothing else reported would show it.
/// <para>
/// A class rather than a loop, because all three worlds folded arrivals into a
/// dictionary with the same three lines and the histogram is the one thing every
/// one of them reads back afterwards.
/// </para>
/// </remarks>
public sealed class Chains
{
    private readonly Dictionary<int, int> _byLength = [];

    /// <summary>Counts the chain each of these arrivals travelled on.</summary>
    public void Fold(IEnumerable<Arrival> arrivals)
    {
        ArgumentNullException.ThrowIfNull(arrivals);

        foreach (var arrival in arrivals)
        {
            var length = arrival.Chain.Length;
            _byLength[length] = _byLength.GetValueOrDefault(length) + 1;
        }
    }

    /// <summary>
    /// A copy, so a result handed one cannot watch it move afterwards.
    /// </summary>
    public IReadOnlyDictionary<int, int> ByLength => new Dictionary<int, int>(_byLength);
}

/// <summary>
/// Whether fork 21 was running, and what it wrote.
/// </summary>
/// <remarks>
/// <b>The two travel together because neither means anything alone.</b> "Wrote
/// nothing" is the healthy state when reflection is off and a wiring fault when
/// it is on, and every world had its own copy of that pair — including its own
/// copy of deciding whether the dial was on at all.
/// </remarks>
/// <param name="On">Whether reflection was switched on for this run.</param>
/// <param name="Wrote">
/// Conclusions written back as observations. <b>Zero when fork 21 is off</b>,
/// which is how a run says out loud whether the mechanism was even running.
/// </param>
public readonly record struct Reflections(bool On, int Wrote)
{
    /// <summary>Reads the dial and pairs it with what actually got written.</summary>
    public static Reflections Of(WalkSettings dials, int wrote)
    {
        ArgumentNullException.ThrowIfNull(dials);

        return new Reflections(dials.Reflect is not null, wrote);
    }
}

/// <summary>
/// What the machinery underneath a world did, as opposed to what the world
/// measured.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's ask, 2026-08-02, and the reason is the failure this project keeps
/// having:</b> a number is swept, it barely moves, and much later it turns out
/// something was not wired the way anyone thought. A run that reports its own
/// plumbing makes that visible on the spot instead of after the conclusions.
/// </para>
/// <para>
/// <b>Every world carried its own copy of these six numbers</b>, declared three
/// times and copied out of <see cref="Fabric"/> three times, purely because the
/// results were three unrelated records. They are the same six quantities about
/// the same machinery, so they are one record and <see cref="Fabric.Facts"/>
/// fills it in.
/// </para>
/// </remarks>
public sealed record Plumbing
{
    /// <summary>Nodes across every cluster.</summary>
    public required int Nodes { get; init; }

    /// <summary>Partner entries across every node — the graph's size.</summary>
    public required int Edges { get; init; }

    /// <summary>
    /// The most partners any one node holds. <b>The graph's density, where
    /// <see cref="Edges"/> is its size.</b>
    /// </summary>
    /// <remarks>
    /// <b>REPORTED BECAUSE THE MEAN HIDES IT, MEASURED 2026-08-03.</b> A world
    /// that mints a contentless index per scene creates a great many nodes of
    /// fan-out two, and they drag <c>Edges / Nodes</c> flat while the handful of
    /// nodes the walk actually passes through grow without bound. Cost is set by
    /// the widest row, because <see cref="Graph.Node.Fire"/> snapshots the whole
    /// row and emits one message per surviving partner — so the mean is the one
    /// statistic that cannot see the problem.
    /// </remarks>
    public required int Widest { get; init; }

    /// <summary>How many nodes each cluster holds.</summary>
    public required IReadOnlyList<int> Spread { get; init; }

    /// <inheritdoc cref="Chains"/>
    public required IReadOnlyDictionary<int, int> ChainLengths { get; init; }

    /// <summary>What the bus carried.</summary>
    public required long Messages { get; init; }

    /// <summary>Thoughts whose own accounting did not close.</summary>
    public required int Unbalanced { get; init; }

    /// <summary>How far a route actually walked, at most.</summary>
    public int Deepest => ChainLengths.Count == 0 ? 0 : ChainLengths.Keys.Max();

    /// <summary>The histogram, in the one-line form every world prints it in.</summary>
    public string Lengths =>
        string.Join(" ", ChainLengths.OrderBy(e => e.Key).Select(e => $"{e.Key}:{e.Value}"));
}
