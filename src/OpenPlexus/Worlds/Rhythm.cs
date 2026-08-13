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
    /// <b>This is the only thing in any world here that a prediction can be
    /// wrong about for a good reason.</b> A perfect model of this stream scores
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
    /// <b>The only non-stationary thing in any world here, and supersession has
    /// nothing to say without it.</b> Nothing in this design decays, so a count that
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
public sealed class Rhythm
{
    /// <summary>The one modality this world emits.</summary>
    public const byte Beat = 60;

    private readonly RhythmSettings _settings;
    private int[] _cycle;
    private readonly Random _rng;
    private int _at;

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
    /// <b>The baseline a model with no sense of WHEN scores</b>, and the one that
    /// matters — a system that has learnt only which symbols are frequent, and
    /// nothing about order, lands here.
    /// </remarks>
    public double Marginal => Ceiling / _settings.Period;

    /// <summary>What the cycle calls for at a given moment.</summary>
    public int Wanted(long moment) => _cycle[(int)(moment % _cycle.Length)];

    /// <summary>
    /// The next moment: usually what the cycle calls for, occasionally not.
    /// </summary>
    /// <returns>The symbol shown, and whether it broke the rule.</returns>
    public (Code Shown, bool Violated) Next()
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

        if (_rng.NextDouble() >= _settings.Violations) return (Of(wanted), false);

        // A DIFFERENT SYMBOL, never the one the cycle called for -- a "violation"
        // that happened to draw the expected symbol is not a violation, and
        // counting it as one would put unpredictable-by-construction moments into
        // the predictable column.
        var instead = _rng.Next(_settings.Symbols - 1);
        if (instead >= wanted) instead++;

        return (Of(instead), true);
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
