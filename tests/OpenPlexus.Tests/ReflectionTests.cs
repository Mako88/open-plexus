using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// Fork 21 — a conclusion becomes an observation.
/// </summary>
/// <remarks>
/// <para>
/// The whole claim is that <b>a route walked often enough becomes a direct
/// edge</b>, so the composition stops being re-derived every time. The test of
/// that is an edge which provably did not exist before and provably does after,
/// and the companion is the same run with the mechanism off.
/// </para>
/// <para>
/// <b>The setup is the senses world in miniature.</b> A occurs with B, then B
/// occurs with C, and A never occurs with C. Reaching C from A is therefore a
/// two-hop composition and nothing else — exactly the structure the second world
/// was built around, small enough to assert on directly.
/// </para>
/// </remarks>
public sealed class ReflectionTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    private static readonly Code A = C(1), B = C(2), Z = C(3);

    private static WalkSettings Dials(Reflection? reflect) => new()
    {
        Stamina = 8.0,
        Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum,
        Horizon = 20,
        Reflect = reflect,
    };

    /// <summary>The codes are already codes; there is nothing to quantise.</summary>
    private sealed class Passthrough : IQuantizer<IReadOnlyCollection<Code>>
    {
        public byte Modality => 1;

        public IReadOnlyCollection<Code> Codify(IReadOnlyCollection<Code> observation) =>
            observation;
    }

    // ---- the weight, which is what made any of this expressible -------------

    [Fact]
    public void An_occasion_can_be_worth_less_than_one()
    {
        var node = new Node(A, Dials(null));

        node.Note(0.25);
        node.Observe(B, 0.25);

        Assert.Equal(0.25, node.Seen, precision: 10);
        Assert.Equal(0.25, node.Together(B), precision: 10);
    }

    [Fact]
    public void An_occasion_is_worth_one_unless_it_says_otherwise()
    {
        // The companion. Without it the test above passes for an implementation
        // that ignores the parameter and always writes 0.25.
        var node = new Node(A, Dials(null));

        node.Note();
        node.Observe(B);

        Assert.Equal(1.0, node.Seen, precision: 10);
        Assert.Equal(1.0, node.Together(B), precision: 10);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void An_occasion_worth_nothing_is_refused(double by)
    {
        // A zero-weight write would move `together` without moving `seen` on the
        // other side and score the pair above 1.0 -- the ever-present-partner
        // failure the forward weighting exists to prevent.
        var node = new Node(A, Dials(null));

        Assert.Throws<ArgumentOutOfRangeException>(() => node.Note(by));
        Assert.Throws<ArgumentOutOfRangeException>(() => node.Observe(B, by));
    }

    // ---- why a route died, which is what makes compression self-regulating ---

    private static Message Arriving(Code to, double held, double together, params Code[] chain) =>
        new()
        {
            Broadcast = BroadcastId.New(),
            ReturnTo = new MachineAddress("m"),
            To = to,
            Held = held,
            Chain = [.. chain],
            Carried = 1.0,
            Together = together,
        };

    [Fact]
    public void A_route_that_could_not_pay_for_the_hop_it_was_on_says_so()
    {
        // It could not complete the hop it was already taking, so it reached
        // NOTHING. That is the walk saying "I could not get there", and it is
        // the condition under which minting a shortcut pays.
        var node = new Node(B, Dials(null));
        node.Note();
        node.Observe(Z);

        // Weight is together/seen = 1.0, so the hop costs 1 and 0.5 cannot pay.
        var fired = node.Fire(Arriving(B, held: 0.5, together: 1.0, A, B));

        Assert.Null(fired.Reached);
        Assert.Equal(1, fired.Accounting.Deaths);
        Assert.Equal(1, fired.Accounting.Starved);
    }

    [Fact]
    public void A_route_that_arrived_and_stopped_there_is_not_starved()
    {
        // MEASURED, AND COUNTING THIS AS STARVATION BROKE THE SIGNAL. Inverse
        // cost exists to exhaust the budget, so running out is how nearly every
        // route ends. This one ARRIVED and produced its arrival; it is finished
        // rather than thwarted, and calling it hungry made hunger high on every
        // walk -- which turned the adaptive weight into the fixed one in
        // disguise.
        var node = new Node(B, Dials(null));
        node.Note();
        node.Observe(Z);

        var fired = node.Fire(Arriving(B, held: 1.5, together: 1.0, A, B));

        Assert.NotNull(fired.Reached);
        Assert.Equal(1, fired.Accounting.Deaths);
        Assert.Equal(0, fired.Accounting.Starved);
    }

    [Fact]
    public void A_route_that_dies_having_run_out_of_partners_does_not()
    {
        // THE COMPANION, AND WITHOUT IT THE SIGNAL IS MEANINGLESS. If every
        // death counted as starvation, hunger would be 1.0 on every walk and
        // the adaptive weight would just be the fixed one wearing a disguise.
        var node = new Node(B, Dials(null));
        node.Note();
        node.Observe(A);

        // Plenty of budget; its only partner is already in the chain.
        var fired = node.Fire(Arriving(B, held: 500.0, together: 1.0, A, B));

        Assert.Equal(1, fired.Accounting.Deaths);
        Assert.Equal(0, fired.Accounting.Starved);
    }

    [Fact]
    public async Task A_walk_that_reached_everything_it_could_is_not_hungry()
    {
        using var world = new World(null);
        await world.TeachAsync(times: 5);

        var thought = await world.Machine.ThinkAsync([A], 500.0);
        await world.SettleAsync(thought);

        Assert.True(thought.Deaths > 0, "nothing died, so hunger is untested");
        Assert.Equal(0.0, thought.Hunger);
    }

    [Fact]
    public async Task A_walk_that_could_not_afford_the_graph_is()
    {
        // The companion. A budget too small to take a second hop starves.
        using var world = new World(null);
        await world.TeachAsync(times: 5);

        var thought = await world.Machine.ThinkAsync([A], 2.0);
        await world.SettleAsync(thought);

        Assert.True(thought.Hunger > 0.0, $"hunger {thought.Hunger} on a starved walk");
    }

    // ---- the mechanism ------------------------------------------------------

    private sealed class World : IDisposable
    {
        public readonly HybridBus Bus = new();
        public readonly LocalClusters Local;
        public readonly InputMachine<IReadOnlyCollection<Code>> Machine;
        private readonly List<IDisposable> _handles = [];

        public World(Reflection? reflect)
        {
            var dials = Dials(reflect);
            var ring = new Ring(seed: 7, replicas: 64);
            Local = new LocalClusters(ring);

            foreach (var name in (string[])["a", "b"])
            {
                var address = new ClusterAddress(name);
                ring.Join(address);
                var cluster = new Cluster(address, Bus, ring, dials);
                Local.Include(cluster);
                _handles.Add(Bus.Subscribe(cluster));
            }

            Machine = new InputMachine<IReadOnlyCollection<Code>>(
                new MachineAddress("m"), new Passthrough(), new LocalRendezvous(Local),
                Bus, ring, dials);

            _handles.Add(Bus.Subscribe(Machine));
        }

        /// <summary>A occurs with B; then B occurs with Z. A and Z never meet.</summary>
        public async Task TeachAsync(int times)
        {
            for (var round = 0; round < times; round++)
            {
                await Machine.ObserveAsync([A, B], round * 2);
                await Quiet();
                await Machine.ObserveAsync([B, Z], (round * 2) + 1);
                await Quiet();
            }
        }

        public Task Quiet() =>
            Bus.WhenQuiet().WaitAsync(TimeSpan.FromSeconds(30));

        /// <summary>
        /// Waits on the THOUGHT'S OWN ACCOUNTING rather than on the bus.
        /// </summary>
        /// <remarks>
        /// <b>The bus going quiet does not mean the walk finished.</b> In-flight
        /// hits zero in the gap between a cluster handling a message and
        /// dispatching what that message produced, so <c>WhenQuiet</c> can
        /// return mid-walk — fork 12, observed here as a thought with
        /// <c>live=2, deaths=0</c> and no arrivals at all. <see cref="Thought.Settled"/>
        /// is the signal that every route has returned or died.
        /// </remarks>
        public async Task SettleAsync(Thought thought)
        {
            for (var waited = 0; !thought.Settled && waited < 30_000; waited += 2)
                await Task.Delay(2);

            await Quiet();
        }

        public void Dispose()
        {
            foreach (var handle in _handles) handle.Dispose();
        }
    }

    private static Reflection Eager => new() { Threshold = 0.0, Weight = 0.5, Names = 5 };

    [Fact]
    public async Task A_and_Z_never_meet_in_the_world_itself()
    {
        // THE PREMISE, ASSERTED BEFORE ANY RESULT IS READ FROM IT. If A and Z
        // ever co-occurred, a minted A-Z edge would prove nothing whatever.
        using var world = new World(null);
        await world.TeachAsync(times: 5);

        Assert.Equal(0.0, world.Local.For(A).Together(Z));
        Assert.Equal(0.0, world.Local.For(Z).Together(A));

        // The companion: the one-hop edges the composition must run through DO
        // exist, so the zero above is an absence rather than an empty graph.
        Assert.True(world.Local.For(A).Together(B) > 0.0);
        Assert.True(world.Local.For(B).Together(Z) > 0.0);
    }

    [Fact]
    public async Task Reflection_mints_the_edge_that_did_not_exist()
    {
        using var world = new World(Eager);
        await world.TeachAsync(times: 5);

        var thought = await world.Machine.ThinkAsync(
            [A]);
        await world.SettleAsync(thought);

        var written = await world.Machine.ReflectAsync(
            thought, now: 99);

        Assert.True(written > 0, "the walk reached nothing worth writing down");
        Assert.True(world.Local.For(A).Together(Z) > 0.0,
            "A and Z have still never been connected, so nothing was composed");
    }

    [Fact]
    public async Task Reflection_off_mints_nothing()
    {
        // The companion, and it is the control the measurement will use.
        using var world = new World(null);
        await world.TeachAsync(times: 5);

        var thought = await world.Machine.ThinkAsync(
            [A]);
        await world.SettleAsync(thought);

        var written = await world.Machine.ReflectAsync(
            thought, now: 99);

        Assert.Equal(0, written);
        Assert.Equal(0.0, world.Local.For(A).Together(Z));
    }

    [Fact]
    public async Task The_nucleation_threshold_bites()
    {
        // Above every reachable score, so nothing is worth its own storage.
        using var world = new World(new Reflection
        {
            Threshold = 1_000.0, Weight = 0.5, Names = 5,
        });
        await world.TeachAsync(times: 5);

        var thought = await world.Machine.ThinkAsync(
            [A]);
        await world.SettleAsync(thought);

        Assert.Equal(0, await world.Machine.ReflectAsync(
            thought, now: 99));
        Assert.Equal(0.0, world.Local.For(A).Together(Z));
    }

    [Fact]
    public async Task A_conclusion_is_written_lighter_than_an_observation()
    {
        // The discount is the whole defence against the system learning its own
        // hallucinations, so an implementation that ignored it would still pass
        // every test above.
        using var world = new World(new Reflection
        {
            Threshold = 0.0, Weight = 0.25, Names = 5,
        });
        await world.TeachAsync(times: 5);

        var thought = await world.Machine.ThinkAsync(
            [A]);
        await world.SettleAsync(thought);
        var written = await world.Machine.ReflectAsync(thought, now: 99);

        var minted = world.Local.For(A).Together(Z);
        var saw = string.Join(",", thought.Best(9).Select(a => $"{a.Endpoint}:{a.Score:F3}"));

        Assert.True(minted > 0.0,
            $"nothing was minted: written={written} endpoints=[{saw}] " +
            $"AB={world.Local.For(A).Together(B)} seenB={world.Local.For(B).Seen} " +
            $"BZ={world.Local.For(B).Together(Z)} seenZ={world.Local.For(Z).Seen} " +
            $"live={thought.Live} deaths={thought.Deaths} splits={thought.Splits} released={thought.Released}");
        Assert.True(minted <= 0.25 + 1e-9,
            $"a conclusion was written at {minted}, which is heavier than the 0.25 it was worth");
    }

    [Fact]
    public async Task Reflecting_never_re_pairs_the_codes_it_started_from()
    {
        // Those coincided when they were observed and that was counted then.
        // Counting them again on every thought would inflate exactly the
        // association the walk set out from.
        using var world = new World(Eager);
        await world.TeachAsync(times: 5);

        var thought = await world.Machine.ThinkAsync(
            [A, B]);
        await world.SettleAsync(thought);

        var before = world.Local.For(A).Together(B);
        await world.Machine.ReflectAsync(thought, now: 99);

        Assert.Equal(before, world.Local.For(A).Together(B), precision: 10);

        // The companion: something DID move, so the equality above is not just a
        // reflection that did nothing at all.
        Assert.True(world.Local.For(A).Together(Z) > 0.0);
    }

    [Fact]
    public async Task Together_never_exceeds_seen_after_reflecting()
    {
        // THE INVARIANT THE WEIGHT COULD HAVE BROKEN. An edge is scored
        // together(here, other) / seen(other); a pair written heavier than it
        // was noted scores above 1.0 and becomes the strongest partner in the
        // graph by arithmetic rather than by evidence.
        using var world = new World(Eager);
        await world.TeachAsync(times: 5);

        for (var round = 0; round < 5; round++)
        {
            var thought = await world.Machine.ThinkAsync(
                [A]);
            await world.SettleAsync(thought);
            await world.Machine.ReflectAsync(
                thought, now: 100 + round);
        }

        foreach (var code in (Code[])[A, B, Z])
        {
            var node = world.Local.For(code);

            foreach (var partner in node.Partners())
                Assert.True(node.Together(partner) <= world.Local.For(partner).Seen + 1e-9,
                    $"together({code}, {partner}) exceeds seen({partner})");
        }
    }
}
