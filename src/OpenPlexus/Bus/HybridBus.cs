
namespace OpenPlexus.Bus;

/// <summary>
/// Lateness, injected — <b>C2 made real instead of assumed.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Every measurement in this project has run in one process with in-memory
/// delivery.</b> C2 says messages are late, jittered and out of order, and the
/// whole design rests on that being survivable — but nothing had ever checked it.
/// Thread-pool dispatch already reorders; what it never produces is a message
/// arriving LONG after its siblings, which is the case a real network adds and
/// the one that can outlive a thought's patience.
/// </para>
/// <para>
/// <b>A small share, delayed a lot — not everything delayed a little.</b> Delaying
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

/// <summary>
/// The bus in one process, with C2 injected rather than assumed.
/// </summary>
/// <remarks>
/// <b>The harsher test of the same traffic, and that is why it stays.</b> Delivery here is
/// <see cref="Task.Run(Action)"/> with delays sprinkled in, so it reorders on purpose;
/// <see cref="Posted"/> crosses a socket and TCP does not reorder within a connection. A
/// green run over the wire says the bytes and the routing are right and says nothing about
/// C2 — <i>a simulated constraint can be harsher than the real one</i>, and here that is the
/// point rather than the trap.
/// </remarks>
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

    /// <summary>
    /// Who can be asked about commitments, and who is owed the answers.
    /// </summary>
    /// <remarks>
    /// <b>Here so the simulator stays the harsher test of the same traffic.</b> This bus
    /// reorders and delays on purpose and <see cref="Posted"/> does not, so an ask that
    /// crosses a socket cleanly says the bytes and the routing are right and says nothing
    /// about C2 — and the arm that would measure that has to exist here or the constraint
    /// is being honoured by whichever bus was convenient.
    /// </remarks>
    private readonly Dictionary<MachineAddress, IReceiveAsks> _holders = [];

    private readonly Dictionary<MachineAddress, IReceiveAnswers> _askers = [];

    private readonly Lock _gate = new();

    /// <summary>Deliveries dispatched and not yet finished.</summary>
    private int _inFlight;
    private long _messages;
    private long _answers;

    private TaskCompletionSource _quiet = Quiet();

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Nearly unreachable here, and that is a fact about one process rather than about
    /// the mechanism.</b> A holder that has unsubscribed is not in the table, so it is never
    /// in the roster and never owed anything; what is left is a holder that took the ask and
    /// threw, which is the local spelling of a refused connection. Every other way to lose a
    /// question needs a wire, so <see cref="Posted"/> is where fork 53 is measured.
    /// </remarks>
    public event Action<BroadcastId, MachineAddress>? Unreached;

    /// <summary>
    /// A delivery threw. <b>Surfaced rather than swallowed</b> — a send that
    /// returns before delivery has no other way to report failure, and
    /// swallowing is how a thing turns out never to have been wired up.
    /// </summary>
    public event Action<Exception>? Faults;

    /// <summary>
    /// Every ask put on the bus, counted once per holder it went to.
    /// </summary>
    /// <remarks>
    /// What a real network would have had to carry, and the scatter half of it. A fan-out
    /// to twelve holders is twelve messages however concurrently they were dispatched.
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
    /// Answers put on the return path, across every gathering.
    /// </summary>
    /// <remarks>
    /// <b>Counted separately from <see cref="Messages"/> so an answer that never arrives can
    /// be distinguished from one that was never sent.</b> A gathering that never completes
    /// looks the same from the asker either way, and only one of the two is the bus's fault.
    /// </remarks>
    public long Answers
    {
        get { lock (_gate) return _answers; }
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveAsks holder)
    {
        ArgumentNullException.ThrowIfNull(holder);

        return Joins.At(_gate, _holders, holder.Address, holder);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IReceiveAnswers asker)
    {
        ArgumentNullException.ThrowIfNull(asker);

        return Joins.At(_gate, _askers, asker.Address, asker);
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlyCollection<MachineAddress>> AskAsync(
        Ask ask,
        CancellationToken ct = default,
        Action<IReadOnlyCollection<MachineAddress>>? ready = null)
    {
        ArgumentNullException.ThrowIfNull(ask);

        // NOT NAMED `Holder`, and the reason is a check rather than a style. `DeadCodeTests`
        // asks whether the library ever NAMES a public type, and a tuple field spelt like
        // one answers yes for free -- so `Machines.Holder` read as wired for exactly as
        // long as this line called its second element that.
        List<(MachineAddress Who, IReceiveAsks Asks)> everyone;
        lock (_gate)
        {
            everyone =
            [
                .. _holders.Select(pair => (pair.Key, pair.Value))
                    .OrderBy(one => one.Key.Value, StringComparer.Ordinal),
            ];

            _inFlight += everyone.Count;
            _messages += everyone.Count;
        }

        IReadOnlyCollection<MachineAddress> asked = [.. everyone.Select(one => one.Who)];

        // Who is about to be asked, before anyone is asked -- the same window
        // `BroadcastAsync` opens, and for the same measured reason. Dispatch is `Task.Run`,
        // so a holder can answer before this returns, and an answer to an ask nobody
        // remembers is dropped by design.
        ready?.Invoke(asked);

        foreach (var (who, holder) in everyone)
            Dispatch(
                () => holder.DeliverAsync(ask, ct),
                () => Unreached?.Invoke(ask.Broadcast, who));

        return ValueTask.FromResult(asked);
    }

    /// <inheritdoc/>
    public ValueTask SendAsync(MachineAddress to, Answer answer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        IReceiveAnswers? receiver;
        lock (_gate)
        {
            // DROPPED RATHER THAN THROWN. An asker that has gone between asking and being
            // answered is C3 happening, and it is the case this whole payload exists to
            // measure -- throwing would make one machine's departure another's error.
            if (!_askers.TryGetValue(to, out receiver)) return ValueTask.CompletedTask;

            _inFlight++;
            _answers++;
        }

        Dispatch(() => receiver.DeliverAsync(answer, ct));
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

    private void Dispatch(Func<Task> delivery) => Dispatch(delivery, failed: null);

    /// <param name="delivery">What to do.</param>
    /// <param name="failed">
    /// What to say when it throws, beyond reporting the fault. <b>Only the ask path passes
    /// one</b>, because it is the only delivery here whose loss leaves somebody waiting.
    /// </param>
    private void Dispatch(Func<Task> delivery, Action? failed) =>
        _ = Task.Run(async () =>
        {
            try
            {
                // Late, before delivery and inside the in-flight count. See
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
                failed?.Invoke();
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

}
