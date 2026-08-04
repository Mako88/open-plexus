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
    /// last two are what <see cref="Edge"/> and <see cref="Tie"/> split here.
    /// <para>
    /// <b>THE FIRST TWO TOOK TWO GOES.</b> <see cref="WalkSettings.Doubt"/> split
    /// the ARITHMETIC — what an edge is believed against what it costs — and left
    /// both reading the same statistic, so evidence still set the price.
    /// <see cref="Toll"/> splits the STATISTIC, and with it on, this row does one
    /// job.
    /// </para>
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

    /// <summary>
    /// How much of that marginal was of each relation — <b>the base rate a hit
    /// rate cannot see.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>IT IS NOT THE ROW AND THAT IS THE WHOLE OF WHY IT IS AFFORDABLE.</b> This
    /// is keyed by RELATION and not by partner, so it is bounded by how many
    /// relations exist and never by how many codes this node has met.
    /// <see cref="Tie"/> names the row as the scaling wall; this sits beside it and
    /// does not widen it.
    /// </para>
    /// <para>
    /// <b>Every entry only ever rises</b>, so the G-Counter property is untouched —
    /// and the quantity read off it, <see cref="Contingency"/>, can still fall. That
    /// is the same argument that made <see cref="Kind.Hindered"/> legal.
    /// </para>
    /// </remarks>
    private readonly Dictionary<Kind, double> _noted = [];

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
    /// <param name="of">
    /// Which relation this occasion was about. <b><see cref="Seen"/> counts it
    /// either way</b>, so nothing already measured moves — what this adds is a
    /// second tally, split by relation, which is what a base rate is made of.
    /// <b>Null is a caller with nothing to say</b>, and leaves the split alone.
    /// </param>
    public void Note(double by = 1.0, Kind? of = null)
    {
        if (by <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(by),
                "an occasion worth nothing is not an occasion; it would move " +
                "`together` without moving `seen` and score the pair above 1.0");

        lock (_gate)
        {
            _seen += by;

            if (of is { } relation)
                _noted[relation] = _noted.GetValueOrDefault(relation) + by;
        }
    }

    /// <summary>
    /// How much of this node's marginal was of one relation.
    /// </summary>
    /// <remarks>
    /// <b>THE DENOMINATOR AND THE NUMERATOR OF THE BASE RATE.</b>
    /// <c>Noted(With)</c> is how many ordinary occasions this node was in;
    /// <c>Noted(Helped)</c> is how many of them were followed by something getting
    /// better. Neither is a fact about any partner, which is what makes the
    /// contrast below cost nothing per edge.
    /// </remarks>
    public double Noted(Kind of)
    {
        lock (_gate) return _noted.GetValueOrDefault(of);
    }

    /// <summary>
    /// How much taking one act CHANGES the chance that things get better —
    /// <b>ΔP, and the thing a co-occurrence count structurally could not say.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Kind.Helped"/> IS A HIT RATE AND THIS IS A CONTINGENCY.</b>
    /// <c>helped / taken</c> answers <i>when I did this, how often did things
    /// improve</i>, which sounds like the question and is not: an act that helps
    /// six times in ten looks strong until you notice things improved six times in
    /// ten anyway. Rescorla and Wagner's quantity is
    /// <c>P(better | act) − P(better | ¬act)</c>, and the second term is what
    /// nothing here recorded.
    /// </para>
    /// <para>
    /// <b>THE BASE RATE HAS TO VARY BY STATE OR IT CANNOT RE-RANK ANYTHING.</b> A
    /// world-level rate — <see cref="Learning.Drives"/>'s, say — subtracts the same
    /// number from every candidate, and an ordering is unchanged by subtracting a
    /// constant from all of it. So it is read from THIS node's own tallies, and the
    /// complement term still moves per act because both its parts do.
    /// </para>
    /// <para>
    /// <b>C1 IS UNTOUCHED AND THAT IS NOT A COINCIDENCE.</b> All four numbers —
    /// the pair's two cells and this node's two marginals — are things this node
    /// owns, so the contrast is computed where the fan-out already happens and
    /// nothing reads another node's data.
    /// </para>
    /// <para>
    /// <b>NOUGHT WHERE THERE IS NO CONTRAST TO DRAW</b>: an act never taken here,
    /// or one taken on every occasion this node ever had, leaves one side of the
    /// subtraction with an empty denominator. <b>That is a reading of "nothing to
    /// say" and not of "no effect"</b>, and the two are indistinguishable in the
    /// number — which is why silence is reported beside a score and not folded
    /// into it.
    /// </para>
    /// </remarks>
    /// <param name="act">The partner whose contribution is being asked about.</param>
    /// <returns>
    /// The difference of the two rates, in -1..1. <b>Negative is inhibition</b> —
    /// things went better without this act than with it.
    /// </returns>
    public double Contingency(Code act)
    {
        lock (_gate)
            return Contrast(
                taken: _together.GetValueOrDefault(new Edge(act, Kind.With)).Count,
                helped: _together.GetValueOrDefault(new Edge(act, Kind.Helped)).Count,
                occasions: _noted.GetValueOrDefault(Kind.With),
                improved: _noted.GetValueOrDefault(Kind.Helped));
    }

    /// <summary>
    /// The arithmetic itself, over four numbers one node owns.
    /// </summary>
    /// <remarks>
    /// <b>ONE COPY, TWO CALLERS, AND THE SECOND IS WHY IT IS SEPARATE.</b>
    /// <see cref="Contingency"/> looks the four up under the lock;
    /// <see cref="Fire"/> reads them off the snapshot it has already taken, because
    /// a fan-out must not re-enter the lock once per partner. Writing the
    /// subtraction twice is the duplication <c>DuplicationTests</c> exists to
    /// refuse.
    /// </remarks>
    private static double Contrast(
        double taken, double helped, double occasions, double improved)
    {
        var without = occasions - taken;

        if (taken <= 0.0 || without <= 0.0) return 0.0;

        return (helped / taken) - ((improved - helped) / without);
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
    /// What the entry means. <b>Null is <see cref="Kind.With"/>, which is every
    /// write made before kinds existed</b>, so a caller that does not say gets
    /// exactly the old behaviour and every measurement taken up to now still
    /// stands. <b>It is nullable rather than defaulted because a kind stopped being
    /// an enum</b> — a derived name is not a compile-time constant, and the
    /// alternative was a zero value masquerading as a relation.
    /// </param>
    /// <param name="when">
    /// The observing machine's clock, for the supersession channel. <b>Zero is a
    /// caller with no clock to offer</b> and leaves the entry's stamp alone.
    /// </param>
    public void Observe(Code other, double by = 1.0, Kind? kind = null, long when = 0)
    {
        if (other == _code)
            throw new ArgumentException(
                "a code cannot be its own partner; counting one would make " +
                "every statistic read its own presence as evidence", nameof(other));

        if (by <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(by),
                "a coincidence worth nothing is not a coincidence");

        var edge = new Edge(other, kind ?? Kind.With);

        lock (_gate)
        {
            _together[edge] = _together.GetValueOrDefault(edge).Plus(by, when);

            Evict();
        }
    }

    /// <summary>
    /// Drops the least recently touched entries until the row fits its cap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NOT TOUCHED SINCE, NEVER A COUNT ERODED.</b> Halving a count to make room
    /// would break the G-Counter property and with it the convergence the whole
    /// coordination-free design rests on. Removing an entry does not: the number was
    /// never revised downward, it stopped being resident. <b>That is the same
    /// distinction cold storage would rest on</b>, and it is why this is expressible
    /// where decay is not.
    /// </para>
    /// <para>
    /// <b>THE TIE-BREAK IS NOT COSMETIC.</b> Every pair written in one occasion
    /// shares a clock exactly, so on a full row the entries competing to be dropped
    /// are usually all stamped the same — and picking among them by dictionary order
    /// would make a fixed seed stop reproducing its run, which is fork 12. The key's
    /// own order settles it.
    /// </para>
    /// <para>
    /// <b>An entry written by a caller with no clock to offer is stamped nought</b>,
    /// so it is evicted first. That is the honest ordering — nothing is known about
    /// when it was touched — and it is why a bounded row and a clockless caller do
    /// not belong together.
    /// </para>
    /// </remarks>
    private void Evict()
    {
        if (_settings.Row is not { } cap) return;

        // ONE PASS PER ENTRY DROPPED, AND A WRITE DROPS AT MOST ONE. Sorting the
        // row would be the obvious way and it is the wrong complexity for the one
        // structure this exists to make cheap: a sort on every write past the cap
        // is worse than the unbounded row it replaces. **This is still linear in
        // the cap** — the plan names Space-Saving as what a real bound uses, and
        // that stays the honest thing to reach for if the cap ever gets large.
        while (_together.Count > cap)
        {
            var worst = default(KeyValuePair<Edge, Tie>);
            var found = false;

            foreach (var entry in _together)
            {
                if (found && !Older(entry, worst)) continue;

                worst = entry;
                found = true;
            }

            _together.Remove(worst.Key);
        }
    }

    /// <summary>Which of two entries a bounded row gives up first.</summary>
    private static bool Older(KeyValuePair<Edge, Tie> one, KeyValuePair<Edge, Tie> than)
    {
        if (one.Value.When != than.Value.When) return one.Value.When < than.Value.When;

        var partner = one.Key.Partner.CompareTo(than.Key.Partner);

        return partner != 0
            ? partner < 0
            : one.Key.Kind.CompareTo(than.Key.Kind) < 0;
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
    public long When(Code other, Kind? kind = null)
    {
        lock (_gate) return _together.GetValueOrDefault(new Edge(other, kind ?? Kind.With)).When;
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
        double occasions;
        double improved;
        lock (_gate)
        {
            row = [.. _together];
            seen = _seen;

            // THE BASE RATE, TAKEN IN THE SAME BREATH AS THE ROW. Two scalars, and
            // reading them here rather than per partner is what keeps a contrastive
            // fan-out the same number of lock acquisitions as an ordinary one.
            occasions = _noted.GetValueOrDefault(Kind.With);
            improved = _noted.GetValueOrDefault(Kind.Helped);
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

            // AND THE NEGATIVE HALF DISCOUNTS THE SCORE, ON THE SAME SIDE OF THAT
            // LINE AND FOR THE SAME REASON. `helped / (helped + hindered)` is at
            // most one, so a partner that hurt more than it helped is believed
            // less and is never made harder to REACH -- the price above still
            // comes from the raw ratio, so a hop still costs at least one and the
            // walk stays bounded. See Message.Against and Kind.Hindered.
            if (!message.Unheeding && message.Against > 0.0 && message.Together > 0.0)
                believed *= message.Together / (message.Together + message.Against);

            // AND THE BASE RATE DISCOUNTS THE SCORE, ON THAT SAME SIDE OF THE LINE
            // AND FOR THE FIFTH TIME. ΔP is at most one, so this can only ever
            // believe a partner LESS -- it never makes one cheaper or dearer to
            // reach, and the price above still comes from the raw ratio.
            //
            // CLAMPED AT NOUGHT, WHICH IS WHERE THE INHIBITION IS. An act that does
            // no better than standing aside is believed nothing at all rather than
            // believed negatively; a negative score would invert the ranking it was
            // meant to lower. See Kind.Hindered for the same clamp and the same
            // reason.
            if (message.Contrasted)
                believed *= Math.Max(0.0, message.Contrast);

            // AND RECENCY DISCOUNTS THE SCORE, ON THE SAME SIDE OF THE LINE AS
            // EVERY OTHER THING THAT ARGUES ABOUT AN EDGE RATHER THAN PRICING IT.
            // A stale entry is believed less and is never made harder to REACH:
            // nothing here decays, so a superseded count still stands at whatever
            // it reached, and this is the only way a walk can prefer what is still
            // true. See Question.Recent.
            if (message.Recent) believed *= message.Fresh;

            // EVERY HOP COSTS AT LEAST 1, AND UNDER BOTH TOLLS. Under `Evidence`
            // because a weight cannot exceed 1.0; under `Traffic` because a row
            // with a single entry still costs the one that is added to the log.
            // That is what bounds the walk -- see Toll.
            //
            // AND THE TRAFFIC TOLL READS THIS NODE'S OWN SNAPSHOT AND NOTHING
            // ELSE. It is the width of the row the route just landed in, which is
            // exactly how many messages the fan-out below can emit, so the budget
            // is denominated in the thing it is actually spent on. `Entries`
            // rather than `Partners` for the same reason that property says: a
            // partner met in two relations is two messages.
            held -= _settings.Toll == Toll.Traffic
                ? 1.0 + Math.Log2(Math.Max(row.Length, 1))
                : 1.0 / arriving;

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

        // WHICH RELATION THIS HOP MAY TAKE, WHEN THE QUESTION NAMED A PATH. The
        // position is read off the chain rather than carried: a chain begins at
        // the origin and ends at the node being fired, so its length minus one is
        // how many hops have already been taken. A carried counter would be the
        // same fact twice, free to disagree with the chain under C2.
        //
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

        // HOW FAST THIS ROW IS WRITTEN, IN ITS OWN CLOCK. The span between its
        // oldest and newest stamps, divided by how many entries there are, is the
        // mean gap between writes -- and dividing an age by that removes the clock's
        // units, which is what stops a recency preference being a dial wanting a
        // different value in every world. See Message.Fresh.
        var newest = 0L;
        var oldest = long.MaxValue;

        if (message.Recent)
            foreach (var (_, tie) in row)
            {
                newest = Math.Max(newest, tie.When);
                oldest = Math.Min(oldest, tie.When);
            }

        var interval = message.Recent && row.Length > 0 && newest > oldest
            ? (newest - oldest) / (double)row.Length
            : 0.0;

        // AND THE ORDINARY CELL, WHERE THE QUESTION WANTS A CONTRAST. `helped` is
        // the numerator of P(better | act) and this is its denominator -- how often
        // the act was taken here at all -- and the walk about to happen is over the
        // credit cells, so the ordinary ones are not otherwise looked at. Built in
        // one pass over a row already copied, exactly as the negative half above.
        Dictionary<Code, double>? taken = null;

        if (message.Contrasted)
            foreach (var (edge, tie) in row)
            {
                if (edge.Kind != Kind.With) continue;

                taken ??= [];
                taken[edge.Partner] = taken.GetValueOrDefault(edge.Partner) + tie.Count;
            }

        foreach (var (edge, tie) in row)
        {
            // A CELL WITH A NEGATIVE COUNTERPART IS READ AS THE DIFFERENCE, and
            // the counterpart itself is never walked -- it is evidence about the
            // positive cell, not a route.
            if (edge.Kind == Kind.Hindered) continue;

            // THE NEGATIVE HALF IS CARRIED, NOT APPLIED HERE, AND THAT WAS
            // MEASURED TWICE OVER. Folding it into the count -- by subtracting or
            // by scaling -- discounts the RANKING and the PRICE together, because
            // this one number is still both. Every discounted partner became
            // dearer to reach, routes starved, and the walk went quiet: silence
            // rose from 330 of 400 steps to 387 and the arm gave back what the
            // credit had bought, under a hard cut and a soft scaling alike.
            //
            // So it rides on the message and the receiver applies it to the score
            // alone -- which is exactly what `Doubt` does, for exactly the same
            // reason, having been got wrong the same way once already.
            var opposed = edge.Kind == Kind.Helped && against is not null
                && against.TryGetValue(edge.Partner, out var hurt)
                    ? hurt
                    : 0.0;

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
                Together = tie.Count,

                // WHAT ARGUES AGAINST THIS EDGE. See Message.Against.
                Against = opposed,

                // HOW CURRENT THIS ENTRY IS, in units of this row's own rhythm. One
                // where the row has no spread, which is the honest reading: entries
                // all written at once are all equally current, and ranking them
                // would invent a staleness that is not there.
                Fresh = !message.Recent
                    ? 0.0
                    : interval <= 0.0
                        ? 1.0
                        : 1.0 / (1.0 + ((newest - tie.When) / interval)),

                // AND WHAT IT IS WORTH ABOVE DOING SOMETHING ELSE. Only the sender
                // can work this out -- the base rate is its own marginal -- so it
                // is computed here and carried. See Message.Contrast.
                Contrast = message.Contrasted && edge.Kind == Kind.Helped
                    ? Contrast(
                        taken: taken?.GetValueOrDefault(edge.Partner) ?? 0.0,
                        helped: tie.Count,
                        occasions: occasions,
                        improved: improved)
                    : 0.0,

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
