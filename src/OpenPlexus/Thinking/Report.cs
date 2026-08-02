using System.Collections.Immutable;

namespace OpenPlexus.Thinking;

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
    /// <summary>What reached nodes in the sending cluster.</summary>
    public required ImmutableArray<Arrival> Arrivals { get; init; }

    /// <summary>
    /// The termination arithmetic, summed over every node that fired. Carries
    /// its own broadcast id, so a report for another thought is refused rather
    /// than folded in.
    /// </summary>
    public required Accounting Accounting { get; init; }
}
