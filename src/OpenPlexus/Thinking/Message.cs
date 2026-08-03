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
    /// How many occasions the sender and the addressee both fired on — read
    /// from the <b>sender's own row</b>.
    /// </summary>
    /// <remarks>
    /// <b>Half of the edge weight, carried so the other half never has to be
    /// fetched.</b> The receiver divides this by its own marginal, so neither
    /// node reads the other's data. Zero on an origin message, which has no
    /// sender.
    /// </remarks>
    public double Together { get; init; }

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

    /// <summary>
    /// This envelope went to every cluster, so fire only what you already hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's call on fork 6, 2026-08-02: broadcast the initial input.</b>
    /// An origin has no address by nature — <i>what is this thing I am
    /// sensing</i> cannot be routed, because you do not know what you are
    /// looking for. A hop is the opposite: a route standing on a node knows
    /// exactly which partner it walks to, so hops stay routed.
    /// </para>
    /// <para>
    /// <b>A broadcast never creates a node.</b> A routed message is addressed
    /// to a code and brings it into existence on arrival; a broadcast is a
    /// question put to everyone, and a cluster that has never seen that code
    /// has nothing to say. Admitting on a broadcast would put every code on
    /// every cluster.
    /// </para>
    /// </remarks>
    public bool Everywhere { get; init; }
}
