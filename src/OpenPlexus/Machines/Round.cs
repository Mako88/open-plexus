using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>
/// One observation's worth of learning, and the bookkeeping every world wants.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written once because the clone budget refused it twice.</b> The second world
/// arrived with the same predict, score, settle, sweep, cover, repair loop copied
/// into it — and two copies of a learning loop is the one duplication that could
/// silently start learning two different things. `DuplicationTests` caught it on the
/// day the second world was written, which is what that budget is for.
/// </para>
/// <para>
/// <b>A source pushes a moment and the brain answers</b>, and the loop scores what came
/// back. How a reading becomes codes is the world's business and how a commitment learns is
/// the brain's, so nothing here reaches into either.
/// </para>
/// <para>
/// <b>And it holds no commitments</b>, which is what separates a bench from the machine it
/// is watching. Everything below is a count over what <see cref="Brain.ReceiveAsync"/>
/// returned — a run under one arrangement of holders and a run under another are the same
/// numbers taken the same way.
/// </para>
/// </remarks>
public sealed class Round
{
    private readonly Brain _brain;
    private readonly int _sweep;
    private readonly double _target;
    private readonly int _window;
    private readonly long _from;

    private readonly Queue<bool> _trailing;
    private int _standing;

    /// <param name="brain">The one brain, whatever it is made of.</param>
    /// <param name="rounds">How many rounds the run will be, for the closing report.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy <see cref="Reached"/> waits for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Round(Brain brain, long rounds, int sweep, double target, int window)
    {
        ArgumentNullException.ThrowIfNull(brain);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sweep);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(window);

        _brain = brain;
        _sweep = sweep;
        _target = target;
        _window = window;

        // The last tenth is the assessment, and it is a reporting choice rather than
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

    /// <summary>Moments the brain would not take.</summary>
    /// <remarks>
    /// <b>Apart from every other count, because it is not a round.</b> A moment whose stamp
    /// does not advance its source was never asked about, so it has no vote to be silent and
    /// no settlement to abstain on — folding it into either would make a repeated push read
    /// as a gap in what the population knows.
    /// </remarks>
    public long Refused { get; private set; }

    /// <summary>Children minted by repair.</summary>
    public long Repaired { get; private set; }

    /// <summary>Narrower commitments a general one took the place of.</summary>
    /// <remarks>
    /// <b>The mechanism that was written up as never firing</b>, on evidence that could not
    /// say. <c>Judged.Narrowed</c> counts unsound residents a resident SOUND one
    /// covers, which is a fact about what is left rather than about whether subsumption
    /// runs — and reading its nought as the clause being unreachable cost a commit that
    /// had to be corrected. This is the count that answers the question asked, and it is
    /// here because `Subsuming` now has three rules and a null result between them is
    /// unreadable without it.
    /// </remarks>
    public long Subsumed { get; private set; }


    /// <summary>Commitments minted by genesis.</summary>
    /// <remarks>
    /// <b>Reported because a gate</b> that does nothing and a gate that does everything
    /// look alike from the score. The resident count is what survives culling and
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
    /// <b>The plan calls this already instrumented and it was computed and read by
    /// nobody.</b> <i>The margin between first and second is a confidence, free. A
    /// persistently thin margin is the two-conflated-cases signal, already
    /// instrumented.</i> <see cref="Vote.Margin"/> has been calculated every round for
    /// the life of the branch and nothing in the library has ever looked at it — one
    /// assertion in a unit test was its entire readership. That is `Surprise` again, and
    /// `Drives.Improving` before it.
    /// </para>
    /// <para>
    /// <b>Relative rather than absolute</b>, because the absolute one is not comparable
    /// between runs. A weight is an accuracy, and it was once an accuracy raised to a
    /// power — which collapsed every weight toward nought and its margins with them, so two
    /// settings reported different numbers for identical behaviour. The lead as a share of
    /// the winner is in nought to one either way, which is why it survived the dial.
    /// </para>
    /// <para>
    /// <b>And it is the signal for the dial nobody can set.</b> Near one, the winner
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
    /// <b>The brain's clock and not the loop's</b>, which is where the phases actually
    /// are. Firing, settling, sweeping, covering and repairing all happen on whoever
    /// holds the commitments, so a loop timing them from outside would be timing its own
    /// wait — and on a fleet that wait is the network rather than the mechanism.
    /// </remarks>
    public Spent Spent => _brain.Spent;

    /// <summary>Rounds whose settlement could not say what followed.</summary>
    /// <remarks>
    /// <b>DIFFERENT FROM <see cref="Silent"/> at both ends</b>, and the pair is why either
    /// means anything. Silence is the POPULATION having nothing to say about a moment
    /// whose outcome is known; this is the WORLD having nothing to say about a moment the
    /// population may well have answered. One is a gap in what has been learnt and the
    /// other is a gap in the evidence, and a run reporting only their sum could not tell
    /// which it had.
    /// </remarks>
    public long Abstained { get; private set; }

    /// <summary>Pushes one moment at the brain and scores what came back.</summary>
    /// <param name="moment">What a source pushed, and what it says followed.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// <para>
    /// <b>Asynchronous because a gathering arrives when it arrives</b>, and that is the
    /// whole of why the harness moved with it. The vote is a scatter to every holder and a
    /// gather of whatever comes back, so a loop that could not wait could not take one —
    /// and building a second loop that could is the duplication that would let two copies
    /// of this start learning different things. In one process nothing here ever yields;
    /// see <see cref="Alone"/> and <see cref="Bench.Run"/>, which refuses a
    /// substrate that would have made it wait.
    /// </para>
    /// <para>
    /// <b>A null outcome is the third verdict.</b> <c>Commitment.Settle</c> has always
    /// handled <c>Verdict.Abstain</c> correctly — nothing moves, not the counters and not
    /// the table — and <c>Population.Settle</c> has always taken a nullable code. What was
    /// missing was a source able to say <i>nothing followed that I saw</i>.
    /// </para>
    /// <para>
    /// <b>And nothing else in the round happens</b>, which is the whole content of the
    /// verdict. No score, because there is nothing to be right or wrong against. No
    /// genesis, because a surprise needs something to have arrived. No repair, because
    /// blame needs a failure. A monotone counter cannot retract a slur, so a round the
    /// world could not settle must cost a commitment exactly nothing.
    /// </para>
    /// <para>
    /// <b>A refused moment is not a round</b>, and it is counted nowhere here. The brain
    /// takes a moment whose stamp advances its source and declines anything else, so a
    /// repeat costs the loop nothing and moves no counter — see
    /// <see cref="Response.Took"/>. Scoring it would be scoring an answer twice.
    /// </para>
    /// </remarks>
    public async ValueTask StepAsync(Pushed moment, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(moment.Codes);

        var round = Rounds;

        var answer = await _brain
            .ReceiveAsync(moment, sweeping: round % _sweep == _sweep - 1, ct)
            .ConfigureAwait(false);

        if (!answer.Took)
        {
            Refused++;
            return;
        }

        Rounds++;

        Minted += answer.Learnt.Minted;
        Repaired += answer.Learnt.Repaired;
        Subsumed += answer.Learnt.Subsumed;

        var vote = answer.Vote;

        if (moment.Followed is not { } outcome)
        {
            // Nothing is scored, which is the whole content of the third verdict. Not
            // `Silent`, because the population may well have spoken; not `Voted`, because
            // there is nothing for a confidence to be a confidence about.
            Abstained++;
            return;
        }

        if (vote.Expects is not { } said) Silent++;
        else
        {
            var hit = said == outcome;

            // Guarded, because a zero weight is reachable and silent. Every accuracy
            // starts at nought, so the first rounds of any run vote with weights of
            // exactly nought -- and a lead divided by that is a NaN that poisons the mean
            // for the whole run without ever failing anything.
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
    }
}
