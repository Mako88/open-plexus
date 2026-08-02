using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// A cluster vanishing mid-thought, end to end. C3 says this is normal.
/// </summary>
public sealed class DepartureTests : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private static Code C(ulong value) => new(Modality: 1, value);

    private static readonly WalkSettings Dials = new()
    {
        Stamina = 10.0, Cost = StepCost.Best, Refuel = Refuel.Strength,
        Value = ArrivalValue.Strength, Accumulate = Accumulate.Sum, Horizon = 6,
    };

    private readonly HybridBus _bus = new();
    private readonly Ring _ring = new(seed: 42, replicas: 64);
    private readonly List<IDisposable> _handles = [];
    private readonly Dictionary<ClusterAddress, IDisposable> _stubs = [];
    private readonly InputMachine<Code[]> _machine;

    public DepartureTests()
    {
        _bus.Faults += failure => throw failure;

        // STUBS THAT NEVER PROCESS, so routes sent to them stay in flight and
        // a departure has something real to strand. A live cluster in one
        // process answers too fast for a mid-thought death to be observable.
        foreach (var name in (string[])["a", "b", "c", "d"])
        {
            var address = new ClusterAddress(name);
            _ring.Join(address);
            _stubs[address] = _bus.Subscribe(new Sink(address));
        }

        _machine = new InputMachine<Code[]>(
            new MachineAddress("eye"), new Passthrough(),
            new Nothing(), _bus, _ring, Dials);
        _handles.Add(_bus.Subscribe(_machine));
    }

    public void Dispose()
    {
        foreach (var handle in _handles) handle.Dispose();
        foreach (var stub in _stubs.Values) stub.Dispose();
    }

    private sealed class Sink(ClusterAddress address) : IReceiveEnvelopes
    {
        public ClusterAddress Address { get; } = address;

        public Task DeliverAsync(Envelope envelope, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class Passthrough : IQuantizer<Code[]>
    {
        public byte Modality => 1;

        public IReadOnlyCollection<Code> Codify(Code[] observation) => observation;
    }

    private sealed class Nothing : IRendezvous
    {
        public ValueTask JoinAsync(Occasion occasion, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }

    [Fact]
    public async Task A_departing_cluster_takes_exactly_the_routes_heading_into_it()
    {
        var codes = Enumerable.Range(1, 20).Select(i => C((ulong)i)).ToArray();
        var thought = await _machine.ThinkAsync(codes);
        await _bus.WhenQuiet().WaitAsync(Patience);

        // THE ORIGIN'S OWN FIRST SEND IS TRACKED. Without it a cluster that
        // dies before reporting anything would strand routes nobody knew about.
        var doomed = new ClusterAddress("a");
        var heading = thought.InFlightTo(doomed);
        Assert.True(heading > 0, "no origin routed to the cluster under test");
        Assert.Equal(20, thought.Live);

        _stubs[doomed].Dispose();

        // The loss is exact, and those routes are deaths rather than a mystery.
        Assert.Equal(20 - heading, thought.Live);
        Assert.Equal(heading, thought.Deaths);
        Assert.Equal(0, thought.InFlightTo(doomed));
    }

    [Fact]
    public async Task Losing_every_cluster_settles_the_thought()
    {
        // WHAT THE EVENT BUS WAS INTRODUCED FOR. Nothing here ever replies, so
        // without death events this thought waits forever and only a deadline
        // could end it — and a deadline is a constant nobody measured.
        var thought = await _machine.ThinkAsync(
            [.. Enumerable.Range(1, 20).Select(i => C((ulong)i))]);
        await _bus.WhenQuiet().WaitAsync(Patience);

        Assert.False(thought.Settled);

        foreach (var stub in _stubs.Values) stub.Dispose();

        Assert.True(thought.Settled);
        Assert.True(thought.Balanced());
        Assert.Equal(0, _machine.Pending);
    }

    [Fact]
    public async Task A_cluster_this_thought_never_reached_strands_nothing()
    {
        // The companion. Without it the tests above pass for a machine that
        // writes off every route whenever anything at all departs.
        var thought = await _machine.ThinkAsync([C(1)]);
        await _bus.WhenQuiet().WaitAsync(Patience);

        var owner = _ring.OwnerOf(C(1));
        var innocent = _stubs.Keys.First(a => a != owner);

        _stubs[innocent].Dispose();

        Assert.Equal(1, thought.Live);
        Assert.Equal(0, thought.Deaths);
        Assert.False(thought.Settled);
    }

    [Fact]
    public async Task A_cluster_reports_where_it_sent_the_routes_it_made()
    {
        // Covers the other half: a real cluster naming its destinations, which
        // is what the origin's per-cluster count is built from.
        var seen = new Listening();
        using var _ = _bus.Subscribe(seen);

        var ring = new Ring(seed: 7, replicas: 64);
        var local = new LocalClusters(ring);
        var marginals = new LocalMarginals(local);
        var address = new ClusterAddress("solo");
        ring.Join(address);
        var cluster = new Cluster(address, _bus, ring, Dials, marginals);
        local.Include(cluster);
        using var __ = _bus.Subscribe(cluster);

        var node = cluster.Admit(C(1));
        foreach (var partner in (Code[])[C(2), C(3), C(4)])
        {
            node.Observe(partner);
            cluster.Admit(partner).Note();
        }

        await cluster.DeliverAsync(new Envelope
        {
            To = address,
            Messages =
            [
                new Message
                {
                    Broadcast = BroadcastId.New(),
                    ReturnTo = seen.Address,
                    To = C(1),
                    Held = 10.0,
                    Chain = [C(1)],
                    Carried = 1.0,
                },
            ],
        });

        await _bus.WhenQuiet().WaitAsync(Patience);

        // Two reports come back: the origin's fork, then the three children
        // finding nowhere to go. The first is the one that names destinations.
        var forked = seen.Got.Single(r => r.Handled == 1);
        Assert.Equal(3, forked.SentInto.Sum(r => r.Count));
        Assert.Equal(address, forked.SentInto.Single().To);

        // The companion: the children reported handling those three and
        // sending nowhere, so the two halves of the count meet.
        var died = seen.Got.Single(r => r.Handled == 3);
        Assert.Empty(died.SentInto);
        Assert.Equal(3, died.Accounting.Deaths);
    }

    [Fact]
    public async Task Tracking_costs_one_entry_per_cluster_and_not_per_route()
    {
        // WHAT THE BOOKKEEPING ACTUALLY COSTS. A node forking to hundreds of
        // partners still names only the handful of clusters they went to, so a
        // report grows with the size of the NETWORK and not with the size of
        // the fan-out. That is the whole reason this is affordable.
        var seen = new Listening();
        using var _ = _bus.Subscribe(seen);

        var ring = new Ring(seed: 7, replicas: 256);
        var local = new LocalClusters(ring);
        var marginals = new LocalMarginals(local);
        var homes = new List<Cluster>();
        var handles = new List<IDisposable>();

        foreach (var name in (string[])["p", "q", "r", "s", "t"])
        {
            var address = new ClusterAddress(name);
            ring.Join(address);
            var cluster = new Cluster(address, _bus, ring, Dials, marginals);
            local.Include(cluster);
            homes.Add(cluster);
            handles.Add(_bus.Subscribe(cluster));
        }

        var start = local.For(C(1));
        for (var i = 2UL; i <= 300; i++)
        {
            start.Observe(C(i));
            local.For(C(i)).Note();
        }

        var owner = homes.Single(c => c.Address == ring.OwnerOf(C(1)));
        await owner.DeliverAsync(new Envelope
        {
            To = owner.Address,
            Messages =
            [
                new Message
                {
                    Broadcast = BroadcastId.New(), ReturnTo = seen.Address, To = C(1),
                    Held = 10.0, Chain = [C(1)], Carried = 1.0,
                },
            ],
        });
        await _bus.WhenQuiet().WaitAsync(Patience);
        foreach (var handle in handles) handle.Dispose();

        var forked = seen.Got.Single(r => r.Handled == 1 && r.From == owner.Address);

        // 299 routes created, named in at most five entries.
        Assert.Equal(299, forked.SentInto.Sum(r => r.Count));
        Assert.True(forked.SentInto.Length <= 5,
            $"{forked.SentInto.Length} entries for 299 routes");
    }

    private sealed class Listening : IReceiveReports
    {
        private readonly List<Report> _got = [];

        public MachineAddress Address { get; } = new("listener");

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
}
