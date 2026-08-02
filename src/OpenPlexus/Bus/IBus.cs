using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Thinking;

namespace OpenPlexus.Bus;

/// <summary>
/// How anything reaches anything else.
/// </summary>
/// <remarks>
/// <b>The cluster subscribes, not the node.</b> A node is still reachable by
/// any broadcast; the cluster is the envelope, and that is what lets 200
/// partners across 12 clusters cost 12 sends.
/// </remarks>
public interface IBus
{
    /// <summary>
    /// A cluster becomes reachable. Disposing the handle leaves the bus, and
    /// <b>leaving is not silent</b> — it fires a death event, which is the
    /// whole reason this is a bus rather than point-to-point sends.
    /// </summary>
    IDisposable Subscribe(Cluster cluster);

    /// <summary>Get this envelope to that cluster. The thinking path.</summary>
    ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default);

    /// <summary>Get this occasion to whoever joins it. The learning path.</summary>
    ValueTask SendAsync(MachineAddress to, Occasion occasion, CancellationToken ct = default);

    /// <summary>
    /// A machine left. Thoughts waiting on routes through it can release their
    /// state.
    /// </summary>
    /// <remarks>
    /// <b>Housekeeping, not correctness.</b> Under continuous input the system
    /// acts on the best chain arrived so far, so a thought stranded by a
    /// vanished machine leaks state rather than hanging anything.
    /// </remarks>
    event Action<MachineAddress>? Deaths;
}
