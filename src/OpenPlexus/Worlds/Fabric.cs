using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Machines;
using OpenPlexus.Thinking;

namespace OpenPlexus.Worlds;

/// <summary>
/// A bus, a ring, and a set of clusters — everything a world needs underneath it
/// that has nothing to do with the world.
/// </summary>
/// <remarks>
/// <para>
/// <b>This was written three times.</b> Snake, senses and binding each stood up
/// their own bus, joined their own clusters, collected their own faults, computed
/// their own node and edge totals and disposed their own handles, in code that
/// differed only in whitespace — and two of them carried an identical copy of the
/// settle loop, which is the one piece here that has ever been subtly wrong.
/// </para>
/// <para>
/// <b>It holds no machine.</b> A world's input machine knows its frame type and
/// its quantiser, and snake has an output machine besides — so the machines stay
/// with the worlds and only the fabric underneath is shared.
/// </para>
/// </remarks>
public sealed class Fabric : IDisposable
{
    /// <summary>
    /// How long anything here will wait on the bus before deciding something is
    /// wrong. <b>Long, because it is a failure signal and not a timing dial.</b>
    /// </summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a caller waits for one thought to finish.
    /// </summary>
    /// <remarks>
    /// <b>Short, and every expiry is counted rather than absorbed</b> — a wait
    /// that quietly gives up produces "nothing reached", which is
    /// indistinguishable in a score from a graph that had nothing to say. That
    /// was fork 22's symptom for weeks.
    /// </remarks>
    private static readonly TimeSpan Waiting = TimeSpan.FromMilliseconds(250);

    private readonly List<Cluster> _clusters = [];
    private readonly List<IDisposable> _handles = [];
    private readonly List<Exception> _faults = [];

    /// <summary>
    /// The standing preamble every world runner opens with: check what it was
    /// handed, then stand up the fabric.
    /// </summary>
    /// <remarks>
    /// <b>EXTRACTED BECAUSE THE DIAL MIGRATION MADE THE WORLDS IDENTICAL HERE, and
    /// the clone budget said so within the hour.</b> Once a world stopped choosing
    /// its own arms, every runner's constructor became the same four lines — which
    /// is the migration working rather than a fault, but duplicated code is
    /// duplicated whatever produced it.
    /// <para>
    /// <b>The honest fix is a shared base for the runners</b>, which is a larger
    /// change than this and is written down rather than half-done here.
    /// </para>
    /// </remarks>
    /// <param name="world">The world's own settings, checked and not otherwise used.</param>
    /// <param name="dials">The walk.</param>
    /// <param name="seed">The ring's seed.</param>
    /// <param name="clusters">How many clusters the fabric holds.</param>
    /// <param name="replicas">Ring replicas per cluster.</param>
    public static Fabric Standing(
        object world, WalkSettings dials, int seed, int clusters, int replicas)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dials);

        return new Fabric(dials, seed, clusters, replicas);
    }

    /// <param name="dials">The walk every cluster is built with.</param>
    /// <param name="seed">The ring's seed, and the jitter's.</param>
    /// <param name="clusters">How many clusters to stand up.</param>
    /// <param name="replicas">Ring points per cluster.</param>
    /// <param name="late">
    /// <inheritdoc cref="Lateness" path="/summary"/> <b>Null is every measurement
    /// taken before it existed.</b>
    /// </param>
    public Fabric(
        WalkSettings dials,
        int seed,
        int clusters = 8,
        int replicas = 256,
        Lateness? late = null)
    {
        ArgumentNullException.ThrowIfNull(dials);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clusters);

        Bus = new HybridBus(late);
        Ring = new Ring(seed, replicas);
        Local = new LocalClusters(Ring);

        // A delivery that throws would otherwise vanish, and a run reporting
        // numbers built on swallowed failures is worse than one that stops.
        Bus.Faults += failure => { lock (_faults) _faults.Add(failure); };

        for (var i = 0; i < clusters; i++)
        {
            var address = new ClusterAddress($"c{i}");
            Ring.Join(address);
            var cluster = new Cluster(address, Bus, Ring, dials);
            Local.Include(cluster);
            _clusters.Add(cluster);
            _handles.Add(Bus.Subscribe(cluster));
        }
    }

    public HybridBus Bus { get; }

    public Ring Ring { get; }

    public LocalClusters Local { get; }

    /// <summary>Nodes across every cluster.</summary>
    public int Nodes => _clusters.Sum(cluster => cluster.Count);

    /// <summary>Partner entries across every node. The graph's size.</summary>
    public int Edges => _clusters.Sum(cluster => cluster.Edges);

    /// <summary>How many nodes each cluster holds.</summary>
    public IReadOnlyList<int> Spread => [.. _clusters.Select(cluster => cluster.Count)];

    /// <inheritdoc cref="Plumbing.Widest"/>
    public int Widest => _clusters.Count == 0 ? 0 : _clusters.Max(cluster => cluster.Widest);

    /// <inheritdoc cref="Graph.Cluster.Temporal"/>
    public int Temporal => _clusters.Sum(cluster => cluster.Temporal);

    /// <inheritdoc cref="Graph.Cluster.Meddled"/>
    public int Meddled => _clusters.Sum(cluster => cluster.Meddled);

    /// <summary>
    /// What the machinery did, in the form every world's result carries it.
    /// </summary>
    /// <remarks>
    /// <b>Read at the end of a run, never during one.</b> Everything here is a
    /// live total over the clusters, so a <see cref="Plumbing"/> taken mid-run is
    /// a snapshot of an unfinished graph rather than a wrong number.
    /// </remarks>
    /// <param name="chains">The histogram the run collected as it went.</param>
    /// <param name="unbalanced">Thoughts whose own accounting did not close.</param>
    public Plumbing Facts(Chains chains, int unbalanced)
    {
        ArgumentNullException.ThrowIfNull(chains);

        return new Plumbing
        {
            Nodes = Nodes,
            Edges = Edges,
            Widest = Widest,
            Spread = Spread,
            ChainLengths = chains.ByLength,
            Messages = Bus.Messages,
            Unbalanced = unbalanced,
            Temporal = Temporal,
            Meddled = Meddled,
        };
    }

    /// <summary>Puts a machine on the bus and keeps the handle.</summary>
    public void Subscribe(IReceiveReports machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        _handles.Add(Bus.Subscribe(machine));
    }

    /// <summary>
    /// A sense that takes codes as they come, wired up and listening.
    /// </summary>
    /// <remarks>
    /// <b>THE SAME SIX STATEMENTS WERE IN EVERY WORLD, AND MAKING THE FRONT END
    /// SHARED IS WHAT MADE THEM IDENTICAL.</b> While each run built its own nested
    /// quantiser the construction differed by that one word, which was enough to
    /// keep <c>DuplicationTests</c> quiet; with <see cref="Codes.Passthrough"/>
    /// named once, <c>ClevrRun</c> and <c>MotifRun</c> became the same six lines
    /// and the budget said so immediately. <b>The duplication was always there —
    /// what changed is that it became detectable.</b>
    /// </remarks>
    /// <param name="name">What to call this machine on the bus.</param>
    /// <param name="dials">The walk this sense thinks with.</param>
    public InputMachine<Coded> Watching(string name, WalkSettings dials) =>
        Watching(name, dials, new Passthrough());

    /// <summary>
    /// A sense with a front end of its own, wired up and listening.
    /// </summary>
    /// <remarks>
    /// <b>ONE MACHINE PER BODY AND NOT PER SENSOR.</b> A body reading several
    /// streams hands them all to one machine through
    /// <see cref="Codes.Compound{TFrame}"/>, because an occasion is what pairs
    /// codes together and a sensor on its own machine could never co-occur with
    /// anything — which is the sight–sound edge, and the whole point.
    /// </remarks>
    public InputMachine<TFrame> Watching<TFrame>(
        string name, WalkSettings dials, IQuantizer<TFrame> sense)
    {
        var machine = new InputMachine<TFrame>(
            new MachineAddress(name), sense, new LocalRendezvous(Local),
            Bus, Ring, dials);

        Subscribe(machine);

        return machine;
    }

    /// <summary>Waits for the dispatch queue to drain.</summary>
    public Task QuietAsync(CancellationToken ct = default) =>
        Bus.WhenIdle().WaitAsync(Patience, ct);

    /// <summary>
    /// Waits on the THOUGHT'S OWN ACCOUNTING, not on the bus.
    /// </summary>
    /// <remarks>
    /// <b>The bus going quiet does not mean the walk finished.</b> In-flight
    /// reaches zero in the gap between a cluster handling a message and
    /// dispatching what that message produced — fork 12. Reading a thought there
    /// gives "nothing reached" where the truth is "not finished".
    /// <para>
    /// <b>Real elapsed time, not an iteration count.</b> <c>Task.Delay(1)</c> on
    /// Windows sleeps about fifteen milliseconds, so counting one per pass made a
    /// two-second budget wait for thirty — which looked exactly like a hang.
    /// </para>
    /// </remarks>
    /// <returns>Whether it settled rather than running out of patience.</returns>
    public async Task<bool> SettleAsync(Thought thought, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(thought);

        await QuietAsync(ct).ConfigureAwait(false);

        var until = Environment.TickCount64 + (long)Waiting.TotalMilliseconds;

        while (!thought.Settled && Environment.TickCount64 < until)
        {
            await Task.Delay(1, ct).ConfigureAwait(false);
            await QuietAsync(ct).ConfigureAwait(false);
        }

        return thought.Settled;
    }

    /// <summary>Throws whatever the bus swallowed, if anything.</summary>
    public void Failures()
    {
        lock (_faults)
        {
            if (_faults.Count > 0) throw new AggregateException(_faults);
        }
    }

    public void Dispose()
    {
        foreach (var handle in _handles) handle.Dispose();
    }
}
