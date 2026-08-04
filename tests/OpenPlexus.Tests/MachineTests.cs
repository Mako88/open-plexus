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

    private static Code C(ulong value) => Fixture.C(value);

    private static readonly WalkSettings Dials = Fixture.Dials(stamina: 10.0, horizon: 6);

    private readonly Bench _bench = new(Dials, listening: true);
    private readonly InputMachine<Code[]> _machine;

    public MachineTests()
    {
        _machine = new InputMachine<Code[]>(
            new MachineAddress("eye"), new Passthrough(),
            _bench.Rendezvous, _bench.Bus, _bench.Ring, Dials);

        _bench.Subscribe(_machine);
    }

    public void Dispose() => _bench.Dispose();

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
        await _bench.Bus.WhenIdle().WaitAsync(Patience);
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
        Assert.Equal(1.0, _bench.Local.For(C(1)).Together(C(2)));

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
    public async Task A_settled_thought_is_retired_but_not_the_instant_it_settles()
    {
        await Observe(0, C(1), C(2));

        // NOT ZERO, AND THAT IS FORK 22'S FIX RATHER THAN A REGRESSION. This
        // used to untrack a thought the moment its live count hit zero, and a
        // live count of zero is not durable: reports arrive out of order, so it
        // dips transiently whenever a downstream death is folded before the
        // upstream split that created it. A thought untracked in that dip lost
        // every later report and could never settle -- 7 of 60 questions on the
        // senses world.
        Assert.Equal(1, _machine.Pending);

        // Retirement asks TWICE instead, on the two following thoughts: settled
        // last time, settled now, and nothing folded in between. A report is the
        // only thing that can move the count, so two clean looks cannot both
        // land inside a flicker.
        await Observe(1, C(3), C(4));
        await Observe(2, C(5), C(6));

        // So the machine runs a couple of thoughts behind rather than growing.
        Assert.True(_machine.Pending <= 2, $"{_machine.Pending} thoughts still tracked");
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
        await _bench.Bus.WhenIdle().WaitAsync(Patience);

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
        await _bench.Bus.WhenIdle().WaitAsync(Patience);

        Assert.False(_bench.Local.TryOwner(C(500), out var owner) && owner.TryGet(C(500), out _));

        // The companion: learning DOES create it, so the assertion above is
        // about broadcasts rather than about nodes never appearing.
        await Observe(0, C(500), C(501));
        Assert.True(_bench.Local.TryOwner(C(500), out var home) && home.TryGet(C(500), out _));
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
            new MachineAddress("probe"), new Passthrough(), seen, _bench.Bus, _bench.Ring, Dials);
        using var _ = _bench.Bus.Subscribe(machine);

        await machine.ObserveAsync([C(1), C(2)], 0);
        await machine.ObserveAsync([C(1), C(2), C(3)], 1);
        await _bench.Bus.WhenIdle().WaitAsync(Patience);

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

    [Fact]
    public async Task A_budget_handed_to_a_thought_reaches_the_messages()
    {
        // THE WIRING TEST THIS PARAMETER SHOULD HAVE HAD FROM THE START. It was
        // added, passed at the call site, and silently dropped at the
        // destination -- so a sweep of it measured a disconnected dial and
        // reported it inert. Nothing else in the system could have noticed.
        var seen = new Watching();
        using var _ = _bench.Bus.Subscribe(seen);

        var machine = new InputMachine<Code[]>(
            new MachineAddress("probe"), new Passthrough(), new Nothing(), _bench.Bus, _bench.Ring, Dials);
        using var __ = _bench.Bus.Subscribe(machine);

        await machine.ThinkAsync([C(700)], 3.5);
        await _bench.Bus.WhenIdle().WaitAsync(Patience);

        Assert.Equal(3.5, seen.Held.Single(), precision: 10);
    }

    [Fact]
    public async Task Handing_no_budget_takes_the_dial_it_was_built_with()
    {
        // The companion. Without it the test above passes for a machine that
        // ignores its own settings instead.
        var seen = new Watching();
        using var _ = _bench.Bus.Subscribe(seen);

        var machine = new InputMachine<Code[]>(
            new MachineAddress("probe2"), new Passthrough(), new Nothing(), _bench.Bus, _bench.Ring, Dials);
        using var __ = _bench.Bus.Subscribe(machine);

        await machine.ThinkAsync([C(701)], null);
        await _bench.Bus.WhenIdle().WaitAsync(Patience);

        Assert.Equal(Dials.Stamina, seen.Held.Single(), precision: 10);
    }

    /// <summary>Catches what a broadcast actually put on the bus.</summary>
    private sealed class Watching : IReceiveEnvelopes
    {
        private readonly List<double> _held = [];

        public ClusterAddress Address { get; } = new("watcher");

        public IReadOnlyList<double> Held
        {
            get { lock (_held) return [.. _held]; }
        }

        public Task DeliverAsync(Envelope envelope, CancellationToken ct = default)
        {
            lock (_held)
                foreach (var message in envelope.Messages)
                    if (!_held.Contains(message.Held)) _held.Add(message.Held);

            return Task.CompletedTask;
        }
    }

    private sealed class Nothing : IRendezvous
    {
        public ValueTask JoinAsync(Occasion occasion, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }

    // ---- the registration window -------------------------------------------

    /// <summary>
    /// A bus that replies before <c>BroadcastAsync</c> returns.
    /// </summary>
    /// <remarks>
    /// <b>Not a contrived case — it is what the real bus does.</b> Dispatch is
    /// <c>Task.Run</c>, so a cluster can finish and report back while the origin
    /// is still inside the broadcast call. This one does it deterministically.
    /// </remarks>
    private sealed class RepliesImmediately : IBus
    {
        private readonly ClusterAddress _one = new("eager");

        public IReceiveReports? Machine { get; set; }

        public ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
            Envelope envelope,
            CancellationToken ct = default,
            Action<IReadOnlyCollection<ClusterAddress>>? ready = null)
        {
            IReadOnlyCollection<ClusterAddress> everyone = [_one];
            ready?.Invoke(everyone);

            // THE WHOLE POINT: the reply lands before the caller gets control
            // back. If the thought was recorded after the broadcast returned,
            // this report is dropped and the thought never settles.
            Machine!.DeliverAsync(new Report
            {
                From = _one,
                Handled = 1,
                SentInto = [],
                Arrivals = [],
                Accounting = new Accounting(
                    envelope.Messages[0].Broadcast, Splits: 0, Deaths: 1),
            }, ct).GetAwaiter().GetResult();

            return ValueTask.FromResult(everyone);
        }

        public ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default) =>
            ValueTask.CompletedTask;

        public ValueTask SendAsync(MachineAddress to, Report report, CancellationToken ct = default) =>
            ValueTask.CompletedTask;

        public IDisposable Subscribe(IReceiveEnvelopes cluster) => new Handle();

        public IDisposable Subscribe(IReceiveReports machine) => new Handle();

        // NOT SILENT. This double exists to reproduce one race on the report
        // path; a test that reached fork 11's path through it would be measuring
        // a fake rather than the bus.
        public IDisposable Listen(IReceiveArrivals machine, IReadOnlyCollection<Code> codes) =>
            throw new NotSupportedException("this double does not route arrivals");

        public ValueTask PublishAsync(Settled settled, CancellationToken ct = default) =>
            throw new NotSupportedException("this double does not route arrivals");

        /// <summary>Nothing ever leaves here; the race is the subject, not death.</summary>
        public event Action<ClusterAddress>? Deaths
        {
            add { }
            remove { }
        }

        private sealed class Handle : IDisposable
        {
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task A_reply_that_beats_the_broadcast_back_is_not_lost()
    {
        // MEASURED, AND IT WAS LOSING THEM. Registering the thought after the
        // broadcast returned dropped every report that raced it -- the thought
        // then held no arrivals and never settled, which downstream is
        // indistinguishable from a graph that had nothing to say.
        var bus = new RepliesImmediately();
        var machine = new InputMachine<Code[]>(
            new MachineAddress("m"), new Passthrough(), new Nothing(), bus, _bench.Ring, Dials);
        bus.Machine = machine;

        var thought = await machine.ThinkAsync([C(1)]);

        Assert.True(thought.Settled,
            "the only reply was dropped, so the thought is still waiting on it");
        Assert.Equal(1, thought.Deaths);
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

        _bench.Depart();

        Assert.Equal(1, _machine.DeathsSeen);
    }
}
