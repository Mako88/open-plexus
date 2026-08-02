using System.Collections.Concurrent;
using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Thinking;

namespace OpenPlexus.Machines;

/// <summary>
/// The world boundary on the way in.
/// </summary>
/// <remarks>
/// <b>Holds an address, holds no edges, is in no walk.</b> That is why an
/// arbitrary sensor can be attached without the graph knowing what it is, and
/// it is what keeps modality entirely outside the graph — so partitioning by
/// modality stays a deployment choice rather than a rewrite.
/// </remarks>
/// <typeparam name="TFrame">What this machine's sensor produces.</typeparam>
public sealed class InputMachine<TFrame> : IReceiveReports
{
    private readonly MachineAddress _address;
    private readonly IQuantizer<TFrame> _quantizer;
    private readonly LiveSet _liveSet = new();
    private readonly IRendezvous _rendezvous;
    private readonly IBus _bus;
    private readonly Ring _ring;
    private readonly WalkSettings _settings;

    /// <summary>Thoughts this machine started and has not released.</summary>
    private readonly ConcurrentDictionary<BroadcastId, Thought> _thoughts = [];

    private int _deaths;

    /// <summary>A placeholder address; a broadcast is not addressed to anyone.</summary>
    private static readonly ClusterAddress _everywhere = new("*");

    public InputMachine(
        MachineAddress address,
        IQuantizer<TFrame> quantizer,
        IRendezvous rendezvous,
        IBus bus,
        Ring ring,
        WalkSettings settings)
    {
        ArgumentNullException.ThrowIfNull(quantizer);
        ArgumentNullException.ThrowIfNull(rendezvous);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(settings);

        _address = address;
        _quantizer = quantizer;
        _rendezvous = rendezvous;
        _bus = bus;
        _ring = ring;
        _settings = settings;

        _bus.Deaths += OnDeath;
    }

    public MachineAddress Address => _address;

    /// <summary>Thoughts started and not yet settled or released.</summary>
    public int Pending => _thoughts.Count;

    /// <summary>Cluster departures seen. See the note on <see cref="OnDeath"/>.</summary>
    public int DeathsSeen => Volatile.Read(ref _deaths);

    /// <summary>
    /// The whole input path, in one place.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Quantise the frame; diff against the live set for onsets and offsets;
    /// <b>learn</b> by joining the onsets with what was already live;
    /// <b>think</b> by starting a thought from the onsets. Persistence produces
    /// neither — a stable scene is silent, and a frame that changed nothing
    /// returns null.
    /// </para>
    /// <para>
    /// <b>Learning happens before thinking, and that is a choice.</b> The
    /// thought then walks a graph that already includes this moment, which is
    /// what an always-learning system does — C4 forbids a run that stops, so
    /// there is no "before training" for the walk to sit in.
    /// </para>
    /// </remarks>
    public async Task<Thought?> ObserveAsync(TFrame frame, long now, CancellationToken ct = default)
    {
        var changes = _liveSet.Update(_quantizer.Codify(frame), now);
        if (changes.Started.IsEmpty) return null;

        // What was already there AND still is. Something that stopped in the
        // same frame is gone, and did not persist through the onset.
        var onsets = changes.Started.ToHashSet();
        ImmutableArray<Code> live = [.. _liveSet.Live.Where(code => !onsets.Contains(code))];

        await _rendezvous.JoinAsync(
            new Occasion { Onsets = changes.Started, Live = live, At = now }, ct)
            .ConfigureAwait(false);

        return await ThinkAsync(changes.Started, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a thought, mints a broadcast id, and sends the origins to their
    /// owning clusters — <b>one envelope per cluster, not per code.</b>
    /// </summary>
    public async Task<Thought> ThinkAsync(
        IReadOnlyCollection<Code> origins, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(origins);
        ArgumentOutOfRangeException.ThrowIfZero(origins.Count);

        var broadcast = BroadcastId.New();

        var messages = origins.Select(code => new Message
        {
            Broadcast = broadcast,
            ReturnTo = _address,
            To = code,
            Held = _settings.Stamina,

            // A chain ends with the node the message is addressed to, so an
            // origin's chain is just itself.
            Chain = [code],
            Carried = 1.0,
        });

        // BROADCAST, NOT ROUTED -- John's call on fork 6. An origin has no
        // address by nature: for "what is this thing I am sensing" you cannot
        // route, because you do not know what you are looking for. The ring is
        // not consulted here at all.
        var reached = await _bus.BroadcastAsync(
            new Envelope { To = _everywhere, Messages = [.. messages], Everywhere = true },
            ct).ConfigureAwait(false);

        // ONE PENDING UNIT PER CLUSTER. The origin cannot know how many routes
        // it started -- that depends on who holds what, which is exactly the
        // knowledge a broadcast exists to avoid needing. What it does know is
        // how many clusters it asked, and every one of them replies.
        var thought = new Thought(broadcast, Math.Max(reached.Count, 1), _settings.Accumulate);
        _thoughts[broadcast] = thought;

        foreach (var cluster in reached) thought.SentInto(cluster, 1);

        return thought;
    }

    /// <summary>
    /// A cluster's arrivals and accounting came back.
    /// </summary>
    /// <remarks>
    /// <b>Arrivals are folded before the accounting</b>, because the accounting
    /// can settle the thought and a settled thought is released — an arrival
    /// applied after that would be dropped.
    /// <para>
    /// A report for a broadcast this machine does not know is dropped. C2 says
    /// late is normal, and a thought that has already settled has nothing left
    /// to refine.
    /// </para>
    /// </remarks>
    public Task DeliverAsync(Report report, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!_thoughts.TryGetValue(report.Accounting.Broadcast, out var thought))
            return Task.CompletedTask;

        thought.Receive(report);

        // SETTLING IS NOT RELEASING, and getting that wrong wiped the answer
        // before anything could read it. A settled thought is exactly the one
        // whose arrivals are complete, so releasing it there would destroy the
        // result at the moment it became final. This stops TRACKING it — the
        // caller holds the object it was handed and reads it at its leisure.
        if (thought.Settled) _thoughts.TryRemove(report.Accounting.Broadcast, out _);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Drops a thought's state and stops tracking it.
    /// </summary>
    /// <remarks>
    /// For the case where <b>nobody will ever read it</b> — a thought stranded
    /// by a departure, which is what <see cref="Thought.Release"/> is for.
    /// Ordinary settling does not come through here.
    /// </remarks>
    public void Forget(BroadcastId broadcast)
    {
        if (_thoughts.TryRemove(broadcast, out var thought)) thought.Release();
    }

    /// <summary>
    /// A cluster left the bus. Every thought with routes in flight toward it
    /// writes those off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's design, 2026-08-02, and the answer fork 5 was waiting for.</b>
    /// A thought now knows how many of its routes are heading into each
    /// cluster, because every report says where the routes it created went. So
    /// a departure is not a question — the loss is exact, those routes are
    /// counted as deaths, and the thought settles by its own accounting.
    /// </para>
    /// <para>
    /// <b>This is what the event bus was introduced for.</b> Without it an
    /// origin waits on routes that are never coming back, and the only
    /// alternative is a deadline guessing on its behalf.
    /// </para>
    /// </remarks>
    private void OnDeath(ClusterAddress gone)
    {
        Interlocked.Increment(ref _deaths);

        foreach (var (broadcast, thought) in _thoughts)
        {
            if (thought.Lost(gone) > 0 && thought.Settled) _thoughts.TryRemove(broadcast, out _);
        }
    }
}
