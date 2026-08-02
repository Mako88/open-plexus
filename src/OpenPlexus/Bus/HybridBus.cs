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
public sealed class HybridBus : IBus
{
    private readonly Dictionary<ClusterAddress, IReceiveEnvelopes> _clusters = [];
    private readonly Dictionary<MachineAddress, IReceiveReports> _machines = [];
    private readonly Lock _gate = new();

    /// <summary>Deliveries dispatched and not yet finished.</summary>
    private int _inFlight;

    private TaskCompletionSource _quiet = Settled();

    /// <inheritdoc/>
    public event Action<ClusterAddress>? Deaths;

    /// <summary>
    /// A delivery threw. <b>Surfaced rather than swallowed</b> — a send that
    /// returns before delivery has no other way to report failure, and
    /// swallowing is how a thing turns out never to have been wired up.
    /// </summary>
    public event Action<Exception>? Faults;

    /// <summary>Deliveries dispatched and not yet finished.</summary>
    public int InFlight
    {
        get { lock (_gate) return _inFlight; }
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
    public ValueTask SendAsync(ClusterAddress to, Envelope envelope, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        IReceiveEnvelopes receiver;
        lock (_gate)
        {
            if (!_clusters.TryGetValue(to, out receiver!)) throw Unreachable(to.Value);
            _inFlight++;
        }

        Dispatch(() => receiver.DeliverAsync(envelope, ct));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlyCollection<ClusterAddress>> BroadcastAsync(
        Envelope envelope, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        List<(ClusterAddress Address, IReceiveEnvelopes Receiver)> everyone;
        lock (_gate)
        {
            everyone = [.. _clusters.Select(pair => (pair.Key, pair.Value))];
            _inFlight += everyone.Count;
        }

        foreach (var (address, receiver) in everyone)
        {
            var addressed = envelope with { To = address, Everywhere = true };
            Dispatch(() => receiver.DeliverAsync(addressed, ct));
        }

        return ValueTask.FromResult<IReadOnlyCollection<ClusterAddress>>(
            [.. everyone.Select(one => one.Address)]);
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
    public Task WhenQuiet()
    {
        lock (_gate) return _inFlight == 0 ? Task.CompletedTask : _quiet.Task;
    }

    private void Dispatch(Func<Task> delivery) =>
        _ = Task.Run(async () =>
        {
            try
            {
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
                _quiet = Settled();
            }
        }

        settling?.TrySetResult();
    }

    private static TaskCompletionSource Settled() =>
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
