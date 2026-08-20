using System.Collections.Immutable;
using System.Diagnostics;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>Where a run's wall clock went, by phase, in milliseconds.</summary>
/// <remarks>
/// <para>
/// <b>No report has ever carried a cost</b>, and this project's own trap list says so.
/// <i>A cost can be in memory while every instrument watches time</i> — and the truth was
/// worse than that: nothing watched EITHER. A run that was memory-bound and a run that was
/// spending its life in a quadratic sweep report the same everything.
/// </para>
/// <para>
/// <b>So the point is which phase, never the total.</b> A total says a run was slow, which
/// anybody watching already knew. What decides where to spend an optimisation is whether
/// the clock is in matching, in the per-code tally, or in a sweep that is quadratic in the
/// population — and those want three completely different answers.
/// </para>
/// <para>
/// <b>And it is a measurement rather than a check</b>, so nothing may assert on it. A
/// duration is not reproducible under a fixed seed, and a threshold on one would fail the
/// build on a busy machine — which is how a timing number becomes a thing that must not
/// change. <c>Tally.Separations</c> beside it IS reproducible, and is the one to bar.
/// </para>
/// </remarks>
public sealed record Spent
{
    /// <summary>Gathering what fired and taking the vote.</summary>
    public required double Firing { get; init; }

    /// <summary>Telling everything that fired what the settlement said, and tallying it.</summary>
    public required double Settling { get; init; }

    /// <summary>Subsuming, abstracting and culling on the sweep.</summary>
    public required double Sweeping { get; init; }

    /// <summary>Genesis.</summary>
    public required double Genesis { get; init; }

    /// <summary>Choosing a condition and minting a child.</summary>
    public required double Repair { get; init; }

    /// <summary>
    /// <b>Two runs that differ only in how long they took</b> are the same run.
    /// </summary>
    /// <param name="other">The other clock, which is not compared.</param>
    /// <remarks>
    /// <para>
    /// <b>The rule above was written and broken in one commit</b>, and only the compiler was
    /// enforcing anything. <i>Nothing may assert on it</i> is three lines up, and
    /// this record went inside <see cref="Machines.Tally"/> — whose generated equality
    /// asserts on every field it has. So the three <i>a fixed seed reproduces a run
    /// exactly</i> tests began comparing a wall clock, and went red on a machine doing
    /// nothing wrong. Fork 12, reopened by the instrument that was supposed to be the
    /// one thing nobody could bar.
    /// </para>
    /// <para>
    /// <b>And the half that was not red was worse.</b> Every <c>Assert.NotEqual</c> over
    /// a <see cref="Machines.Tally"/> passed the moment the clocks differed, which they
    /// always do — so the controls beside those three tests could not fail. A check that
    /// cannot fire reads as a pass, and this project has a line in its trap list about
    /// exactly that.
    /// </para>
    /// <para>
    /// <b>So it is enforced here rather than at the three call sites.</b> Normalising the
    /// clock away in each test would be a guard mounted on one caller, and the fourth
    /// determinism test — written later, by somebody who never read this — would
    /// reintroduce it. Excluding it by hand from <see cref="Machines.Tally"/>'s equality
    /// would be worse still: that list would then have to be edited every time the report
    /// grows a field, and the field that got forgotten would be silently uncompared.
    /// Here, both records may grow freely and the clock never counts.
    /// </para>
    /// </remarks>
    /// <returns><see langword="true"/> for any other clock at all.</returns>
    public bool Equals(Spent? other) => other is not null;

    /// <inheritdoc/>
    public override int GetHashCode() => 0;
}

/// <summary>What one round of learning added, from whoever did it.</summary>
/// <remarks>
/// <b>Three counts rather than three return values</b>, because on a fleet they come back
/// together. A holder settles, sweeps, covers and repairs on one telling — that is the
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
/// <b>The seam is two calls wide</b>, and which two is the finding rather than a choice.
/// Every operator on <see cref="Population"/> decides local except two: the vote reads what
/// everything that fired advocates, and <see cref="Population.Abstract"/> reads how often
/// scopes recur across the whole population. Settling, covering and repairing are each a
/// commitment's own business, so they need no seam at all — they need only to happen on the
/// machine that holds the commitment.
/// </para>
/// <para>
/// <b>So asking is a scatter-gather and telling is a push.</b> A vote has to come back
/// before the round can be scored; a settlement has to reach every holder and has no answer
/// worth waiting for beyond what it did. That asymmetry is why this is two methods and not
/// one round-shaped call.
/// </para>
/// <para>
/// <b>And it is asynchronous on purpose, including where it never yields.</b>
/// <see cref="Alone"/> completes every task synchronously and allocates nothing to do it,
/// so a one-process run pays for this seam in exactly nothing — see
/// <c>Machines.Bench.Run</c>, which refuses a council that would have made it wait. What the
/// shape buys is that a gathering arriving when it arrives needs no second learning loop,
/// and two copies of this loop is the one duplication that could silently start learning two
/// different things.
/// </para>
/// </remarks>
public interface ICouncil
{
    /// <summary>Where the wall clock went, by phase.</summary>
    /// <remarks>
    /// <b>And no dials beside it, which is a deliberate absence.</b> <c>Round</c> read
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
    /// <b>Raw, because a name is folded by whoever holds it.</b> A minted code is reached by
    /// inference from the codes it stands for, so a moment folded by the loop would carry one
    /// machine's vocabulary onto machines that may not share it — and whether they share it
    /// is exactly what the counts merge exists to establish.
    /// </remarks>
    /// <param name="fleeting">
    /// Which of those codes the source says will not come back, or nothing where it did not
    /// say. <b>It reaches the table and nothing else</b>, so a holder that ignored it would
    /// hold a bigger table and take every identical decision.
    /// </param>
    ValueTask<Vote> AskAsync(
        IReadOnlySet<Code> raw,
        IReadOnlySet<Code>? fleeting = null,
        CancellationToken ct = default);

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
/// <b>It must reproduce every recorded number exactly</b>, which is why the order here looks
/// fussy. Settle, then sweep, then repair every round, then cover, then repair after a
/// failure: the sweep sits between the settlement and the repair because it always has, and
/// a sweep round that culls before a child is minted is not the same run as one that culls
/// after. <c>DeterminismTests</c> is what says so.
/// </remarks>
public sealed class Alone : ICouncil
{
    private readonly Population _held;

    private IReadOnlySet<Code> _moment = new HashSet<Code>();
    private IReadOnlySet<Code>? _fleeting;
    private ImmutableArray<Commitment> _firing;

    private long _firingTicks;
    private long _settling;
    private long _sweeping;
    private long _genesis;
    private long _repair;

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
        Genesis = Milliseconds(_genesis),
        Repair = Milliseconds(_repair),
    };

    /// <summary>
    /// What the last ask's firing has to say, with nothing of the commitments in it.
    /// </summary>
    /// <remarks>
    /// <b>The half of a vote a machine may compute alone.</b> And the reason a holder is a
    /// thin thing. <c>Machines.Holder</c> answers a vote with this and a settlement
    /// with <see cref="Tell"/>, so every phase of a round lives here and exactly here
    /// whether there is one machine or twenty — which is what stops a fleet growing a
    /// second copy of the learning loop.
    /// </remarks>
    public Weights Weighed => _held.Weigh(_firing);

    /// <inheritdoc/>
    public ValueTask<Vote> AskAsync(
        IReadOnlySet<Code> raw,
        IReadOnlySet<Code>? fleeting = null,
        CancellationToken ct = default) =>
        ValueTask.FromResult(Ask(raw, fleeting));

    /// <summary>
    /// The same ask, and <b>the shape everything here is really in.</b>
    /// </summary>
    /// <param name="raw">What the front end said, before any minted name is folded in.</param>
    /// <remarks>
    /// <b>Synchronous because it is, rather than wrapped because the interface is.</b> A
    /// council of one never waits for anything, and a holder answering an ask over a wire
    /// wants the answer on the thread the transport handed it. Writing the task first and
    /// unwrapping it at every call site would put a sync-over-async at each of them; this
    /// way there is exactly one place where the two shapes meet, and it is the line above.
    /// </remarks>
    /// <param name="fleeting">Which of the moment's codes the source says will not come back.</param>
    public Vote Ask(IReadOnlySet<Code> raw, IReadOnlySet<Code>? fleeting = null)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var at = Stopwatch.GetTimestamp();

        // Kept for the settlement, because the mark arrives with the ask and is spent by the
        // telling. A holder is told a moment once and settles it once, so carrying it here
        // is the same slot the firing is already kept in.
        _fleeting = fleeting;

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
    /// <b>One extra argument and not an extra method body</b>, which is the whole point.
    /// Abstraction is the only operator here whose statistic is the whole population's, so
    /// it is the only thing a holder cannot decide by itself — and it is one parameter deep
    /// rather than a second arrangement of the round. The interface does not carry it
    /// because a council of one has nobody to hear from.
    /// </remarks>
    public Learnt Tell(
        Code? arrived, bool wrong, bool sweeping, Recurrence? heard = null)
    {
        var at = Stopwatch.GetTimestamp();

        _held.Settle(_firing, _moment, arrived, _fleeting);

        at = Mark(ref _settling, at);

        long subsumed = 0;

        // The sweep is not part of failing, and it sat inside the failure branch for the
        // whole of step one. Once the learner is right most of the time, the chance of a
        // wrong round landing on a sweep round is the miss rate itself -- so subsumption
        // and culling ran a handful of times in thirty thousand rounds and read as
        // mechanisms that bought nothing.
        //
        // And a round the world could not settle still sweeps, or the calendar becomes
        // conditional on how often the world was quiet -- the same trap by a new door.
        if (sweeping)
        {
            subsumed = _held.Subsume();
            _held.Abstract(heard);
            _held.Cull();

            at = Mark(ref _sweeping, at);
        }

        // Nothing else in the round happens where the world could not say, which is the
        // whole content of the third verdict. No genesis, because a surprise needs
        // something to have arrived; no repair, because blame needs a failure. A monotone
        // counter cannot retract a slur.
        if (arrived is not { } outcome)
            return new Learnt { Minted = 0, Repaired = 0, Subsumed = subsumed };

        long repaired = 0;

        // Repair need not wait for the vote, and whether it should is an arm. An outvoted
        // commitment still accrues its own hits and misses -- and returning early on a
        // right answer meant it could never spend them, so how hard the machine searched
        // was a function of how good its answers already were.
        if (_held.Dials.Repairing == Repairing.EveryRound
            && _held.Repair(_firing, outcome) is not null)
            repaired++;

        at = Mark(ref _repair, at);

        if (!wrong)
            return new Learnt { Minted = 0, Repaired = repaired, Subsumed = subsumed };

        // Genesis runs only on a failure and is not moved with repair. It mints per
        // live code, so running it every round walks the whole `code -> outcome` space --
        // which is the refutation that put `Surprising` back, and it would arrive again by
        // this door. And it is gated again inside, on whether anything that fired proposed
        // what arrived.
        long minted = _held.Genesis(_moment, outcome, _firing);

        at = Mark(ref _genesis, at);

        if (_held.Dials.Repairing == Repairing.AfterFailure
            && _held.Repair(_firing, outcome) is not null)
            repaired++;

        Mark(ref _repair, at);

        return new Learnt { Minted = minted, Repaired = repaired, Subsumed = subsumed };
    }

    private static double Milliseconds(long ticks) =>
        ticks * 1000.0 / Stopwatch.Frequency;

    /// <summary>Charges the ticks since a mark to a phase, and returns a fresh mark.</summary>
    /// <param name="phase">The running total to add to.</param>
    /// <param name="since">When the phase started.</param>
    /// <remarks>
    /// <b>One call a phase rather than a <c>Stopwatch</c> EACH.</b> Five allocations a round
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
