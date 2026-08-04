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
    private readonly Window _window;

    /// <inheritdoc cref="Learning.Surprise"/>
    /// <remarks><b>Null is off, and off is every measurement taken before it.</b></remarks>
    private readonly Surprise? _surprise;
    private readonly IRendezvous _rendezvous;
    private readonly IBus _bus;
    private readonly Ring _ring;
    private readonly WalkSettings _settings;

    /// <summary>Thoughts this machine started and has not released.</summary>
    private readonly ConcurrentDictionary<BroadcastId, Thought> _thoughts = [];

    /// <summary>
    /// How many reports each settled thought had folded when it was last looked
    /// at. <b>The second wave of <see cref="Retire"/>.</b>
    /// </summary>
    private readonly ConcurrentDictionary<BroadcastId, int> _quiet = [];

    private int _deaths;

    /// <summary>A placeholder address; a broadcast is not addressed to anyone.</summary>
    private static readonly ClusterAddress _everywhere = new("*");

    public InputMachine(
        MachineAddress address,
        IQuantizer<TFrame> quantizer,
        IRendezvous rendezvous,
        IBus bus,
        Ring ring,
        WalkSettings settings,
        int span = 0,
        Surprise? surprise = null)
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
        _window = new Window(span);
        _surprise = surprise;

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
    /// <param name="frame">What the sense just read.</param>
    /// <param name="now">The observing machine's own clock.</param>
    /// <param name="asking">
    /// What the resulting broadcast is a question ABOUT. <b>Null is every
    /// observation made before edge kinds existed</b> — an undirected flood that
    /// walks whatever it finds.
    /// </param>
    /// <param name="worth">
    /// What this occasion counts for. <b>One is something that happened</b>, and
    /// below or above it is the third factor of step 4 saying how much the moment
    /// earned — see <see cref="Learning.Drives"/>. Never zero or negative: counts
    /// only increment, and a factor that could cancel one would break the
    /// convergence the whole coordination-free design rests on.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    public async Task<Thought?> ObserveAsync(
        TFrame frame,
        long now,
        Question? asking = null,
        double worth = 1.0,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(worth);

        var changes = _liveSet.Update(_quantizer.Codify(frame), now);
        if (changes.Started.IsEmpty) return null;

        // What was already there AND still is. Something that stopped in the
        // same frame is gone, and did not persist through the onset.
        var onsets = changes.Started.ToHashSet();
        ImmutableArray<Code> live = [.. _liveSet.Live.Where(code => !onsets.Contains(code))];

        // WHAT HAS RECENTLY STOPPED, AND THE ORDER OF THESE TWO LINES IS THE
        // WHOLE OF THE TEMPORAL EDGE. Carrying AFTER reading meant the code that
        // stopped as this one started was not yet in the window, so it could never
        // join -- and since `Live` is what was already there AND STILL IS, it was
        // excluded from that too. The immediate predecessor was therefore the one
        // relation the graph could never record: on a stream where nothing
        // overlaps, it learnt the step before that instead, measured on `Rhythm`
        // as predicting the next symbol at chance and the one after it far above.
        //
        // Carrying first makes the previous frame available to this one. `Window`
        // counts strictly inside the span to keep zero meaning off.
        _window.Carry(changes.Stopped, changes.Started, now);
        ImmutableArray<Code> recent = [.. _window.Recent(now)];

        await _rendezvous.JoinAsync(
            new Occasion
            {
                Onsets = changes.Started,
                Live = live,
                Recent = recent,
                At = now,
                Weight = worth,

                // WHAT THE FRONT END COULD SAY ABOUT WHICH THING IS WHICH, and
                // null for every front end that cannot. See Occasion.Groups.
                Groups = _quantizer.Bind(frame),

                // AND WHAT ORDER THEY CAME IN, where the front end can say and
                // null everywhere else. The order has to be said HERE, inside the
                // occasion, because a phase cannot survive C2 — see
                // Occasion.Sequence.
                Sequence = _quantizer.Order(frame),

                // AND WHICH OF THEM NAME THIS OCCASION RATHER THAN A KIND. Also
                // null for every front end that cannot. See Occasion.Fleeting.
                Fleeting = _quantizer.Fleeting(frame),
            }, ct)
            .ConfigureAwait(false);

        // ONLY THE SURPRISE PROPAGATES — step 2, and it happens AFTER the join
        // above rather than before it. An expected onset still moves the counts,
        // or the graph stops getting better at the thing it already predicts and
        // the silence stops being earned. What is skipped is the broadcast.
        if (_surprise is null)
            return await ThinkAsync(changes.Started, null, asking, ct).ConfigureAwait(false);

        // BOTH HALVES OF THE ERROR, AND ONLY THE POSITIVE ONE TRAVELS. What was
        // expected and did not arrive is counted where it is computed and goes
        // nowhere: there is no code for the thing that did not happen, so absence
        // is a signal the machine can read about itself and never a broadcast.
        var residual = _surprise.Residual(changes.Started).Surprising;

        return residual.Count == 0
            ? null
            : await ThinkAsync(residual, null, asking, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Announces a finished thought to whatever is listening for what it reached
    /// — <b>fork 11, and this is the call that replaces handing an output machine
    /// the thought object.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>IT REFUSES AN UNSETTLED THOUGHT, WHICH IS THE WHOLE SAFETY OF THE
    /// SHAPE.</b> A listener cannot judge for itself whether a walk has finished
    /// without either duplicating the settle loop or reading it early, and
    /// reading a question before its walk had finished is fork 22 — it made every
    /// number taken under one load incomparable with any other. This machine
    /// knows, so this machine is the one allowed to say.
    /// </para>
    /// <para>
    /// <b>Nothing happens if nobody is listening</b>, which is every run that has
    /// no output machine — so this can be called unconditionally by a harness
    /// that does not know whether anything is attached.
    /// </para>
    /// </remarks>
    public ValueTask PublishAsync(Thought thought, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(thought);

        if (!thought.Settled)
            throw new InvalidOperationException(
                "an unsettled thought cannot be published: routes are still in "
                + "flight, so what it reached is a function of when it was read "
                + "rather than of what the graph holds — see fork 22");

        return _bus.PublishAsync(
            new Settled
            {
                Broadcast = thought.Id,
                From = _address,
                Arrivals = [.. thought.Best(int.MaxValue)],
            }, ct);
    }

    /// <summary>
    /// Opens a thought, mints a broadcast id, and sends the origins to their
    /// owning clusters — <b>one envelope per cluster, not per code.</b>
    /// </summary>
    /// <param name="origins">The codes the broadcast goes out from.</param>
    /// <param name="stamina">
    /// What each route starts with. <b>Null takes the dial's own value</b>;
    /// a caller passes one when the question wants a different depth from
    /// acting — see fork 20.
    /// </param>
    /// <param name="question">
    /// What the asker knows about its own question — <b>how it wants ranking, and
    /// which of its origins are one thing said several ways.</b> Null asks the way
    /// everything asked before <see cref="Question"/> existed.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>THE RANKING IS THE QUESTION'S AND NOT THE MACHINE'S</b>, because
    /// <see cref="Accumulate.Agreement"/> is right on a conjunction and harmful on
    /// an indexed one — see <see cref="Question"/> for why fusing them instead was
    /// refuted.
    /// </remarks>
    public async Task<Thought> ThinkAsync(
        IReadOnlyCollection<Code> origins,
        double? stamina = null,
        Question? question = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(origins);
        ArgumentOutOfRangeException.ThrowIfZero(origins.Count);

        Retire();

        var broadcast = BroadcastId.New();

        var messages = origins.Select(code => new Message
        {
            Broadcast = broadcast,
            ReturnTo = _address,
            To = code,

            // SET ONCE AT THE ORIGIN AND COPIED ALONG EVERY HOP, because a node
            // cannot see the question -- see Message.Through. Null is every
            // question asked before edge kinds existed.
            Through = question?.Through,
            Held = stamina ?? _settings.Stamina,

            // A chain ends with the node the message is addressed to, so an
            // origin's chain is just itself.
            Chain = [code],
            Carried = 1.0,
        });

        Thought? opened = null;

        // BROADCAST, NOT ROUTED -- John's call on fork 6. An origin has no
        // address by nature: for "what is this thing I am sensing" you cannot
        // route, because you do not know what you are looking for. The ring is
        // not consulted here at all.
        await _bus.BroadcastAsync(
            new Envelope { To = _everywhere, Messages = [.. messages], Everywhere = true },
            ct,

            // REGISTERED BEFORE THE FIRST CLUSTER IS ASKED, AND THAT ORDER IS
            // LOAD-BEARING. Doing this after the broadcast returned lost every
            // report that beat it back, because a report for a broadcast this
            // machine does not know is dropped -- so the thought never settled
            // and held no arrivals, which reads as "the graph had nothing to
            // say" and is indistinguishable from a real silence.
            reached =>
            {
                // ONE PENDING UNIT PER CLUSTER. The origin cannot know how many
                // routes it started -- that depends on who holds what, which is
                // exactly the knowledge a broadcast exists to avoid needing.
                // What it does know is how many clusters it asked, and every one
                // of them replies.
                opened = new Thought(
                    broadcast, Math.Max(reached.Count, 1),
                    question?.Ranking ?? Accumulate.Sum,
                    [.. origins], question?.Asking);

                foreach (var cluster in reached) opened.SentInto(cluster, 1);

                _thoughts[broadcast] = opened;
            }).ConfigureAwait(false);

        return opened ?? throw new InvalidOperationException(
            "the bus broadcast without announcing who it was about to ask, so " +
            "the thought could not be recorded before the replies arrived");
    }

    /// <summary>
    /// Writes a settled thought's conclusions back as an occasion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 21, AND IT IS THE COMPRESSION PRINCIPLE FINALLY BUILT.</b> A
    /// conclusion becomes an observation: what the walk reached is joined to
    /// what it started from, so a route walked often enough becomes a direct
    /// edge and the composition stops being re-derived every time.
    /// </para>
    /// <para>
    /// <b>The reached codes are the onsets and the origins are merely live</b>,
    /// which is not a detail. Onsets pair with everything present; live never
    /// pairs with live. So the origins do NOT re-pair with each other — that
    /// coincidence was counted when it was actually observed, and counting it
    /// again on every thought would inflate exactly the association the walk
    /// started from.
    /// </para>
    /// <para>
    /// <b>Nothing here waits for the thought.</b> A caller reflects what has
    /// arrived so far, which is what C4 requires of a system with no moment
    /// between thoughts.
    /// </para>
    /// </remarks>
    /// <returns>How many conclusions were written. Zero when reflection is off.</returns>
    public async ValueTask<int> ReflectAsync(
        Thought thought,
        long now,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(thought);

        if (_settings.Reflect is not { } reflect) return 0;

        var starting = thought.Started.ToHashSet();
        if (starting.Count == 0) return 0;

        // ABOVE THE THRESHOLD AND NOT SOMEWHERE WE STARTED. Minting an edge from
        // an origin to itself is not compression, and a conclusion below the
        // nucleation threshold is not worth its own storage.
        ImmutableArray<Code> reached =
        [
            .. thought.Best(reflect.Names)
                .Where(arrival => arrival.Score >= reflect.Threshold)
                .Select(arrival => arrival.Endpoint)
                .Where(code => !starting.Contains(code))
        ];

        if (reached.IsEmpty) return 0;

        await _rendezvous.JoinAsync(
            new Occasion
            {
                Onsets = reached,
                Live = [.. starting],
                At = now,
                Weight = reflect.Weight,
            }, ct).ConfigureAwait(false);

        return reached.Length;
    }

    /// <summary>
    /// A cluster's arrivals and accounting came back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Arrivals are folded before the accounting</b>, because the accounting
    /// can settle the thought and a settled thought is released — an arrival
    /// applied after that would be dropped.
    /// </para>
    /// <para>
    /// <b>NOTHING IS UNTRACKED HERE, AND THAT IS FORK 22'S FIX.</b> This used to
    /// stop tracking a thought the moment <see cref="Thought.Settled"/> went
    /// true. <b>A live count of zero is not a durable state</b> — reports arrive
    /// out of order, so it dips to zero transiently whenever a downstream death
    /// is folded before the upstream split that created it, which is fork 12
    /// exactly. One thread would see that dip and untrack the thought while other
    /// threads were still folding reports that pushed the count back up, and
    /// every report after that was dropped at the lookup above. The thought could
    /// then never settle.
    /// </para>
    /// <para>
    /// <b>Measured: 7 of 60 questions stuck, every one of them with more reports
    /// sent than folded</b>, and waiting twelve times longer did not move it. So
    /// fork 12 and fork 22 were one bug seen from two sides. Retirement moved to
    /// <see cref="Retire"/>, which asks twice.
    /// </para>
    /// </remarks>
    public Task DeliverAsync(Report report, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (_thoughts.TryGetValue(report.Accounting.Broadcast, out var thought))
            thought.Receive(report);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops tracking thoughts that have finished, <b>asking twice before
    /// believing it</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Mattern's shape, at the scale of one machine.</b> A single look at a
    /// termination condition can catch it mid-flicker; two consecutive looks with
    /// <i>no report in between</i> cannot, because a report is the only thing that
    /// can move the count. So a thought is retired when it was settled last time,
    /// is settled now, and folded nothing in between.
    /// </para>
    /// <para>
    /// <b>The residual window is named rather than claimed away:</b> a report can
    /// land between the last read of <see cref="Thought.Reports"/> and the
    /// removal. That retires a thought a moment early, which costs a late
    /// arrival — the ordinary C2 loss the design already admits — where the bug
    /// this replaces cost the whole thought, permanently.
    /// </para>
    /// <para>
    /// Called when a new thought opens, so the cost is bounded by how many
    /// thoughts one machine has in flight rather than by a timer.
    /// </para>
    /// </remarks>
    private void Retire()
    {
        foreach (var (broadcast, thought) in _thoughts)
        {
            if (!thought.Settled)
            {
                _quiet.TryRemove(broadcast, out _);
                continue;
            }

            var folded = thought.Reports;

            // Settled when we last looked, and nothing has arrived since.
            if (_quiet.TryGetValue(broadcast, out var before) && before == folded)
            {
                _thoughts.TryRemove(broadcast, out _);
                _quiet.TryRemove(broadcast, out _);
            }
            else
            {
                _quiet[broadcast] = folded;
            }
        }
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
        _quiet.TryRemove(broadcast, out _);
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
