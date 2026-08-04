using System.Collections.Immutable;
using System.Diagnostics;
using OpenPlexus.Codes;
using OpenPlexus.Thinking;

namespace OpenPlexus.Graph;

/// <summary>
/// One code, and its own row of counts.
/// </summary>
/// <remarks>
/// <para>
/// Holds edges, holds no address, knows nothing about the network. There is no
/// list of other nodes here, no view of the graph, no total occasion count and
/// no clock shared with anyone. <b>A node knows its own row and nothing else</b>
/// — that is C1 holding, in one class.
/// </para>
/// <para>
/// <b>A connection is a count.</b> There is no edge object and no connect
/// operation anywhere in this design. An entry in <c>_together</c> going from
/// absent to 1 <i>is</i> the connection forming.
/// </para>
/// </remarks>
public sealed class Node
{
    /// <summary>This node's identity. Never changes.</summary>
    private readonly Code _code;

    /// <summary>
    /// Partner and relation, to count and clock: how many occasions that code and
    /// this one met on, and in what relation. <b>The node's whole row, and the
    /// only thing that learns.</b>
    /// </summary>
    /// <remarks>
    /// <b>IT WAS <c>Code</c> TO <c>double</c>, AND THAT NUMBER WAS DOING FOUR
    /// JOBS.</b> It ranked a partner, it priced the hop to it, it was the only
    /// memory of the pair, and it held simultaneity and sequence in one cell. The
    /// first two are split by <see cref="WalkSettings.Doubt"/>; the last two are
    /// what <see cref="Edge"/> and <see cref="Tie"/> split here.
    /// </remarks>
    private readonly Dictionary<Edge, Tie> _together = [];

    /// <inheritdoc cref="WalkSettings"/>
    private readonly WalkSettings _settings;

    /// <summary>
    /// Guards this node's own row and marginal, and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>Never held across anything that reads another node.</b> Weighing an
    /// edge from the far side would read the partner's node, so a node holding
    /// its own lock while doing that would deadlock against a partner firing
    /// back at it — which is an ordinary case, since edges are mutual.
    /// <see cref="Fire"/> takes a snapshot and releases before it weighs
    /// anything.
    /// </remarks>
    private readonly Lock _gate = new();

    /// <summary>How many occasions this node fired on at all. Its own marginal.</summary>
    private double _seen;

    /// <param name="code">This node's identity.</param>
    /// <param name="settings">
    /// The dials. Validated here so a node cannot exist holding a contradictory
    /// pair — an argument that silently does nothing is a sweep arm that looks
    /// distinct and is not.
    /// </param>
    public Node(Code code, WalkSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Stamina <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "a route with no stamina cannot take its first step");

        _code = code;
        _settings = settings;
    }

    /// <inheritdoc cref="_code"/>
    public Code Code => _code;

    /// <summary>
    /// How many occasions this node fired on. <b>Its own marginal, and the
    /// denominator of every edge weight it receives</b> — a message carries the
    /// sender's <c>together</c> and this node divides by this, so neither node
    /// ever reads the other's data.
    /// </summary>
    public double Seen
    {
        get { lock (_gate) return _seen; }
    }

    // ---- learning: these two are the entirety of what changes over time ----

    /// <summary>
    /// "I fired on this occasion." Adds to the marginal.
    /// </summary>
    /// <param name="by">
    /// How much this occasion counts. <b>One for something observed; less for
    /// something merely concluded</b> — see fork 21. A count became a weight so
    /// that a reflected occasion cannot outweigh a real one.
    /// </param>
    public void Note(double by = 1.0)
    {
        if (by <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(by),
                "an occasion worth nothing is not an occasion; it would move " +
                "`together` without moving `seen` and score the pair above 1.0");

        lock (_gate) _seen += by;
    }

    /// <summary>
    /// "That code fired on the same occasion I did." Adds one to that partner's
    /// entry, creating it if new.
    /// </summary>
    /// <remarks>
    /// <b>Writes only this node's row.</b> The partner writes its own. A node
    /// that quietly kept both directions would look identical from outside and
    /// would be holding data it does not own, which is the shared state C1
    /// forbids.
    /// </remarks>
    /// <param name="other">The code that fired alongside this one.</param>
    /// <param name="by">
    /// <b>Must match the <see cref="Note"/> that goes with it.</b> The weight is
    /// the numerator and the denominator of the same edge, so a pair written
    /// heavier than it was noted would score above 1.0 — the exact failure the
    /// forward weighting exists to prevent.
    /// </param>
    /// <param name="kind">
    /// What the entry means. <b><see cref="Kind.With"/> is every write made before
    /// kinds existed</b>, so a caller that does not say gets exactly the old
    /// behaviour and every measurement taken up to now still stands.
    /// </param>
    /// <param name="when">
    /// The observing machine's clock, for the supersession channel. <b>Zero is a
    /// caller with no clock to offer</b> and leaves the entry's stamp alone.
    /// </param>
    public void Observe(Code other, double by = 1.0, Kind kind = Kind.With, long when = 0)
    {
        if (other == _code)
            throw new ArgumentException(
                "a code cannot be its own partner; counting one would make " +
                "every statistic read its own presence as evidence", nameof(other));

        if (by <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(by),
                "a coincidence worth nothing is not a coincidence");

        var edge = new Edge(other, kind);

        lock (_gate)
            _together[edge] = _together.GetValueOrDefault(edge).Plus(by, when);
    }

    /// <summary>
    /// Reads back the whole pair, however it was met. <b>The sum across kinds,
    /// which is what the single cell used to hold.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE WALK DOES NOT USE THIS, and that is the point of it existing
    /// separately.</b> Summing is right for asking <i>how connected are these
    /// two</i> and wrong for stepping, because stepping is exactly where
    /// <i>follows</i> must not be added to <i>accompanies</i>.
    /// </remarks>
    public double Together(Code other)
    {
        lock (_gate)
            return _together
                .Where(entry => entry.Key.Partner == other)
                .Sum(entry => entry.Value.Count);
    }

    /// <summary>Reads back one cell of the row.</summary>
    public double Together(Code other, Kind kind)
    {
        lock (_gate) return _together.GetValueOrDefault(new Edge(other, kind)).Count;
    }

    /// <summary>
    /// When this cell was last written, by the observing machine's own clock.
    /// <b>The supersession channel, and nothing ranks by it yet.</b>
    /// </summary>
    public long When(Code other, Kind kind = Kind.With)
    {
        lock (_gate) return _together.GetValueOrDefault(new Edge(other, kind)).When;
    }

    /// <summary>
    /// Every code this node has ever met. The fan-out of one hop, in <b>distinct
    /// partners</b>.
    /// </summary>
    public IReadOnlyCollection<Code> Partners()
    {
        lock (_gate) return [.. _together.Keys.Select(edge => edge.Partner).Distinct()];
    }

    /// <summary>
    /// How many entries the row holds. <b>THE COST, as against
    /// <see cref="Partners"/>'s count</b> — <see cref="Fire"/> emits one message
    /// per ENTRY, so a partner met in two relations is two messages and the
    /// scaling wall is built of these and not of distinct codes.
    /// </summary>
    public int Entries
    {
        get { lock (_gate) return _together.Count; }
    }

    /// <summary>
    /// How many entries of one relation the row holds. <b>Zero <see cref="Kind.After"/>
    /// means nothing temporal was ever written</b>, which tells an arm that did
    /// nothing apart from an arm that was never connected.
    /// </summary>
    public int Entered(Kind kind)
    {
        lock (_gate) return _together.Keys.Count(edge => edge.Kind == kind);
    }

    // ---- thinking ----------------------------------------------------------

    /// <summary>
    /// A message arrived. Work out what should be sent next.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In order: weigh every partner; price the step once for this node; drop
    /// partners with zero weight and partners <b>already in the arriving
    /// chain</b>; for each survivor work out <c>held - price + fuel</c> and
    /// drop it if that is not positive; build one outgoing message per survivor
    /// with the partner appended to the chain and the strength multiplied by
    /// the edge weight; report <c>k-1</c> splits if <c>k</c> survived, or one
    /// death if none did.
    /// </para>
    /// <para>
    /// <b>Returns rather than sends.</b> See <see cref="Fired"/>.
    /// </para>
    /// <para>
    /// <b>A chain ends with the node the message is addressed to</b>, so the
    /// receiver is already in it when this runs. That is what makes the cycle
    /// check free — the partner is refused if it appears anywhere in the chain
    /// already being carried.
    /// </para>
    /// </remarks>
    public Fired Fire(Message message)
    {
        if (message.To != _code)
            throw new ArgumentException(
                $"message addressed to {message.To} reached the node for {_code}",
                nameof(message));

        // SNAPSHOT FIRST. Nothing here reads another node, so there is no
        // deadlock to avoid any more -- but the row must not move underneath a
        // fan-out, and two thoughts can fire this node at once.
        KeyValuePair<Edge, Tie>[] row;
        double seen;
        lock (_gate)
        {
            row = [.. _together];
            seen = _seen;
        }

        // An origin message has not travelled, so nothing arrived here and
        // there is no edge to value. Its strength is the starting 1.0.
        var isOrigin = message.Chain.Length <= 1;

        var held = message.Held;
        var arriving = 1.0;

        // What the edge is WORTH, as against what it costs. The two are the
        // same number until Doubt says otherwise.
        var believed = 1.0;

        if (!isOrigin)
        {
            // THE RECEIVER WEIGHS THE EDGE IT ARRIVED ON, and chooses WHICH
            // MARGINAL to divide by. The sender put its own together(sender, me)
            // and its own seen in the message, so either end can weigh it and
            // neither node reads the other's data. See Pricing.
            var by = _settings.Pricing == Pricing.Sender ? message.Seen : seen;

            arriving = by <= 0.0 ? 0.0 : message.Together / by;
            if (arriving <= 0.0) return Died(message, message.Carried);

            // A WEIGHT CANNOT EXCEED 1.0 UNDER EITHER PRICING, because
            // together(a, b) never exceeds either marginal -- which is what
            // makes the hop cost at least 1 and the walk bounded by
            // construction. Clamped rather than trusted: a partial row written
            // under a moved ring view could break it, and an unbounded walk is
            // the one failure that takes the process with it.
            arriving = Math.Min(arriving, 1.0);

            // SHRINKAGE APPLIES TO THE SCORE AND NOT TO THE PRICE, and measured,
            // that separation is the whole of whether it works. One weight was
            // doing both jobs -- it ranks a partner AND it says what the hop
            // costs -- so pulling a thin edge's ratio down also made every hop
            // dearer, the walk starved before it could compose, and the senses
            // world fell from most questions right to almost none.
            //
            // The price below still comes from the raw ratio, so a hop still
            // costs at least one and the walk stays bounded by construction.
            // What moves is only how much a thin partner is BELIEVED.
            believed = _settings.Doubt <= 0.0
                ? arriving
                : Math.Min(message.Together / (by + _settings.Doubt), 1.0);

            // EVERY HOP COSTS AT LEAST 1, because a weight cannot exceed 1.0.
            // That is what bounds the walk.
            held -= 1.0 / arriving;

            // COULD NOT AFFORD THE HOP IT WAS ALREADY TAKING. Starvation, not
            // exhaustion of the graph -- see Accounting.Starved.
            if (held <= 0.0) return Starved(message, message.Carried);
        }

        var travelled = message.Carried * believed;

        var carried = travelled;

        var reached = isOrigin
            ? null
            : new Arrival
            {
                Endpoint = _code,
                Score = carried,
                Chain = message.Chain,
                Best = carried,
                Routes = 1,
            };

        // A SENDER CAN STILL PRUNE EXACTLY ONCE, needing nothing from anyone:
        // no hop costs less than 1, so a budget of 1 or less affords nothing.
        if (held <= 1.0) return Spent(message, reached, carried);

        // THE HORIZON, a backstop that has not fired since the cost became
        // inverse -- see WalkSettings.Horizon.
        if (message.Chain.Length >= _settings.Horizon)
        {
            return new Fired
            {
                Outgoing = [],
                Reached = reached,
                Accounting = new Accounting(
                    message.Broadcast, 0, Deaths: 1, Halted: 1, Ended: carried),
            };
        }

        var outgoing = ImmutableArray.CreateBuilder<Message>(row.Length);

        // THE NEGATIVE HALF OF THE PN-COUNTER, READ ONCE FOR THE WHOLE FAN-OUT.
        // Built from the snapshot rather than looked up per partner, so inhibition
        // costs one pass over a row that has already been copied. See
        // `Kind.Hindered`.
        Dictionary<Code, double>? against = null;

        foreach (var (edge, tie) in row)
        {
            if (edge.Kind != Kind.Hindered) continue;

            against ??= [];
            against[edge.Partner] = against.GetValueOrDefault(edge.Partner) + tie.Count;
        }

        foreach (var (edge, tie) in row)
        {
            // A CELL WITH A NEGATIVE COUNTERPART IS READ AS THE DIFFERENCE, and
            // the counterpart itself is never walked -- it is evidence about the
            // positive cell, not a route.
            if (edge.Kind == Kind.Hindered) continue;

            var carrying = tie.Count;

            if (edge.Kind == Kind.Helped && against is not null
                && against.TryGetValue(edge.Partner, out var hurt))
            {
                // CLAMPED, BECAUSE A NEGATIVE WEIGHT BREAKS THE BOUND that makes a
                // hop cost at least one and the walk terminate. At or below
                // nought the partner is not walked at all -- which is the whole of
                // what inhibition buys: an edge that says `not that one`.
                carrying -= hurt;
                if (carrying <= 0.0) continue;
            }

            // The cycle check: free, because the chain is already travelling.
            // ON THE PARTNER AND NOT ON THE ENTRY: a route that reached B by
            // accompaniment must not reach it again by sequence, or one node
            // appears twice in a chain that exists to say where the route has
            // been.
            if (message.Chain.Contains(edge.Partner)) continue;

            // WHAT THE QUESTION WILL WALK, and null is every question asked
            // before kinds existed. A question about what FOLLOWS should not be
            // ranking things that merely accompany -- which is the whole of why
            // a deeper walk for prediction was monotonically worse.
            if (message.Through is { } only && edge.Kind != only) continue;

            outgoing.Add(message with
            {
                To = edge.Partner,
                Held = held,
                Chain = message.Chain.Add(edge.Partner),
                Carried = carried,
                Together = carrying,

                // WHAT IT ARRIVED ON, carried so the far end knows what it is
                // holding without reading anything it does not own.
                Kind = edge.Kind,

                // THIS NODE'S OWN COUNT, ABOUT ITSELF. The receiver may divide
                // by it instead of by its own -- see Pricing.
                Seen = seen,
            });
        }

        var children = outgoing.Count;

        return new Fired
        {
            Outgoing = outgoing.ToImmutable(),
            Reached = reached,

            // One route became `children` routes, so the live count moves by
            // the difference -- a split is not the birth of `children` new ones.
            Accounting = new Accounting(
                message.Broadcast,
                Splits: children > 0 ? children - 1 : 0,
                Deaths: children == 0 ? 1 : 0,

                // NOT THWARTED: it went everywhere there was to go. Its strength
                // is still booked, because the ratio needs a denominator that
                // includes the healthy endings or it is 1.0 by construction.
                Ended: children == 0 ? carried : 0.0),
        };
    }

    /// <summary>
    /// A route that arrived and cannot afford to go further. <b>NOT starvation.</b>
    /// </summary>
    /// <remarks>
    /// <b>MEASURED, AND COUNTING THIS AS STARVATION BROKE THE SIGNAL.</b> Inverse
    /// cost exists to exhaust the budget — running out is how a walk is bounded,
    /// so it is the normal way nearly every route ends. A route here <i>arrived</i>
    /// and produced its arrival; it is finished, not thwarted. Marking it hungry
    /// made <see cref="Thinking.Thought.Hunger"/> high on every walk, so the
    /// adaptive weight was the fixed one wearing a disguise: 0.7788 against
    /// off's 0.8333 where it should have written nothing at all.
    /// </remarks>
    private static Fired Spent(Message message, Arrival? reached, double carried) => new()
    {
        Outgoing = [],
        Reached = reached,

        // A BUDGET DEATH, AND ITS STRENGTH IS THE WHOLE POINT. It arrived and
        // could not go on; whether that mattered depends entirely on how much
        // promise it still held -- see the note on Accounting.Thwarted.
        Accounting = new Accounting(
            message.Broadcast, 0, Deaths: 1, Thwarted: carried, Ended: carried),
    };

    /// <summary>
    /// A route that could not pay for the hop it was on. <b>Starvation</b>, and
    /// it reaches nowhere at all — it did not survive the arrival.
    /// </summary>
    private static Fired Starved(Message message, double carried) => new()
    {
        Outgoing = [],
        Reached = null,
        Accounting = new Accounting(
            message.Broadcast, 0, Deaths: 1, Starved: 1,
            Thwarted: carried, Ended: carried),
    };

    /// <summary>
    /// A route that could not go on from here because there was <b>nowhere to
    /// go</b>, not because it was broke. The edge it arrived on weighs nothing.
    /// </summary>
    private static Fired Died(Message message, double carried) => new()
    {
        Outgoing = [],
        Reached = null,

        // NOT THWARTED: there was no edge to walk, so no budget would have
        // helped it.
        Accounting = new Accounting(message.Broadcast, 0, Deaths: 1, Ended: carried),
    };
}
