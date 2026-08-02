using OpenPlexus.Codes;

namespace OpenPlexus.Bus;

/// <summary>
/// Which cluster owns which code. Computed locally, agreed globally.
/// </summary>
/// <remarks>
/// <para>
/// <b>No directory and nobody to ask.</b> Every machine computes the same
/// answer from the code and the shared seed, which is what lets a machine join
/// a network it has never spoken to and route correctly immediately. Nothing is
/// assigned and nobody is told.
/// </para>
/// <para>
/// <b>Views differ between machines while membership is changing, and that is
/// allowed.</b> A misrouted message is a lost count, not a corruption — the
/// statistics are counts over many occasions, and C2 already says messages go
/// astray.
/// </para>
/// </remarks>
public sealed class Ring
{
    /// <summary>The constant every ring and every quantiser is built from.
    /// Handed out once and frozen, which C1 permits.</summary>
    private readonly long _seed;

    /// <summary>
    /// How many points each cluster occupies on the ring.
    /// </summary>
    /// <remarks>
    /// <b>The dial that decides how evenly load falls.</b> One point per
    /// cluster leaves the split to a handful of hash values and is badly
    /// lopsided; more points average it out at a linear cost in memory and in
    /// lookup depth. Required rather than defaulted, because a constant that
    /// never changes looks like the background.
    /// </remarks>
    private readonly int _replicas;

    /// <summary>
    /// The ring, sorted by point and then by address.
    /// </summary>
    /// <remarks>
    /// <b>Sorted by address as well as point so that two rings given the same
    /// members in different orders are identical.</b> Insertion order must not
    /// survive anywhere in here — every machine builds this from whatever
    /// sequence of joins it happened to see.
    /// </remarks>
    private readonly List<(ulong Point, ClusterAddress Address)> _points = [];

    private readonly HashSet<ClusterAddress> _members = [];

    public Ring(long seed, int replicas)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(replicas);

        _seed = seed;
        _replicas = replicas;
    }

    /// <summary>The current membership view.</summary>
    public IReadOnlyCollection<ClusterAddress> Clusters => _members;

    /// <summary>Which cluster holds the node for this code.</summary>
    /// <remarks>
    /// Hashes the whole code today. <b>Open fork 3</b> is whether to hash a
    /// prefix instead, which would put similar codes on the same machine and
    /// give a column for free — at the cost of the uniform load a whole-code
    /// hash guarantees.
    /// </remarks>
    public ClusterAddress OwnerOf(Code code)
    {
        if (_points.Count == 0)
            throw new InvalidOperationException("no cluster has joined, so no code has an owner");

        var point = PointOf(code);

        // The first point at or after this one, wrapping. Only the codes
        // between a departed cluster's points and its predecessor's move when
        // membership changes, which is the whole reason for a ring.
        var lo = 0;
        var hi = _points.Count;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_points[mid].Point < point) lo = mid + 1;
            else hi = mid;
        }

        return _points[lo == _points.Count ? 0 : lo].Address;
    }

    /// <summary>A cluster became reachable. Joining twice changes nothing.</summary>
    /// <remarks>
    /// <b>A mutation removing the guard survives, and that is recorded rather
    /// than hidden.</b> Joining twice would re-add the same points at the same
    /// positions, so no lookup changes its answer — the guard saves memory, not
    /// correctness, and no test can see the difference.
    /// </remarks>
    public void Join(ClusterAddress address)
    {
        if (!_members.Add(address)) return;

        for (var replica = 0; replica < _replicas; replica++)
            _points.Add((PointOf(address, replica), address));

        Sort();
    }

    /// <summary>A cluster went away. <b>Normal, not an error</b> — C3.</summary>
    public void Leave(ClusterAddress address)
    {
        if (!_members.Remove(address)) return;

        _points.RemoveAll(entry => entry.Address == address);
    }

    /// <summary>
    /// Point, then address. The second key is what makes the ring independent
    /// of the order members were seen in.
    /// </summary>
    /// <remarks>
    /// <b>A mutation removing the address tie-break survives every test, and
    /// that is recorded rather than hidden.</b> It is only reachable on a
    /// 64-bit hash collision between two cluster points, which no test
    /// produces. Absent a collision the point alone already determines the
    /// order. It is kept because <see cref="List{T}.Sort"/> is not stable, so
    /// without it a collision would let insertion order reach the answer — and
    /// that is a claim about improbability, not about tested behaviour.
    /// </remarks>
    private void Sort() => _points.Sort(static (left, right) =>
    {
        var point = left.Point.CompareTo(right.Point);
        return point != 0
            ? point
            : string.CompareOrdinal(left.Address.Value, right.Address.Value);
    });

    /// <summary>
    /// Where a code sits on the ring. <b>Does not use the seed</b>, and that is
    /// deliberate.
    /// </summary>
    /// <remarks>
    /// The seed was originally folded in here as well as into cluster
    /// placement, and a mutation found the two covering for each other:
    /// removing it from either place alone changed no answer, because the other
    /// still moved the arrangement. One implementation per behaviour, so it
    /// lives on cluster placement alone — a code's position is intrinsic, and
    /// what the seed varies is where the clusters sit.
    /// </remarks>
    private static ulong PointOf(Code code) =>
        Mix(Fold(Fold(Basis, code.Modality), code.Value));

    private ulong PointOf(ClusterAddress address, int replica)
    {
        var hash = Fold(Basis, (ulong)_seed);

        // NOT string.GetHashCode. That is randomised per process in .NET, so a
        // ring built from it would put the same code on a different cluster in
        // every process — and "every machine computes the same answer" would
        // fail silently, which is the one failure this class cannot have.
        foreach (var character in address.Value) hash = Fold(hash, character);

        return Mix(Fold(hash, (ulong)replica));
    }

    private const ulong Basis = 0xcbf29ce484222325;
    private const ulong Prime = 0x100000001b3;

    /// <summary>FNV-1a over the eight bytes of a value.</summary>
    private static ulong Fold(ulong hash, ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            hash ^= (byte)(value >> shift);
            hash *= Prime;
        }

        return hash;
    }

    /// <summary>
    /// The splitmix64 finaliser. FNV alone leaves neighbouring values close
    /// together, and codes for adjacent cells of one view are neighbouring
    /// values — without this they would land on one cluster by accident rather
    /// than by the decision fork 3 records.
    /// </summary>
    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xbf58476d1ce4e5b9;
        value ^= value >> 27;
        value *= 0x94d049bb133111eb;
        value ^= value >> 31;
        return value;
    }
}
