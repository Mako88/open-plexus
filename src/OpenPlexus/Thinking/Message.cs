using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;

namespace OpenPlexus.Thinking;

/// <summary>Which thought a message belongs to.</summary>
/// <remarks>
/// <b>Without this, two thoughts in flight mix their chains and their death
/// counts.</b> The Python has no equivalent and records that as a known gap.
/// Under continuous input there are always many thoughts in flight, so this is
/// not optional.
/// </remarks>
public readonly record struct BroadcastId(Guid Value)
{
    /// <summary>
    /// A fresh id, minted without asking anyone.
    /// </summary>
    /// <remarks>
    /// <b>C1 forbids a counter.</b> Any shared sequence would need every
    /// machine to agree on what comes next, so this is a value large enough
    /// that independent machines do not collide by accident.
    /// </remarks>
    public static BroadcastId New() => new(Guid.NewGuid());
}

/// <summary>
/// What travels on the thinking path.
/// </summary>
/// <remarks>
/// This is the Python's frontier tuple <c>(here, held, chain, carried)</c> plus
/// the two fields it could afford to leave out in one process, because nothing
/// ever left and nothing had to come back.
/// </remarks>
public readonly record struct Message
{
    /// <inheritdoc cref="BroadcastId"/>
    public required BroadcastId Broadcast { get; init; }

    /// <summary>Where arrivals and death reports go.</summary>
    public required MachineAddress ReturnTo { get; init; }

    /// <summary>The code this message is addressed to.</summary>
    public required Code To { get; init; }

    /// <summary>Budget remaining. The fuel, as against <see cref="Carried"/>.</summary>
    public required double Held { get; init; }

    /// <summary>
    /// Every node walked, in order. <b>The cycle check and the explanation in
    /// one field</b> — a route may not revisit a node already in its own chain,
    /// which is a local check costing nothing because the chain is already
    /// being carried, and it is what makes an unbounded walk terminate on a
    /// cyclic graph.
    /// </summary>
    public required ImmutableArray<Code> Chain { get; init; }

    /// <summary>Accumulated path strength. The score, as against <see cref="Held"/>.</summary>
    public required double Carried { get; init; }
}

/// <summary>
/// Many messages for one cluster, sent as one. The unit the wire carries.
/// </summary>
/// <remarks>
/// <b>Where the message economy lives.</b> A node forking to 200 partners
/// spread over 12 clusters produces 12 envelopes, not 200 messages. Wire cost
/// scales with distinct clusters reached, never with nodes reached.
/// </remarks>
public sealed record Envelope
{
    public required ClusterAddress To { get; init; }
    public required ImmutableArray<Message> Messages { get; init; }
}
