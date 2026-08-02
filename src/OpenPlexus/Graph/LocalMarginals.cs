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
    private readonly LocalClusters _clusters;

    public LocalMarginals(LocalClusters clusters)
    {
        ArgumentNullException.ThrowIfNull(clusters);
        _clusters = clusters;
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
    public double SeenOf(Code code) =>
        _clusters.TryOwner(code, out var cluster) && cluster.TryGet(code, out var node)
            ? node.Seen
            : 0.0;
}
