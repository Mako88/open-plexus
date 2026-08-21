using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>The shape of the stream: how long the cycle, and how often it lies.</summary>
public sealed record RhythmSettings
{
    /// <summary>How many distinct symbols exist.</summary>
    public int Symbols { get; init; } = 12;

    /// <summary>
    /// How many moments the repeating cycle lasts.
    /// </summary>
    /// <remarks>
    /// <b>Shorter than the alphabet, always.</b> A cycle as long as the alphabet
    /// makes every symbol equally frequent and the marginal baseline collapses
    /// onto the blind one, which removes the control that matters.
    /// </remarks>
    public int Period { get; init; } = 5;

    /// <summary>
    /// How often a moment shows something the cycle did not call for, in 0..1.
    /// </summary>
    /// <remarks>
    /// <b>This is the only thing</b> in any world here that a prediction can be
    /// wrong about for a good reason. A perfect model of this stream scores
    /// exactly one minus this and cannot do better, which is what makes the
    /// ceiling a number rather than an aspiration.
    /// </remarks>
    public double Violations { get; init; } = 0.1;

    /// <summary>
    /// After how many moments the cycle is REDRAWN. <b>Null never turns, which is
    /// every measurement taken before this existed.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The only non-stationary thing in any world here</b>, and supersession has
    /// nothing to say without it. Nothing in this design decays, so a count that
    /// stopped rising still stands at whatever it reached — which is only a defect
    /// where the answer CHANGES. Every world here keeps its rules for the whole run,
    /// so the machinery for preferring what is still true had no way to be right or
    /// wrong.
    /// </para>
    /// <para>
    /// <b>The statistics do not move and that is the point.</b> A new cycle is drawn
    /// the same way, over the same alphabet, at the same period and the same
    /// violation rate — so the ceiling, the chance line and the marginal baseline
    /// are all exactly where they were. <b>What changes is the answer and not the
    /// difficulty</b>, which is what keeps this from being a world that drifts under
    /// a dial being swept over it.
    /// </para>
    /// </remarks>
    public int? Turns { get; init; }
}

/// <summary>
/// A long, stationary, regular stream that occasionally breaks its own rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's specification, and it exists because Snake dies at seventy-seven
/// steps.</b> Snake was the only predictive world here, and a run that short gave
/// the walk's prediction-budget controller almost no samples to hunt with — it was
/// measured never moving, and nobody could tell whether that was the controller or
/// the sample size. This stream never ends and never changes its statistics, so a
/// dial can be swept against it at two run lengths, which the traps section asks
/// for and no world here could previously offer.
/// </para>
/// <para>
/// <b>It is built BEFORE the mechanism it is for, deliberately.</b> The baseline
/// is measured first so that the comparison cannot be assembled after the fact —
/// and the mechanism it was built for lost, which is the version of this that
/// costs something to have written down.
/// </para>
/// <para>
/// <b>One symbol per moment, which makes carrying the past the whole task.</b>
/// Nothing is ever simultaneous with anything, so a moment holds one code and
/// nothing co-fires with it — a learner reading moments alone has no scope longer
/// than one code to reach for. What carries the previous symbol forward is
/// therefore not an arm here but the mechanism under test: with nothing carried,
/// this world states nothing at all, which is the sharpest control in the project.
/// </para>
/// </remarks>
public sealed class Rhythm : IWorld<Coded>
{
    /// <summary>The one modality this world emits.</summary>
    public const byte Beat = 60;

    private readonly RhythmSettings _settings;
    private int[] _cycle;
    private readonly Random _rng;
    private int _at;

    /// <summary>The symbol the stream sounded last, or nothing before it has sounded.</summary>
    private int? _last;

    /// <param name="settings">The shape of the stream.</param>
    /// <param name="seed">The world's own generator.</param>
    public Rhythm(RhythmSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Symbols);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Period);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Period, settings.Symbols);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Violations);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Violations, 1.0);

        _settings = settings;
        _rng = new Random(seed);

        // The cycle is drawn once and redrawn only if the world is asked to turn.
        // It is the stationary part: the statistics of this stream must not move,
        // or a dial swept over a long run would be measuring the world changing
        // under it. `Turns` moves the ANSWER and leaves the statistics alone.
        _cycle = Drawn();
    }

    /// <summary>A fresh cycle, drawn exactly as the first one was.</summary>
    private int[] Drawn() =>
        [.. Enumerable.Range(0, _settings.Symbols)
            .OrderBy(_ => _rng.Next())
            .Take(_settings.Period)];

    /// <summary>
    /// How many times the cycle has been redrawn. <b>Zero on a world that never
    /// turns</b>, which is what makes an arm that meant to turn and did not visible
    /// rather than silent.
    /// </summary>
    public int Turned { get; private set; }

    /// <summary>
    /// The best any model could do: everything but the violations.
    /// </summary>
    public double Ceiling => 1.0 - _settings.Violations;

    /// <summary>A blind draw over the alphabet.</summary>
    public double Chance => 1.0 / _settings.Symbols;

    /// <summary>
    /// Always naming the commonest symbol, which is any one of the cycle's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The baseline a model with no sense of WHEN scores, and the one that
    /// matters — a system that has learnt only which symbols are frequent, and
    /// nothing about order, lands here.
    /// </para>
    /// <para>
    /// <b>Exact rather than <c>Ceiling / Period</c></b>, which was the same number rounded
    /// down. A cycle symbol also arrives as somebody else's violation, so its share is
    /// the times the cycle called for it plus the times a violation happened to draw it —
    /// and a control the reading rests on may not be approximate in the direction that
    /// flatters the learner.
    /// </para>
    /// </remarks>
    public double Marginal =>
        (Ceiling / _settings.Period)
        + ((_settings.Period - 1) * Strayed / _settings.Period);

    /// <summary>
    /// The best any model reading only the LAST symbol could score — <b>the bar the
    /// learner is actually run against, computed with no learning.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Ceiling"/> is unreachable through this world's own moment</b>, and a
    /// bar the learner cannot reach reads as a failure. A perfect model of the stream knows
    /// WHERE IN THE CYCLE it is; a moment here carries one symbol and nothing else, so the
    /// phase has to be inferred from that symbol — and a violation puts a cycle symbol
    /// somewhere the cycle never called for it.
    /// </para>
    /// <para>
    /// A symbol the cycle does call for names its own position on nearly every showing, so
    /// the best rule for it is the successor of that position. A symbol the cycle never
    /// calls for arrives only as a violation, so it says nothing about where the stream is
    /// and every successor is equally good.
    /// </para>
    /// <para>
    /// <b>So the distance between this and <see cref="Ceiling"/></b> is what one symbol of
    /// memory costs, which is a fact about the observation and not about the learner.
    /// A world whose cycle repeated a symbol would put a second gap under this one, and
    /// that gap is what rung three would have to earn.
    /// </para>
    /// </remarks>
    public double Carried
    {
        get
        {
            var kept = Ceiling;
            var stray = Strayed;
            var others = _settings.Period - 1;

            // Shown where the cycle called for it, and the successor arrived as called.
            // Plus the same symbol arriving at one of the other positions as a violation,
            // where the successor this rule names arrives as a violation too.
            var named = (kept * kept) + (others * stray * stray);

            // And the symbols the cycle never calls for, which reach the moment only by
            // violation. Their best rule is any one successor, right at that successor's
            // own rate.
            var strangers = _settings.Symbols - _settings.Period;

            return named
                + (strangers / (double)_settings.Period * stray * (kept + (others * stray)));
        }
    }

    /// <summary>How often a violation draws one PARTICULAR other symbol.</summary>
    private double Strayed =>
        _settings.Symbols == 1 ? 0.0 : _settings.Violations / (_settings.Symbols - 1);

    /// <summary>What the cycle calls for at a given moment.</summary>
    public int Wanted(long moment) => _cycle[(int)(moment % _cycle.Length)];

    /// <inheritdoc/>
    public int Outcomes => _settings.Symbols;

    /// <summary>
    /// The symbol before this one, and the symbol that followed it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One code in the moment</b>, so repair, rung three and rung five are all inert
    /// here — which is what this world is worth to the rest of the suite. A scope
    /// cannot be made longer where there is nothing to add, so what a run of this measures
    /// is genesis, the vote and the local decaying estimate with every other mechanism held
    /// still. Nothing else here can hold them still.
    /// </para>
    /// <para>
    /// <b>And it is the only world that can change its answer</b>, which is where
    /// <i>malleability is the record</i> is either true or not. Under
    /// <see cref="RhythmSettings.Turns"/> the successor of every symbol is redrawn while the
    /// statistics stay exactly where they were, so a population that tracks and a population
    /// that keeps what it first learnt are separated by the score and by nothing else.
    /// </para>
    /// <para>
    /// <b>The stream is primed here rather than in the constructor</b>, so a caller reading
    /// <see cref="Strike"/> alone gets the sequence it always got. A world that had sounded
    /// once before anybody asked would shift <see cref="Wanted"/> under every test written
    /// against the raw stream.
    /// </para>
    /// </remarks>
    public Turn<Coded> Next()
    {
        _last ??= Struck().Symbol;

        var before = _last.Value;
        var struck = Struck().Symbol;

        _last = struck;

        return new Turn<Coded> { Seen = Coded.Of(Of(before)), Outcome = struck };
    }

    /// <summary>
    /// The next moment: usually what the cycle calls for, occasionally not.
    /// </summary>
    /// <returns>The symbol shown, and whether it broke the rule.</returns>
    public (Code Shown, bool Violated) Strike()
    {
        var (symbol, violated) = Struck();

        return (Of(symbol), violated);
    }

    /// <summary>The same draw, as the number the world holds it as.</summary>
    private (int Symbol, bool Violated) Struck()
    {
        // THE WORLD CHANGES ITS MIND, and it does so BEFORE the moment is drawn so
        // that the turn is complete rather than half-applied. `Wanted` is what any
        // observer would have to predict, and after this line it means the new
        // cycle for everybody -- including whatever is scoring the prediction.
        if (_settings.Turns is { } every && _at > 0 && _at % every == 0)
        {
            _cycle = Drawn();
            Turned++;
        }

        var wanted = Wanted(_at++);

        if (_rng.NextDouble() >= _settings.Violations) return (wanted, false);

        // A DIFFERENT SYMBOL, never the one the cycle called for -- a "violation"
        // that happened to draw the expected symbol is not a violation, and
        // counting it as one would put unpredictable-by-construction moments into
        // the predictable column.
        var instead = _rng.Next(_settings.Symbols - 1);
        if (instead >= wanted) instead++;

        return (instead, true);
    }

    /// <summary>The code for one symbol.</summary>
    public static Code Of(int symbol)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(symbol);
        return new Code(Beat, (ulong)symbol);
    }

    /// <inheritdoc cref="RhythmSettings.Period"/>
    public int Period => _settings.Period;

    /// <inheritdoc cref="RhythmSettings.Violations"/>
    public double Violations => _settings.Violations;
}
