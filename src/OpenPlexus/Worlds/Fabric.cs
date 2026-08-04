using OpenPlexus.Bus;
using OpenPlexus.Graph;
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

    public Fabric(WalkSettings dials, int seed, int clusters = 8, int replicas = 256)
    {
        ArgumentNullException.ThrowIfNull(dials);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clusters);

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

    public HybridBus Bus { get; } = new();

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
        };
    }

    /// <summary>Puts a machine on the bus and keeps the handle.</summary>
    public void Subscribe(IReceiveReports machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        _handles.Add(Bus.Subscribe(machine));
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
