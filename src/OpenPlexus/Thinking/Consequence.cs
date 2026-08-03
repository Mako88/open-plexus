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
    private int _differed;
    private int _apart, _echoed;

    /// <summary>How many pairs of predictions were made and scored.</summary>
    public int Asked
    {
        get { lock (_gate) return _asked; }
    }

    /// <summary>
    /// Steps where the two arms named different codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>REPORTED, AND IT IS NOT A WIRING CHECK IN EITHER DIRECTION.</b> It was
    /// built to be one and measurement said no, twice.
    /// </para>
    /// <para>
    /// <b>A positive count proves nothing</b>, because delivery is concurrent:
    /// two IDENTICAL broadcasts already produce different arrivals. Removing the
    /// action from the prediction entirely leaves this above zero, so the
    /// mutation it was meant to kill SURVIVES.
    /// </para>
    /// <para>
    /// <b>And zero proves nothing either</b>, which is the more surprising half.
    /// On a small graph the top-ranked sensory codes are the same whichever
    /// action is named — measured at knowing 0.900, counterfactual 0.900,
    /// differed 0, with the action correctly wired throughout.
    /// </para>
    /// <para>
    /// <b>The third arm is what fixed it</b> — see <see cref="Echoed"/>. A count
    /// on its own can never say whether a difference is the action or the jitter,
    /// because it has nothing to compare the jitter against.
    /// </para>
    /// </remarks>
    public int Differed
    {
        get { lock (_gate) return _differed; }
    }

    /// <summary>
    /// How far a prediction lands from one made with a DIFFERENT action, in codes.
    /// </summary>
    /// <remarks>
    /// <b>A size and not a flag.</b> Counting steps where the two arms disagreed
    /// at all throws away how much they disagreed, which is the only thing that
    /// can be compared against the jitter.
    /// </remarks>
    public double Apart
    {
        get { lock (_gate) return _asked == 0 ? 0.0 : _apart / (double)_asked; }
    }

    /// <summary>
    /// How far a prediction lands from one made with the SAME action.
    /// <b>The walk's distance from itself, and the mutation killer.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS IS THE ARM THE PROJECT SPENT THREE ATTEMPTS NOT HAVING.</b>
    /// Delivery is concurrent, so two identical broadcasts already land in
    /// different places; that floor is what <see cref="Differed"/> was
    /// unknowingly measuring, which is why a positive count proved nothing and a
    /// zero count proved nothing either.
    /// </para>
    /// <para>
    /// <b>Measure the floor and the question becomes answerable.</b> If naming a
    /// different action moves the prediction further than asking the same
    /// question twice does, the action is in the walk. If the two distances match,
    /// it is not — and removing the action from the broadcast entirely makes them
    /// match by construction, which is exactly the mutation that used to survive.
    /// </para>
    /// </remarks>
    public double Echoed
    {
        get { lock (_gate) return _asked == 0 ? 0.0 : _echoed / (double)_asked; }
    }

    /// <summary>
    /// How much further a different action moves the prediction than asking twice
    /// does. <b>Zero means the action is not in the walk.</b>
    /// </summary>
    public double Moved => Apart - Echoed;

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
    /// <param name="again">
    /// What was named asking the SAME question a second time. <b>The jitter
    /// floor</b> — see <see cref="Echoed"/>.
    /// </param>
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
        IReadOnlyCollection<Code> again,
        IReadOnlyCollection<Code> blind,
        IReadOnlyCollection<Code> actual)
    {
        ArgumentNullException.ThrowIfNull(knowing);
        ArgumentNullException.ThrowIfNull(otherwise);
        ArgumentNullException.ThrowIfNull(again);
        ArgumentNullException.ThrowIfNull(blind);
        ArgumentNullException.ThrowIfNull(actual);

        if (knowing.Count == 0 || otherwise.Count == 0) return;

        var came = actual as HashSet<Code> ?? [.. actual];

        var named = knowing.ToHashSet();
        var apart = !named.SetEquals(otherwise);

        // HOW FAR APART, NOT WHETHER. Both distances are counted the same way, so
        // the comparison between them is of like with like.
        var moved = Away(named, otherwise);
        var jitter = again.Count == 0 ? 0 : Away(named, again);

        lock (_gate)
        {
            _asked++;
            if (apart) _differed++;

            _apart += moved;
            _echoed += jitter;

            _named += knowing.Count;
            _right += knowing.Count(came.Contains);

            _namedElse += otherwise.Count;
            _rightElse += otherwise.Count(came.Contains);

            _blind += blind.Count;
            _rightBlind += blind.Count(came.Contains);
        }
    }

    /// <summary>
    /// How many codes one prediction holds that the other does not, both ways.
    /// </summary>
    /// <remarks>
    /// <b>Symmetric, because neither side is the reference.</b> A prediction that
    /// named three codes the other missed is exactly as far away as one that
    /// missed three the other named.
    /// </remarks>
    private static int Away(HashSet<Code> one, IReadOnlyCollection<Code> other)
    {
        var theirs = other as HashSet<Code> ?? [.. other];

        return one.Count(code => !theirs.Contains(code))
            + theirs.Count(code => !one.Contains(code));
    }
}
