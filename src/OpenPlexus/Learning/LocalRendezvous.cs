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

    public LocalRendezvous(LocalClusters clusters)
    {
        ArgumentNullException.ThrowIfNull(clusters);
        _clusters = clusters;
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
        foreach (var code in present) _clusters.For(code).Note();

        var written = new HashSet<(Code, Code)>();

        foreach (var onset in occasion.Onsets)
        {
            foreach (var other in present)
            {
                if (other == onset) continue;

                // Two onsets in one frame are one coincidence, not two. Without
                // this they would each pair with the other and the count would
                // double.
                if (!written.Add(Unordered(onset, other))) continue;

                // EACH SIDE WRITES ITS OWN ROW. A node that quietly kept both
                // directions would be holding data it does not own, which is
                // the shared state C1 forbids.
                _clusters.For(onset).Observe(other);
                _clusters.For(other).Observe(onset);
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
                if (past != onset)
                    _clusters.For(past).Observe(onset);
        }

        // ONSET-TO-EVERYTHING, never live-to-live. Two codes that were both
        // already there did not just coincide -- they coincided whenever they
        // started, and that was counted then. Incrementing them again on every
        // unrelated onset would inflate exactly the stable background the
        // weighting has to refuse.
        return ValueTask.CompletedTask;
    }

    private static (Code, Code) Unordered(Code one, Code other) =>
        one.CompareTo(other) <= 0 ? (one, other) : (other, one);
}
