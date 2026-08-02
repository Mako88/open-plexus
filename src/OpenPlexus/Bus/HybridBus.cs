using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Thinking;

namespace OpenPlexus.Bus;

/// <summary>
/// Local delivery by direct call, remote delivery over the wire, one interface.
/// </summary>
/// <remarks>
/// <para>
/// <b>The speed difference between local and remote is not a wart, it is the
/// experiment.</b> Codes that land together are cheap to walk between and codes
/// that land apart are not, so a machine's contents become a region of the
/// graph that thinks faster internally than externally. That is what a column
/// is, and here it costs nothing extra — see open fork 3.
/// </para>
/// </remarks>
public sealed class HybridBus : IBus
{
    /// <summary>
    /// Clusters in this process. Sending to one of these is <b>a direct method
    /// call, not awaited on the sender's path, with no serialization at all.</b>
    /// </summary>
    private readonly Dictionary<ClusterAddress, Cluster> _local = [];

    /// <summary>Machines reachable over the wire. Same call, real latency.</summary>
    private readonly Dictionary<ClusterAddress, IPeer> _peers = [];

    /// <summary>The shared constant every quantiser and every ring is built from.</summary>
    private readonly long _seed;

    public HybridBus(long seed) => throw new NotImplementedException();

    /// <inheritdoc/>
    public event Action<MachineAddress>? Deaths;

    /// <inheritdoc/>
    public IDisposable Subscribe(Cluster cluster) => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default) =>
        throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SendAsync(MachineAddress to, Occasion occasion, CancellationToken ct = default) =>
        throw new NotImplementedException();
}

/// <summary>
/// One remote machine, as seen from here.
/// </summary>
/// <remarks>
/// <b>Not implemented and deliberately not yet.</b> Snake runs on one machine;
/// building the wire before a second machine exists is how the Python grew
/// three modules with no caller. The interface exists so
/// <see cref="HybridBus"/> is shaped for it now.
/// </remarks>
public interface IPeer
{
    ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default);
}
