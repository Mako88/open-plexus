using System.Collections.Immutable;
using OpenPlexus.Bus;

namespace OpenPlexus.Thinking;

/// <summary>
/// A thought that has finished, published to whoever registered an interest in
/// what it reached — <b>fork 11.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE OUTPUT MACHINE WAS NOT ADDRESSED, AND THAT IS WHY NO SECOND ONE COULD
/// EXIST.</b> <see cref="Message.ReturnTo"/> is always the machine that asked, and
/// the harness handed the finished <see cref="Thought"/> to an output machine by
/// direct call — so acting required holding the thought object, which only the
/// asker ever holds. Several output machines acting at once was not a feature
/// nobody had written; it was not expressible.
/// </para>
/// <para>
/// <b>IT IS PUBLISHED AFTER SETTLEMENT, AND THAT IS THE WHOLE OF WHY THIS SHAPE
/// AND NOT ANOTHER.</b> A listener subscribed to raw <see cref="Report"/>s would
/// have to decide for itself when the walk had finished, which means either a
/// second copy of the settle loop — the drift `DuplicationTests` exists to catch
/// — or reading a question before its walk was done, which is fork 22's trap and
/// made every number taken under one load incomparable with any other. <b>The
/// machine that owns the thought already knows.</b> So it says.
/// </para>
/// <para>
/// <b>THE BUS ROUTES IT AND NOTHING ELSE KNOWS WHO IS LISTENING.</b> A node
/// cannot know which codes mean an action — the graph must be able to have an
/// arbitrary actuator attached without knowing what it is — and the asker must
/// not have to hold a list of output machines, because that is a coordinator.
/// Interest is registered by CODE, so the routing table lives where every other
/// routing table lives.
/// </para>
/// </remarks>
public sealed record Settled
{
    /// <summary>Which thought finished.</summary>
    public required BroadcastId Broadcast { get; init; }

    /// <summary>Where it was asked from, for anything that wants to answer back.</summary>
    public required MachineAddress From { get; init; }

    /// <summary>
    /// What it reached, already accumulated and ranked. <b>Not one arrival per
    /// route</b> — the folding is the asker's and is done.
    /// </summary>
    public required ImmutableArray<Arrival> Arrivals { get; init; }
}
