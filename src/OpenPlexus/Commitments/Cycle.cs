using System.Diagnostics;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>Where a run's wall clock went, by phase, in milliseconds.</summary>
/// <remarks>
/// <para>
/// <b>NO REPORT HAS EVER CARRIED A COST, AND THIS PROJECT'S OWN TRAP LIST SAYS SO.</b>
/// <i>A cost can be in memory while every instrument watches time</i> — and the truth was
/// worse than that: nothing watched EITHER. A run that was memory-bound and a run that was
/// spending its life in a quadratic sweep report the same everything.
/// </para>
/// <para>
/// <b>SO THE POINT IS WHICH PHASE, NEVER THE TOTAL.</b> A total says a run was slow, which
/// anybody watching already knew. What decides where to spend an optimisation is whether
/// the clock is in matching, in the per-code tally, or in a sweep that is quadratic in the
/// population — and those want three completely different answers.
/// </para>
/// <para>
/// <b>AND IT IS A MEASUREMENT RATHER THAN A CHECK, so nothing may assert on it.</b> A
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
    public required double Covering { get; init; }

    /// <summary>Choosing a condition and minting a child.</summary>
    public required double Mending { get; init; }

    /// <summary>
    /// <b>TWO RUNS THAT DIFFER ONLY IN HOW LONG THEY TOOK ARE THE SAME RUN.</b>
    /// </summary>
    /// <param name="other">The other clock, which is not compared.</param>
    /// <remarks>
    /// <para>
    /// <b>THE RULE ABOVE WAS WRITTEN AND BROKEN IN ONE COMMIT, AND ONLY THE COMPILER WAS
    /// ENFORCING ANYTHING.</b> <i>Nothing may assert on it</i> is three lines up, and
    /// this record went inside <see cref="Machines.Tally"/> — whose generated equality
    /// asserts on every field it has. So the three <i>a fixed seed reproduces a run
    /// exactly</i> tests began comparing a wall clock, and went red on a machine doing
    /// nothing wrong. Fork 12, reopened by the instrument that was supposed to be the
    /// one thing nobody could bar.
    /// </para>
    /// <para>
    /// <b>AND THE HALF THAT WAS NOT RED WAS WORSE.</b> Every <c>Assert.NotEqual</c> over
    /// a <see cref="Machines.Tally"/> passed the moment the clocks differed, which they
    /// always do — so the controls beside those three tests could not fail. A check that
    /// cannot fire reads as a pass, and this project has a line in its trap list about
    /// exactly that.
    /// </para>
    /// <para>
    /// <b>SO IT IS ENFORCED HERE RATHER THAN AT THE THREE CALL SITES.</b> Normalising the
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

/// <summary>
/// One observation's worth of learning, and the bookkeeping every world wants.
/// </summary>
/// <remarks>
/// <para>
/// <b>WRITTEN ONCE BECAUSE THE CLONE BUDGET REFUSED IT TWICE.</b> The second world
/// arrived with the same predict, score, settle, sweep, cover, repair loop copied
/// into it — and two copies of a learning loop is the one duplication that could
/// silently start learning two different things. `DuplicationTests` caught it on the
/// day the second world was written, which is what that budget is for.
/// </para>
/// <para>
/// <b>A WORLD SUPPLIES A MOMENT AND WHAT FOLLOWED IT, AND NOTHING ELSE.</b> How a
/// reading becomes codes is the world's business and how a commitment learns is not,
/// so the seam is exactly one call wide.
/// </para>
/// </remarks>
public sealed class Cycle
{
    private readonly ICouncil _council;
    private readonly int _sweep;
    private readonly double _target;
    private readonly int _window;
    private readonly long _from;

    private readonly Queue<bool> _trailing;
    private int _standing;

    /// <param name="council">Whoever votes, settles and sweeps — one machine or a fleet.</param>
    /// <param name="rounds">How many rounds the run will be, for the closing report.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy <see cref="Reached"/> waits for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Cycle(ICouncil council, long rounds, int sweep, double target, int window)
    {
        ArgumentNullException.ThrowIfNull(council);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sweep);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(window);

        _council = council;
        _sweep = sweep;
        _target = target;
        _window = window;

        // THE LAST TENTH IS THE ASSESSMENT, and it is a reporting choice rather than
        // a dial: a lifetime accuracy over a learning run measures how long the run
        // was at least as much as it measures the mechanism.
        _from = rounds - (rounds / 10);

        _trailing = new Queue<bool>(window);
    }

    /// <summary>Rounds stepped.</summary>
    public long Rounds { get; private set; }

    /// <summary>Predictions that matched what followed.</summary>
    public long Right { get; private set; }

    /// <summary>Predictions that did not.</summary>
    public long Wrong { get; private set; }

    /// <summary>Rounds where nothing fired, so there was no prediction to be wrong.</summary>
    public long Silent { get; private set; }

    /// <summary>Children minted by repair.</summary>
    public long Repaired { get; private set; }

    /// <summary>Narrower commitments a general one took the place of.</summary>
    /// <remarks>
    /// <b>THE MECHANISM THAT WAS WRITTEN UP AS NEVER FIRING ON EVIDENCE THAT COULD NOT
    /// SAY.</b> <c>Judged.Narrowed</c> counts unsound residents a resident SOUND one
    /// covers, which is a fact about what is left rather than about whether subsumption
    /// runs — and reading its nought as the clause being unreachable cost a commit that
    /// had to be corrected. This is the count that answers the question asked, and it is
    /// here because `Subsuming` now has three rules and a null result between them is
    /// unreadable without it.
    /// </remarks>
    public long Subsumed { get; private set; }

    /// <summary>Shorter commitments proposed by generalisation.</summary>
    /// <remarks>
    /// <b>BESIDE <see cref="Repaired"/> BECAUSE THEY ARE THE TWO DIRECTIONS OF THE
    /// LADDER.</b> Repair is the only thing that makes a scope longer and this is the only
    /// thing that makes one shorter, so a population's drift toward one rule per instance
    /// is the difference between them — and until this existed only one of the two numbers
    /// could be read at all.
    /// </remarks>
    public long Widened { get; private set; }


    /// <summary>Commitments minted by genesis.</summary>
    /// <remarks>
    /// <b>REPORTED BECAUSE A GATE THAT DOES NOTHING AND A GATE THAT DOES EVERYTHING
    /// LOOK ALIKE FROM THE SCORE.</b> The resident count is what survives culling and
    /// says nothing about the rate this ran at — and the rate is the whole question,
    /// since minting on every failure enumerates the <c>code → outcome</c> space.
    /// </remarks>
    public long Minted { get; private set; }

    /// <summary>Predictions over the last tenth that were right.</summary>
    private long Settled { get; set; }

    /// <summary>Predictions over the last tenth that were answered at all.</summary>
    private long Answered { get; set; }

    /// <summary>The round a trailing window first held the target, or zero if never.</summary>
    public long Reached { get; private set; }

    /// <summary>The share of answered predictions right over the last tenth.</summary>
    public double Recent => Answered == 0 ? 0.0 : Settled / (double)Answered;

    /// <summary>How much of the winner's weight its lead accounted for, on average.</summary>
    /// <remarks>
    /// <para>
    /// <b>THE PLAN CALLS THIS ALREADY INSTRUMENTED AND IT WAS COMPUTED AND READ BY
    /// NOBODY.</b> <i>The margin between first and second is a confidence, free. A
    /// persistently thin margin is the two-conflated-cases signal, already
    /// instrumented.</i> <see cref="Vote.Margin"/> has been calculated every round for
    /// the life of the branch and nothing in the library has ever looked at it — one
    /// assertion in a unit test was its entire readership. That is `Surprise` again, and
    /// `Drives.Improving` before it.
    /// </para>
    /// <para>
    /// <b>RELATIVE RATHER THAN ABSOLUTE, BECAUSE THE ABSOLUTE ONE IS NOT COMPARABLE
    /// BETWEEN RUNS.</b> A weight is an accuracy, and it was once an accuracy raised to a
    /// power — which collapsed every weight toward nought and its margins with them, so two
    /// settings reported different numbers for identical behaviour. The lead as a share of
    /// the winner is in nought to one either way, which is why it survived the dial.
    /// </para>
    /// <para>
    /// <b>AND IT IS THE SIGNAL FOR THE DIAL NOBODY CAN SET.</b> Near one, the winner
    /// stands alone and the vote is deciding on accuracy. Near nought, the runner-up is
    /// level with it and the answer is being settled by how many advocates each side
    /// happened to have — which is the count deciding, and is exactly what raising the
    /// power is meant to prevent.
    /// </para>
    /// </remarks>
    public double Confidence => Voted == 0 ? 0.0 : _leads / Voted;

    /// <summary>Rounds anything fired on.</summary>
    private long Voted { get; set; }

    private double _leads;

    /// <summary>Where the wall clock went, by phase.</summary>
    /// <remarks>
    /// <b>THE COUNCIL'S CLOCK AND NOT THE LOOP'S, WHICH IS WHERE THE PHASES ACTUALLY
    /// ARE.</b> Firing, settling, sweeping, covering and repairing all happen on whoever
    /// holds the commitments, so a loop timing them from outside would be timing its own
    /// wait — and on a fleet that wait is the network rather than the mechanism.
    /// </remarks>
    public Spent Spent => _council.Spent;

    /// <summary>Rounds whose settlement could not say what followed.</summary>
    /// <remarks>
    /// <b>DIFFERENT FROM <see cref="Silent"/> AT BOTH ENDS, AND THE PAIR IS WHY EITHER
    /// MEANS ANYTHING.</b> Silence is the POPULATION having nothing to say about a moment
    /// whose outcome is known; this is the WORLD having nothing to say about a moment the
    /// population may well have answered. One is a gap in what has been learnt and the
    /// other is a gap in the evidence, and a run reporting only their sum could not tell
    /// which it had.
    /// </remarks>
    public long Abstained { get; private set; }

    /// <summary>Predicts, scores, settles, sweeps, and repairs what was wrong.</summary>
    /// <param name="moment">
    /// What is live, <b>raw</b> — a minted name is folded in by whoever holds it.
    /// </param>
    /// <param name="arrived">
    /// What followed it, or nothing where the settlement could not say.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <para>
    /// <b>ASYNCHRONOUS BECAUSE A GATHERING ARRIVES WHEN IT ARRIVES, AND THAT IS THE WHOLE
    /// OF WHY THE HARNESS MOVED WITH IT.</b> The vote is a scatter to every holder and a
    /// gather of whatever comes back, so a loop that could not wait could not take one —
    /// and building a second loop that could is the duplication that would let two copies
    /// of this start learning different things. In one process nothing here ever yields;
    /// see <see cref="Alone"/> and <c>Machines.Trial.Run</c>, which refuses a council that
    /// would have made it wait.
    /// </para>
    /// <para>
    /// <b>A NULL OUTCOME IS THE THIRD VERDICT AND IT COULD NOT BE EXPRESSED HERE UNTIL
    /// NOW.</b> <c>Commitment.Settle</c> has always handled <c>Verdict.Abstain</c>
    /// correctly — nothing moves, not the counters and not the table — and
    /// <c>Population.Settle</c> has always taken a nullable code. This signature was the
    /// wall: no caller could pass one, so the plan's <i>`Abstain` is unarmed in any run</i>
    /// was true for a reason that had nothing to do with there being one process.
    /// </para>
    /// <para>
    /// <b>AND NOTHING ELSE IN THE ROUND HAPPENS, WHICH IS THE WHOLE CONTENT OF THE
    /// VERDICT.</b> No score, because there is nothing to be right or wrong against. No
    /// genesis, because a surprise needs something to have arrived. No repair, because
    /// blame needs a failure. A monotone counter cannot retract a slur, so a round the
    /// world could not settle must cost a commitment exactly nothing.
    /// </para>
    /// <para>
    /// <b>THE SWEEP STILL RUNS, AND THAT IS DELIBERATE.</b> Subsumption, abstraction and
    /// culling are on the calendar rather than on the outcome — this repo's trap list has a
    /// line about a periodic sweep inside a conditional running at that condition's rate,
    /// and skipping them here would reintroduce it keyed on how often the world was quiet.
    /// </para>
    /// </remarks>
    public async ValueTask StepAsync(
        IReadOnlySet<Code> moment, Code? arrived, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(moment);

        var round = Rounds++;

        // THE VOTE IS TAKEN BEFORE THE SETTLEMENT IS KNOWN, AND THAT IS THE DEPLOYMENT
        // ARRIVING RATHER THAN A REARRANGEMENT. A machine does not learn that the world is
        // about to go quiet and then decline to predict; the observation arrives, it says
        // what it expects, and the settlement either comes or does not. Reading it is
        // free -- `Predict` writes nothing -- so a round the world cannot settle costs a
        // vote nobody scores, which is what it costs in the world too.
        var vote = await _council.AskAsync(moment, ct).ConfigureAwait(false);

        if (arrived is not { } outcome)
        {
            Abstained++;

            // NOTHING IS SCORED, WHICH IS THE WHOLE CONTENT OF THE THIRD VERDICT. Not
            // `Silent`, because the population may well have spoken; not `Voted`, because
            // there is nothing for a confidence to be a confidence about.
            await Told(arrived: null, wrong: false, round, ct).ConfigureAwait(false);
            return;
        }

        if (vote.Expects is not { } said) Silent++;
        else
        {
            var hit = said == outcome;

            // GUARDED, BECAUSE A ZERO WEIGHT IS REACHABLE AND SILENT. Every accuracy
            // starts at nought, so the first rounds of any run vote with weights of
            // exactly nought -- and a lead
            // divided by that is a NaN that poisons the mean for the whole run without
            // ever failing anything.
            Voted++;
            if (vote.Weight > 0) _leads += vote.Margin / vote.Weight;

            if (hit) Right++; else Wrong++;

            if (round >= _from)
            {
                Answered++;
                if (hit) Settled++;
            }

            _trailing.Enqueue(hit);
            if (hit) _standing++;
            if (_trailing.Count > _window && _trailing.Dequeue()) _standing--;

            if (Reached == 0 && _trailing.Count >= _window
                && _standing / (double)_trailing.Count >= _target)
                Reached = round;
        }

        // WRONG IS THE FLEET'S VERDICT AND NEVER A HOLDER'S, which is why it is told
        // rather than worked out where genesis runs. A shard that had nothing to say about
        // a moment the population as a whole answered correctly has not witnessed a
        // failure, and covering there would mint on every machine that happened to be
        // quiet -- the ungated genesis refutation arriving through the distribution.
        await Told(outcome, wrong: vote.Expects != outcome, round, ct).ConfigureAwait(false);
    }

    /// <summary>Tells the council what the settlement said, and counts what it did.</summary>
    /// <param name="arrived">What followed, or nothing where the settlement could not say.</param>
    /// <param name="wrong">Whether the vote missed.</param>
    /// <param name="round">Which round it is, for the sweep's calendar.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <b>THE SWEEP'S CALENDAR IS THE LOOP'S AND THE SWEEP IS THE COUNCIL'S.</b> Which
    /// round it is cannot be a fact each holder works out for itself — a fleet whose
    /// members swept on their own count would sweep at different moments, and rung five's
    /// evidence is the whole population.
    /// </remarks>
    private async ValueTask Told(Code? arrived, bool wrong, long round, CancellationToken ct)
    {
        var learnt = await _council
            .TellAsync(arrived, wrong, sweeping: round % _sweep == _sweep - 1, ct)
            .ConfigureAwait(false);

        Minted += learnt.Minted;
        Repaired += learnt.Repaired;
        Subsumed += learnt.Subsumed;
        Widened += learnt.Widened;
    }
}
