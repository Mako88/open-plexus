using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// The world boundary, both directions.
/// </summary>
public sealed class MachineTests : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private static Code C(ulong value) => new(Modality: 1, value);

    private static readonly WalkSettings Dials = new()
    {
        Stamina = 10.0,
        Cost = StepCost.Inverse,
        Refuel = Refuel.Strength,
        Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum,
            Horizon = 6,
    };

    private readonly HybridBus _bus = new();
    private readonly Ring _ring = new(seed: 42, replicas: 64);
    private readonly LocalClusters _local;
    private readonly LocalRendezvous _rendezvous;
    private readonly List<IDisposable> _handles = [];
    private readonly InputMachine<Code[]> _machine;

    public MachineTests()
    {
        _local = new LocalClusters(_ring);
        _rendezvous = new LocalRendezvous(_local);
        _bus.Faults += failure => throw failure;

        var marginals = new LocalMarginals(_local);
        foreach (var name in (string[])["a", "b", "c", "d"])
        {
            var address = new ClusterAddress(name);
            _ring.Join(address);
            var cluster = new Cluster(address, _bus, _ring, Dials, marginals);
            _local.Include(cluster);
            _handles.Add(_bus.Subscribe(cluster));
        }

        _machine = new InputMachine<Code[]>(
            new MachineAddress("eye"), new Passthrough(), _rendezvous, _bus, _ring, Dials);
        _handles.Add(_bus.Subscribe(_machine));
    }

    public void Dispose()
    {
        foreach (var handle in _handles) handle.Dispose();
    }

    /// <summary>A front end that hands back exactly what it was given.</summary>
    private sealed class Passthrough : IQuantizer<Code[]>
    {
        public byte Modality => 1;

        public IReadOnlyCollection<Code> Codify(Code[] observation) => observation;
    }

    /// <summary>
    /// Clears the live set, then observes — so the frame really does onset.
    /// </summary>
    /// <remarks>
    /// Needed because repeating a frame is SILENT by design. A test that asked
    /// a question by re-sending the same observation would get no thought at
    /// all, which is the mechanism working rather than failing.
    /// </remarks>
    private async Task<Thought?> Ask(long now, params Code[] frame)
    {
        await Observe(now, []);
        return await Observe(now + 1, frame);
    }

    private async Task<Thought?> Observe(long now, params Code[] frame)
    {
        var thought = await _machine.ObserveAsync(frame, now);
        await _bus.WhenQuiet().WaitAsync(Patience);
        return thought;
    }

    // ---- the input path ---------------------------------------------------

    [Fact]
    public async Task A_frame_that_changed_nothing_starts_no_thought()
    {
        Assert.NotNull(await Observe(0, C(1), C(2)));

        // A stable scene is silent, all the way up to the machine.
        Assert.Null(await Observe(1, C(1), C(2)));
    }

    [Fact]
    public async Task An_onset_both_writes_counts_and_starts_a_thought()
    {
        await Observe(0, C(1), C(2));

        // LEARNING happened...
        Assert.Equal(1.0, _local.For(C(1)).Together(C(2)));

        // ...and THINKING happened. Two paths, one call, and the companion for
        // each is the other.
        var thought = await Observe(1, C(1), C(2), C(3));
        Assert.NotNull(thought);

        // A BROADCAST IS ONE PENDING UNIT PER CLUSTER, so the thought starts
        // with as many routes as there are clusters — not as many as there are
        // origin codes. The origin cannot know how many nodes will fire; that
        // depends on who holds what, which is the knowledge a broadcast exists
        // to avoid needing.
        Assert.True(thought.Balanced());
    }

    [Fact]
    public async Task A_thought_walks_to_what_the_graph_learned()
    {
        // Build a habit: C(9) is always there when C(1) starts.
        for (var i = 0; i < 5; i++)
        {
            await Observe(i * 2, C(9));
            await Observe(i * 2 + 1, C(9), C(1));
        }

        var thought = await Ask(100, C(9), C(1));

        Assert.NotNull(thought);
        Assert.True(thought.Endpoints > 0, "the broadcast reached nothing at all");
    }

    [Fact]
    public async Task A_settled_thought_is_released_rather_than_kept()
    {
        await Observe(0, C(1), C(2));

        // Termination is housekeeping, and in one process with nothing lost
        // every route really does return or die.
        Assert.Equal(0, _machine.Pending);
    }

    [Fact]
    public async Task A_report_for_an_unknown_broadcast_is_dropped()
    {
        await _machine.DeliverAsync(new Report
        {
            From = new ClusterAddress("a"),
            Handled = 1,
            SentInto = [],
            Arrivals = [],
            Accounting = new Accounting(BroadcastId.New(), 0, 1),
        });

        // C2 says late is normal, and a settled thought has nothing left to
        // refine — so this is dropped rather than throwing.
        Assert.Equal(0, _machine.Pending);
    }

    [Fact]
    public async Task An_origin_goes_to_every_cluster_and_the_ring_is_not_asked()
    {
        var codes = Enumerable.Range(1, 12).Select(i => C((ulong)i)).ToArray();
        var thought = await _machine.ThinkAsync(codes);
        await _bus.WhenQuiet().WaitAsync(Patience);

        // FORK 6. Every cluster is asked, and each replies — including the ones
        // holding none of these codes, which is what lets the count close when
        // the origin cannot know how many routes it started.
        Assert.True(thought.Settled);
        Assert.True(thought.Balanced());

        // Nothing has been learned, so no cluster holds any of these codes and
        // every unit dies. Four clusters, four deaths.
        Assert.Equal(4, thought.Deaths);
    }

    [Fact]
    public async Task A_broadcast_never_creates_a_node()
    {
        // A routed message is addressed to a code and brings it into existence.
        // A broadcast is a question put to everyone, and admitting on one would
        // put every code on every cluster.
        await _machine.ThinkAsync([C(500), C(501)]);
        await _bus.WhenQuiet().WaitAsync(Patience);

        Assert.False(_local.TryOwner(C(500), out var owner) && owner.TryGet(C(500), out _));

        // The companion: learning DOES create it, so the assertion above is
        // about broadcasts rather than about nodes never appearing.
        await Observe(0, C(500), C(501));
        Assert.True(_local.TryOwner(C(500), out var home) && home.TryGet(C(500), out _));
    }

    [Fact]
    public async Task What_just_started_is_not_also_reported_as_already_live()
    {
        // `Occasion` is the learning path's wire format, so `Live` has to be a
        // true statement about the world: what was ALREADY there. A distributed
        // rendezvous reading onsets out of it would be reading a lie, even
        // though the local one unions the two and cannot tell.
        var seen = new Recording();
        var machine = new InputMachine<Code[]>(
            new MachineAddress("probe"), new Passthrough(), seen, _bus, _ring, Dials);
        using var _ = _bus.Subscribe(machine);

        await machine.ObserveAsync([C(1), C(2)], 0);
        await machine.ObserveAsync([C(1), C(2), C(3)], 1);
        await _bus.WhenQuiet().WaitAsync(Patience);

        var second = seen.Occasions[1];
        Assert.Equal([C(3)], second.Onsets.ToArray());
        Assert.DoesNotContain(C(3), second.Live);
        Assert.Equal(2, second.Live.Length);
    }

    /// <summary>Keeps every occasion it is handed, and joins nothing.</summary>
    private sealed class Recording : IRendezvous
    {
        private readonly List<Occasion> _occasions = [];

        public IReadOnlyList<Occasion> Occasions
        {
            get { lock (_occasions) return [.. _occasions]; }
        }

        public ValueTask JoinAsync(Occasion occasion, CancellationToken ct = default)
        {
            lock (_occasions) _occasions.Add(occasion);
            return ValueTask.CompletedTask;
        }
    }

    // ---- the output path --------------------------------------------------

    [Fact]
    public async Task Arrival_narrows_to_the_machines_own_codes()
    {
        var action = C(50);
        var other = C(51);
        var output = new OutputMachine(new MachineAddress("hand"), [action]);

        // Both are reachable from C(1), and only one of them is an action.
        for (var i = 0; i < 6; i++)
        {
            await Observe(i * 3, C(1));
            await Observe(i * 3 + 1, C(1), action);
            await Observe(i * 3 + 2, C(1), action, other);
        }

        var thought = await Ask(500, C(1));
        Assert.NotNull(thought);

        var chosen = output.Choose(thought);

        // Selection IS routing: the candidates are exactly the chains that
        // reached this machine's codes.
        Assert.Equal(action, chosen);

        // The companion. Without it this passes for a Choose that returns its
        // first code regardless of whether anything arrived.
        Assert.Contains(other, thought.Best(int.MaxValue).Select(a => a.Endpoint));
    }

    [Fact]
    public async Task Nothing_reached_is_a_real_answer()
    {
        var output = new OutputMachine(new MachineAddress("hand"), [C(999)]);

        var thought = await Observe(0, C(1), C(2));
        Assert.NotNull(thought);

        // The only honest answer for a situation nothing was ever written
        // about, and the caller has to decide what to do with a system that has
        // nothing to say.
        Assert.Null(output.Choose(thought));
    }

    [Fact]
    public async Task The_chosen_action_comes_with_the_chain_that_produced_it()
    {
        var action = C(50);
        var output = new OutputMachine(new MachineAddress("hand"), [action]);

        for (var i = 0; i < 6; i++)
        {
            await Observe(i * 2, C(1));
            await Observe(i * 2 + 1, C(1), action);
        }

        var thought = await Ask(500, C(1));
        var explained = output.Explain(thought!);

        Assert.NotNull(explained);
        Assert.Equal(action, explained.Endpoint);

        // A route arrives carrying the whole chain of reasoning that produced
        // it — which is the property the broadcast design exists for.
        Assert.Equal(action, explained.Chain[^1]);
        Assert.True(explained.Chain.Length >= 2, "the chain does not say where it came from");
    }

    [Fact]
    public void An_output_machine_with_no_codes_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OutputMachine(new MachineAddress("hand"), []));
    }

    // ---- fork 5 -----------------------------------------------------------

    [Fact]
    public void A_cluster_leaving_is_counted_and_nothing_else_happens()
    {
        // OPEN FORK 5, asserted as it currently stands rather than as it should
        // stand. A thought does not track which clusters its routes sit in, so
        // it cannot tell whether a death affects it. This records the count and
        // releases nothing.
        Assert.Equal(0, _machine.DeathsSeen);

        _handles[0].Dispose();

        Assert.Equal(1, _machine.DeathsSeen);
    }
}
