using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Learning;

/// <summary>
/// The join, when every cluster is in one process.
/// </summary>
/// <remarks>
/// <para>
/// <b>IT DOES NOT TEST THE HARD PART, and that is why it is called Local.</b>
/// Two machines seeing different halves of the same moment is the case that
/// needs a real rendezvous — open fork 1. The shape on <c>master</c> is a
/// bucket owner computed by hash, noticing an overlap and then being discarded,
/// measured at exactly 1.0 messages per observation.
/// </para>
/// <para>
/// <b>Onsets change what that has to do.</b> Joining overlapping intervals is a
/// different job from joining matched instants, and it is a strictly easier
/// one: if a thing was visible for two seconds, 50ms of clock skew between
/// machines is irrelevant. Overlap is robust against C2 where coincidence is
/// brittle. The existing bucket join was built for the brittle version.
/// </para>
/// </remarks>
public sealed class LocalRendezvous : IRendezvous
{
    private readonly LocalClusters _clusters;

    /// <inheritdoc cref="Graph.Kind"/>
    private readonly bool _kinds;

    /// <summary>What a carried pair counts for, against a simultaneous one.</summary>
    private readonly double _carried;

    /// <param name="clusters">Where the codes live.</param>
    /// <param name="kinds">
    /// Whether a temporal pair gets its own cell — <b>step 6, and OFF is every
    /// measurement taken before it existed.</b>
    /// </param>
    /// <param name="carried">
    /// What a CARRIED pair is worth, as a share of what a simultaneous one is
    /// worth — <b>the standing revival condition on the window's refuted row.</b>
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>IT HAS TO BE AN ARM, BECAUSE IT MOVES COUNTS THAT WERE ALREADY
    /// MEASURED.</b> A carried edge is currently added to the same cell as a
    /// simultaneous one, so splitting them is not additive the way
    /// <see cref="Occasion.Groups"/> was — the same history produces a different
    /// graph. Every number taken up to now was taken with this off, and stays
    /// comparable only while it can still be turned off.
    /// </para>
    /// <para>
    /// <b>The revival condition on two refuted rows is exactly this arm being
    /// on</b>, so it is the thing to sweep and not a default to assume.
    /// </para>
    /// <para>
    /// <b>AND THE DISCOUNT IS THE OTHER ROW'S REVIVAL CONDITION — <i>something that
    /// makes a carried edge worth its row</i>.</b> Two codes that were carried
    /// together were not in fact in one moment, so counting that as heavily as a
    /// real coincidence says <i>followed</i> is evidence as strong as
    /// <i>accompanied</i>. <b>It is exactly the move <see cref="Occasion.Weight"/>
    /// already makes for a reflected occasion</b> — a count became a weight so that
    /// something merely concluded cannot outweigh something observed, and something
    /// merely remembered is the same case.
    /// </para>
    /// <para>
    /// <b>ONE, AND NOT NOUGHT, IS THE OLD BEHAVIOUR.</b> A discount does not save a
    /// row entry or a message — the cell is written either way — so what it can buy
    /// is a cheaper HOP, the count being the numerator of the weight the price is
    /// read from. <b>Whether that is worth having is the measurement</b>, and it
    /// carries the standing risk that anything making a route dearer starves it.
    /// </para>
    /// </remarks>
    public LocalRendezvous(LocalClusters clusters, bool kinds = false, double carried = 1.0)
    {
        ArgumentNullException.ThrowIfNull(clusters);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(carried);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(carried, 1.0);

        _clusters = clusters;
        _kinds = kinds;
        _carried = carried;
    }

    /// <inheritdoc/>
    public ValueTask JoinAsync(Occasion occasion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(occasion);

        // A stable scene is silent. No onset, no occasion, nothing written.
        if (occasion.Onsets.IsEmpty) return ValueTask.CompletedTask;

        var present = new HashSet<Code>(occasion.Onsets);
        present.UnionWith(occasion.Live);

        // EVERYTHING PRESENT NOTES THE OCCASION, including what was already
        // live and did not itself start. Two reasons, and the second is the
        // load-bearing one.
        //
        // An occasion is a SET -- everything in one moment met everything else
        // -- and something that was there was there.
        //
        // AND `seen` IS THE DENOMINATOR OF EVERY EDGE WEIGHT. A partner is
        // scored `together(here, other) / seen(other)`, so a code that is
        // present through many events and notes none of them would carry a
        // tiny marginal against a large shared count, and score ABOVE 1.0 --
        // turning the ever-present background into the strongest partner in
        // the graph, which is the exact failure the forward weighting exists
        // to prevent. Noting keeps `together(x, y) <= seen(y)`.
        var weight = occasion.Weight;

        // AND THE MARGINAL IS SPLIT BY RELATION AS WELL AS TOTALLED. `Seen` counts
        // this occasion either way, so every number already measured is where it
        // was; what is new is a second tally saying how many of a node's occasions
        // were ORDINARY and how many were a reinforcement of some kind. That split
        // is the base rate `Node.Contingency` subtracts, and without it a credit
        // cell is a hit rate that cannot see what would have happened anyway.
        foreach (var code in present) _clusters.For(code).Note(weight, occasion.As ?? Kind.With);

        // THE FIFTH CHANNEL: EACH FILLER MEETS ITS OWN SLOT. `Roles` says which
        // argument position a code occupies in `As`, and pairing a filler with
        // `As.Role(slot)` puts it in a cell that names NO ARGUMENT -- so the same
        // cell accumulates across every pair the relation was ever observed on,
        // which is the thing a count between two fillers structurally cannot do.
        //
        // AND IT IS THE `Groups` TRICK ONCE MORE, which is why it costs so little:
        // the front end says what it is looking at, the graph finds the regularity.
        // A filler meets ITS slot and no other, exactly as an attribute meets its
        // own object and no other.
        if (occasion.As is { } filled && occasion.Roles is { } roles)
            foreach (var (filler, slot) in roles)
            {
                if (!present.Contains(filler)) continue;

                var role = filled.Role(slot);

                // UNDER ITS OWN KIND, or the binding does not survive one hop.
                // Two fillers of one arrangement co-occur under `With`, so a walk
                // reaches the WRONG slot through the other filler as fast as it
                // reaches the right one. See Kind.Fills.
                _clusters.For(role).Note(weight, Kind.Fills);
                _clusters.For(filler).Observe(role, weight, Kind.Fills, occasion.At);
                _clusters.For(role).Observe(filler, weight, Kind.Fills, occasion.At);
            }

        var written = new HashSet<(Code, Code)>();

        foreach (var onset in occasion.Onsets)
        {
            foreach (var other in present)
            {
                if (other == onset) continue;

                // STEP 1A: TWO THINGS IN ONE MOMENT ARE NOT ONE OCCASION. Where
                // the front end could say which codes belong to which object,
                // pairing across objects is refused -- otherwise a colour joins
                // both shapes present and the binding is destroyed at the front
                // door, which is what fork 25 measured.
                if (!Bound(occasion.Groups, onset, other)) continue;

                // Two onsets in one frame are one coincidence, not two. Without
                // this they would each pair with the other and the count would
                // double.
                if (!written.Add(Unordered(onset, other))) continue;

                // A SECOND STATISTIC ABOUT A MOMENT ALREADY WRITTEN, and it takes
                // precedence over the ordering because a cell is (partner, kind)
                // and one pair cannot be filed under two at once. The order was
                // recorded by the first write; this write is about something else.
                // See Occasion.As and Kind.Helped.
                if (occasion.As is { } relation)
                {
                    if (!Passing(occasion.Fleeting, other))
                        _clusters.For(onset).Observe(other, weight, relation, occasion.At);

                    if (!Passing(occasion.Fleeting, onset))
                        _clusters.For(other).Observe(onset, weight, relation, occasion.At);

                    continue;
                }

                // WHAT THE FRONT END SAID ABOUT ORDER INSIDE THIS MOMENT. Where
                // it said nothing, nothing came first and the pair is symmetric,
                // which is every occasion emitted before Sequence existed.
                var order = Ordered(occasion.Sequence, onset, other);

                if (order != 0)
                {
                    var (first, second) = order < 0 ? (onset, other) : (other, onset);

                    // BOTH WAYS, IN DIFFERENT CELLS. The one-way rule existed
                    // because a single cell per pair could not say `then` except
                    // by being asymmetric; a row that holds the kind does not
                    // need that, and paying for it severed every path that has
                    // to walk from a consequence back to its cause -- measured
                    // on snake, where choosing an action stopped working
                    // entirely. See Kind.Before.
                    if (!Passing(occasion.Fleeting, second))
                        _clusters.For(first).Observe(second, weight, Kind.After, occasion.At);

                    // SAFE HERE AND NOT FOR A CARRIED CODE, because both of
                    // these were IN the occasion and both noted it -- so the
                    // reverse edge is weighed against a marginal that counted
                    // this moment, and `together` cannot exceed `seen`.
                    if (!Passing(occasion.Fleeting, first))
                        _clusters.For(second).Observe(first, weight, Kind.Before, occasion.At);

                    continue;
                }

                // EACH SIDE WRITES ITS OWN ROW. A node that quietly kept both
                // directions would be holding data it does not own, which is
                // the shared state C1 forbids.
                //
                // AND A ROW DOES NOT RECORD A CODE THAT WILL NEVER RECUR. See
                // Occasion.Fleeting: that entry can never gain a second count,
                // so it is never evidence -- and it is what makes a lasting
                // node's row grow without bound.
                if (!Passing(occasion.Fleeting, other))
                    _clusters.For(onset).Observe(other, weight, Kind.With, occasion.At);

                if (!Passing(occasion.Fleeting, onset))
                    _clusters.For(other).Observe(onset, weight, Kind.With, occasion.At);
            }
        }

        // THE TEMPORAL EDGE, and it is written ONE WAY. A code that stopped
        // before this moment records what followed it; what followed records
        // nothing about it. So a broadcast can walk forward from what just
        // happened and cannot walk back, which is the only thing that makes an
        // edge mean "then" rather than "with".
        //
        // The departed do NOT note the occasion -- they were not in it. The
        // invariant still holds, because this edge is weighed by the RECEIVER's
        // marginal and the receiver is an onset, which did note.
        foreach (var past in occasion.Recent)
        {
            if (present.Contains(past)) continue;

            foreach (var onset in occasion.Onsets)
                if (past != onset && !Passing(occasion.Fleeting, onset))

                    // AND THIS IS THE CELL THE WINDOW WAS ALWAYS WANTING. With
                    // kinds off it lands on top of the simultaneous count, which
                    // is why the span arm helps where everything is sequential
                    // and hurts where things overlap.
                    //
                    // AND IT IS THE ONE WRITE THE DISCOUNT APPLIES TO. These two
                    // codes were not in one moment -- one had already stopped --
                    // so at full weight the row says `followed` is evidence as
                    // strong as `accompanied`. Nothing else here is discounted,
                    // which is what makes this an arm rather than a rescaling of
                    // the whole graph.
                    _clusters.For(past).Observe(
                        onset,
                        weight * _carried,
                        _kinds ? Kind.After : Kind.With,
                        occasion.At);
        }

        // ONSET-TO-EVERYTHING, never live-to-live. Two codes that were both
        // already there did not just coincide -- they coincided whenever they
        // started, and that was counted then. Incrementing them again on every
        // unrelated onset would inflate exactly the stable background the
        // weighting has to refuse.
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Whether two codes are allowed to pair, given what the front end could say
    /// about which thing each belongs to.
    /// </summary>
    /// <remarks>
    /// <b>Ungrouped pairs with everything</b>, and that is what keeps this
    /// additive rather than a rewrite: a front end that can segment some of what
    /// it sees is not forced to lie about the rest, and a front end that can
    /// segment none of it behaves exactly as before.
    /// </remarks>
    private static bool Bound(IReadOnlyDictionary<Code, int>? groups, Code one, Code other)
    {
        if (groups is null) return true;

        // Either being unassigned means nothing is being claimed about them.
        return !groups.TryGetValue(one, out var mine)
            || !groups.TryGetValue(other, out var theirs)
            || mine == theirs;
    }

    /// <summary>
    /// Whether a code names this occasion rather than a kind of thing, so
    /// nothing lasting should record it.
    /// </summary>
    /// <remarks>
    /// <b>Unsaid means lasting</b>, which is what keeps this additive: a front
    /// end that cannot tell behaves exactly as before. See
    /// <see cref="Occasion.Fleeting"/>.
    /// </remarks>
    private static bool Passing(IReadOnlySet<Code>? fleeting, Code code) =>
        fleeting is not null && fleeting.Contains(code);

    /// <summary>
    /// Which of two codes the front end said came first, within one moment.
    /// <b>Zero is simultaneous</b> — either because nothing was said about them,
    /// or because what was said gave them the same rank.
    /// </summary>
    /// <remarks>
    /// <b>THIS NEEDS NO ARM, WHERE THE WINDOW DID.</b> No occasion emitted before
    /// <see cref="Occasion.Sequence"/> existed carries one, so a front end that
    /// cannot sequence produces exactly the graph it always did — which is the
    /// same additivity <see cref="Occasion.Groups"/> has. Splitting the window's
    /// carried edge is different in kind: that history already exists and its
    /// counts move.
    /// </remarks>
    private static int Ordered(IReadOnlyDictionary<Code, int>? sequence, Code one, Code other)
    {
        if (sequence is null) return 0;

        return !sequence.TryGetValue(one, out var mine)
            || !sequence.TryGetValue(other, out var theirs)
            ? 0
            : mine.CompareTo(theirs);
    }

    private static (Code, Code) Unordered(Code one, Code other) =>
        one.CompareTo(other) <= 0 ? (one, other) : (other, one);
}
