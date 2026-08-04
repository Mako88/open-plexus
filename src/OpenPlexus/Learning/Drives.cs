namespace OpenPlexus.Learning;

/// <summary>
/// The third factor — <b>step 4, and what says an action was FOR something.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE GRAPH RECORDS WHAT CO-OCCURRED AND NOTHING ELSE, WHICH IS WHY
/// ASSOCIATION PICKS WORSE THAN RANDOM.</b> A state and the act taken in it join
/// one occasion, so walking from that state later reaches whatever was done there
/// last time — helpful or ruinous, the count is the same. Measured on
/// <c>Homeostat</c>: choosing by association scores BELOW drawing at random, which
/// is not a tuning failure but the absence of any signal saying which of two
/// equally-frequent partners was worth having.
/// </para>
/// <para>
/// <b>ASHBY, AND THERE IS NO REWARD FUNCTION HERE.</b> Nothing scores an action.
/// What exists is a body whose variables fall on their own and are bad when out of
/// bounds, so <i>out of bounds is bad and returning is good</i> is not a rule
/// somebody chose — it is what having a body means. That is the whole of why this
/// is not a reward in disguise: it is computed from the system's own state and
/// nobody hands it in from outside.
/// </para>
/// <para>
/// <b>IT MODULATES HOW MUCH IS ADDED AND NEVER SUBTRACTS, which is what keeps the
/// CRDT property.</b> Counts only increment — that is the G-Counter property the
/// whole coordination-free design rests on — so a factor that could go to zero or
/// negative would break convergence. This scales the weight of an occasion within
/// a positive band instead: a bad transition is written FAINTLY, not undone. See
/// <see cref="Occasion.Weight"/>, which fork 21 already uses for a discount of the
/// same shape.
/// </para>
/// <para>
/// <b>The trace is <see cref="Window"/>'s job and not this one.</b> Three-factor
/// Hebbian learning (Izhikevich 2007, the distal reward problem) wants a fading
/// record of what recently fired and a third signal that consolidates whatever is
/// still in it. This is only the third signal.
/// </para>
/// </remarks>
public sealed class Drives
{
    private readonly Lock _gate = new();

    /// <summary>How far from its worst a transition has to move to earn full weight.</summary>
    private readonly double _reach;

    private double? _worst;
    private double _credit = 1.0;
    private int _better, _worse, _same;

    /// <param name="reach">
    /// The change in the most-at-risk variable that earns the full band.
    /// <b>Scaled to the world's own arithmetic rather than set as a constant</b> —
    /// a drain per step is the natural unit, because a transition that recovers
    /// one step's worth of fall is exactly one that broke even.
    /// </param>
    public Drives(double reach)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reach);
        _reach = reach;
    }

    /// <summary>
    /// What the last transition earns, as a multiplier on an occasion's weight.
    /// </summary>
    /// <remarks>
    /// <b>One until something has been felt twice</b>, so the first occasion of a
    /// run is written exactly as it would have been without any of this.
    /// </remarks>
    public double Credit
    {
        get { lock (_gate) return _credit; }
    }

    /// <summary>Transitions that improved the most-at-risk variable.</summary>
    public int Better
    {
        get { lock (_gate) return _better; }
    }

    /// <summary>Transitions that worsened it.</summary>
    public int Worse
    {
        get { lock (_gate) return _worse; }
    }

    /// <summary>Transitions that moved it not at all.</summary>
    public int Same
    {
        get { lock (_gate) return _same; }
    }

    /// <summary>
    /// The share of transitions that improved things. <b>A third internal signal,
    /// beside <see cref="Surprise.Rate"/> and <see cref="Surprise.Overreach"/>.</b>
    /// </summary>
    public double Improving
    {
        get
        {
            lock (_gate)
            {
                var seen = _better + _worse + _same;
                return seen == 0 ? 0.0 : _better / (double)seen;
            }
        }
    }

    /// <summary>
    /// Feel the body, and price the transition since the last time it was felt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE MOST-AT-RISK VARIABLE IS THE ONE THAT MATTERS, not the mean.</b> A
    /// body is out of bounds when ANY variable is, so an average over all of them
    /// can rise while the one about to fail keeps falling — which is the same
    /// shape as reading a mean fan-out over rows that grew without bound.
    /// </para>
    /// <para>
    /// <b>Called once per step, before the occasion for that step is written.</b>
    /// The credit it leaves behind belongs to the transition that just happened,
    /// so what it weights is the PREVIOUS occasion — an act is priced by what
    /// followed it, which is the one-step version of the trace.
    /// </para>
    /// </remarks>
    /// <param name="at">Every internal variable's current value.</param>
    public void Feel(IReadOnlyList<double> at)
    {
        ArgumentNullException.ThrowIfNull(at);
        ArgumentOutOfRangeException.ThrowIfZero(at.Count);

        var worst = at[0];
        for (var i = 1; i < at.Count; i++) worst = Math.Min(worst, at[i]);

        lock (_gate)
        {
            if (_worst is not { } before)
            {
                _worst = worst;
                _credit = 1.0;
                return;
            }

            var moved = worst - before;

            if (moved > 0.0) _better++;
            else if (moved < 0.0) _worse++;
            else _same++;

            // A BAND AROUND ONE, AND BOTH ENDS ARE POSITIVE. Full recovery of a
            // step's fall doubles what the occasion is worth; a full step's
            // further fall writes it at a fifth. Neither end can reach zero,
            // because an occasion worth nothing is not an occasion and a count
            // that could stop increasing is not a G-Counter.
            var scaled = Math.Clamp(moved / _reach, -1.0, 1.0);

            _credit = scaled >= 0.0
                ? 1.0 + scaled
                : 1.0 / (1.0 + (4.0 * -scaled));

            _worst = worst;
        }
    }

    public override string ToString() =>
        $"credit={Credit:F4} improving={Improving:F4} " +
        $"better={Better} worse={Worse} same={Same}";
}
