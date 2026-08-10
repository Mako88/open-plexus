using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Thinking;

namespace OpenPlexus.Machines;

/// <summary>
/// One ask, and what has come back for it so far.
/// </summary>
/// <remarks>
/// <para>
/// <b>THERE IS NO CLOCK IN HERE AND THAT IS THE DESIGN RATHER THAN AN OMISSION.</b> The
/// obvious build waits a while and decides; the plan's revival table refuses it outright —
/// <i>a miss decided by a deadline</i>, because C2 makes late indistinguishable from absent
/// and a monotone counter cannot retract. So this accumulates and reports, and WHEN to
/// read it is the caller's decision, made from something other than a timer.
/// </para>
/// <para>
/// <b>AND THE DENOMINATOR IS CARRIED BESIDE THE NUMERATOR, WHICH IS THE WHOLE C3
/// INSTRUMENT.</b> <see cref="Population.Decide"/> cannot tell a silent holder from a dead
/// one and is not supposed to; <see cref="Asked"/> against <see cref="Heard"/> is where
/// that difference lives, and without it a vote over eight survivors of twelve reads
/// exactly like a vote over twelve.
/// </para>
/// <para>
/// <b>KEYED BY WHO SPOKE RATHER THAN BY WHAT ARRIVED.</b> C2 permits a message to arrive
/// twice, and a merge that absorbed one holder's counts twice would weigh one machine's
/// scopes double — silently, plausibly, and in the direction that makes a redundancy look
/// better certified than it is.
/// </para>
/// </remarks>
public sealed class Gathering : IDisposable
{
    private readonly Dictionary<MachineAddress, Answer> _heard = [];
    private readonly Lock _gate = new();
    private readonly Action _closing;

    private bool _closed;

    /// <param name="asked">Who was asked.</param>
    /// <param name="closing">What to do when the asker stops caring.</param>
    /// <remarks>
    /// <b>IT DOES NOT CARRY WHICH ASK IT IS, BECAUSE NOTHING OUTSIDE THE ASKER NEEDS TO
    /// KNOW.</b> The correlation is the <see cref="Asker"/>'s bookkeeping and holding it
    /// here as well would be a second copy of one fact — and this repo's dead-code budget
    /// is the check that asks whether a public member has a caller at all.
    /// </remarks>
    internal Gathering(IReadOnlyCollection<MachineAddress> asked, Action closing)
    {
        Asked = asked.Count;
        _closing = closing;

        // ASKING NOBODY IS ALREADY WHOLE, which is not a special case so much as the
        // degenerate one: a fleet of nought holders has been heard from in full, and
        // waiting on that would be waiting for an answer that cannot exist.
        if (Asked == 0) _everyone.TrySetResult();
    }

    /// <summary>How many holders were asked.</summary>
    public int Asked { get; }

    /// <summary>How many distinct holders have answered.</summary>
    public int Heard
    {
        get { lock (_gate) return _heard.Count; }
    }

    /// <summary>How many answering holders had anything to advocate.</summary>
    /// <remarks>
    /// <b>SILENCE REPORTED BESIDE THE SCORE, WHICH IS A TRAP THIS PROJECT ALREADY HAS A
    /// LINE FOR.</b> A gathering where every holder answered and none of them fired
    /// decides identically to one that was never asked, so a run comparing a whole vote
    /// with a split one can agree perfectly while combining nothing — and read as the
    /// merge working.
    /// </remarks>
    public int Spoke
    {
        get { lock (_gate) return _heard.Values.Count(one => one.Said is { Silent: false }); }
    }

    /// <summary>Whether everyone asked has answered.</summary>
    /// <remarks>
    /// <b>NOT A CONDITION ANYTHING MAY BLOCK ON FOREVER.</b> Under C3 a holder that has
    /// died never answers, so this is false for the rest of the run — which is why it is
    /// reported rather than awaited, and why the vote is defined over whoever spoke.
    /// </remarks>
    public bool Whole => Heard == Asked;

    /// <summary>
    /// Completes when everyone asked has answered, and never on a timer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>SO A CALLER CAN WAIT ON THE EVENT RATHER THAN LOOK FOR IT.</b> Polling
    /// <see cref="Whole"/> works and puts the poll's granularity into anything timing the
    /// exchange — which would make a measurement of what distance costs a measurement of
    /// how often somebody looked.
    /// </para>
    /// <para>
    /// <b>IT IS NOT A PROMISE THAT IT COMPLETES.</b> Under C3 a dead holder never answers,
    /// so this stays pending for the rest of the run; whether to give up, and on what
    /// grounds, is the caller's decision — and there is deliberately nothing here to make
    /// it on.
    /// </para>
    /// </remarks>
    public Task Everyone => _everyone.Task;

    private readonly TaskCompletionSource _everyone =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Folds one holder's answer in.</summary>
    /// <param name="answer">What arrived.</param>
    internal void Fold(Answer answer)
    {
        bool whole;

        lock (_gate)
        {
            _heard[answer.From] = answer;
            whole = _heard.Count >= Asked;
        }

        if (whole) _everyone.TrySetResult();
    }

    /// <summary>The vote over whoever answered.</summary>
    /// <remarks>
    /// <b>A VOTE WITH NOTHING IN IT COMES BACK WITH NO EXPECTATION, AND THAT IS THE THIRD
    /// OUTCOME ARRIVING.</b> The plan records <c>Abstain</c> as unarmed in any run because
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
    /// <b>THREE COUNTS AND NOT A POPULATION, WHICH IS WHAT C1 LEAVES A FLEET ABLE TO
    /// REPORT.</b> How many rules a machine minted is a number; which rules they are is the
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
    /// <b>KEPT APART RATHER THAN MERGED, BECAUSE THE MERGE EACH HOLDER NEEDS EXCLUDES
    /// ITSELF.</b> <see cref="Merged"/> is the asker's own view and is what a name is
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
                    .Select(one => new Tabled { From = one.From, Counted = one.Counted! }),
            ];
    }

    /// <summary>Every answering holder's counts, added up.</summary>
    /// <remarks>
    /// <b>INTEGER ADDITION, SO NO ORDERING CAVEAT APPLIES AND THE ORDER IS FIXED
    /// ANYWAY.</b> <see cref="Recurrence.Absorb"/> is commutative and exact — unlike the
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
        if (_closed) return;

        _closed = true;
        _closing();
    }
}

/// <summary>
/// A machine that puts a question to every holder at once and takes what comes back.
/// </summary>
/// <remarks>
/// <para>
/// <b>SCATTER AND GATHER, WITH NOTHING BLOCKING BETWEEN THEM.</b>
/// <see cref="IBus.AskAsync"/> returns who was asked and never what they said, so the two
/// halves are separate messages and a holder that has died costs an answer rather than a
/// timeout. See <see cref="Ask"/> for why the request-and-wait shape was refused.
/// </para>
/// <para>
/// <b>THE ASKER NEED HOLD NO COMMITMENTS, WHICH IS WHY THIS IS NOT A METHOD ON
/// <see cref="Population"/>.</b> An input machine putting a question to a fleet is the
/// ordinary case, and a merger that reached into a population to finish a vote would make
/// itself an extra voter without saying so.
/// </para>
/// </remarks>
public sealed class Asker : IReceiveAnswers
{
    private readonly IBus _bus;
    private readonly Dictionary<BroadcastId, Gathering> _open = [];
    private readonly Lock _gate = new();

    /// <param name="address">Where its answers come back to.</param>
    /// <param name="bus">How its asks go out.</param>
    public Asker(MachineAddress address, IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        Address = address;
        _bus = bus;
    }

    /// <inheritdoc/>
    public MachineAddress Address { get; }

    /// <summary>Asks every holder, and returns the gathering their answers land in.</summary>
    /// <param name="wants">What to ask for.</param>
    /// <param name="moment">What is live, for a vote.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>THE GATHERING IS REGISTERED BEFORE THE FIRST HOLDER IS ASKED.</b> A local holder
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
    /// <b>THE SAME SCATTER AS A QUESTION, WHICH IS WHY IT IS AN <see cref="Ask"/>.</b> A
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
            gathering = new Gathering(asked, () =>
            {
                lock (_gate) _open.Remove(ask.Broadcast);
            });

            lock (_gate) _open[ask.Broadcast] = gathering;
        }).ConfigureAwait(false);

        // A BUS THAT NEVER OPENED THE WINDOW ASKED NOBODY, and an empty gathering is the
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

        // AN ANSWER TO AN ASK NOBODY REMEMBERS IS DROPPED, exactly as a report for an
        // unknown broadcast is. C2 says an answer can arrive after the asker has moved on,
        // and there is nothing for it to be folded into.
        at?.Fold(answer);

        return Task.CompletedTask;
    }
}
