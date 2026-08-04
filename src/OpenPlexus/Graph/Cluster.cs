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

    public Cluster(
        ClusterAddress address,
        IBus bus,
        Ring ring,
        WalkSettings settings)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(settings);

        _address = address;
        _bus = bus;
        _ring = ring;
        _settings = settings;
    }

    /// <inheritdoc cref="_address"/>
    public ClusterAddress Address => _address;

    /// <summary>How many nodes have come into existence here.</summary>
    public int Count => _nodes.Count;

    /// <summary>Every code this cluster holds a node for.</summary>
    public IEnumerable<Code> Codes => _nodes.Keys;

    /// <summary>Every row entry across every node here. The graph's size.</summary>
    /// <remarks>
    /// <b>ENTRIES AND NOT DISTINCT PARTNERS, because entries are what cost.</b>
    /// <see cref="Node.Fire"/> emits one message per entry, so a partner met both
    /// alongside and after is two messages — and with kinds off the two counts
    /// are the same number, which is what keeps every earlier reading comparable.
    /// </remarks>
    public int Edges => _nodes.Values.Sum(node => node.Entries);

    /// <inheritdoc cref="Worlds.Plumbing.Widest"/>
    public int Widest => _nodes.IsEmpty ? 0 : _nodes.Values.Max(node => node.Entries);

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

        // A BROADCAST IS ONE PENDING UNIT PER CLUSTER, and that is what keeps
        // the accounting working when the origin cannot know how many routes it
        // started. The unit forks into however many of the broadcast codes this
        // cluster actually holds, or dies if it holds none -- so every cluster
        // replies, including with nothing, and the origin's count closes.
        var fanned = 0;

        foreach (var message in envelope.Messages)
        {
            // A broadcast never creates a node: a cluster that has never seen
            // this code has nothing to say about it.
            if (envelope.Everywhere && !_nodes.TryGetValue(message.To, out _)) continue;

            var fired = Admit(message.To).Fire(message);
            fanned++;

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

            // WHERE THE ROUTES WENT, per thought. John's design: a node knows
            // which cluster it is in, so it can report not only that it forked
            // but where the forks are headed -- which is what lets an origin
            // know whether a cluster's death concerns it.
            //
            // A BROADCAST'S NODES ARE NOT SEPARATELY IN FLIGHT. The origin sent
            // one unit to this cluster, and these fired because the unit forked
            // here -- counting each of them as handled would take credit for
            // routes the origin never dispatched.
            if (!envelope.Everywhere) owing.Handled++;
            foreach (var next in fired.Outgoing)
            {
                var owner = _ring.OwnerOf(next.To);
                owing.SentInto[owner] = owing.SentInto.GetValueOrDefault(owner) + 1;
            }

            if (fired.Reached is { } arrival) owing.Arrivals.Add(arrival);
            owing.Splits += fired.Accounting.Splits;
            owing.Deaths += fired.Accounting.Deaths;
            owing.Halted += fired.Accounting.Halted;
            owing.Starved += fired.Accounting.Starved;
            owing.Thwarted += fired.Accounting.Thwarted;
            owing.Ended += fired.Accounting.Ended;
        }

        if (envelope.Everywhere) Unit(envelope, owed, fanned);

        // SEND ONWARD BEFORE REPORTING, AND THAT ORDER IS LOAD-BEARING.
        // A delivery that sends onward does so before it finishes, which is
        // what stops the bus going quiet while a thought is still propagating.
        //
        // MEASURED, by trying the other way round: reporting first destabilised
        // whole runs -- steps, choices, graph size and energy all varied at a
        // fixed seed, because `WhenIdle` could fire in the gap between the
        // report completing and these sends being issued, so the harness acted
        // on a thought that was still in flight.
        //
        // The cost of this order is that a downstream cluster can report a
        // route's death before the upstream reports the split that created it,
        // which makes `Halted` approximate. Every other reported quantity is
        // stable at a fixed seed. See open fork 12.
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
                From = _address,
                Handled = owing.Handled,
                SentInto = [.. owing.SentInto.Select(pair => new Routed(pair.Key, pair.Value))],
                Arrivals = [.. owing.Arrivals],
                Accounting = new Accounting(
                    broadcast, owing.Splits, owing.Deaths, owing.Halted, owing.Starved,
                    owing.Thwarted, owing.Ended),
            }, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Books the broadcast's single pending unit: it became <paramref name="fanned"/>
    /// routes, or died if this cluster held none of the codes.
    /// </summary>
    /// <remarks>
    /// <b>A cluster holding nothing still replies</b>, and it has to. The origin
    /// knows only how many clusters it broadcast to, so silence from one of them
    /// is indistinguishable from a route still walking.
    /// </remarks>
    private static void Unit(
        Envelope envelope,
        Dictionary<(MachineAddress To, BroadcastId Broadcast), Owing> owed,
        int fanned)
    {
        if (envelope.Messages.IsEmpty) return;

        var sample = envelope.Messages[0];
        var key = (sample.ReturnTo, sample.Broadcast);
        if (!owed.TryGetValue(key, out var owing)) owed[key] = owing = new Owing();

        owing.Handled++;
        if (fanned == 0) owing.Deaths++;
        else owing.Splits += fanned - 1;
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

        /// <summary>Deaths that were routes running out of budget. Fork 21.</summary>
        public int Starved { get; set; }

        /// <summary>Strength still carried by routes the budget killed. Fork 23.</summary>
        public double Thwarted { get; set; }

        /// <summary>Strength carried by every route that died, whatever killed it.</summary>
        public double Ended { get; set; }

        public int Handled { get; set; }

        public Dictionary<ClusterAddress, int> SentInto { get; } = [];
    }
}
