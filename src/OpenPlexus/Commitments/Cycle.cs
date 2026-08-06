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

    /// <summary>Predictions over the last tenth that were right.</summary>
    private long Settled { get; set; }

    /// <summary>Predictions over the last tenth that were answered at all.</summary>
    private long Answered { get; set; }

    /// <summary>The round a trailing window first held the target, or zero if never.</summary>
    public long Reached { get; private set; }

    /// <summary>The share of answered predictions right over the last tenth.</summary>
    public double Recent => Answered == 0 ? 0.0 : Settled / (double)Answered;

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

        if (vote.Expects == arrived) return;

        // COVERING AND REPAIR BOTH RUN ONLY ON A FAILURE. Minting every round would
        // fill the population with restatements of moments already predicted, and
        // repairing every round would spend the budget on commitments that work.
        _held.Cover(moment, arrived);

        if (_held.Mend(firing, arrived) is not null) Repaired++;
    }
}
