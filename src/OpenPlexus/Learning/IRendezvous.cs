using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// One moment's worth of change: what started, and what was already there.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart of <see cref="Thinking.Message"/>, which is the thinking
/// path. <b>Two kinds of traffic, and the Python conflates them</b> because one
/// process and one dictionary make them the same code path — which is how a
/// design ends up with no account of where its graph came from.
/// </para>
/// <para>
/// <b>A frame's onsets are ONE occasion, not one each.</b> They came from a
/// single observation, and an occasion is a set: everything in one moment met
/// everything else and nothing came first. Splitting them would count a pair of
/// simultaneous onsets twice.
/// </para>
/// </remarks>
public sealed record Occasion
{
    /// <summary>What just started. <b>Empty means nothing happened</b> — a
    /// stable scene is silent, and silence is the point of onsets.</summary>
    public required ImmutableArray<Code> Onsets { get; init; }

    /// <summary>What was already there when they started.</summary>
    public required ImmutableArray<Code> Live { get; init; }

    /// <summary>
    /// What had recently stopped, carried forward by the window.
    /// </summary>
    /// <remarks>
    /// <b>These join ONE WAY — the past records the future and not the
    /// reverse.</b> That asymmetry is the whole of what makes an edge temporal:
    /// a broadcast of what has just happened can walk forward to what usually
    /// follows, and a broadcast of what follows cannot walk back. Simultaneity
    /// stays symmetric, because nothing came first.
    /// </remarks>
    public ImmutableArray<Code> Recent { get; init; } = [];

    /// <summary>When, by the observing machine's own clock.</summary>
    public required long At { get; init; }

    /// <summary>
    /// Which codes belong to which thing, when the front end can say. <b>Null is
    /// today's behaviour: one moment, one group, everything pairs.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>STEP 1A — AN OCCASION STOPS BEING A FLAT SET.</b> Fork 25 measured the
    /// ceiling: two objects with swapped attributes emit the identical code set,
    /// so no amount of counting separates *red ball beside blue box* from *blue
    /// ball beside red box*. This is the smallest change that lifts it — the
    /// pairing is gated by group, so a colour joins only the shape it actually
    /// belonged to.
    /// </para>
    /// <para>
    /// <b>Pylyshyn's visual indexes and Kahneman &amp; Treisman's object files.</b>
    /// A contentless pointer assigned by attention on spatiotemporal grounds,
    /// *before* any feature is identified — which is where biology solves binding,
    /// and it is not in association cortex. <b>It is not a phase</b>: a phase is a
    /// continuous oscillator relationship measured in milliseconds, and C2 says
    /// messages are late, jittered and out of order, which is exactly what
    /// destroys one. A group travels inside the occasion and lateness cannot
    /// touch it.
    /// </para>
    /// <para>
    /// <b>A code that is absent from the map is UNGROUPED and pairs with
    /// everything</b>, which is what makes this additive: a front end that can
    /// segment some of what it sees is not forced to lie about the rest.
    /// </para>
    /// <para>
    /// <b>What it does not do:</b> the front end supplies the grouping, so this
    /// tests whether the graph can USE binding, not whether it can DISCOVER it.
    /// That is the right split — vision groups at attention — but it is not the
    /// whole problem and must not be written up as if it were.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Groups { get; init; }

    /// <summary>
    /// Which codes name THIS OCCASION rather than a kind of thing, when the
    /// front end can say. <b>Null is today's behaviour: every code recurs, and
    /// every pair is written both ways.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A FLEETING CODE IS RECORDED BY WHAT IT MET, AND DOES NOT RECORD INTO
    /// IT.</b> The pair is written one way — <c>fleeting → lasting</c> — exactly
    /// as <see cref="Recent"/> is, and for a reason of the same shape.
    /// </para>
    /// <para>
    /// <b>The reverse edge can never be worth anything, and it is not free.</b>
    /// A code minted fresh for one occasion is never seen again, so an entry for
    /// it in a lasting node's row can never accumulate past its first count and
    /// can never be evidence of anything. What it does do is sit there: the row
    /// grows by one entry per occasion FOREVER, and cost is set by the widest row
    /// — <see cref="Graph.Node.Fire"/> snapshots all of it and emits one message
    /// per surviving partner. Measured on the binding world, the widest row went
    /// from fourteen to a hundred and six between fifty scenes and eight hundred,
    /// with no sign of levelling, while the same world without indexes saturated.
    /// </para>
    /// <para>
    /// <b>The forward edge is the one the walk uses</b>, and it is untouched. A
    /// question carries the index it is asking about, so the walk STARTS there
    /// and never has to arrive at one.
    /// </para>
    /// <para>
    /// <b>A fleeting code still notes the occasion</b>, or it would carry a
    /// marginal smaller than the counts weighed against it and score above 1.0 —
    /// see <see cref="LocalRendezvous"/>.
    /// </para>
    /// </remarks>
    public IReadOnlySet<Code>? Fleeting { get; init; }

    /// <summary>
    /// What order the codes of this moment came in, when the front end can say.
    /// <b>Null is today's behaviour: nothing came first, so everything pairs
    /// symmetrically.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>STEP 6 — THE FRONT END SAYS THE ORDER, AND IT IS THE <see cref="Groups"/>
    /// TRICK AGAIN.</b> A phase cannot survive C2: an oscillator relationship
    /// measured in milliseconds is exactly what late, jittered, out-of-order
    /// messages destroy. An order that travels INSIDE the occasion cannot be
    /// touched by lateness, because by the time anything is late the ordering has
    /// already been said.
    /// </para>
    /// <para>
    /// <b>Lower comes first, and a pair with different ranks writes ONE WAY</b> —
    /// exactly as <see cref="Recent"/> does, and for the same reason: what makes
    /// an edge mean <i>then</i> rather than <i>with</i> is that the reverse is not
    /// written. Equal ranks are simultaneous and stay symmetric.
    /// </para>
    /// <para>
    /// <b>A code absent from the map is UNORDERED and pairs symmetrically with
    /// everything</b>, which is what keeps this additive: a front end that can
    /// sequence some of what it emits is not forced to lie about the rest.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<Code, int>? Sequence { get; init; }

    /// <summary>
    /// How much this occasion counts. One is something that happened.
    /// </summary>
    /// <remarks>
    /// <b>FORK 21 — below one is something the system merely concluded.</b> A
    /// thought that settles can be fed back as an occasion, which is how a route
    /// walked often enough becomes a direct edge and stops being re-derived. The
    /// discount is the whole defence against the system learning its own
    /// hallucinations: a conclusion may reinforce what it already believes, but
    /// it must never do so as fast as seeing it would.
    /// </remarks>
    public double Weight { get; init; } = 1.0;
}

/// <summary>
/// How a node learns who it fired with.
/// </summary>
/// <remarks>
/// <b>The rendezvous is where connections are formed</b>, and it is the half of
/// the design that has no implementation on <c>master</c> either.
/// </remarks>
public interface IRendezvous
{
    /// <summary>
    /// Make sure everything in this occasion ends up in the others' rows.
    /// </summary>
    /// <remarks>
    /// <b>Not onset-with-onset only.</b> A sound starting while a ball is
    /// already visible must connect, and that is the cross-modal binding the
    /// design exists for. Counted once per occasion, never per tick.
    /// </remarks>
    ValueTask JoinAsync(Occasion occasion, CancellationToken ct = default);
}
