using System.Collections.Immutable;
using System.Diagnostics;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>What one round of learning added, from whoever did it.</summary>
/// <remarks>
/// <b>THREE COUNTS RATHER THAN THREE RETURN VALUES, BECAUSE ON A FLEET THEY COME BACK
/// TOGETHER.</b> A holder settles, sweeps, covers and repairs on one telling — that is the
/// whole point of pushing the settlement rather than driving each phase from the asker — so
/// what it did arrives as one answer. In one process the same record is filled in by four
/// consecutive calls, which is what keeps the two arrangements comparable.
/// </remarks>
public readonly record struct Learnt
{
    /// <summary>Commitments minted by genesis.</summary>
    public required long Minted { get; init; }

    /// <summary>Children minted by repair.</summary>
    public required long Repaired { get; init; }

    /// <summary>Narrower commitments a general one took the place of.</summary>
    public required long Subsumed { get; init; }
}

/// <summary>
/// Whoever the learning loop asks and tells — <b>one population, or a fleet of them.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE SEAM IS TWO CALLS WIDE, AND WHICH TWO IS THE FINDING RATHER THAN A CHOICE.</b>
/// Every operator on <see cref="Population"/> decides local except two: the vote reads what
/// everything that fired advocates, and <see cref="Population.Abstract"/> reads how often
/// scopes recur across the whole population. Settling, covering and repairing are each a
/// commitment's own business, so they need no seam at all — they need only to happen on the
/// machine that holds the commitment.
/// </para>
/// <para>
/// <b>SO ASKING IS A SCATTER-GATHER AND TELLING IS A PUSH.</b> A vote has to come back
/// before the round can be scored; a settlement has to reach every holder and has no answer
/// worth waiting for beyond what it did. That asymmetry is why this is two methods and not
/// one round-shaped call.
/// </para>
/// <para>
/// <b>AND IT IS ASYNCHRONOUS ON PURPOSE, INCLUDING WHERE IT NEVER YIELDS.</b>
/// <see cref="Alone"/> completes every task synchronously and allocates nothing to do it,
/// so a one-process run pays for this seam in exactly nothing — see
/// <c>Machines.Trial.Run</c>, which refuses a council that would have made it wait. What the
/// shape buys is that a gathering arriving when it arrives needs no second learning loop,
/// and two copies of this loop is the one duplication that could silently start learning two
/// different things.
/// </para>
/// </remarks>
public interface ICouncil
{
    /// <summary>Where the wall clock went, by phase.</summary>
    /// <remarks>
    /// <b>AND NO DIALS BESIDE IT, WHICH IS A DELIBERATE ABSENCE.</b> <c>Cycle</c> read
    /// <c>Repairing</c> off the population to decide where a call sat, so one setting was
    /// consulted in two places and a cell that moved the gate also moved the timing.
    /// Every dial that decides something is now read where it decides, and a loop that
    /// could see them would be a second opinion about the brain's numbers.
    /// </remarks>
    Spent Spent { get; }

    /// <summary>
    /// Notes what is live, gathers what fires, and takes the vote.
    /// </summary>
    /// <param name="raw">What the front end said, before any minted name is folded in.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>RAW, BECAUSE A NAME IS FOLDED BY WHOEVER HOLDS IT.</b> A minted code is reached by
    /// inference from the codes it stands for, so a moment folded by the loop would carry one
    /// machine's vocabulary onto machines that may not share it — and whether they share it
    /// is exactly what the counts merge exists to establish.
    /// </remarks>
    ValueTask<Vote> AskAsync(IReadOnlySet<Code> raw, CancellationToken ct = default);

    /// <summary>
    /// Settles what fired on the last ask, sweeps if it is a sweep round, and covers and
    /// repairs what the settlement blamed.
    /// </summary>
    /// <param name="arrived">
    /// What followed, or nothing where the settlement could not say.
    /// </param>
    /// <param name="wrong">
    /// Whether the vote missed. <b>The FLEET's vote and never a holder's</b>, which is why
    /// it is told rather than recomputed: genesis is gated on the population having no
    /// account of what happened, and a shard's own silence is not that.
    /// </param>
    /// <param name="sweeping">Whether this is a sweep round.</param>
    /// <param name="ct">Cancellation.</param>
    ValueTask<Learnt> TellAsync(
        Code? arrived, bool wrong, bool sweeping, CancellationToken ct = default);
}

/// <summary>
/// The council of one — <b>everything this project has ever measured, behind the seam.</b>
/// </summary>
/// <remarks>
/// <b>IT MUST REPRODUCE EVERY RECORDED NUMBER EXACTLY, WHICH IS WHY THE ORDER HERE LOOKS
/// FUSSY.</b> Settle, then sweep, then repair every round, then cover, then repair after a
/// failure: the sweep sits between the settlement and the repair because it always has, and
/// a sweep round that culls before a child is minted is not the same run as one that culls
/// after. <c>DeterminismTests</c> is what says so.
/// </remarks>
public sealed class Alone : ICouncil
{
    private readonly Population _held;

    private IReadOnlySet<Code> _moment = new HashSet<Code>();
    private ImmutableArray<Commitment> _firing;

    private long _firingTicks;
    private long _settling;
    private long _sweeping;
    private long _covering;
    private long _mending;

    /// <param name="held">What the machine holds.</param>
    public Alone(Population held)
    {
        ArgumentNullException.ThrowIfNull(held);

        _held = held;
    }

    /// <inheritdoc/>
    public Spent Spent => new()
    {
        Firing = Milliseconds(_firingTicks),
        Settling = Milliseconds(_settling),
        Sweeping = Milliseconds(_sweeping),
        Covering = Milliseconds(_covering),
        Mending = Milliseconds(_mending),
    };

    /// <summary>
    /// What the last ask's firing has to say, with nothing of the commitments in it.
    /// </summary>
    /// <remarks>
    /// <b>THE HALF OF A VOTE A MACHINE MAY COMPUTE ALONE, AND THE REASON A HOLDER IS A
    /// THIN THING.</b> <c>Machines.Holder</c> answers a vote with this and a settlement
    /// with <see cref="Tell"/>, so every phase of a round lives here and exactly here
    /// whether there is one machine or twenty — which is what stops a fleet growing a
    /// second copy of the learning loop.
    /// </remarks>
    public Testimony Said => _held.Speak(_firing);

    /// <inheritdoc/>
    public ValueTask<Vote> AskAsync(IReadOnlySet<Code> raw, CancellationToken ct = default) =>
        ValueTask.FromResult(Ask(raw));

    /// <summary>
    /// The same ask, and <b>the shape everything here is really in.</b>
    /// </summary>
    /// <param name="raw">What the front end said, before any minted name is folded in.</param>
    /// <remarks>
    /// <b>SYNCHRONOUS BECAUSE IT IS, RATHER THAN WRAPPED BECAUSE THE INTERFACE IS.</b> A
    /// council of one never waits for anything, and a holder answering an ask over a wire
    /// wants the answer on the thread the transport handed it. Writing the task first and
    /// unwrapping it at every call site would put a sync-over-async at each of them; this
    /// way there is exactly one place where the two shapes meet, and it is the line above.
    /// </remarks>
    public Vote Ask(IReadOnlySet<Code> raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var at = Stopwatch.GetTimestamp();

        _moment = _held.Moment(raw);

        // NOTED BEFORE ANYTHING READS IT, so a code is counted live in the very moment
        // genesis may be asked about it. Recording it after covering would let the first
        // moment a code appears be judged against a table that had not seen it.
        _held.Witness(_moment);

        _firing = _held.Firing(_moment);

        var vote = _held.Predict(_firing);

        Mark(ref _firingTicks, at);

        return vote;
    }

    /// <inheritdoc/>
    public ValueTask<Learnt> TellAsync(
        Code? arrived, bool wrong, bool sweeping, CancellationToken ct = default) =>
        ValueTask.FromResult(Tell(arrived, wrong, sweeping));

    /// <summary>
    /// The same round, told what the other holders counted.
    /// </summary>
    /// <param name="arrived">What followed, or nothing where the settlement could not say.</param>
    /// <param name="wrong">Whether the fleet's vote missed.</param>
    /// <param name="sweeping">Whether this is a sweep round.</param>
    /// <param name="heard">
    /// What the OTHER holders counted, or nothing where this machine is alone.
    /// </param>
    /// <remarks>
    /// <b>ONE EXTRA ARGUMENT AND NOT AN EXTRA METHOD BODY, WHICH IS THE WHOLE POINT.</b>
    /// Abstraction is the only operator here whose statistic is the whole population's, so
    /// it is the only thing a holder cannot decide by itself — and it is one parameter deep
    /// rather than a second arrangement of the round. The interface does not carry it
    /// because a council of one has nobody to hear from.
    /// </remarks>
    public Learnt Tell(
        Code? arrived, bool wrong, bool sweeping, Recurrence? heard = null)
    {
        var at = Stopwatch.GetTimestamp();

        _held.Settle(_firing, _moment, arrived);

        at = Mark(ref _settling, at);

        long subsumed = 0;

        // THE SWEEP IS NOT PART OF FAILING, and it sat inside the failure branch for the
        // whole of step one. Once the learner is right most of the time, the chance of a
        // wrong round landing on a sweep round is the miss rate itself -- so subsumption
        // and culling ran a handful of times in thirty thousand rounds and read as
        // mechanisms that bought nothing.
        //
        // AND A ROUND THE WORLD COULD NOT SETTLE STILL SWEEPS, or the calendar becomes
        // conditional on how often the world was quiet -- the same trap by a new door.
        if (sweeping)
        {
            subsumed = _held.Subsume();
            _held.Abstract(heard);
            _held.Cull();

            at = Mark(ref _sweeping, at);
        }

        // NOTHING ELSE IN THE ROUND HAPPENS WHERE THE WORLD COULD NOT SAY, which is the
        // whole content of the third verdict. No genesis, because a surprise needs
        // something to have arrived; no repair, because blame needs a failure. A monotone
        // counter cannot retract a slur.
        if (arrived is not { } outcome)
            return new Learnt { Minted = 0, Repaired = 0, Subsumed = subsumed };

        long repaired = 0;

        // REPAIR NEED NOT WAIT FOR THE VOTE, AND WHETHER IT SHOULD IS AN ARM. An outvoted
        // commitment still accrues its own hits and misses -- and returning early on a
        // right answer meant it could never spend them, so how hard the machine searched
        // was a function of how good its answers already were.
        if (_held.Dials.Repairing == Repairing.EveryRound
            && _held.Mend(_firing, outcome) is not null)
            repaired++;

        at = Mark(ref _mending, at);

        if (!wrong)
            return new Learnt { Minted = 0, Repaired = repaired, Subsumed = subsumed };

        // COVERING RUNS ONLY ON A FAILURE AND IS NOT MOVED WITH REPAIR. Genesis mints per
        // live code, so running it every round walks the whole `code -> outcome` space --
        // which is the refutation that put `Surprising` back, and it would arrive again by
        // this door. AND IT IS GATED AGAIN INSIDE, on whether anything that fired proposed
        // what arrived.
        long minted = _held.Cover(_moment, outcome, _firing);

        at = Mark(ref _covering, at);

        if (_held.Dials.Repairing == Repairing.AfterFailure
            && _held.Mend(_firing, outcome) is not null)
            repaired++;

        Mark(ref _mending, at);

        return new Learnt { Minted = minted, Repaired = repaired, Subsumed = subsumed };
    }

    private static double Milliseconds(long ticks) =>
        ticks * 1000.0 / Stopwatch.Frequency;

    /// <summary>Charges the ticks since a mark to a phase, and returns a fresh mark.</summary>
    /// <param name="phase">The running total to add to.</param>
    /// <param name="since">When the phase started.</param>
    /// <remarks>
    /// <b>ONE CALL A PHASE RATHER THAN A <c>Stopwatch</c> EACH.</b> Five allocations a round
    /// over thirty thousand rounds is a cost the instrument would be adding to the thing it
    /// measures; a timestamp is a single counter read.
    /// </remarks>
    private static long Mark(ref long phase, long since)
    {
        var now = Stopwatch.GetTimestamp();
        phase += now - since;
        return now;
    }
}
