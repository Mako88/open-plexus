using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>
/// One ask, and what has come back for it so far.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no clock in here and that is the design rather than an omission.</b> The
/// obvious build waits a while and decides; the plan's revival table refuses it outright —
/// <i>a miss decided by a deadline</i>, because C2 makes late indistinguishable from absent
/// and a monotone counter cannot retract. So this accumulates and reports, and WHEN to
/// read it is the caller's decision, made from something other than a timer.
/// </para>
/// <para>
/// <b>And the denominator is carried beside the numerator, which is the whole C3
/// INSTRUMENT.</b> <see cref="Population.Decide"/> cannot tell a silent holder from a dead
/// one and is not supposed to; <see cref="Asked"/> against <see cref="Heard"/> is where
/// that difference lives, and without it a vote over eight survivors of twelve reads
/// exactly like a vote over twelve.
/// </para>
/// <para>
/// <b>Keyed by who spoke rather than by what arrived.</b> C2 permits a message to arrive
/// twice, and a merge that absorbed one holder's counts twice would weigh one machine's
/// scopes double — silently, plausibly, and in the direction that makes a redundancy look
/// better certified than it is.
/// </para>
/// </remarks>
public sealed class Gathering : IDisposable
{
    private readonly Dictionary<MachineAddress, Answer> _heard = [];

    /// <summary>Which slot each holder that was asked belongs to.</summary>
    private readonly Dictionary<MachineAddress, string> _slotOf = [];

    /// <summary>
    /// Per slot, who was asked and has neither answered nor been written off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The roster rather than the count, which is the whole of what fork 53 needed
    /// here.</b> A count can only be compared with another count, so a gathering holding one
    /// could tell that somebody was missing and never which somebody — and a write-off has
    /// to name a machine or it is a decrement, which is a deadline with the clock hidden.
    /// </para>
    /// <para>
    /// <b>And grouped by slot, which is fork 62 and is the other half of the same
    /// sentence.</b> A write-off ends the wait for a holder the question never reached; a
    /// slot ends the wait for a holder that TOOK the question and went, because somebody
    /// else holding the identical population can answer in its place. Neither is a clock,
    /// and the second is the only thing that reaches the round a machine dies inside.
    /// </para>
    /// </remarks>
    private readonly Dictionary<string, HashSet<MachineAddress>> _owed = [];

    /// <summary>Slots that have had an answer folded in.</summary>
    private readonly HashSet<string> _spoken = [];

    /// <summary>Slots nothing more is owed from, however that came about.</summary>
    private readonly HashSet<string> _closed = [];

    private readonly Lock _gate = new();
    private readonly Action _closing;

    private int _left;
    private int _unreached;
    private int _echoed;
    private bool _shut;

    /// <param name="asked">Who was asked.</param>
    /// <param name="closing">What to do when the asker stops caring.</param>
    /// <param name="slots">
    /// Which slot a holder is in, or nothing where the fleet is not partitioned.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>It does not carry which ask it is, because nothing outside the asker needs to
    /// know.</b> The correlation is the <see cref="Asker"/>'s bookkeeping and holding it
    /// here as well would be a second copy of one fact — and this repo's dead-code budget
    /// is the check that asks whether a public member has a caller at all.
    /// </para>
    /// <para>
    /// <b>A holder alone is a slot of one named after itself, so no partition is not a
    /// special case.</b> With nothing handed in, every holder is its own slot and every
    /// condition below reduces to counting holders — which is what makes R=1 bit-identical
    /// to the machine before fork 62 rather than merely close to it.
    /// </para>
    /// </remarks>
    internal Gathering(
        IReadOnlyCollection<MachineAddress> asked,
        Action closing,
        Func<MachineAddress, string>? slots = null)
    {
        Asked = asked.Count;
        _closing = closing;

        foreach (var who in asked)
        {
            var slot = slots?.Invoke(who) ?? who.Value;

            _slotOf[who] = slot;

            if (!_owed.TryGetValue(slot, out var here)) _owed[slot] = here = [];

            here.Add(who);
        }

        _left = _owed.Count;

        // ASKING NOBODY IS ALREADY WHOLE, which is not a special case so much as the
        // degenerate one: a fleet of nought holders has been heard from in full, and
        // waiting on that would be waiting for an answer that cannot exist.
        if (_left == 0) _everyone.TrySetResult();
    }

    /// <summary>How many holders were asked.</summary>
    public int Asked { get; }

    /// <summary>How many distinct holders have answered and been counted.</summary>
    /// <remarks>
    /// <b>One a slot, which is why this is not simply how many messages came back.</b>
    /// Replicas in a slot hold the identical population, so adding two of them up would
    /// weigh one shard's scopes double — which is the fault this class's own header warns
    /// about, arriving from the deployment rather than from a duplicate message. The second
    /// voice is <see cref="Echoed"/>.
    /// </remarks>
    public int Heard
    {
        get { lock (_gate) return _heard.Count; }
    }

    /// <summary>
    /// How many holders answered a slot that had already spoken — <b>the check that says
    /// the replicas are there rather than merely configured.</b>
    /// </summary>
    /// <remarks>
    /// <b>A check can be wired and unable to fire, which is why the dropped answers are
    /// counted rather than dropped silently.</b> A fleet declared with two machines a slot
    /// where one of them never answers is a fleet of one wearing redundancy's name, and it
    /// survives every death test by being lucky about which machine died. This reads nought
    /// in exactly that case and never in the healthy one.
    /// </remarks>
    public int Echoed
    {
        get { lock (_gate) return _echoed; }
    }

    /// <summary>How many answering holders had anything to advocate.</summary>
    /// <remarks>
    /// <b>Silence reported beside the score, which is a trap this project already has a
    /// line for.</b> A gathering where every holder answered and none of them fired
    /// decides identically to one that was never asked, so a run comparing a whole vote
    /// with a split one can agree perfectly while combining nothing — and read as the
    /// merge working.
    /// </remarks>
    public int Spoke
    {
        get { lock (_gate) return _heard.Values.Count(one => one.Said is { Silent: false }); }
    }

    /// <summary>How many holders were never handed the question at all.</summary>
    /// <remarks>
    /// <b>The term that separates a silence from a loss, and it is not the same as
    /// <see cref="Asked"/> MINUS <see cref="Heard"/>.</b> That difference is every holder
    /// yet to speak, of which some are thinking and some are gone; this is the share the
    /// sender watched fail to leave. A run reading only the first cannot tell a fleet that
    /// was slow from a fleet that was smaller than it thought.
    /// </remarks>
    public int Unreached
    {
        get { lock (_gate) return _unreached; }
    }

    /// <summary>Whether everyone asked has answered.</summary>
    /// <remarks>
    /// <b>Not a condition anything may block on forever, and deliberately not what
    /// <see cref="Everyone"/> WAITS FOR SINCE FORK 53.</b> A round finished by a write-off
    /// completes without being whole, and keeping the two apart is what stops a fleet of
    /// twelve that heard eight reading like a fleet of eight — which is the C3 instrument
    /// and the reason the denominator is carried at all.
    /// </remarks>
    /// <remarks>
    /// <b>And an echo counts towards it, because a replica that spoke was heard from.</b>
    /// What this asks is whether every machine put to answered, which is a question about the
    /// fleet's health; what <see cref="Heard"/> asks is how much distinct evidence arrived,
    /// which is a question about the vote. Under R=1 they are the same number and this is the
    /// only place the difference shows.
    /// </remarks>
    public bool Whole
    {
        get { lock (_gate) return _heard.Count + _echoed == Asked; }
    }

    /// <summary>
    /// Completes when every holder still owed an answer has given one, and never on a timer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>So a caller can wait on the event rather than look for it.</b> Polling
    /// <see cref="Whole"/> works and puts the poll's granularity into anything timing the
    /// exchange — which would make a measurement of what distance costs a measurement of
    /// how often somebody looked.
    /// </para>
    /// <para>
    /// <b>And what is owed comes down as well as up, which is fork 53 and was the whole
    /// difference from the walk.</b> A holder the ask never reached is written off, exactly
    /// as a departing cluster takes the routes heading into it, so a fleet that has lost a
    /// machine finishes its round on the arrivals it can still expect. Nothing here is
    /// decided by elapsed time and nothing is retracted: the write-off removes a claim on
    /// the future, never a count.
    /// </para>
    /// <para>
    /// <b>It is still not a promise that it completes, and what it waits on is a slot.</b> A
    /// holder that took the question and died is owed forever because late and absent are
    /// one thing under C2 — so what finishes that round is another machine holding the same
    /// population answering in its place. A slot every one of whose replicas is silent still
    /// stops the round, correctly, and that is the promise this cannot make.
    /// </para>
    /// </remarks>
    public Task Everyone => _everyone.Task;

    private readonly TaskCompletionSource _everyone =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Folds one holder's answer in.</summary>
    /// <param name="answer">What arrived.</param>
    internal void Fold(Answer answer)
    {
        var whole = false;

        lock (_gate)
        {
            if (_slotOf.TryGetValue(answer.From, out var slot))
            {
                // A slot speaks once, and the second voice is dropped rather than added.
                // Replicas in a slot are fed one stream and mint the same children
                // independently, so their answers are the same answer -- and `Merged`,
                // `Added` and `Tables` all ADD what they are given. Two of them is one
                // shard's evidence counted twice, which is the failure this class's header
                // describes for a duplicate message and is the same failure by deployment.
                if (!_spoken.Add(slot))
                {
                    // A different machine is a replica; the same one twice is a duplicate
                    // message, and only the first says anything about the fleet's shape.
                    if (!_heard.ContainsKey(answer.From)) _echoed++;

                    return;
                }

                _owed[slot].Remove(answer.From);

                if (_closed.Add(slot)) whole = --_left == 0;
            }

            _heard[answer.From] = answer;
        }

        if (whole) _everyone.TrySetResult();
    }

    /// <summary>Writes off a holder the ask never reached, and never one that is merely quiet.</summary>
    /// <param name="who">The holder that was not handed the question.</param>
    /// <remarks>
    /// <para>
    /// <b>The loss is exact rather than estimated, which is the property borrowed from the
    /// walk.</b> A departure there writes off the routes heading into one cluster because a
    /// report said how many there were; here it writes off one answer because the sender
    /// watched the question fail to leave. Neither is a guess about what a silent machine
    /// might be doing, and that is the only kind of write-off this design permits.
    /// </para>
    /// <para>
    /// <b>And an answer that arrives anyway is still folded in.</b> C2 allows the question
    /// to have got through while the acknowledgement did not, so this removes a claim on an
    /// answer rather than a right to one — the count is monotone in both directions it
    /// matters, and the only thing given up is the waiting.
    /// </para>
    /// </remarks>
    internal void WriteOff(MachineAddress who)
    {
        var whole = false;

        lock (_gate)
        {
            if (!_slotOf.TryGetValue(who, out var slot)) return;
            if (!_owed[slot].Remove(who)) return;

            _unreached++;

            // And a slot is only lost when every replica in it is, which is fork 62's whole
            // arithmetic: with R=1 this fires on the first write-off and is exactly what
            // fork 53 shipped, and with R=2 a fleet writes off one machine and waits on the
            // one that can still answer for the same population.
            if (_owed[slot].Count == 0 && _closed.Add(slot)) whole = --_left == 0;
        }

        if (whole) _everyone.TrySetResult();
    }

    /// <summary>The vote over whoever answered.</summary>
    /// <remarks>
    /// <b>A vote with nothing in it comes back with no expectation, and that is the third
    /// outcome arriving.</b> The plan records <c>Abstain</c> as unarmed in any run because
    /// nothing in one process can die; a gathering whose every advocate was on a machine
    /// that has gone is the case it was written for, and it is reachable here for the first
    /// time.
    /// </remarks>
    public Vote Decide()
    {
        List<Testimony> said;

        lock (_gate)
            said =
            [
                .. _heard.Values
                    .Where(one => one.Said is not null)
                    .Select(one => one.Said!.Value),
            ];

        return Population.Decide(said);
    }

    /// <summary>What every answering holder added, added up.</summary>
    /// <remarks>
    /// <b>Three counts and not a population, which is what C1 leaves a fleet able to
    /// report.</b> How many rules a machine minted is a number; which rules they are is the
    /// thing that may never leave. So a distributed run can say how hard it searched and
    /// cannot say what it holds, and the second of those is a fact an experimenter outside
    /// the machine assembles rather than one the machine reports about itself.
    /// </remarks>
    public Learnt Added()
    {
        long minted = 0, repaired = 0, subsumed = 0, widened = 0;

        lock (_gate)
            foreach (var answer in _heard.Values)
            {
                if (answer.Did is not { } did) continue;

                minted += did.Minted;
                repaired += did.Repaired;
                subsumed += did.Subsumed;
                widened += did.Widened;
            }

        return new Learnt
        {
            Minted = minted,
            Repaired = repaired,
            Subsumed = subsumed,
            Widened = widened,
        };
    }

    /// <summary>Every answering holder's table, with the holder it came from.</summary>
    /// <remarks>
    /// <b>Kept apart rather than merged, because the merge each holder needs excludes
    /// itself.</b> <see cref="Merged"/> is the asker's own view and is what a name is
    /// certified against; this is what goes back out, so that every holder can add up the
    /// others and leave its own row alone — see <see cref="Tabled"/>.
    /// </remarks>
    public ImmutableArray<Tabled> Tables()
    {
        lock (_gate)
            return
            [
                .. _heard.Values
                    .Where(one => one.Counted is not null)
                    .OrderBy(one => one.From.Value, StringComparer.Ordinal)
                    .Select(one => new Tabled
                    {
                        From = one.From,

                        // The slot travels and the partition does not, which is the one
                        // thing that had to cross for a replicated sweep to be right. A
                        // holder drops the row belonging to its OWN slot, and under
                        // replication that row carries somebody else's name — so filtering
                        // by who sent it would have every replica absorb a copy of its own
                        // scopes and certify a redundancy it is the sole evidence for.
                        Slot = _slotOf.TryGetValue(one.From, out var slot)
                            ? slot
                            : one.From.Value,

                        Counted = one.Counted!,
                    }),
            ];
    }

    /// <summary>Every answering holder's counts, added up.</summary>
    /// <remarks>
    /// <b>Integer addition, so no ordering caveat applies and the order is fixed
    /// anyway.</b> <see cref="Recurrence.Absorb"/> is commutative and exact — unlike the
    /// summed vote, which is not associative in its last bits — but the walk is ordered
    /// regardless, because anything that hashes or logs what crossed would otherwise move
    /// with delivery order.
    /// </remarks>
    public Recurrence Merged()
    {
        var merged = new Recurrence();

        lock (_gate)
            foreach (var answer in _heard.Values
                .Where(one => one.Counted is not null)
                .OrderBy(one => one.From.Value, StringComparer.Ordinal))
                merged.Absorb(Recurrence.From(answer.Counted!));

        return merged;
    }

    /// <summary>The asker stops caring, and a late answer to this ask is dropped.</summary>
    public void Dispose()
    {
        if (_shut) return;

        _shut = true;
        _closing();
    }
}

/// <summary>
/// A machine that puts a question to every holder at once and takes what comes back.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scatter and gather, with nothing blocking between them.</b>
/// <see cref="IBus.AskAsync"/> returns who was asked and never what they said, so the two
/// halves are separate messages and a holder that has died costs an answer rather than a
/// timeout. See <see cref="Ask"/> for why the request-and-wait shape was refused.
/// </para>
/// <para>
/// <b>The asker need hold no commitments, which is why this is not a method on
/// <see cref="Population"/>.</b> An input machine putting a question to a fleet is the
/// ordinary case, and a merger that reached into a population to finish a vote would make
/// itself an extra voter without saying so.
/// </para>
/// </remarks>
public sealed class Asker : IReceiveAnswers
{
    private readonly IBus _bus;
    private readonly Func<MachineAddress, string>? _slots;
    private readonly Dictionary<BroadcastId, Gathering> _open = [];
    private readonly Lock _gate = new();

    /// <param name="address">Where its answers come back to.</param>
    /// <param name="bus">How its asks go out.</param>
    /// <param name="slots">
    /// Which slot a holder is in — <b>fork 62, and it is HANDED IN rather than announced.</b>
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>The partition is the experimenter's deployment fact and not something on the
    /// wire.</b> Putting it in <c>Posted.Roster</c> is the obvious build and it would teach
    /// the transport what a holder HOLDS — <see cref="IBus"/> does not know what a cluster
    /// is, and it has no business knowing that two machines hold the same population either.
    /// This arrives the way <see cref="Population.Places"/> does: from whoever composed the
    /// fleet, once, in the process that composed it.
    /// </para>
    /// <para>
    /// <b>And nothing is a special case, because a holder alone is a slot of one.</b> Left
    /// null, every gathering below partitions into one slot per holder and behaves exactly
    /// as it did before this existed.
    /// </para>
    /// </remarks>
    public Asker(
        MachineAddress address, IBus bus, Func<MachineAddress, string>? slots = null)
    {
        ArgumentNullException.ThrowIfNull(bus);

        Address = address;
        _bus = bus;
        _slots = slots;

        // Subscribed and never unsubscribed, which is `InputMachine`'S shape for the same
        // event and for the same reason. An asker outlives every gathering it opens and dies
        // with its bus, so there is no moment where dropping this would be right and one
        // where it would silently stop writing off.
        _bus.Unreached += OnUnreached;
    }

    /// <inheritdoc/>
    public MachineAddress Address { get; }

    /// <summary>Asks every holder, and returns the gathering their answers land in.</summary>
    /// <param name="wants">What to ask for.</param>
    /// <param name="moment">What is live, for a vote.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>The gathering is registered before the first holder is asked.</b> A local holder
    /// answers by direct call, so an answer can arrive before this method returns — which
    /// is the same measured race <c>BroadcastAsync</c> opens its <c>ready</c> window for,
    /// and it lost reports the first time nobody accounted for it.
    /// </remarks>
    public Task<Gathering> AskAsync(
        Wanted wants, IReadOnlySet<Code>? moment = null, CancellationToken ct = default) =>
        ScatterAsync(new Ask
        {
            Broadcast = BroadcastId.New(),
            ReturnTo = Address,
            Wants = wants,
            Moment = moment is null ? [] : [.. moment.Order()],
        }, ct);

    /// <summary>
    /// Tells every holder what the settlement said, and returns where what they did with
    /// it lands.
    /// </summary>
    /// <param name="moment">What was live, carried again so nobody has to remember it.</param>
    /// <param name="arrived">What followed, or nothing where the settlement could not say.</param>
    /// <param name="wrong">Whether the fleet's vote missed.</param>
    /// <param name="sweeping">Whether this is a sweep round.</param>
    /// <param name="counted">Every holder's table, for a sweep. Empty otherwise.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>The same scatter as a question, which is why it is an <see cref="Ask"/>.</b> A
    /// settlement goes to every holder at once and each says what it added, so the
    /// accounting — how many did I tell, how many answered — is one mechanism rather than
    /// two. What makes it a telling rather than a question is only which fields are filled.
    /// </remarks>
    public Task<Gathering> TellAsync(
        IReadOnlySet<Code> moment,
        Code? arrived,
        bool wrong,
        bool sweeping,
        ImmutableArray<Tabled> counted,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(moment);

        return ScatterAsync(new Ask
        {
            Broadcast = BroadcastId.New(),
            ReturnTo = Address,
            Wants = Wanted.Settle,
            Moment = [.. moment.Order()],
            Arrived = arrived,
            Wrong = wrong,
            Sweeping = sweeping,
            Counted = counted.IsDefault ? [] : counted,
        }, ct);
    }

    /// <summary>Puts one ask to every holder and opens the gathering its answers land in.</summary>
    /// <param name="ask">The question, or the telling.</param>
    /// <param name="ct">Cancellation.</param>
    private async Task<Gathering> ScatterAsync(Ask ask, CancellationToken ct)
    {
        Gathering? gathering = null;

        await _bus.AskAsync(ask, ct, asked =>
        {
            gathering = new Gathering(
                asked,
                () => { lock (_gate) _open.Remove(ask.Broadcast); },
                _slots);

            lock (_gate) _open[ask.Broadcast] = gathering;
        }).ConfigureAwait(false);

        // A bus that never opened the window asked nobody, and an empty gathering is the
        // honest answer rather than a null: nought heard of nought asked, which decides to
        // silence and merges to an empty table.
        return gathering ?? new Gathering([], static () => { });
    }

    /// <inheritdoc/>
    public Task DeliverAsync(Answer answer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        Gathering? at;
        lock (_gate) _open.TryGetValue(answer.Broadcast, out at);

        // An answer to an ask nobody remembers is dropped, exactly as a report for an
        // unknown broadcast is. C2 says an answer can arrive after the asker has moved on,
        // and there is nothing for it to be folded into.
        at?.Fold(answer);

        return Task.CompletedTask;
    }

    /// <summary>One holder was never handed one ask, so that gathering stops waiting on it.</summary>
    /// <param name="broadcast">Which ask never left.</param>
    /// <param name="who">Which holder it was going to.</param>
    /// <remarks>
    /// <b>One gathering rather than all of them, which is sharper than the walk's event and
    /// not merely different.</b> <c>InputMachine.OnDeath</c> walks every thought it holds
    /// because a departed cluster strands routes in all of them; a question that failed to
    /// leave failed for one question, and a holder unreachable for this ask may take the
    /// next one. Writing off the others would be inferring a death from a dropped message,
    /// which is the guess this whole path exists to avoid.
    /// </remarks>
    private void OnUnreached(BroadcastId broadcast, MachineAddress who)
    {
        Gathering? at;
        lock (_gate) _open.TryGetValue(broadcast, out at);

        at?.WriteOff(who);
    }
}
