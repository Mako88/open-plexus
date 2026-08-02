using System.Collections.Concurrent;
using OpenPlexus.Codes;

namespace OpenPlexus.Graph;

/// <summary>
/// Partner marginals, read straight out of whatever clusters happen to be in
/// this process.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS CLASS IS OPEN FORK 2, AND IT IS A C1 VIOLATION. It is named and kept
/// in one place so that it cannot be mistaken for a solved problem.</b>
/// </para>
/// <para>
/// <see cref="Node.Fire"/> weighs an edge as
/// <c>together(here, other) / seen(other)</c> — the <i>partner's</i> marginal.
/// In one process that is a dictionary lookup, which is why the Python never
/// had to answer this. Across machines that number lives on the partner's
/// machine, and this class has no way to reach it: a second machine's nodes are
/// simply not here, and every edge pointing at one would weigh zero, so no
/// route would ever leave the process.
/// </para>
/// <para>
/// <b>So this works exactly as far as one process and no further</b>, and that
/// is the whole of the honest claim. The two candidate resolutions are recorded
/// in <see cref="IMarginals"/>; neither is measured, and this class should
/// disappear when one of them lands.
/// </para>
/// </remarks>
public sealed class LocalMarginals : IMarginals
{
    private readonly ConcurrentDictionary<ClusterName, Cluster> _clusters = [];

    private readonly record struct ClusterName(string Value);

    /// <summary>Make a cluster's nodes readable to everything else in the process.</summary>
    public void Include(Cluster cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        _clusters[new ClusterName(cluster.Address.Value)] = cluster;
    }

    /// <summary>
    /// How many occasions that code fired on.
    /// </summary>
    /// <remarks>
    /// <b>Zero for a code no local cluster holds</b>, which weighs its edge at
    /// zero and kills the route. In one process that is correct — a code
    /// nothing has ever seen has no marginal. Across machines it would be
    /// wrong, and silently so, which is why the class says what it is.
    /// </remarks>
    public double SeenOf(Code code)
    {
        foreach (var cluster in _clusters.Values)
        {
            if (cluster.TryGet(code, out var node)) return node.Seen;
        }

        return 0.0;
    }
}
