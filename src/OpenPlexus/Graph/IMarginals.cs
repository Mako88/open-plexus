using OpenPlexus.Codes;

namespace OpenPlexus.Graph;

/// <summary>
/// Reads another node's own marginal.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS INTERFACE IS OPEN FORK 2, MADE VISIBLE ON PURPOSE.</b> It exists so
/// the one place the design collides with C1 is a seam you can see rather than
/// a dictionary lookup buried in a loop.
/// </para>
/// <para>
/// <see cref="Node.WeightOf"/> needs <c>together(here, other) / seen(other)</c>
/// — the <i>partner's</i> marginal. In one process that is a free lookup, which
/// is why the Python never had to answer this. Across machines that number
/// lives on the partner's machine, and reading it is a remote read on every
/// edge of every hop.
/// </para>
/// <para>
/// Two candidate resolutions, neither measured:
/// </para>
/// <list type="bullet">
/// <item><b>The receiver weighs.</b> The message carries
/// <c>together(here, other)</c> out of the sender's own row and the receiver
/// divides by its own marginal, which it owns. C1-legal by construction — and
/// then the sender cannot price a step before sending it, which
/// <see cref="StepCost.Best"/> requires.</item>
/// <item><b>Marginals gossip</b> on the bus and go stale. C2 says late is
/// normal, so staleness may be acceptable. Untested.</item>
/// </list>
/// <para>
/// <b>When fork 2 is resolved this interface should disappear.</b> If it is
/// still here later, the fork went quiet.
/// </para>
/// </remarks>
public interface IMarginals
{
    /// <summary>How many occasions that code fired on. Its own marginal.</summary>
    double SeenOf(Code code);
}
