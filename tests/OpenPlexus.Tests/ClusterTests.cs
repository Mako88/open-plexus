using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// The first place a message crosses from one node to another, and the place
/// the message economy lives.
/// </summary>
public sealed class ClusterTests : IDisposable
{
    private static readonly MachineAddress Origin = new("origin");

    private static Code C(ulong value) => Fixture.C(value);

    private static readonly WalkSettings Dials = Fixture.Dials(stamina: 10.0, horizon: 6);

    private readonly HybridBus _bus = new();
    private readonly Counting _counted;
    private readonly Ring _ring = new(seed: 42, replicas: 64);
    private readonly LocalClusters _local;
    private readonly List<IDisposable> _handles = [];
    private readonly Machine _origin = new(Origin);

    public ClusterTests()
    {
        _bus.Faults += failure => throw failure;
        _local = new LocalClusters(_ring);
        _counted = new Counting(_bus);
        _handles.Add(_bus.Subscribe(_origin));
    }

    /// <summary>
    /// Wraps the bus and records what was sent where.
    /// </summary>
    /// <remarks>
    /// Counting sends is the only way to observe the economy at all: the claim
    /// is about how many envelopes leave, and a test that only checked what
    /// arrived could not tell one envelope of three messages from three
    /// envelopes of one.
    /// </remarks>
    private sealed class Counting(IBus inner) : IBus
    {
        private readonly Dictionary<ClusterAddress, int> _envelopes = [];
        private int _messages;

        public event Action<ClusterAddress>? Deaths
        {
            add => inner.Deaths += value;
            remove => inner.Deaths -= value;
        }

        public event Action<BroadcastId, MachineAddress>? Unreached
        {
            add => inner.Unreached += value;
            remove => inner.Unreached -= value;
        }

        public int Messages
        {
            get { lock (_envelopes) return _messages; }
        }

        public int EnvelopesTo(params Cluster[] clusters)
        {
            lock (_envelopes)
            {
                return clusters.Select(c => c.Address).Distinct()
                    .Sum(address => _envelopes.GetValueOrDefault(address));
            }
        }

        public IDisposable Subscribe(IReceiveEnvelopes cluster) => inner.Subscribe(cluster);

        public IDisposable Subscribe(IReceiveReports machine) => inner.Subscribe(machine);

        public IDisposable Listen(IReceiveArrivals machine, IReadOnlyCollection<Code> codes) =>
            inner.Listen(machine, codes);

        // THE LEARNING PATH IS PASSED THROUGH AND NOT COUNTED. This double exists to count
        // ENVELOPES, because the economy claim is about how many leave; an ask is not an
        // envelope and folding it into the same tally would make a number this file
        // asserts on move for a reason it is not about.
        public IDisposable Subscribe(IReceiveAsks holder) => inner.Subscribe(holder);

        public IDisposable Subscribe(IReceiveAnswers asker) => inner.Subscribe(asker);

        public ValueTask<IReadOnlyCollection<MachineAddress>> AskAsync(
            Ask ask,
            CancellationToken ct = default,
            Action<IReadOnlyCollection<MachineAddress>>? ready = null) =>
            inner.AskAsync(ask, ct, ready);

        public ValueTask SendAsync(MachineAddress to, Answer answer, CancellationToken ct = default) =>
            inner.SendAsync(to, answer, ct);

        public ValueTask PublishAsync(Settled settled, CancellationToken ct = default) =>
            inner.PublishAsync(settled, ct);

        public ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default)
        {
            lock (_envelopes)
            {
                _envelopes[to] = _envelopes.GetValueOrDefault(to) + 1;
                _messages += envelope.Messages.Length;
            }

            return inner.SendAsync(to, envelope, ct);
        }

        public ValueTask SendAsync(MachineAddress to, Report report, CancellationToken ct = default) =>
            inner.SendAsync(to, report, ct);

        public ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
            Envelope envelope,
            CancellationToken ct = default,
            Action<IReadOnlyCollection<ClusterAddress>>? ready = null) =>
            inner.BroadcastAsync(envelope, ct, ready);
    }

    public void Dispose()
    {
        foreach (var handle in _handles) handle.Dispose();
    }

    /// <summary>Records what came back to the machine that started a thought.</summary>
    private sealed class Machine(MachineAddress address) : IReceiveReports
    {
        private readonly List<Report> _got = [];

        public MachineAddress Address { get; } = address;

        public IReadOnlyList<Report> Got
        {
            get { lock (_got) return [.. _got]; }
        }

        public Task DeliverAsync(Report report, CancellationToken ct = default)
        {
            lock (_got) _got.Add(report);
            return Task.CompletedTask;
        }
    }

    private Cluster Join(string name)
    {
        var address = new ClusterAddress(name);
        _ring.Join(address);
        var cluster = new Cluster(address, _counted, _ring, Dials);
        _local.Include(cluster);
        _handles.Add(_bus.Subscribe(cluster));
        return cluster;
    }

    /// <summary>Spreads clusters until every one of these codes owns a different home.</summary>
    private Dictionary<Code, Cluster> Spread(params Code[] codes)
    {
        var clusters = Enumerable.Range(0, 6).Select(i => Join($"c{i}")).ToDictionary(c => c.Address);
        return codes.ToDictionary(code => code, code => clusters[_ring.OwnerOf(code)]);
    }

    private static Message Origins(Code to) => new()
    {
        Broadcast = BroadcastId.New(),
        ReturnTo = Origin,
        To = to,
        Held = 10.0,
        Chain = [to],
        Carried = 1.0,
    };

    private Task Deliver(Cluster cluster, params Message[] messages) =>
        cluster.DeliverAsync(new Envelope { To = cluster.Address, Messages = [.. messages] });

    // ---- nodes exist on first mention -------------------------------------

    [Fact]
    public async Task A_node_comes_into_existence_when_a_message_names_it()
    {
        var cluster = Join("only");
        Assert.Equal(0, cluster.Count);

        await Deliver(cluster, Origins(C(1)));

        Assert.Equal(1, cluster.Count);
        Assert.True(cluster.TryGet(C(1), out _));
    }

    [Fact]
    public void Holding_follows_the_ring_and_not_what_happens_to_be_here()
    {
        var homes = Spread(C(1), C(2), C(3));

        foreach (var (code, cluster) in homes)
        {
            Assert.True(cluster.Holds(code));

            // The companion: every other cluster says it does not hold it.
            foreach (var other in homes.Values.Where(c => c != cluster))
                Assert.False(other.Holds(code));
        }
    }

    // ---- the economy ------------------------------------------------------

    [Fact]
    public async Task Partners_in_one_cluster_cost_one_envelope()
    {
        // Three partners that all live on the same cluster must produce ONE
        // send, not three. Wire cost scales with distinct clusters reached.
        var cluster = Join("only");

        var node = cluster.Admit(C(1));
        foreach (var partner in (Code[])[C(2), C(3), C(4)])
        {
            node.Observe(partner);
            cluster.Admit(partner).Note();
        }

        await Deliver(cluster, Origins(C(1)));

        Assert.Equal(1, _counted.EnvelopesTo(cluster));
        Assert.Equal(3, _counted.Messages);
    }

    [Fact]
    public async Task Partners_across_clusters_cost_one_envelope_each()
    {
        // The companion. Without it the test above passes for a cluster that
        // batches everything into one envelope regardless of destination.
        var homes = Spread(C(2), C(3), C(4));
        var start = Join("start");

        var node = start.Admit(C(1));
        foreach (var (partner, home) in homes)
        {
            node.Observe(partner);
            home.Admit(partner).Note();
        }

        await Deliver(start, Origins(C(1)));
        await _bus.WhenIdle().WaitAsync(Fixture.Patience);

        Assert.Equal(3, homes.Values.Distinct().Count());
        Assert.Equal(3, _counted.EnvelopesTo([.. homes.Values]));
        Assert.Equal(3, _counted.Messages);
    }

    // ---- the return path --------------------------------------------------

    [Fact]
    public async Task Arrivals_and_accounting_come_back_to_the_return_address()
    {
        var cluster = Join("only");
        var node = cluster.Admit(C(1));
        node.Observe(C(2));
        cluster.Admit(C(2)).Note();

        await Deliver(cluster, Origins(C(1)));
        await _bus.WhenIdle().WaitAsync(Fixture.Patience);

        var accounting = _origin.Got.Select(r => r.Accounting).ToArray();

        // C(1) forked into one, so no split and no death. C(2) had nowhere to
        // go, so one death.
        Assert.Equal(1, accounting.Sum(a => a.Deaths));
        Assert.Equal(0, accounting.Sum(a => a.Splits));
        Assert.Contains(_origin.Got, r => r.Arrivals.Any(a => a.Endpoint == C(2)));
    }

    [Fact]
    public async Task An_origin_reports_no_arrival_but_still_reports_its_accounting()
    {
        var cluster = Join("only");

        await Deliver(cluster, Origins(C(1)));
        await _bus.WhenIdle().WaitAsync(Fixture.Patience);

        var report = Assert.Single(_origin.Got);
        Assert.Empty(report.Arrivals);
        Assert.Equal(1, report.Accounting.Deaths);
    }

    [Fact]
    public async Task Two_thoughts_in_one_envelope_are_reported_separately()
    {
        // Merging their accounting is exactly what the broadcast id exists to
        // prevent, and the Python has no equivalent.
        var cluster = Join("only");
        var first = Origins(C(1));
        var second = Origins(C(2));

        await Deliver(cluster, first, second);
        await _bus.WhenIdle().WaitAsync(Fixture.Patience);

        var broadcasts = _origin.Got.Select(r => r.Accounting.Broadcast).ToHashSet();
        Assert.Equal(2, broadcasts.Count);
        Assert.Contains(first.Broadcast, broadcasts);
        Assert.Contains(second.Broadcast, broadcasts);
    }

    // ---- a chain actually crosses a cluster boundary -----------------------

    [Fact]
    public async Task A_route_walks_from_one_cluster_into_another_and_the_chain_grows()
    {
        var homes = Spread(C(2));
        var start = Join("start");
        var far = homes[C(2)];
        Assert.NotSame(start, far);

        var node = start.Admit(C(1));
        node.Observe(C(2));
        far.Admit(C(2)).Note();

        await Deliver(start, Origins(C(1)));
        await _bus.WhenIdle().WaitAsync(Fixture.Patience);

        // THE CHAIN OF REASONING ARRIVED, carrying where it had been. This is
        // the first point in the project where a route crosses a boundary.
        var arrival = _origin.Got.SelectMany(r => r.Arrivals).Single(a => a.Endpoint == C(2));
        Assert.Equal([C(1), C(2)], arrival.Chain.ToArray());
    }

    // ---- concurrency ------------------------------------------------------

    [Fact]
    public async Task Concurrent_deliveries_do_not_lose_counts()
    {
        var cluster = Join("only");
        var node = cluster.Admit(C(1));

        await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                node.Note();
                node.Observe(C(2));
            }
        })));

        Assert.Equal(6400.0, node.Seen);
        Assert.Equal(6400.0, node.Together(C(2)));
    }

    [Fact]
    public async Task Two_nodes_that_are_partners_can_fire_at_once()
    {
        // Weighing an edge reads the partner's node, so a node holding its own
        // lock while doing that would deadlock against a partner firing back.
        // Edges are mutual, so that is an ordinary case rather than a corner.
        var cluster = Join("only");
        var left = cluster.Admit(C(1));
        var right = cluster.Admit(C(2));
        left.Observe(C(2));
        right.Observe(C(1));
        left.Note();
        right.Note();

        var storm = Enumerable.Range(0, 64)
            .Select(i => Task.Run(() => Deliver(cluster, Origins(i % 2 == 0 ? C(1) : C(2)))))
            .ToArray();

        await Task.WhenAll(storm).WaitAsync(Fixture.Patience);
        await _bus.WhenIdle().WaitAsync(Fixture.Patience);
    }

}
