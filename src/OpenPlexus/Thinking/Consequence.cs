using OpenPlexus.Codes;

namespace OpenPlexus.Thinking;

/// <summary>
/// Whether the graph knows how the world answers back to <i>it</i>.
/// </summary>
/// <remarks>
/// <para>
/// <b>FORK 18, ANSWERED BY JOHN 2026-08-02: this is what the project scores.</b>
/// Not survival, which is disqualified — the arm that lives longest is the one
/// that circles and eats nothing, 133.71 mean steps against the chain's 92.85
/// and two fruit against forty. Not passive prediction either, which asks *what
/// comes next* and can be answered well by something that has no idea it is
/// present in the world at all.
/// </para>
/// <para>
/// <b>The question is "what will the world look like if I do X".</b> That is a
/// world model rather than a sequence model, and the difference is that it can
/// be asked a counterfactual.
/// </para>
/// <para>
/// <b>THE CONTROL IS THE SAME PREDICTION WITH A DIFFERENT ACTION IN IT</b>, and
/// it is what makes this measure understanding rather than familiarity. Every
/// mechanism runs identically; the graph, the budget, the walk and the scoring
/// are untouched. Only the action named in the question changes. So:
/// </para>
/// <list type="bullet">
/// <item>If naming the true action predicts better than naming a false one, the
/// graph holds something about <b>its own effect on the world</b>.</item>
/// <item>If the two score the same, it is predicting the next frame regardless
/// of what it does — <b>which is exactly the thing that looks like understanding
/// and is not</b>, and no accuracy number alone would tell them apart.</item>
/// </list>
/// <para>
/// <b>Scored prequentially.</b> Both guesses are taken before the world is
/// stepped and settled before anything is counted.
/// </para>
/// </remarks>
public sealed class Consequence
{
    private readonly Lock _gate = new();

    private int _asked;
    private int _named, _right;
    private int _namedElse, _rightElse;
    private int _blind, _rightBlind;

    /// <summary>How many pairs of predictions were made and scored.</summary>
    public int Asked
    {
        get { lock (_gate) return _asked; }
    }

    /// <summary>Codes named across every true-action prediction.</summary>
    public int Named
    {
        get { lock (_gate) return _named; }
    }

    /// <summary>Of those, how many turned up.</summary>
    public int Right
    {
        get { lock (_gate) return _right; }
    }

    /// <summary>Of the codes named knowing what was about to be done, the share that turned up.</summary>
    public double Knowing
    {
        get { lock (_gate) return _named == 0 ? 0.0 : _right / (double)_named; }
    }

    /// <summary>The same, with a DIFFERENT action named. <b>The control.</b></summary>
    public double Counterfactual
    {
        get { lock (_gate) return _namedElse == 0 ? 0.0 : _rightElse / (double)_namedElse; }
    }

    /// <summary>What naming codes without consulting the graph would have scored.</summary>
    public double Blind
    {
        get { lock (_gate) return _blind == 0 ? 0.0 : _rightBlind / (double)_blind; }
    }

    /// <summary>
    /// <b>THE NUMBER FORK 18 CHOSE.</b> How much better the graph foresees the
    /// world when it knows what it is about to do.
    /// </summary>
    /// <remarks>
    /// <b>Zero means the action is not in the model</b>, however high
    /// <see cref="Knowing"/> is on its own — the graph would be predicting the
    /// next frame and not its own part in producing it.
    /// </remarks>
    public double Gap
    {
        get { lock (_gate) return Knowing - Counterfactual; }
    }

    /// <summary>
    /// Settles one prediction against what the world actually did next.
    /// </summary>
    /// <param name="knowing">What was named with the TRUE action in the question.</param>
    /// <param name="otherwise">What was named with a DIFFERENT action in it.</param>
    /// <param name="blind">The same many codes, drawn without consulting the graph.</param>
    /// <param name="actual">What the next observation turned out to contain.</param>
    /// <remarks>
    /// <b>A step where either prediction named nothing is not counted at all</b>,
    /// rather than counted as a miss. Scoring it would make the gap depend on how
    /// often each arm stayed silent, and silence is a property of the budget
    /// rather than of the model — see fork 24.
    /// </remarks>
    public void Settle(
        IReadOnlyCollection<Code> knowing,
        IReadOnlyCollection<Code> otherwise,
        IReadOnlyCollection<Code> blind,
        IReadOnlyCollection<Code> actual)
    {
        ArgumentNullException.ThrowIfNull(knowing);
        ArgumentNullException.ThrowIfNull(otherwise);
        ArgumentNullException.ThrowIfNull(blind);
        ArgumentNullException.ThrowIfNull(actual);

        if (knowing.Count == 0 || otherwise.Count == 0) return;

        var came = actual as HashSet<Code> ?? [.. actual];

        lock (_gate)
        {
            _asked++;

            _named += knowing.Count;
            _right += knowing.Count(came.Contains);

            _namedElse += otherwise.Count;
            _rightElse += otherwise.Count(came.Contains);

            _blind += blind.Count;
            _rightBlind += blind.Count(came.Contains);
        }
    }
}
