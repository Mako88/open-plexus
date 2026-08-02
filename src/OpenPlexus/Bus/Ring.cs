using OpenPlexus.Codes;

namespace OpenPlexus.Bus;

/// <summary>
/// Which cluster owns which code. Computed locally, agreed globally.
/// </summary>
/// <remarks>
/// <para>
/// <b>No directory and nobody to ask.</b> Every machine computes the same
/// answer from the code and the shared seed, which is what lets a machine join
/// a network it has never spoken to and route correctly immediately. Nothing is
/// assigned and nobody is told.
/// </para>
/// <para>
/// <b>Views differ between machines while membership is changing, and that is
/// allowed.</b> A misrouted message is a lost count, not a corruption — the
/// statistics are counts over many occasions, and C2 already says messages go
/// astray.
/// </para>
/// </remarks>
public sealed class Ring
{
    /// <summary>The constant every ring and every quantiser is built from.
    /// Handed out once and frozen, which C1 permits.</summary>
    private readonly long _seed;

    /// <summary>The current membership view, as hash points around the ring.</summary>
    private readonly SortedList<ulong, ClusterAddress> _points = [];

    public Ring(long seed) => throw new NotImplementedException();

    /// <summary>Which cluster holds the node for this code.</summary>
    /// <remarks>
    /// Hashes the whole code today. <b>Open fork 3</b> is whether to hash a
    /// prefix instead, which would put similar codes on the same machine and
    /// give a column for free — at the cost of the uniform load a whole-code
    /// hash guarantees.
    /// </remarks>
    public ClusterAddress OwnerOf(Code code) => throw new NotImplementedException();

    /// <summary>A cluster became reachable.</summary>
    public void Join(ClusterAddress address) => throw new NotImplementedException();

    /// <summary>A cluster went away. <b>Normal, not an error</b> — C3.</summary>
    public void Leave(ClusterAddress address) => throw new NotImplementedException();

    /// <summary>The current membership view.</summary>
    public IReadOnlyCollection<ClusterAddress> Clusters => throw new NotImplementedException();
}
