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

    /// <summary>
    /// Whether the front end also says <b>where each variable stands relative to
    /// the others</b> — step 4's blocker, and OFF is every measurement taken
    /// before it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TWO MEASUREMENTS SAID THE FRONT END WAS THE PROBLEM, FROM OPPOSITE
    /// DIRECTIONS.</b> A band is an ABSOLUTE fact about one variable, and
    /// <i>attend to whichever is lowest</i> is a RELATIONAL fact about all of
    /// them — the same limit as <c>A is north of B</c>. And the ceiling policy
    /// holds the body so steady that every banded code sits still: it visits
    /// exactly ONE state, so the correct policy is, to the graph, a constant with
    /// no state variation for a state-conditional association to attach to.
    /// </para>
    /// <para>
    /// <b>A RANK FIXES BOTH AT ONCE, AND THE SECOND IS THE SURPRISING HALF.</b>
    /// Drains are uneven, so attending to the lowest makes which-variable-is-worst
    /// ROTATE while the values themselves barely move. The ordering varies exactly
    /// where the magnitudes do not, which is what gives a well-behaved run
    /// something to be conditional on.
    /// </para>
    /// <para>
    /// <b>IT IS THE <see cref="Codes.IQuantizer{TObservation}.Bind"/> SPLIT AGAIN,
    /// AND THE SAME CAVEAT APPLIES WORD FOR WORD.</b> Comparison is pre-attentive
    /// here exactly as segmentation is there — Ashby's units deviate against each
    /// other by their physics, not by deliberation. So the front end supplies the
    /// ORDERING and the learner must still work out what an ordering MEANS: nothing
    /// says rank nought is the urgent end rather than the safe one, and nothing
    /// connects <c>Act:2</c> to <c>Need+2</c>. <b>This tests whether a learner can
    /// USE an ordinal, not whether it can DISCOVER ordering</b>, and it must not
    /// be written up as if it were the whole problem.
    /// </para>
    /// <para>
    /// <b>ADDITIVE: the bands are untouched and the rank codes are extra</b>, so
    /// off reproduces every earlier number exactly and on leaves the graph holding
    /// both facts. Which of the two matters is the graph's job to find, which is
    /// the right split and the reason the rank does not simply replace the band.
    /// </para>
    /// </remarks>
    public bool Ranked { get; init; }
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
    private const byte Need = 80;

    /// <summary>What the body can do about it.</summary>
    public const byte Act = 79;

    /// <summary>
    /// Where variable <c>i</c> stands against the others; its modality is
    /// <c>Rank + i</c> and its value is the position, nought being lowest.
    /// </summary>
    /// <remarks>
    /// <b>Far enough from <see cref="Need"/> that the two blocks cannot meet</b>,
    /// and the constructor refuses a body with more variables than the gap holds
    /// rather than letting a rank quietly land on a need's modality — which would
    /// be one code meaning two things, this design's recurring fault at its most
    /// literal.
    /// </remarks>
    public const byte Rank = 120;

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

        // ONE MODALITY BLOCK MUST NOT RUN INTO THE NEXT. See Rank.
        if (Need + settings.Needs > Rank || Rank + settings.Needs > byte.MaxValue + 1)
            throw new ArgumentOutOfRangeException(nameof(settings),
                $"{settings.Needs} variables do not fit between the need block at "
                + $"{Need} and the rank block at {Rank}; a rank would land on a "
                + "need's modality and one code would mean two things");

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
    /// <para>
    /// <b>A band and not a number.</b> The graph holds codes, so an internal
    /// variable has to be quantised exactly as an external sense is — and that is
    /// the point of step 4 rather than a compromise: a drive is felt as a state,
    /// not read as a float.
    /// </para>
    /// <para>
    /// <b>AND, WHERE THE BODY CAN SAY SO, WHERE EACH ONE STANDS AGAINST THE
    /// OTHERS.</b> A band cannot express <i>lowest</i>, which is the one fact this
    /// world turns on — see <see cref="HomeostatSettings.Ranked"/>. The ranks are
    /// EXTRA codes rather than replacements, so the arm is additive and the graph
    /// holds the absolute fact and the relational one at once.
    /// </para>
    /// </remarks>
    public ImmutableArray<Code> Feels()
    {
        var felt = new Code[_settings.Ranked ? _at.Length * 2 : _at.Length];

        for (var which = 0; which < _at.Length; which++)
        {
            var band = (int)(_at[which] * _settings.Bands);
            felt[which] = new Code(
                (byte)(Need + which), (ulong)Math.Clamp(band, 0, _settings.Bands - 1));
        }

        if (!_settings.Ranked) return [.. felt];

        for (var which = 0; which < _at.Length; which++)
            felt[_at.Length + which] = new Code((byte)(Rank + which), (ulong)Standing(which));

        return [.. felt];
    }

    /// <summary>
    /// Where variable <paramref name="which"/> stands against the others.
    /// <b>Nought is the lowest</b>, which is the one this world's task is about.
    /// </summary>
    /// <remarks>
    /// <b>TIES BREAK ON THE INDEX, so the ordering is a permutation and never a
    /// near-miss.</b> Two variables at the same value would otherwise both claim a
    /// position and the front end would emit a rank no variable held — a state the
    /// graph would learn about and that the body can never be in again.
    /// </remarks>
    public int Standing(int which)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(which);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(which, _at.Length);

        var below = 0;

        for (var other = 0; other < _at.Length; other++)
        {
            if (other == which) continue;

            if (_at[other] < _at[which] || (_at[other] == _at[which] && other < which)) below++;
        }

        return below;
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
