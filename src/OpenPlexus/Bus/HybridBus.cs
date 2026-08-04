using OpenPlexus.Thinking;

namespace OpenPlexus.Bus;

/// <summary>
/// Local delivery by direct call, remote delivery over the wire, one interface.
/// </summary>
/// <remarks>
/// <para>
/// <b>ONLY THE LOCAL HALF EXISTS.</b> There is no second machine yet, so there
/// is no wire, and building one before it has a caller is how the Python grew
/// three dead modules. An address that is not local <b>throws</b> rather than
/// being dropped: with no wire, an unknown address can only be a routing bug,
/// and a silent drop would be indistinguishable from the ordinary C2 message
/// loss it is not. When the wire lands, that same case becomes a lost message.
/// </para>
/// <para>
/// <b>The speed difference between local and remote is not a wart, it is the
/// experiment.</b> Codes that land together are cheap to walk between and codes
/// that land apart are not, so a machine's contents become a region of the
/// graph that thinks faster internally than externally. That is what a column
/// is, and here it costs nothing extra — see open fork 3.
/// </para>
/// </remarks>
/// <summary>
/// Lateness, injected — <b>C2 made real instead of assumed.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY MEASUREMENT IN THIS PROJECT HAS RUN IN ONE PROCESS WITH IN-MEMORY
/// DELIVERY.</b> C2 says messages are late, jittered and out of order, and the
/// whole design rests on that being survivable — but nothing had ever checked it.
/// Thread-pool dispatch already reorders; what it never produces is a message
/// arriving LONG after its siblings, which is the case a real network adds and
/// the one that can outlive a thought's patience.
/// </para>
/// <para>
/// <b>A SMALL SHARE, DELAYED A LOT — not everything delayed a little.</b> Delaying
/// every message would multiply run times by the scheduler's resolution and
/// measure the harness rather than the design. A few percent arriving very late
/// is both the realistic shape and the one that actually stresses settling.
/// </para>
/// <para>
/// <b>The delay is INSIDE the dispatched task, so the in-flight count still
/// covers it</b> and <see cref="HybridBus.WhenIdle"/> keeps meaning what it
/// means. A late message is late, not uncounted.
/// </para>
/// </remarks>
/// <param name="Share">The fraction of deliveries held back, in 0..1.</param>
/// <param name="Delay">How long a held-back delivery waits.</param>
/// <param name="Seed">
/// The generator, so a jittered run is as reproducible as thread scheduling
/// allows — which is not very, and that is C2 rather than a defect.
/// </param>
public readonly record struct Lateness(double Share, TimeSpan Delay, int Seed);

public sealed class HybridBus : IBus
{
    /// <inheritdoc cref="Bus.Lateness"/>
    private readonly Lateness? _late;

    private readonly Random? _jitter;

    /// <param name="late">
    /// Lateness to inject. <b>Null is every measurement taken before this
    /// existed</b>, and is the control.
    /// </param>
    public HybridBus(Lateness? late = null)
    {
        if (late is not { } setting) return;

        ArgumentOutOfRangeException.ThrowIfNegative(setting.Share);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(setting.Share, 1.0);

        _late = setting;
        _jitter = new Random(setting.Seed);
    }

    /// <summary>How many deliveries were actually held back.</summary>
    /// <remarks>
    /// <b>Reported, because a jitter arm that delayed nothing is a control
    /// wearing the arm's name</b> — the failure this project keeps having.
    /// </remarks>
    public long Delayed => Interlocked.Read(ref _delayed);

    private long _delayed;

    private readonly Dictionary<ClusterAddress, IReceiveEnvelopes> _clusters = [];
    private readonly Dictionary<MachineAddress, IReceiveReports> _machines = [];

    /// <summary>
    /// Who wants to hear about a finished thought reaching which code — <b>fork
    /// 11, and the routing table that lets a second output machine exist.</b>
    /// </summary>
    /// <remarks>
    /// <b>Keyed by CODE and not by address</b>, because the machine publishing a
    /// finished thought must not have to know who is listening — that would be a
    /// coordinator, and the whole design refuses one.
    /// </remarks>
    private readonly Dictionary<Codes.Code, List<IReceiveArrivals>> _listeners = [];

    private readonly Lock _gate = new();

    /// <summary>Deliveries dispatched and not yet finished.</summary>
    private int _inFlight;
    private long _messages;
    private long _reports;

    private TaskCompletionSource _quiet = Quiet();

    /// <inheritdoc/>
    public event Action<ClusterAddress>? Deaths;

    /// <summary>
    /// A delivery threw. <b>Surfaced rather than swallowed</b> — a send that
    /// returns before delivery has no other way to report failure, and
    /// swallowing is how a thing turns out never to have been wired up.
    /// </summary>
    public event Action<Exception>? Faults;

    /// <summary>
    /// Every message put on the bus, across every envelope.
    /// </summary>
    /// <remarks>
    /// What a real network would have had to carry. Counted because
    /// receiver-weighing trades messages for exactness — 26.7 a step against the
    /// deleted sender arm's 17.0 — and the trade is worth knowing the size of.
    /// </remarks>
    public long Messages
    {
        get { lock (_gate) return _messages; }
    }

    /// <summary>Deliveries dispatched and not yet finished.</summary>
    public int InFlight
    {
        get { lock (_gate) return _inFlight; }
    }

    /// <summary>
    /// Reports put on the return path, across every thought.
    /// </summary>
    /// <remarks>
    /// <b>Counted separately from <see cref="Messages"/> so a report that never
    /// arrives can be distinguished from one that was never sent.</b> Fork 22
    /// turned on exactly that question, and nothing recorded either half of it.
    /// </remarks>
    public long Reports
    {
        get { lock (_gate) return _reports; }
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveEnvelopes cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);

        lock (_gate) _clusters.Add(cluster.Address, cluster);
        return new Leaving(this, cluster.Address);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveReports machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        lock (_gate) _machines.Add(machine.Address, machine);
        return new Leaving(this, machine.Address);
    }

    /// <inheritdoc/>
    public IDisposable Listen(IReceiveArrivals machine, IReadOnlyCollection<Codes.Code> codes)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentOutOfRangeException.ThrowIfZero(codes.Count);

        lock (_gate)
            foreach (var code in codes)
            {
                if (!_listeners.TryGetValue(code, out var waiting))
                    _listeners[code] = waiting = [];

                waiting.Add(machine);
            }

        return new Listening(this, machine, codes);
    }

    /// <inheritdoc/>
    public ValueTask PublishAsync(Settled settled, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settled);

        // WHO CARES ABOUT ANYTHING THIS REACHED, each counted once however many
        // of its codes were reached -- a machine that owns four actions and had
        // three of them arrive is still one machine being told once.
        List<IReceiveArrivals> told;
        lock (_gate)
        {
            if (_listeners.Count == 0) return ValueTask.CompletedTask;

            var interested = new HashSet<IReceiveArrivals>();

            foreach (var arrival in settled.Arrivals)
                if (_listeners.TryGetValue(arrival.Endpoint, out var waiting))
                    interested.UnionWith(waiting);

            if (interested.Count == 0) return ValueTask.CompletedTask;

            told = [.. interested];
            _inFlight += told.Count;
            _reports += told.Count;
        }

        foreach (var machine in told)
            Dispatch(() => machine.DeliverAsync(settled, ct));

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        IReceiveEnvelopes receiver;
        lock (_gate)
        {
            if (!_clusters.TryGetValue(to, out receiver!)) throw Unreachable(to.Value);
            _inFlight++;
            _messages += envelope.Messages.Length;
        }

        Dispatch(() => receiver.DeliverAsync(envelope, ct));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
        Envelope envelope,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<ClusterAddress>>? ready = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        List<(ClusterAddress Address, IReceiveEnvelopes Receiver)> everyone;
        lock (_gate)
        {
            everyone = [.. _clusters.Select(pair => (pair.Key, pair.Value))];
            _inFlight += everyone.Count;
            _messages += (long)everyone.Count * envelope.Messages.Length;
        }

        IReadOnlyCollection<ClusterAddress> addresses = [.. everyone.Select(one => one.Address)];

        // WHO IS ABOUT TO BE ASKED, BEFORE ANYONE IS ASKED.
        //
        // MEASURED, and it was losing reports. Dispatch is `Task.Run`, so a
        // cluster could finish and report back before the caller had recorded
        // the broadcast -- and a report for an unknown broadcast is dropped by
        // design, because C2 says late is normal. The thought then never settled
        // and held no arrivals at all, which reads downstream as "the graph had
        // nothing to say" rather than as a lost message.
        ready?.Invoke(addresses);

        foreach (var (address, receiver) in everyone)
        {
            var addressed = envelope with { To = address, Everywhere = true };
            Dispatch(() => receiver.DeliverAsync(addressed, ct));
        }

        return ValueTask.FromResult(addresses);
    }

    /// <inheritdoc/>
    public ValueTask SendAsync(MachineAddress to, Report report, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        IReceiveReports receiver;
        lock (_gate)
        {
            if (!_machines.TryGetValue(to, out receiver!)) throw Unreachable(to.Value);
            _inFlight++;
            _reports++;
        }

        Dispatch(() => receiver.DeliverAsync(report, ct));
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Completes when nothing is in flight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not a C1 violation and not a barrier the design relies on.</b> It
    /// observes one process's own dispatch queue, which no distributed
    /// agreement is involved in, and nothing in the thinking loop waits on it —
    /// the system acts on the best chain arrived so far. It exists so a test or
    /// a harness can ask "has the dust settled" without a sleep.
    /// </para>
    /// <para>
    /// A delivery that sends onward does so <i>before</i> it finishes, so the
    /// count cannot dip to zero while a thought is still propagating.
    /// </para>
    /// </remarks>
    public Task WhenIdle()
    {
        lock (_gate) return _inFlight == 0 ? Task.CompletedTask : _quiet.Task;
    }

    private void Dispatch(Func<Task> delivery) =>
        _ = Task.Run(async () =>
        {
            try
            {
                // LATE, BEFORE DELIVERY AND INSIDE THE IN-FLIGHT COUNT. See
                // `Lateness`. Drawn under the lock because `Random` is not
                // thread-safe and a torn draw would be a defect in the harness
                // rather than in the thing being measured.
                if (_late is { } setting)
                {
                    bool hold;
                    lock (_gate) hold = _jitter!.NextDouble() < setting.Share;

                    if (hold)
                    {
                        Interlocked.Increment(ref _delayed);
                        await Task.Delay(setting.Delay).ConfigureAwait(false);
                    }
                }

                await delivery().ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                Faults?.Invoke(failure);
            }
            finally
            {
                Finished();
            }
        });

    private void Finished()
    {
        TaskCompletionSource? settling = null;

        lock (_gate)
        {
            if (--_inFlight == 0)
            {
                settling = _quiet;
                _quiet = Quiet();
            }
        }

        settling?.TrySetResult();
    }

    private static TaskCompletionSource Quiet() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static InvalidOperationException Unreachable(string address) =>
        new($"nothing local is at {address}, and there is no wire yet; " +
            "with no remote half an unknown address can only be a routing bug, " +
            "and dropping it would look exactly like ordinary message loss");

    private void Leave(ClusterAddress address)
    {
        bool left;
        lock (_gate) left = _clusters.Remove(address);

        // LEAVING IS NOT SILENT. Routes heading into this cluster are never
        // coming back, and the origin has no other way to learn that without a
        // deadline guessing on its behalf.
        if (left) Deaths?.Invoke(address);
    }

    private void Leave(MachineAddress address)
    {
        lock (_gate) _machines.Remove(address);
    }

    /// <summary>
    /// A listener stops listening. <b>Silent, unlike a cluster leaving</b> —
    /// nothing is in flight toward a listener that could be stranded by it, so
    /// there is no death to report.
    /// </summary>
    private void Deafen(IReceiveArrivals machine, IReadOnlyCollection<Codes.Code> codes)
    {
        lock (_gate)
            foreach (var code in codes)
                if (_listeners.TryGetValue(code, out var waiting))
                {
                    waiting.Remove(machine);
                    if (waiting.Count == 0) _listeners.Remove(code);
                }
    }

    private sealed class Listening(
        HybridBus bus, IReceiveArrivals machine, IReadOnlyCollection<Codes.Code> codes)
        : IDisposable
    {
        private bool _gone;

        public void Dispose()
        {
            if (_gone) return;
            _gone = true;
            bus.Deafen(machine, codes);
        }
    }

    private sealed class Leaving : IDisposable
    {
        private readonly HybridBus _bus;
        private readonly ClusterAddress? _cluster;
        private readonly MachineAddress? _machine;
        private bool _gone;

        public Leaving(HybridBus bus, ClusterAddress cluster) => (_bus, _cluster) = (bus, cluster);

        public Leaving(HybridBus bus, MachineAddress machine) => (_bus, _machine) = (bus, machine);

        public void Dispose()
        {
            if (_gone) return;
            _gone = true;

            if (_cluster is { } cluster) _bus.Leave(cluster);
            if (_machine is { } machine) _bus.Leave(machine);
        }
    }
}
