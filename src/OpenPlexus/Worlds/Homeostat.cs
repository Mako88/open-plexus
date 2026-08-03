using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How many internal variables, how fast they fall, and how far they may.</summary>
public sealed record HomeostatSettings
{
    /// <summary>How many variables must be kept in bounds at once.</summary>
    public int Needs { get; init; } = 4;

    /// <summary>
    /// The slowest variable's fall per step. <b>Need <c>i</c> falls at
    /// <c>Drain × (i+1)</c></b>.
    /// </summary>
    /// <remarks>
    /// <b>UNEVEN ON PURPOSE, AND IT IS WHAT MAKES THE WORLD DISCRIMINATE.</b> If
    /// every variable fell at the same rate, attending to whichever is lowest and
    /// attending at random would differ only in variance, and a policy that did
    /// not look at its own state would score nearly as well as one that did. With
    /// uneven rates, spreading attention evenly is systematically wrong on the
    /// fast ones — so the world can only be held by noticing which variable is
    /// actually in trouble.
    /// </remarks>
    public double Drain { get; init; } = 0.01;

    /// <summary>How much one act of attention restores.</summary>
    /// <remarks>
    /// <b>BOUNDED ON BOTH SIDES BY THE WORLD'S OWN ARITHMETIC.</b> Above the sum
    /// of every drain, or nothing can hold the system and the ceiling is not
    /// reachable; below <c>Needs × the fastest drain</c>, or attending at random
    /// suffices and the world measures nothing.
    /// </remarks>
    public double Restore { get; init; } = 0.13;

    /// <summary>How low a variable may fall before it is out of bounds.</summary>
    public double Floor { get; init; } = 0.25;

    /// <summary>How many bands a variable's value is quantised into.</summary>
    public int Bands { get; init; } = 5;
}

/// <summary>
/// Ashby's homeostat: internal variables that must be kept in bounds, and no
/// reward for keeping them there.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE WORLD FOR STEP 4, AND IT EXISTS BECAUSE SURVIVAL WAS GAMEABLE.</b>
/// Snake scored by staying alive, and circling wins that: it lives longest and
/// eats least, which is the refuted row. Keeping variables in bounds cannot be
/// gamed the same way, because <b>standing still stops paying</b> — every
/// variable falls whether or not anything is done, so doing nothing is not a
/// conservative strategy here, it is the fastest way to fail.
/// </para>
/// <para>
/// <b>There is no reward function and there is no goal state.</b> Behaviour is
/// goal-directed only in the sense that some behaviour keeps the system inside
/// its bounds and the rest does not. That is the whole of Ashby's claim, and it
/// is the reason this is worth building rather than a scoring rule.
/// </para>
/// <para>
/// <b>Homeostasis has no episode boundary, which is what fits C4.</b> Nothing
/// resets, nothing is retried, and a run does not end when the system fails — it
/// carries on out of bounds and can come back, so the score is time spent viable
/// rather than time until death.
/// </para>
/// </remarks>
public sealed class Homeostat
{
    /// <summary>The first variable's modality; need <c>i</c> is <c>Need + i</c>.</summary>
    public const byte Need = 80;

    /// <summary>What the body can do about it.</summary>
    public const byte Act = 79;

    private readonly HomeostatSettings _settings;
    private readonly double[] _at;

    /// <param name="settings">The shape of the body.</param>
    public Homeostat(HomeostatSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Needs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Bands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Drain);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Restore);

        _settings = settings;
        _at = [.. Enumerable.Repeat(1.0, settings.Needs)];
    }

    /// <summary>Where every variable stands right now.</summary>
    public IReadOnlyList<double> At => _at;

    /// <summary>How fast need <paramref name="which"/> falls.</summary>
    public double Falls(int which)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(which);
        return _settings.Drain * (which + 1);
    }

    /// <summary>Everything that is falling, added up.</summary>
    public double Falling =>
        Enumerable.Range(0, _settings.Needs).Sum(Falls);

    /// <summary>Whether every variable is inside its bounds.</summary>
    public bool Viable => _at.All(value => value >= _settings.Floor);

    /// <summary>Which variable is furthest from where it should be.</summary>
    /// <remarks>
    /// <b>The oracle's answer, and it is the ceiling rather than a policy.</b>
    /// Nothing in the graph is told this; it exists so a run can be compared
    /// against the best a body could do with the same actions.
    /// </remarks>
    public int Lowest
    {
        get
        {
            var lowest = 0;
            for (var which = 1; which < _at.Length; which++)
                if (_at[which] < _at[lowest]) lowest = which;

            return lowest;
        }
    }

    /// <summary>
    /// One step: everything falls, and one variable is attended to.
    /// </summary>
    /// <remarks>
    /// <b>The fall happens whatever is chosen</b>, including when nothing is —
    /// which is what makes standing still cost rather than save.
    /// </remarks>
    /// <param name="attend">Which variable to restore, or null to do nothing.</param>
    /// <returns>Whether the body is viable after the step.</returns>
    public bool Step(int? attend)
    {
        for (var which = 0; which < _at.Length; which++)
            _at[which] = Math.Max(0.0, _at[which] - Falls(which));

        if (attend is { } which2 && which2 >= 0 && which2 < _at.Length)
            _at[which2] = Math.Min(1.0, _at[which2] + _settings.Restore);

        return Viable;
    }

    /// <summary>What the body can feel about itself, as codes.</summary>
    /// <remarks>
    /// <b>A band and not a number.</b> The graph holds codes, so an internal
    /// variable has to be quantised exactly as an external sense is — and that is
    /// the point of step 4 rather than a compromise: a drive is felt as a state,
    /// not read as a float.
    /// </remarks>
    public ImmutableArray<Code> Feels()
    {
        var felt = new Code[_at.Length];

        for (var which = 0; which < _at.Length; which++)
        {
            var band = (int)(_at[which] * _settings.Bands);
            felt[which] = new Code(
                (byte)(Need + which), (ulong)Math.Clamp(band, 0, _settings.Bands - 1));
        }

        return [.. felt];
    }

    /// <summary>The code for attending to one variable.</summary>
    public static Code Attending(int which)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(which);
        return new Code(Act, (ulong)which);
    }

    /// <summary>Which variable an action code is about.</summary>
    public static int Attended(Code code) => (int)code.Value;

    /// <inheritdoc cref="HomeostatSettings.Needs"/>
    public int Needs => _settings.Needs;

    /// <inheritdoc cref="HomeostatSettings.Restore"/>
    public double Restore => _settings.Restore;

    /// <summary>
    /// How long doing nothing lasts, in steps.
    /// </summary>
    /// <remarks>
    /// <b>Computed, and it is the number that makes idling a failure rather than
    /// a strategy.</b> The fastest-falling variable reaches the floor first.
    /// </remarks>
    public int Idling => (int)Math.Ceiling((1.0 - _settings.Floor) / Falls(_settings.Needs - 1));
}
