using System.Collections.Concurrent;
using System.Collections.Immutable;
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
public sealed class Cluster : IReceiveEnvelopes
{
    /// <summary>Code to node, for every node this cluster holds.</summary>
    private readonly ConcurrentDictionary<Code, Node> _nodes = [];

    private readonly ClusterAddress _address;
    private readonly IBus _bus;
    private readonly Ring _ring;
    private readonly WalkSettings _settings;
    private readonly IMarginals _marginals;

    public Cluster(
        ClusterAddress address,
        IBus bus,
        Ring ring,
        WalkSettings settings,
        IMarginals marginals)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(marginals);

        _address = address;
        _bus = bus;
        _ring = ring;
        _settings = settings;
        _marginals = marginals;
    }

    /// <inheritdoc cref="_address"/>
    public ClusterAddress Address => _address;

    /// <summary>How many nodes have come into existence here.</summary>
    public int Count => _nodes.Count;

    /// <summary>Whether the ring says this cluster owns that node.</summary>
    public bool Holds(Code code) => _ring.OwnerOf(code) == _address;

    /// <summary>
    /// Creates the node for a code this cluster has not seen before.
    /// </summary>
    /// <remarks>
    /// <b>Nodes come into existence on first mention.</b> Nothing pre-creates
    /// the graph, and nothing enumerates it.
    /// <para>
    /// <b>Ownership is not checked here, deliberately.</b> A message addressed
    /// to this cluster under a ring view that has since moved on would be
    /// refused, and refusing loses the count where accepting keeps it. The
    /// consequence is real and recorded rather than prevented: while views
    /// disagree, two clusters can each hold a partial row for one code, and
    /// nothing merges them. That is the lost-count scale of error C2 already
    /// admits, not a corruption.
    /// </para>
    /// </remarks>
    public Node Admit(Code code) => _nodes.GetOrAdd(code, key => new Node(key, _settings));

    /// <summary>The node for a code, if this cluster has one.</summary>
    public bool TryGet(Code code, out Node node) => _nodes.TryGetValue(code, out node!);

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
    public async Task DeliverAsync(Envelope envelope, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var onward = new Dictionary<ClusterAddress, List<Message>>();
        var owed = new Dictionary<(MachineAddress To, BroadcastId Broadcast), Owing>();

        foreach (var message in envelope.Messages)
        {
            var fired = Admit(message.To).Fire(message, _marginals);

            foreach (var next in fired.Outgoing)
            {
                // REGROUPED BY DESTINATION, which is the whole economy: wire
                // cost scales with distinct clusters reached, never with nodes.
                var owner = _ring.OwnerOf(next.To);
                if (!onward.TryGetValue(owner, out var batch)) onward[owner] = batch = [];
                batch.Add(next);
            }

            // Keyed by broadcast as well as machine: one envelope can carry
            // messages from more than one thought, and merging their accounting
            // is exactly what the broadcast id exists to prevent.
            var key = (message.ReturnTo, message.Broadcast);
            if (!owed.TryGetValue(key, out var owing)) owed[key] = owing = new Owing();

            if (fired.Reached is { } arrival) owing.Arrivals.Add(arrival);
            owing.Splits += fired.Accounting.Splits;
            owing.Deaths += fired.Accounting.Deaths;
            owing.Halted += fired.Accounting.Halted;
        }

        foreach (var (destination, batch) in onward)
        {
            await _bus.SendAsync(
                destination,
                new Envelope { To = destination, Messages = [.. batch] },
                ct).ConfigureAwait(false);
        }

        foreach (var ((to, broadcast), owing) in owed)
        {
            await ReportAsync(to, new Report
            {
                Arrivals = [.. owing.Arrivals],
                Accounting = new Accounting(broadcast, owing.Splits, owing.Deaths, owing.Halted),
            }, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends everything this cluster owes one machine for one broadcast, in a
    /// single message, addressed by the return address the route carried.
    /// </summary>
    private ValueTask ReportAsync(MachineAddress returnTo, Report report, CancellationToken ct) =>
        _bus.SendAsync(returnTo, report, ct);

    private sealed class Owing
    {
        public List<Arrival> Arrivals { get; } = [];

        public int Splits { get; set; }

        public int Deaths { get; set; }

        public int Halted { get; set; }
    }
}
