using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

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
    private readonly Population _held;
    private readonly int _sweep;
    private readonly double _target;
    private readonly int _window;
    private readonly long _from;

    private readonly Queue<bool> _trailing;
    private int _standing;

    /// <param name="held">What the machine holds.</param>
    /// <param name="rounds">How many rounds the run will be, for the closing report.</param>
    /// <param name="sweep">How often to subsume, abstract and cull.</param>
    /// <param name="target">The trailing accuracy <see cref="Reached"/> waits for.</param>
    /// <param name="window">How many answered predictions that accuracy is over.</param>
    public Cycle(Population held, long rounds, int sweep, double target, int window)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sweep);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(window);

        _held = held;
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
    /// BETWEEN RUNS.</b> Weights are accuracies raised to <c>Sharpness</c>, so a sharper
    /// vote collapses every weight toward nought and its margins with them — two
    /// settings would report different numbers for identical behaviour. The lead as a
    /// share of the winner is in nought to one whatever the power is.
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

    /// <summary>Predicts, scores, settles, sweeps, and repairs what was wrong.</summary>
    /// <param name="moment">What is live, already folded through any minted names.</param>
    /// <param name="arrived">What followed it.</param>
    public void Step(IReadOnlySet<Code> moment, Code arrived)
    {
        ArgumentNullException.ThrowIfNull(moment);

        var round = Rounds++;

        var firing = _held.Firing(moment);
        var vote = _held.Predict(firing);

        if (vote.Expects is not { } said) Silent++;
        else
        {
            var hit = said == arrived;

            // GUARDED, BECAUSE A ZERO WEIGHT IS REACHABLE AND SILENT. Every accuracy
            // starts at nought and `Sharpness` raises it to a power, so the first
            // rounds of any run vote with weights of exactly nought -- and a lead
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

        _held.Settle(firing, moment, arrived);

        // THE SWEEP IS NOT PART OF FAILING, and it sat inside the failure branch for
        // the whole of step one. Once the learner is right most of the time, the
        // chance of a wrong round landing on a sweep round is the miss rate itself --
        // so subsumption and culling ran a handful of times in thirty thousand rounds
        // and read as mechanisms that did nothing.
        if (round % _sweep == _sweep - 1)
        {
            _held.Subsume();
            _held.Abstract();
            _held.Cull();
        }

        // REPAIR NEED NOT WAIT FOR THE VOTE, AND WHETHER IT SHOULD IS AN ARM. The plan
        // says an outvoted commitment still accrues its own hits and misses -- and then
        // this early return meant it could never spend them, so how hard the machine
        // searched was a function of how good its answers already were. `Mend` refuses
        // anything short of the floor, over budget, or without a condition past the
        // separation bar, so its own gates are not being loosened; only this one is.
        // THE TWO THAT DO NOT WAIT FOR THE VOTE. `Neglected` does wait, and its extra
        // condition lives inside `Mend` -- so the two halves of the conjunction are
        // applied where each is cheapest to ask, rather than both in one place.
        if (_held.Dials.Mending is Mending.Uncovered or Mending.Improving
            && _held.Mend(firing, arrived) is not null)
            Repaired++;

        if (vote.Expects == arrived) return;

        // COVERING RUNS ONLY ON A FAILURE AND IS NOT MOVED WITH REPAIR. Genesis mints
        // per live code, so running it every round walks the whole `code -> outcome`
        // space -- which is the refutation that put `Surprising` back, and it would
        // arrive again by this door.
        //
        // AND COVERING IS GATED AGAIN INSIDE, on whether anything that fired proposed
        // what arrived -- a failure the population already had an account of is
        // repair's business and not genesis's. `Surprising` is the dial.
        Minted += _held.Cover(moment, arrived, firing);

        if (_held.Dials.Mending is Mending.Outvoted or Mending.Neglected
            && _held.Mend(firing, arrived) is not null)
            Repaired++;
    }
}
