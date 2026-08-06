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
    /// <remarks><b>Every machine has one. There is no ungated arm.</b></remarks>
    private readonly Surprise _surprise = new();

    /// <inheritdoc cref="Learning.Chunk"/>
    /// <remarks><b>Every machine has one. There is no unchunked arm.</b></remarks>
    private readonly Chunk _chunks = new();

    /// <inheritdoc cref="Macro"/>
    private readonly Macro _macros = new();

    /// <summary>The last occasion this machine wrote, exactly as written.</summary>
    private Occasion? _joined;

    /// <summary>
    /// Which codes of the PREVIOUS frame nothing about the state selected.
    /// </summary>
    /// <remarks>
    /// <b>Remembered because a code is carried the frame AFTER it stopped</b>, so
    /// by then the quantiser is being asked about a different moment. See
    /// <see cref="Occasion.Forced"/>.
    /// </remarks>
    private IReadOnlySet<Code>? _assigned;

    /// <summary>
    /// The last thought this machine had, <b>which is what it expects next.</b>
    /// </summary>
    /// <remarks>
    /// <b>Null clears the expectation rather than keeping the old one.</b> A
    /// moment that produced no thought produced no prediction, and carrying a
    /// stale one forward would silence an onset on the strength of a walk that
    /// happened two moments ago -- which is the fault <see cref="Surprise.Expect"/>
    /// replaces rather than accumulates to avoid.
    /// </remarks>
    private Thought? _foreseen;
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

    /// <remarks>
    /// <b>THE SPAN COMES OFF THE SETTINGS AND NOT OFF THE CALLER — the regression
    /// of 2026-08-04 is why.</b> It used to be a parameter every world passed, and
    /// two worlds passed a default of their own; when the dial moved to the brain
    /// those defaults were lost and Rhythm silently stopped asking questions with
    /// all 452 tests green. <b>A per-caller default is a scattered piece of
    /// KNOWLEDGE, not just a scattered knob</b>, so there is no longer a way to
    /// scatter it.
    /// </remarks>
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
        _window = new Window(settings.Span);

        _bus.Deaths += OnDeath;
    }

    public MachineAddress Address => _address;

    /// <summary>
    /// The names this machine has minted — <see cref="Learning.Chunk"/>.
    /// </summary>
    /// <remarks>
    /// <b>Readable because a world has to be able to complain about it.</b> The
    /// chunker used to be handed in, so a world could count what it had built;
    /// now that every machine has one unconditionally, the count has to come back
    /// out or <see cref="Worlds.MotifResult"/> would be reading a chunker nothing
    /// writes to and reporting nought forever.
    /// </remarks>
    public Chunk Chunks => _chunks;

    /// <inheritdoc cref="Macro"/>
    /// <remarks><b>Readable for the reason <see cref="Chunks"/> is.</b></remarks>
    public Macro Macros => _macros;

    /// <summary>
    /// What this machine expects next — <see cref="Learning.Surprise"/>.
    /// </summary>
    /// <remarks>
    /// <b>Readable for the reason <see cref="Chunks"/> is.</b> The surprise used
    /// to be handed in, so a bench could prime it and then check what the gate
    /// did; now that every machine has one unconditionally, priming has to go
    /// through here. <b>It is the mechanism's own state and not a dial</b> — there
    /// is nothing to switch, only something to ask.
    /// </remarks>
    public Surprise Expects => _surprise;

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
        // AND WHICH OF THEM NOTHING CHOSE. A code that has just STOPPED was live in
        // the frame before this one, so its forcedness is a frame old by the time
        // it is carried -- which is why it is remembered rather than read here.
        // See Occasion.Forced.
        _window.Carry(changes.Stopped, changes.Started, now, _assigned);
        ImmutableArray<Code> recent = [.. _window.Recent(now)];

        var assigned = _quantizer.Forced(frame);
        _assigned = assigned;

        // THIS MOMENT'S AND THE CARRIED ONES TOGETHER, because the cell that
        // matters is written from a CARRIED code to an onset and the rendezvous
        // looks the sender up in one set. Null stays null, so a body that never
        // says anything costs nothing.
        var carried = _window.Forced(now);

        IReadOnlySet<Code>? forced = carried.Count == 0
            ? assigned
            : assigned is null
                ? carried
                : new HashSet<Code>(carried.Concat(assigned));

        // STEP 3 — A SET THAT KEEPS ARRIVING WHOLE GETS A NAME, and the name then
        // stands in for it.
        //
        // THE CHUNK BECOMES THE ONSET AND ITS MEMBERS BECOME MERELY LIVE, which is
        // where the whole compression comes from and it needed no new rule: an
        // occasion already pairs onsets with everything and NEVER live with live.
        // So the members pair with the name and stop pairing with each other --
        // S(S-1) entries collapse to 2S, and a member's fan-out falls from S-1 to
        // one. Everything present still notes the occasion, so `together` cannot
        // exceed `seen` and the bound that terminates a walk is untouched.
        // THE CANDIDATE IS THE WHOLE MOMENT AND NOT THE ONSETS, and that was
        // measured rather than assumed. An onset set is the moment MINUS whatever
        // happened to carry over from the frame before it, so a set that always
        // arrives whole still presents as a different subset every time one of its
        // codes was already live -- and each of those subsets recurs often enough
        // to earn its own name. Measured on `Motif`: sixteen names for six
        // recurring sets, every extra one a partial view of a set already named.
        // AND A NAME MAY NOT COVER THE WHOLE MOMENT, which is the rule that made
        // this sub-moment at all. When it did, the only entries written were
        // name-to-member -- everything the name's own definition already says --
        // and the member-to-member relations that ARE the task on `Senses` were
        // gone. See `Chunk` for the two defects that one rule closes.
        ImmutableArray<Code> present = [.. changes.Started, .. live];

        // WHAT THE FRONT END SAID WAS ONE THING, READ ONCE AND USED TWICE: a name
        // may not be minted ACROSS two of them, and a name minted inside one has
        // to join it or the gate stops applying to the very code that replaced its
        // members. See `Chunk.Fold`.
        var bound = _quantizer.Bind(frame);

        var folded = _chunks.Notice(present, onsets, bound);

        // STEP 2, AND UNTIL NOW IT RAN NOWHERE. `Surprise.Expect` had exactly one
        // caller in the whole of `src` -- `RhythmRun`, on a private instance of its
        // own -- so THIS machine's expectation was empty at every observation in
        // every world. `Rate` and `Overreach` read 0.0000 always, `Silent` never
        // moved, every onset counted as surprising, and the write-path share below
        // computed a constant 1.0. "Only the surprise propagates" was suppressing
        // nothing anywhere.
        //
        // WHAT THE MACHINE THOUGHT LAST TIME IS WHAT IT EXPECTS NOW, which needs no
        // settling point inside this method: a walk gets a whole observation cycle
        // to finish, and reading it here rather than at the moment it was launched
        // is what fork 22 already cost this project once.
        //
        // ONE CODE, AND THAT IS NOT A CONSTANT PICKED HERE. Naming fewer predicted
        // codes was measured, half-refuted -- coarse ranking informs and fine does
        // not -- and revived AT ONE. See the plan's table.
        if (_foreseen is { } last) _surprise.Expect(last.Best(1).Select(one => one.Endpoint));

        ImmutableArray<Code> starting, standing;

        if (!folded.Folded)
        {
            starting = changes.Started;
            standing = live;
        }
        else
        {
            // A NAME IS AN ONSET WHEN IT COVERS ONE. A part assembled entirely
            // from codes that were already live did not start now, and calling it
            // an onset would claim something arrived that did not -- which is the
            // one thing a front end is never allowed to say.
            var began = folded.Codes
                .Where(code => folded.Names.TryGetValue(code, out var members)
                    ? members.Any(onsets.Contains)
                    : onsets.Contains(code))
                .ToImmutableArray();

            var begun = began.ToHashSet();

            starting = began;

            // EVERYTHING ELSE PRESENT STANDS, AND THE ABSORBED MEMBERS STAND WITH
            // IT. That is where the compression comes from and it needs no new
            // rule: an occasion pairs onsets with everything and NEVER live with
            // live, so a member notes the occasion and stops pairing with its
            // fellows. What is different now is that the name has something OTHER
            // than its own members to pair with.
            standing =
            [
                .. folded.Codes.Where(code => !begun.Contains(code)),
                .. folded.Absorbed,
            ];
        }

        // STEP 3'S OTHER HALF -- A NAME FOR AN ORDER RATHER THAN FOR A SET. See
        // `Macro`. A chunk names what arrived TOGETHER; this names what keeps
        // arriving ONE AFTER ANOTHER, and the two are different claims about a
        // world so neither can stand in for the other.
        //
        // ONE ONSET PER MOMENT OR NOTHING, AND THE LIMIT IS STATED RATHER THAN
        // PAPERED OVER. A sequence needs an unambiguous unit per step, and a moment
        // holding three onsets does not say which of them the sequence is made of --
        // guessing would mint an order the world never claimed. So a moment that
        // begins with exactly one thing extends the sequence and every other moment
        // BREAKS it, which is the honest reading and is exactly right on a stream
        // of single symbols. Naming a whole moment first is what would lift this,
        // and that is the plan's sub-moment chunking item rather than this one.
        Code? made = null;

        if (starting.Length == 1) made = _macros.Notice(starting[0]);
        else _macros.Broke();

        // AND IT DOES NOT JOIN THE OCCASION -- MEASURED, AND THE ARM THAT DID
        // BROKE TWO CONTROLS ON `Rhythm`.
        //
        // Adding the completed name to the onsets is what a chunk does, and it is
        // WRONG HERE for a reason that only shows up in time. A chunk's members are
        // in the moment with it, so `Chunk.Fold` absorbs them and the name pairs
        // with everything EXCEPT what it stands for. A macro's members are in
        // moments already written and gone -- so the only thing left for it to pair
        // with is its own LAST member, which is name-to-member, the one edge that
        // must never be written.
        //
        // WHAT IT COST, and both are the same failure the temporal window was
        // cashed back out for: `Rhythm` at span nought is built to hold NO edges,
        // and it held 38 -- the name manufactured the very link the control exists
        // to withhold. And the silent share fell from 26% to nought, because a
        // freshly minted name is surprising by construction, so the write-path gate
        // stopped tracking the prediction and started tracking the minting.
        //
        // SO A MACRO IS NOT A CO-OCCURRENCE PARTNER. What it is for is being an
        // ORIGIN and a TARGET -- one hop where five were, which is the traffic
        // argument `Motif` makes for chunking, applied to time -- and that is a
        // question a walk asks rather than something the write path does.
        _ = made;

        // BOTH HALVES OF THE ERROR, SETTLED ONCE. `Residual` spends the expectation
        // and moves every counter behind it, so it must be called exactly once per
        // observation -- and it is needed BEFORE the join now that the write path
        // can read it, where it used to be needed only after.
        var residual = _surprise.Residual(changes.Started);

        // STEP 2'S SECOND HALF: HOW MUCH OF THIS MOMENT IS WORTH LEARNING FROM.
        //
        // THE READ PATH HAS BEEN GATED SINCE `Surprise` LANDED AND THE WRITE PATH
        // NEVER WAS, and the comment below the join used to explain why: an expected
        // onset must still move the counts, or the graph stops getting better at
        // what it already predicts. THAT ARGUMENT IS ABOUT PRECISION AND THE COST OF
        // GIVING IT UP IS BLOCKING -- Rescorla and Wagner's whole claim is that
        // learning is proportional to prediction ERROR, so a cue added alongside one
        // that ALREADY predicts the outcome should acquire nothing. A pure
        // co-occurrence count hands it the full association, and that is a known
        // empirical failure of contiguity rather than a quirk of this design.
        //
        // THE SHARE THAT SURPRISED IS THE WEIGHT, which needs no dial: a wholly
        // expected moment is worth nothing to learn from and a wholly unexpected one
        // is worth exactly what it always was. `Occasion.Weight` is the channel the
        // plan named, and scaling the WHOLE occasion rather than picking codes out
        // of it is what keeps `together <= seen` -- both sides scale together.
        // THE WRITE PATH IS GATED UNCONDITIONALLY NOW -- John's call, 2026-08-04.
        var surprising = changes.Started.Length > 0
            ? residual.Surprising.Count / (double)changes.Started.Length
            : 1.0;

        // NOTHING SURPRISED, SO THERE IS NOTHING TO LEARN. `Observe` refuses a
        // weight of nought outright -- a coincidence worth nothing is not one -- so
        // the moment is not joined at all rather than joined at zero.
        // A QUESTION IS ABOUT WHAT IS BEING SHOWN AND NOT ABOUT WHAT SURPRISED.
        // Step 2 gates an OBSERVATION's broadcast by its surprise, and that is the
        // whole mechanism -- but the origins of a QUESTION are the onsets
        // themselves. Asking about a cue the machine has learnt to expect leaves
        // `Surprising` empty, and a walk with no origins reaches nothing, so the
        // better the graph knew the answer the more certainly it returned none.
        IReadOnlyList<Code> origins =
            asking is null ? residual.Surprising : changes.Started;

        if (surprising <= 0.0)
            return await ForeseeAsync(origins, asking, ct).ConfigureAwait(false);

        var joined = new Occasion
            {
                Onsets = starting,
                Live = standing,
                Recent = recent,
                At = now,
                Weight = worth * surprising,

                // WHAT THE FRONT END COULD SAY ABOUT WHICH THING IS WHICH, and
                // null for every front end that cannot. See Occasion.Groups.
                // A NAME JOINS THE OBJECT ITS MEMBERS CAME FROM, because a minted
                // code is one the front end never saw and therefore one no group
                // contains -- so without this it pairs with everything and the
                // gate is off for exactly the code standing in for the members.
                Groups = Grouped(bound, folded),

                // AND WHAT ORDER THEY CAME IN, where the front end can say and
                // null everywhere else. The order has to be said HERE, inside the
                // occasion, because a phase cannot survive C2 — see
                // Occasion.Sequence.
                Sequence = _quantizer.Order(frame),

                // AND WHICH OF THEM NAME THIS OCCASION RATHER THAN A KIND. Also
                // null for every front end that cannot. See Occasion.Fleeting.
                Fleeting = _quantizer.Fleeting(frame),

                // AND WHAT RELATION THE MOMENT STATES, WITH WHICH CODE IN WHICH
                // SLOT. THE CHANNEL THAT HAD NO ROUTE: `Occasion.Roles` and
                // `Kind.Role` were both built and neither could be reached from a
                // front end, so every role cell ever counted came from an occasion
                // a test built by hand. Null for every front end that observes
                // things rather than relations between them, which is all of them
                // until one says otherwise.
                As = _quantizer.Relating(frame),
                Roles = _quantizer.Filling(frame),

                // AND WHICH OF THEM NOTHING ABOUT THE STATE SELECTED, this moment's
                // and the carried ones together. Null for every front end that
                // observes rather than acts, which is all of them until a body says
                // otherwise. See Occasion.Forced.
                Forced = forced,
            };

        _joined = joined;

        await _rendezvous.JoinAsync(joined, ct).ConfigureAwait(false);

        // ONLY THE SURPRISE PROPAGATES — step 2. What is skipped here is the
        // BROADCAST, and the write above is gated too.
        // AND THE BROADCAST GOES OUT FROM THE NAME, which is the compression
        // arriving in the thinking path as well as in the graph: one origin where
        // there were S, and the walk reaches the members through it.
        // BOTH HALVES OF THE ERROR, AND ONLY THE POSITIVE ONE TRAVELS. What was
        // expected and did not arrive is counted where it is computed and goes
        // nowhere: there is no code for the thing that did not happen, so absence
        // is a signal the machine can read about itself and never a broadcast.
        return await ForeseeAsync(origins, asking, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Thinks, and remembers the thought as this machine's expectation.
    /// </summary>
    /// <remarks>
    /// <b>Every path out of an observation goes through here</b>, because an
    /// expectation that is set on one branch and not the other is a prediction
    /// whose denominator depends on which way the moment happened to go.
    /// </remarks>
    private async Task<Thought?> ForeseeAsync(
        IReadOnlyList<Code> surprising, Question? asking, CancellationToken ct)
    {
        // NOTHING SURPRISED, SO THERE IS NOTHING TO THINK ABOUT -- step 2, and it
        // is only reachable now that anything is ever expected.
        //
        // AND THE EXPECTATION IS CLEARED RATHER THAN LEFT STANDING, which is the
        // half a silent moment used to skip. `_foreseen` feeds `Surprise.Expect`,
        // and that replaces rather than accumulates precisely so an onset is never
        // silenced by a walk two moments old -- so a path that returns without
        // touching it would reinstate the fault by the back door.
        //
        // AND IT DOES NOT APPLY TO A QUESTION, WHICH IS THE HOLE THIS NEARLY
        // OPENED. Step 2's claim is about an OBSERVATION: a moment that was
        // entirely predicted need not be broadcast, because the broadcast is what
        // costs. A question is a request for an answer, and refusing to walk
        // because the input was expected would mean nothing predictable can ever
        // be ASKED about -- every world's accuracy silently gutted in exactly the
        // cases the graph had learnt best. `MachineTests` caught it by asking
        // about a code the machine had just been taught to expect.
        if (surprising.Count == 0)
        {
            _foreseen = null;
            return null;
        }

        var thought = await ThinkAsync(surprising, null, asking, ct).ConfigureAwait(false);

        _foreseen = thought;

        return thought;
    }

    /// <summary>
    /// The front end's grouping, with every minted name joined to the object its
    /// members came from.
    /// </summary>
    /// <remarks>
    /// <b>THE MEMBERS CANNOT DISAGREE, because a candidate spanning two groups was
    /// refused before it was ever folded</b> — see <see cref="Learning.Chunk"/>. So
    /// the first member that has a group settles it, and a name whose members were
    /// all ungrouped stays ungrouped, which is what it was.
    /// </remarks>
    private static IReadOnlyDictionary<Code, int>? Grouped(
        IReadOnlyDictionary<Code, int>? bound, Chunk.Substitution folded)
    {
        if (bound is null || !folded.Folded) return bound;

        var grouped = new Dictionary<Code, int>(bound);

        foreach (var (name, members) in folded.Names)
        {
            foreach (var member in members)
            {
                if (!bound.TryGetValue(member, out var group)) continue;

                grouped[name] = group;
                break;
            }
        }

        return grouped;
    }

    /// <summary>
    /// The last occasion this machine wrote, <b>exactly as it was written.</b>
    /// </summary>
    /// <remarks>
    /// <b>SO IT CAN BE REINFORCED, and reconstructing it is not the same thing.</b>
    /// What joins is not the codes the caller handed over: onsets are separated
    /// from what was already live, the window's carried codes are folded in, and
    /// the front end may have said something about grouping, order or which codes
    /// are fleeting. Rebuilding all of that from the outside gets a NEIGHBOURING
    /// occasion — measured on `Homeostat`, where a top-up written as all-onsets
    /// paired codes that had been live and lost to not topping up at all.
    /// </remarks>
    public Occasion? Joined => _joined;

    /// <summary>
    /// Writes an occasion again, carrying what it turned out to be worth —
    /// <b>learning with no thinking, which is what a trace consolidating needs.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ADDING TWICE IS WHAT A G-COUNTER IS FOR.</b> The counts stay monotonic,
    /// so this converges exactly as the first write did — and it is why
    /// reinforcement is expressible here and punishment is not: there is no
    /// second write that can take something back.
    /// </para>
    /// <para>
    /// <b>Nothing is broadcast.</b> The moment has already been thought about; what
    /// is happening now is the graph being told how much that moment was worth,
    /// which is a fact about learning and not a question.
    /// </para>
    /// </remarks>
    /// <param name="occasion">One this machine wrote — see <see cref="Joined"/>.</param>
    /// <param name="worth">
    /// What to add, over and above what was written the first time. <b>Positive</b>:
    /// an occasion worth nothing is not an occasion, and a count that could stop
    /// increasing is not a G-Counter.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    public ValueTask ReinforceAsync(
        Occasion occasion, double worth, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(occasion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(worth);

        return _rendezvous.JoinAsync(occasion with { Weight = worth }, ct);
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

            // AND WHETHER THE CREDIT CELL IS READ AGAINST THE BASE RATE, carried
            Recent = question?.Recent ?? false,
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
            if (thought.Lost(gone) <= 0 || !thought.Settled) continue;

            // BOTH MAPS, OR THE SECOND ONE GROWS FOREVER. `Retire` only ever
            // reaches `_quiet` entries whose thought is still tracked, so a
            // thought removed here left its entry behind with nothing that could
            // ever collect it — a slow leak keyed by broadcast id, which is
            // unique per thought and therefore never reused.
            _thoughts.TryRemove(broadcast, out _);
            _quiet.TryRemove(broadcast, out _);
        }
    }
}
