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
    /// The sender's own marginal — how many occasions <i>it</i> fired on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>C1-LEGAL BY THE SAME ARGUMENT <see cref="Together"/> IS.</b> A node
    /// sending its own count about itself is not reading anybody else's data.
    /// This is the opposite direction from the refuted sender-weighing arm, which
    /// needed the <i>partner's</i> marginal and therefore gossip and staleness.
    /// </para>
    /// <para>
    /// <b>It exists so an edge can be weighed from either end.</b> Dividing by
    /// the receiver's marginal asks *how characteristic is where you came from,
    /// of me*; dividing by this asks *how characteristic am I, of where you came
    /// from*. Those are different questions and the walk currently only has one
    /// of them — see <see cref="Graph.Pricing"/>.
    /// </para>
    /// <para>Zero on an origin message, which has no sender.</para>
    /// </remarks>
    public double Seen { get; init; }

    /// <summary>
    /// What relation this message arrived on. <b>Meaningless on an origin, which
    /// has not travelled.</b>
    /// </summary>
    /// <remarks>
    /// <b>Carried rather than looked up</b>, by the same argument that carries
    /// <see cref="Together"/>: the sending node knows what its own entry meant,
    /// and telling the receiver costs a field where asking would cost a round
    /// trip and a C1 violation.
    /// </remarks>
    public Graph.Kind Kind { get; init; }

    /// <summary>
    /// Which relation this thought is willing to walk. <b>Null walks everything,
    /// which is every question asked before kinds existed.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE QUESTION'S CALL, AND NOT A DIAL.</b> Whether <i>follows</i> or
    /// <i>accompanies</i> is wanted is a fact about what is being asked — the same
    /// argument that moved ranking onto <see cref="Question"/>, and the standing
    /// rule that a dial wanting different values in different worlds is the
    /// recurring fault wearing a new hat.
    /// </para>
    /// <para>
    /// <b>It rides on the message because the node cannot see the question.</b>
    /// <see cref="Graph.Node.Fire"/> knows only what arrived, so the restriction
    /// is set once at the origin and copied along every hop.
    /// </para>
    /// </remarks>
    public Graph.Kind? Through { get; init; }

    /// <summary>
    /// Whether this walk prefers recently-touched entries. <b>Set once at the
    /// origin and copied along every hop</b>; see <see cref="Question.Recent"/>.
    /// </summary>
    public bool Recent { get; init; }

    /// <summary>
    /// How current this entry is, <b>in units of the sending row's own writing
    /// rhythm</b> — one for the freshest, falling toward nought for the stale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE SCALE COMES FROM THE ROW AND NOT FROM THE CLOCK, WHICH IS THE WHOLE
    /// DIFFICULTY.</b> An age in raw clock units means nothing without knowing what
    /// a unit IS — steps on one world, milliseconds on another — so any threshold
    /// over it is a dial wanting a different value per world, which is this design's
    /// recurring fault. Dividing the age by the row's OWN mean interval between
    /// writes removes the unit entirely: a node that fires constantly and one that
    /// fires rarely both read "one interval ago" the same way.
    /// </para>
    /// <para>
    /// <b>SO IT IS BUILT FROM COUNTS THE SENDER ALREADY HOLDS</b> — the row's oldest
    /// and newest stamps and how many entries there are — and it needs no dial, no
    /// signal to be found first, and nothing the world has to supply. <b>That is the
    /// shape the negative cell turned out to have</b>, arrived at deliberately this
    /// time.
    /// </para>
    /// <para>
    /// <b>One where a row has no spread to speak of</b>, which is the honest reading:
    /// entries all written at once are all equally current, and forcing a ranking
    /// over them would invent staleness that is not there. <b>Nought where
    /// <see cref="Recent"/> is false</b>, because a question that did not ask should
    /// not be carrying an answer.
    /// </para>
    /// </remarks>
    public double Fresh { get; init; }

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
