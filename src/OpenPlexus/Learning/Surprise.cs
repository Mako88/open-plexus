using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// What was expected, and therefore what is worth propagating — <b>step 2.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>RAO &amp; BALLARD, AND FRISTON: ONLY THE ERROR TRAVELS.</b> A prediction
/// that came true carries no information, so broadcasting it is paying full price
/// for a message whose content is "as expected". Predictive coding inverts the
/// default: what goes up the hierarchy is the residual, and a perfectly predicted
/// input is silent.
/// </para>
/// <para>
/// <b>THREE THINGS FALL OUT OF ONE CHANGE, WHICH IS WHY THIS IS THE BIGGEST
/// SINGLE GAP.</b> Traffic collapses, because a stationary world stops
/// broadcasting. The system gets an INTERNAL error signal, which it has never had
/// — every error in this project is computed by the harness from outside, and no
/// dial can be driven by a number the machine cannot see. And it unblocks the
/// drives of step 4, which need uncertainty <i>felt</i> rather than scored.
/// </para>
/// <para>
/// <b>IT SUPPRESSES THINKING AND NEVER LEARNING.</b> An expected onset still
/// joins its occasion and still moves the counts — the graph must keep getting
/// better at predicting the thing it already predicts, or the expectation decays
/// and the silence is a lie. What is skipped is the broadcast, which is the
/// expensive half.
/// </para>
/// <para>
/// <b>The expectation comes from outside this class.</b> A prediction is a walk,
/// and walking is the machine's job; this only holds what was predicted, says
/// what was not, and counts how often it was right. That keeps it honest about
/// the one thing that could go wrong — a predictor that says everything is
/// expected would silence the system completely, and <see cref="Foreseen"/> is
/// what makes that visible instead of looking like a quiet world.
/// </para>
/// </remarks>
public sealed class Surprise
{
    private readonly HashSet<Code> _expected = [];
    private readonly Lock _gate = new();

    private int _onsets, _foreseen, _moments, _silent;

    /// <summary>Onsets seen since the beginning.</summary>
    public int Onsets
    {
        get { lock (_gate) return _onsets; }
    }

    /// <summary>Of those, how many had been predicted.</summary>
    public int Foreseen
    {
        get { lock (_gate) return _foreseen; }
    }

    /// <summary>Moments where every onset was expected, so nothing was broadcast.</summary>
    /// <remarks>
    /// <b>THE TRAFFIC COLLAPSE, COUNTED.</b> This is the share of the world the
    /// system stops thinking about because it already knew.
    /// </remarks>
    public int Silent
    {
        get { lock (_gate) return _silent; }
    }

    /// <summary>Moments that produced any onset at all.</summary>
    public int Moments
    {
        get { lock (_gate) return _moments; }
    }

    /// <summary>
    /// The share of onsets that were predicted. <b>The internal error signal, and
    /// the first quantity in this project the machine can read about itself.</b>
    /// </summary>
    /// <remarks>
    /// <b>One means nothing is ever surprising</b>, which is either a solved world
    /// or a broken predictor, and the two are told apart by whether the score
    /// holds up. <b>Zero means the expectation is never right</b>, and then this
    /// mechanism only costs.
    /// </remarks>
    public double Rate
    {
        get { lock (_gate) return _onsets == 0 ? 0.0 : _foreseen / (double)_onsets; }
    }

    /// <summary>
    /// Records what the system expects to see next.
    /// </summary>
    /// <remarks>
    /// <b>Replaces rather than accumulates.</b> An expectation is about the next
    /// moment; carrying old ones forward would silence onsets that were predicted
    /// at some point in the past, which is not the same claim at all.
    /// </remarks>
    public void Expect(IEnumerable<Code> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        lock (_gate)
        {
            _expected.Clear();
            foreach (var code in codes) _expected.Add(code);
        }
    }

    /// <summary>
    /// Which of these onsets were not expected — <b>the residual, and the only
    /// thing worth broadcasting.</b>
    /// </summary>
    /// <returns>
    /// The unexpected onsets. <b>Empty is a real answer</b>: the moment was
    /// entirely predicted and the system has nothing to think about.
    /// </returns>
    public IReadOnlyList<Code> Residual(IReadOnlyCollection<Code> onsets)
    {
        ArgumentNullException.ThrowIfNull(onsets);

        lock (_gate)
        {
            var surprising = onsets.Where(code => !_expected.Contains(code)).ToList();

            _moments++;
            _onsets += onsets.Count;
            _foreseen += onsets.Count - surprising.Count;
            if (surprising.Count == 0) _silent++;

            return surprising;
        }
    }

    public override string ToString() =>
        $"onsets={Onsets} foreseen={Foreseen} rate={Rate:F4} " +
        $"silent={Silent}/{Moments}";
}
