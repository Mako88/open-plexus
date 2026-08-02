using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Thinking;

namespace OpenPlexus.Graph;

/// <summary>
/// A set of nodes and an address. What actually subscribes to the bus.
/// </summary>
/// <remarks>
/// Individual nodes on the wire would be tens of thousands of tiny messages. A
/// cluster is the envelope that makes that affordable, and it is a transport
/// concern only — <b>it decides nothing about what fires.</b>
/// </remarks>
public sealed class Cluster
{
    /// <summary>Code to node, for every node this cluster holds.</summary>
    private readonly Dictionary<Code, Node> _nodes = [];

    private readonly ClusterAddress _address;
    private readonly IBus _bus;
    private readonly Ring _ring;
    private readonly WalkSettings _settings;

    public Cluster(ClusterAddress address, IBus bus, Ring ring, WalkSettings settings) =>
        throw new NotImplementedException();

    /// <inheritdoc cref="_address"/>
    public ClusterAddress Address => throw new NotImplementedException();

    /// <summary>Whether this cluster owns that node.</summary>
    public bool Holds(Code code) => throw new NotImplementedException();

    /// <summary>
    /// Creates the node for a code this cluster owns and has not seen before.
    /// </summary>
    /// <remarks>
    /// <b>Nodes come into existence on first mention.</b> Nothing pre-creates
    /// the graph, and nothing enumerates it.
    /// </remarks>
    public Node Admit(Code code) => throw new NotImplementedException();

    /// <summary>
    /// The economy. Unpacks the messages in an envelope, fires each one's node,
    /// collects every outgoing message, <b>regroups them by owning cluster</b>,
    /// and sends one envelope per destination.
    /// </summary>
    /// <remarks>
    /// A node forking to 200 partners spread over 12 clusters produces 12
    /// sends, and every hop that stays inside this cluster never touches the
    /// wire at all.
    /// </remarks>
    public Task DeliverAsync(Envelope envelope, CancellationToken ct = default) =>
        throw new NotImplementedException();

    /// <summary>
    /// Batches arrivals and accounting back to the machine that started the
    /// thought, addressed by the message's return address.
    /// </summary>
    private Task ReportAsync(
        MachineAddress returnTo,
        IReadOnlyCollection<Arrival> arrivals,
        Accounting accounting,
        CancellationToken ct) => throw new NotImplementedException();
}
