using OpenPlexus.Codes;

namespace OpenPlexus.Thinking;

/// <summary>
/// How well the graph foresees what it is about to see.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE GOAL IS UNDERSTANDING, NOT PREDICTION — John, 2026-08-02.</b> This is
/// not "what is the most likely next thing", which is a sequence model and
/// cannot be asked a counterfactual. It is <i>what would the world look like if
/// I did X</i>, which is a world model: the question is put with a candidate
/// action in it, so a different action asks a different question.
/// </para>
/// <para>
/// <b>Prediction is not a new mechanism.</b> The flood already works out what is
/// associated with a set of codes. Broadcast the current view together with a
/// candidate action and narrow the arrivals to <i>sensory</i> codes rather than
/// action codes, and that is a prediction — the same
/// <see cref="Thought.BestAmong"/> that already does output selection, pointed
/// at a different set.
/// </para>
/// <para>
/// <b>Scored prequentially.</b> The guess is taken before the world is seen and
/// the score is settled before anything is counted, so the graph never sees the
/// answer before being asked. A model scored any other way is being marked on
/// data it has already learned.
/// </para>
/// </remarks>
public sealed class Foresight
{
    private readonly Lock _gate = new();

    private int _asked;
    private int _hit;
    private int _guessed;
    private int _right;
    private int _chance;

    /// <summary>How many predictions were made and scored.</summary>
    public int Asked
    {
        get { lock (_gate) return _asked; }
    }

    /// <summary>
    /// Of those, how many named at least one code that actually turned up.
    /// </summary>
    public int Hit
    {
        get { lock (_gate) return _hit; }
    }

    /// <summary>Every code named across every prediction.</summary>
    public int Guessed
    {
        get { lock (_gate) return _guessed; }
    }

    /// <summary>Of those, how many turned up.</summary>
    public int Right
    {
        get { lock (_gate) return _right; }
    }

    /// <summary>
    /// What the same number of guesses drawn at random would have got right.
    /// </summary>
    /// <remarks>
    /// <b>A control tests the DATA, not the code.</b> Without it, a hit rate is
    /// a number with nothing to be better than — and on a small code alphabet
    /// guessing blind scores well above zero.
    /// </remarks>
    public int Chance
    {
        get { lock (_gate) return _chance; }
    }

    /// <summary>Of the predictions made, the share that named something real.</summary>
    public double Foresaw
    {
        get { lock (_gate) return _asked == 0 ? 0.0 : _hit / (double)_asked; }
    }

    /// <summary>Of the codes named, the share that turned up.</summary>
    public double Precision
    {
        get { lock (_gate) return _guessed == 0 ? 0.0 : _right / (double)_guessed; }
    }

    /// <summary>Of the codes a blind guesser would have named, the share that turned up.</summary>
    public double Blind
    {
        get { lock (_gate) return _guessed == 0 ? 0.0 : _chance / (double)_guessed; }
    }

    /// <summary>
    /// Settles one prediction against what the world actually did.
    /// </summary>
    /// <param name="predicted">What the graph named, in rank order.</param>
    /// <param name="actual">What the next observation turned out to contain.</param>
    /// <param name="control">
    /// The same number of codes drawn without looking at the graph. **Scored
    /// alongside**, so every hit rate here is reported against what guessing
    /// would have managed on the same moment.
    /// </param>
    public void Settle(
        IReadOnlyCollection<Code> predicted,
        IReadOnlyCollection<Code> actual,
        IReadOnlyCollection<Code> control)
    {
        ArgumentNullException.ThrowIfNull(predicted);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(control);

        if (predicted.Count == 0) return;

        var came = actual as HashSet<Code> ?? [.. actual];
        var right = predicted.Count(came.Contains);

        lock (_gate)
        {
            _asked++;
            if (right > 0) _hit++;
            _guessed += predicted.Count;
            _right += right;
            _chance += control.Count(came.Contains);
        }
    }
}
