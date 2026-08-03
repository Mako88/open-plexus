using OpenPlexus.Thinking;

namespace OpenPlexus.Bus;

/// <summary>Something that can be handed envelopes. A cluster, in practice.</summary>
/// <remarks>
/// The bus takes this rather than a <c>Cluster</c> so that <b>it does not know
/// what a cluster is</b> — transport and graph stay separable, and the bus is
/// testable without a single node existing.
/// </remarks>
public interface IReceiveEnvelopes
{
    ClusterAddress Address { get; }

    Task DeliverAsync(Envelope envelope, CancellationToken ct = default);
}

/// <summary>Something that can be handed reports. A machine, in practice.</summary>
public interface IReceiveReports
{
    MachineAddress Address { get; }

    Task DeliverAsync(Report report, CancellationToken ct = default);
}

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
    IDisposable Subscribe(IReceiveEnvelopes cluster);

    /// <summary>A machine becomes reachable, so reports can come back to it.</summary>
    IDisposable Subscribe(IReceiveReports machine);

    /// <summary>
    /// Get this envelope to that cluster. The thinking path, outbound.
    /// </summary>
    /// <remarks>
    /// <b>Returns before delivery happens.</b> A sender never waits on a
    /// receiver — that is what makes a fan-out to many clusters parallel rather
    /// than a queue.
    /// </remarks>
    ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default);

    /// <summary>
    /// Put this envelope to every cluster at once.
    /// </summary>
    /// <remarks>
    /// <b>Returns who it went to</b>, because the sender has to know how many
    /// replies to expect — under a broadcast it cannot work that out from the
    /// ring, and the whole point is that it did not need an address.
    /// </remarks>
    /// <param name="envelope">The messages, one per origin code.</param>
    /// <param name="ct">Cancellation.</param>
    /// <param name="ready">
    /// Called with the clusters about to be asked, <b>before any of them is
    /// asked</b>. An origin has to record its thought inside this window: a
    /// cluster can report back before <c>BroadcastAsync</c> returns, and a
    /// report for an unknown broadcast is dropped.
    /// </param>
    ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
        Envelope envelope,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<ClusterAddress>>? ready = null);

    /// <summary>Get this report back to the machine that started the thought.</summary>
    ValueTask SendAsync(MachineAddress to, Report report, CancellationToken ct = default);

    /// <summary>
    /// A cluster left. Routes that were heading into it are never coming back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Cluster granularity, not machine.</b> A route is stranded by the
    /// departure of whatever holds its next node, and that is a cluster; a
    /// machine leaving is every one of its clusters leaving.
    /// </para>
    /// <para>
    /// <b>What a thought should DO with this is not decided</b> — see open
    /// fork 5. A thought does not track which clusters its routes are sitting
    /// in, so it cannot tell whether a given death affects it.
    /// </para>
    /// </remarks>
    event Action<ClusterAddress>? Deaths;
}
