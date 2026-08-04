using OpenPlexus.Codes;

namespace OpenPlexus.Learning;

/// <summary>
/// One moment's prediction error, <b>signed</b> — what happened unexpectedly, and
/// what was expected and did not happen.
/// </summary>
/// <remarks>
/// <para>
/// <b>BOTH HALVES COME OUT OF ONE CALL BECAUSE THE MIRROR WAS BEING DISCARDED.</b>
/// <c>onsets \ expected</c> was computed and returned; <c>expected \ onsets</c>
/// was computable from the same two sets, cost nothing, and was thrown away — and
/// it is the whole of what this design has to say about ABSENCE.
/// </para>
/// <para>
/// <b>ABSENCE IS A SIGNAL AND NOT A NODE.</b> Minting <c>not-X</c> for every code
/// that failed to arrive would double the alphabet to represent unboundedly many
/// things that are not there, and each minted code would be a row entry earning
/// its keep on nothing. What a co-occurrence count structurally cannot record, a
/// prediction error can still notice.
/// </para>
/// </remarks>
public readonly record struct Prediction
{
    /// <summary>
    /// The onsets nobody expected. <b>The positive half, and the only thing worth
    /// broadcasting.</b>
    /// </summary>
    public required IReadOnlyList<Code> Surprising { get; init; }

    /// <summary>
    /// What was expected and did not arrive. <b>The negative half.</b>
    /// </summary>
    /// <remarks>
    /// <b>Nothing broadcasts this</b>, because there is no code for the thing that
    /// did not happen. It is read, counted, and available to anything that wants a
    /// second opinion about how well the machine is modelling its world.
    /// </remarks>
    public required IReadOnlyList<Code> Absent { get; init; }
}

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
/// what was not, and counts how often it was right.
/// </para>
/// <para>
/// <b>AND <see cref="Rate"/> ALONE CANNOT TELL A SOLVED WORLD FROM A PREDICTOR
/// THAT NAMES EVERYTHING</b>, which is the one failure this mechanism can cause
/// rather than measure: expect every code and every onset is foreseen, the rate
/// reads one, and the machine falls permanently silent having learnt nothing.
/// <b><see cref="Overreach"/> is what separates them</b> — a predictor naming
/// everything is wrong about nearly all of it, and being wrong that way leaves no
/// trace in the positive half at all.
/// </para>
/// </remarks>
public sealed class Surprise
{
    private readonly HashSet<Code> _expected = [];
    private readonly Lock _gate = new();

    private int _onsets, _foreseen, _moments, _silent, _predicted, _absent;

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

    /// <summary>Codes expected across every moment, whether they arrived or not.</summary>
    public int Predicted
    {
        get { lock (_gate) return _predicted; }
    }

    /// <summary>
    /// Of those, how many did not arrive. <b>ABSENCE, COUNTED — the negative half
    /// of the signed prediction error.</b>
    /// </summary>
    /// <remarks>
    /// <b>A co-occurrence count records what happened together and has no way to
    /// say that something did not happen.</b> This is the cheapest thing that can:
    /// it needs no new code, no node and no row entry, because an expectation the
    /// world failed to meet is already the difference between two sets the machine
    /// is holding anyway.
    /// </remarks>
    public int Absent
    {
        get { lock (_gate) return _absent; }
    }

    /// <summary>
    /// The share of what was expected that did not happen. <b>The second internal
    /// signal, and the one that catches a predictor naming everything.</b>
    /// </summary>
    /// <remarks>
    /// <b>Zero means everything expected arrived</b>, which at a healthy
    /// <see cref="Rate"/> is a modelled world and at a rate near zero is a
    /// predictor so timid it barely speaks. <b>One means nothing expected ever
    /// arrives</b>, and then the expectation is noise and the silence it buys is
    /// bought on a lie. <b>The two rates move independently</b>, which is the
    /// point of having both: naming every code drives <see cref="Rate"/> to one
    /// and this to nearly one at the same time.
    /// </remarks>
    public double Overreach
    {
        get { lock (_gate) return _predicted == 0 ? 0.0 : _absent / (double)_predicted; }
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
    /// Settles one moment against what was expected of it — <b>the signed error,
    /// both halves.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE HALVES ARE COMPUTED TOGETHER BECAUSE THEY ARE THE SAME SUBTRACTION
    /// RUN BOTH WAYS</b>, and returning only one of them is how absence stayed
    /// missing for as long as it did. Nothing costs more than it did before: the
    /// expected set is already held, the onsets are already in hand, and neither
    /// direction needs anything the other did not.
    /// </para>
    /// <para>
    /// <b>ABSENCE IS MEASURED AGAINST ONSETS, WHICH IS THE SAME CHANNEL THE
    /// EXPECTATION IS SCORED ON.</b> A code that was already live and stayed live
    /// produces no onset and therefore reads as absent — correct where an
    /// expectation is about what STARTS next, which is what
    /// <see cref="Expect"/> replaces rather than accumulates for, and a real
    /// overcount for any front end that comes to expect continuation.
    /// </para>
    /// </remarks>
    /// <returns>
    /// What was unexpected and what was expected and missing. <b>Empty
    /// <see cref="Prediction.Surprising"/> is a real answer</b>: the moment was
    /// entirely predicted and the system has nothing to think about.
    /// </returns>
    public Prediction Residual(IReadOnlyCollection<Code> onsets)
    {
        ArgumentNullException.ThrowIfNull(onsets);

        lock (_gate)
        {
            var surprising = onsets.Where(code => !_expected.Contains(code)).ToList();

            // THE MIRROR, AND IT IS THE CHEAPER OF THE TWO: the arriving onsets
            // are a set membership test away, and what is left standing in
            // `_expected` is exactly what the world was asked for and did not
            // give.
            var absent = _expected.Where(code => !onsets.Contains(code)).ToList();

            _moments++;
            _onsets += onsets.Count;
            _foreseen += onsets.Count - surprising.Count;
            _predicted += _expected.Count;
            _absent += absent.Count;
            if (surprising.Count == 0) _silent++;

            // SETTLING SPENDS THE EXPECTATION, which is what <see cref="Expect"/>
            // already claims and did not enforce. Every wired path predicts once
            // per observation, so nothing measured moves -- but a front end that
            // observed twice between predictions would have had its second frame
            // silenced by an expectation belonging to the first, and would have
            // counted one prediction as unmet once per moment until it was
            // replaced, which is the wrong denominator for a rate about
            // predictions.
            _expected.Clear();

            return new Prediction { Surprising = surprising, Absent = absent };
        }
    }

    public override string ToString() =>
        $"onsets={Onsets} foreseen={Foreseen} rate={Rate:F4} " +
        $"silent={Silent}/{Moments} " +
        $"predicted={Predicted} absent={Absent} overreach={Overreach:F4}";
}
