using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Thinking;

/// <summary>
/// What reached an endpoint, and the strongest chain that got there.
/// </summary>
/// <remarks>
/// <see cref="Score"/> and <see cref="Best"/> are separate because under
/// <see cref="Graph.Accumulate.Sum"/> the score is not any single route's
/// strength, so it cannot be used to decide which chain to keep as the
/// explanation.
/// </remarks>
public sealed record Arrival
{
    public required Code Endpoint { get; init; }

    /// <summary>What the accumulator made of every route reaching here.</summary>
    public required double Score { get; init; }

    /// <summary>
    /// The strongest single chain. <b>Not the last one to arrive</b>, which
    /// would make the explanation whichever branch happened to finish last.
    /// </summary>
    public required ImmutableArray<Code> Chain { get; init; }

    /// <summary>The strength of that strongest single route.</summary>
    public required double Best { get; init; }

    /// <summary>How many routes reached here.</summary>
    public required int Routes { get; init; }
}

/// <summary>
/// The termination accounting for one firing.
/// </summary>
/// <remarks>
/// <para>
/// Dijkstra-Scholten's shape: a route splitting into <c>k</c> reports
/// <c>k-1</c> splits, a route dying reports one death, and the origin knows the
/// thought is over when the live count reaches zero.
/// </para>
/// <para>
/// <b>This must not be merged with the budget.</b> Merging them is the obvious
/// economy and it breaks: termination detection works precisely because total
/// weight is conserved, and a refuelled budget creates weight from nothing.
/// </para>
/// </remarks>
/// <param name="Starved">
/// Of those deaths, how many were routes that <b>ran out of budget</b> rather
/// than out of anywhere to go.
/// </param>
/// <remarks>
/// <b><paramref name="Starved"/> IS THE SIGNAL THAT MAKES COMPRESSION SELF-REGULATING
/// — John's ask, 2026-08-02: fewer knobs, more things that find their own
/// level.</b> A route that dies holding too little to take another hop is the
/// walk saying *I could not afford to get there*, and that is precisely the
/// condition under which minting a shortcut helps — measured at 0.1827 → 0.7147.
/// A route that dies having run out of partners is saying *I went everywhere
/// there was to go*, and there compression only adds edges to rank against —
/// measured at 0.8462 → 0.7596.
/// <para>
/// <b>It costs nothing to know.</b> The node already distinguishes the two cases
/// to decide whether to fan out; this only reports which one happened. Nothing
/// is consulted, nothing is shared, and no node learns anything about another.
/// </para>
/// </remarks>
/// <param name="Thwarted">
/// The strength routes killed BY THE BUDGET still carried when they died.
/// </param>
/// <param name="Ended">
/// The strength carried by every route that died here, whatever killed it.
/// </param>
/// <remarks>
/// <b>JOHN'S CORRECTION, 2026-08-02, AND IT IS THE ONE THAT MATTERS.</b> Counting
/// budget deaths does not discriminate, because inverse cost exists to exhaust
/// the budget and running out is how nearly every route ends — see fork 23. What
/// differs is HOW MUCH PROMISE A ROUTE STILL HELD when the money ran out.
/// <para>
/// Carried strength decays multiplicatively with every hop, so a route that dies
/// broke after one hop dies <b>strong</b> — it was on a good path and got cut
/// off — while one that dies broke after four dies <b>weak</b>, and the budget
/// merely finished what decay had already done. <b>Same event, opposite
/// meanings, and a count throws the difference away.</b>
/// </para>
/// </remarks>
/// <param name="Broadcast">Which thought this is about. Mixing two thoughts'
/// counts is what the id exists to prevent.</param>
/// <param name="Splits">Routes created beyond the one that arrived — <c>k-1</c>
/// for a fan-out of <c>k</c>.</param>
/// <param name="Deaths">Routes that ended here.</param>
/// <param name="Halted">Of the deaths, how many hit the horizon rather than
/// economics. Reported separately, or the constant would hide.</param>
/// <param name="Pruned">
/// How many partners a node's own width refused — <b>the reporting half of the
/// refuted beam's revival condition</b>, which asked for a width the system sets
/// itself AND reports. Without it the cut is invisible, and a walk quietly
/// starved by its own threshold reads as a walk that ran out of graph.
/// </param>
public readonly record struct Accounting(
    BroadcastId Broadcast,
    int Splits,
    int Deaths,
    int Halted = 0,
    int Starved = 0,
    double Thwarted = 0.0,
    double Ended = 0.0,
    int Pruned = 0);

/// <summary>
/// What a node hands back when it fires.
/// </summary>
/// <remarks>
/// <b>A node returns what should be sent rather than sending it.</b> That makes
/// a node testable with no bus, no cluster and no network — perturb the row and
/// assert the outgoing set moves. A node that sends for itself can only be
/// tested end to end, which is the arrangement where a thing turns out never to
/// have been wired up.
/// </remarks>
public readonly record struct Fired
{
    /// <summary>One message per surviving partner. The cluster batches and sends these.</summary>
    public required ImmutableArray<Message> Outgoing { get; init; }

    /// <summary>What reached this node, to be reported to the thought's origin.</summary>
    public required Arrival? Reached { get; init; }

    /// <inheritdoc cref="Accounting"/>
    public required Accounting Accounting { get; init; }
}
