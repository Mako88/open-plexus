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
    /// <b>THIS IS THE ONLY THING IN ANY WORLD HERE THAT A PREDICTION CAN BE
    /// WRONG ABOUT FOR A GOOD REASON.</b> A perfect model of this stream scores
    /// exactly one minus this and cannot do better, which is what makes the
    /// ceiling a number rather than an aspiration.
    /// </remarks>
    public double Violations { get; init; } = 0.1;
}

/// <summary>
/// A long, stationary, regular stream that occasionally breaks its own rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S SPECIFICATION, AND IT EXISTS BECAUSE SNAKE DIES AT SEVENTY-SEVEN
/// STEPS.</b> Snake is the only predictive world here, and a run that short gives
/// the prediction-budget hunt of fork 24 almost no samples to hunt with — the
/// controller was measured never moving, and nobody could tell whether that was
/// the controller or the sample size. This stream never ends and never changes
/// its statistics, so a dial can be swept against it at two run lengths, which
/// the traps section asks for and no world here could previously offer.
/// </para>
/// <para>
/// <b>It is built BEFORE the mechanism it is for, deliberately.</b> Step 2 is
/// predictive coding, and the type <c>Surprise</c> does not exist yet; this world
/// is the baseline that mechanism will have to beat, measured first so that the
/// comparison cannot be assembled after the fact.
/// </para>
/// <para>
/// <b>ONE SYMBOL PER MOMENT, WHICH MAKES THE TEMPORAL EDGE THE WHOLE TASK.</b>
/// Nothing is ever simultaneous with anything, so an occasion's onsets pair with
/// an empty live set and the graph learns precisely nothing unless
/// <see cref="Learning.Window"/> carries the previous symbol forward. That makes
/// the window's span not an arm here but the mechanism under test: at zero this
/// world has no edges at all, which is the sharpest control in the project.
/// </para>
/// </remarks>
public sealed class Rhythm
{
    /// <summary>The one modality this world emits.</summary>
    public const byte Beat = 60;

    private readonly RhythmSettings _settings;
    private readonly int[] _cycle;
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

        // THE CYCLE IS DRAWN ONCE AND NEVER REDRAWN. It is the stationary part:
        // the statistics of this stream must not move, or a dial swept over a long
        // run would be measuring the world changing under it.
        _cycle = [.. Enumerable.Range(0, settings.Symbols)
            .OrderBy(_ => _rng.Next())
            .Take(settings.Period)];
    }

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
