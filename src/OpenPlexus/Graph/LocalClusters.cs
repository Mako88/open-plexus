using System.Collections.Concurrent;
using OpenPlexus.Bus;
using OpenPlexus.Codes;

namespace OpenPlexus.Graph;

/// <summary>
/// Every cluster in this process, and how to reach the node for a code.
/// </summary>
/// <remarks>
/// <b>Local only, and that is the whole of it.</b> A code whose ring owner is
/// not in this process cannot be reached at all — there is no wire. Its one
/// remaining user says what that costs it: see
/// <see cref="Learning.LocalRendezvous"/>, which is fork 1.
/// </remarks>
public sealed class LocalClusters
{
    private readonly Ring _ring;
    private readonly ConcurrentDictionary<ClusterAddress, Cluster> _clusters = [];

    public LocalClusters(Ring ring)
    {
        ArgumentNullException.ThrowIfNull(ring);
        _ring = ring;
    }

    public void Include(Cluster cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        _clusters[cluster.Address] = cluster;
    }

    /// <summary>Every cluster in this process.</summary>
    public IEnumerable<Cluster> All => _clusters.Values;

    /// <summary>The cluster the ring says owns this code, if it is in this process.</summary>
    public bool TryOwner(Code code, out Cluster cluster) =>
        _clusters.TryGetValue(_ring.OwnerOf(code), out cluster!);

    /// <summary>
    /// The node for a code, created if this is its first mention.
    /// </summary>
    /// <remarks>
    /// Throws when the owner is not local, rather than dropping the write. With
    /// no wire that can only be a wiring error, and a silent drop would look
    /// exactly like the ordinary count loss it is not.
    /// </remarks>
    public Node For(Code code) =>
        TryOwner(code, out var cluster)
            ? cluster.Admit(code)
            : throw new InvalidOperationException(
                $"the cluster owning {code} is not in this process, and there is no wire");
}
