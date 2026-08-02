using System.Collections.Immutable;
using OpenPlexus.Bus;

namespace OpenPlexus.Thinking;

/// <summary>How many routes were sent into one cluster.</summary>
public readonly record struct Routed(ClusterAddress To, int Count);

/// <summary>
/// What a cluster sends back to the machine that started a thought.
/// </summary>
/// <remarks>
/// The return path of the thinking loop, batched the same way an
/// <see cref="Envelope"/> batches the outbound one: everything one cluster owes
/// one machine for one broadcast, in a single send.
/// </remarks>
public sealed record Report
{
    /// <summary>Which cluster is reporting. <b>John's design, 2026-08-02.</b></summary>
    public required ClusterAddress From { get; init; }

    /// <summary>What reached nodes in the sending cluster.</summary>
    public required ImmutableArray<Arrival> Arrivals { get; init; }

    /// <summary>
    /// How many of this thought's routes this cluster took delivery of.
    /// </summary>
    /// <remarks>
    /// Those routes are no longer in flight toward this cluster, so the origin
    /// subtracts them. <b>Where routes are GOING, not where they have been</b> —
    /// a route that passed through a cluster and moved on is not stranded when
    /// that cluster dies.
    /// </remarks>
    public required int Handled { get; init; }

    /// <summary>
    /// Where this cluster sent the routes it created, and how many to each.
    /// </summary>
    /// <remarks>
    /// <b>This is what makes a death event actionable.</b> A node knows which
    /// cluster it is in, so it can say not only that it forked but where the
    /// forks went. The origin keeps a live count per cluster and, when the bus
    /// reports that cluster gone, knows exactly how many routes it just lost —
    /// instead of being unable to tell whether the death concerned it at all.
    /// </remarks>
    public required ImmutableArray<Routed> SentInto { get; init; }

    /// <summary>
    /// The termination arithmetic, summed over every node that fired. Carries
    /// its own broadcast id, so a report for another thought is refused rather
    /// than folded in.
    /// </summary>
    public required Accounting Accounting { get; init; }
}
