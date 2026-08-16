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
    /// <b>Uneven on purpose, and it is what makes the world discriminate.</b> If
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
    /// <b>Bounded on both sides by the world's own arithmetic.</b> Above the sum
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
    /// <b>Two measurements said the front end was the problem, from opposite
    /// directions.</b> A band is an ABSOLUTE fact about one variable, and
    /// <i>attend to whichever is lowest</i> is a RELATIONAL fact about all of
    /// them — the same limit as <c>A is north of B</c>. And the ceiling policy
    /// holds the body so steady that every banded code sits still: it visits
    /// exactly ONE state, so the correct policy is, to the graph, a constant with
    /// no state variation for a state-conditional association to attach to.
    /// </para>
    /// <para>
    /// <b>A rank fixes both at once, and the second is the surprising half.</b>
    /// Drains are uneven, so attending to the lowest makes which-variable-is-worst
    /// ROTATE while the values themselves barely move. The ordering varies exactly
    /// where the magnitudes do not, which is what gives a well-behaved run
    /// something to be conditional on.
    /// </para>
    /// <para>
    /// <b>IT IS THE <see cref="Codes.IQuantizer{TObservation}.Bind"/> split again,
    /// and the same caveat applies word for word.</b> Comparison is pre-attentive
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

/// <summary>What a body felt, and what was done about it.</summary>
/// <remarks>
/// <para>
/// <b>The two halves of a moment in an acted world</b>, and they arrive together because a
/// consequence is a fact about both. <i>Variable two was low</i> predicts nothing on its own;
/// <i>variable two was low and I attended to variable nought</i> predicts what follows, and
/// the difference between those is the whole of what an action buys.
/// </para>
/// <para>
/// <b>Codes rather than numbers, because the body already quantised them.</b> Every other
/// world here hands over its own terms and lets a front end code them, and this one's terms
/// ARE bands — a drive is felt as a state rather than read as a float, which is
/// <see cref="Homeostat.Feels"/>'s own line. What a front end is left to decide is whether the
/// action goes in the moment at all, which is the arm.
/// </para>
/// </remarks>
public readonly record struct Bodily
{
    /// <summary>What the body felt about itself, before anything was done.</summary>
    public required ImmutableArray<Code> Felt { get; init; }

    /// <summary>Which variable was attended to, or nothing where nothing was done.</summary>
    public required int? Did { get; init; }
}

/// <summary>
/// Ashby's homeostat: internal variables that must be kept in bounds, and no
/// reward for keeping them there.
/// </summary>
/// <remarks>
/// <para>
/// <b>The world for step 4, and it exists because survival was gameable.</b>
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
public sealed class Homeostat : IActed<Bodily>
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

    // What a chooser asked for and the next step has not spent yet. Held here rather than
    // passed to `Next` because an action is taken IN a state and a turn reports one that has
    // already been acted in -- see `IActed`. Cleared by the step, so an unanswered `Do` can
    // never be spent twice.
    private int? _pending;

    /// <param name="settings">The shape of the body.</param>
    public Homeostat(HomeostatSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Needs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Bands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Drain);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Restore);

        // One modality block must not run into the next. See Rank.
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

    /// <inheritdoc/>
    /// <remarks>
    /// <b>One a variable</b>, because attending is the only thing this body can do. Ashby's
    /// unit deviates and its neighbours are what it deviates against, so the whole action
    /// space here is <i>which one to look after</i> — and doing nothing is
    /// <see cref="Do"/>'s null rather than an extra doing, so a chooser drawing uniformly
    /// draws over acts and not over acts-plus-idling.
    /// </remarks>
    public int Doings => _settings.Needs;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Nothing done yet</b>, which is what makes this the state an action is chosen in.
    /// <see cref="Bodily.Did"/> is null here and carries the choice in the turn that follows,
    /// so a chooser reading this cannot see its own answer.
    /// </remarks>
    public Bodily Now => new() { Felt = Feels(), Did = null };

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Which variable is worst NOW, which is the consequence a chooser needs.</b> Whether
    /// the body is still viable would be the obvious outcome and is nearly always true, so a
    /// population answering <i>yes</i> forever would score above nine tenths having learnt
    /// nothing. Which one is in trouble moves every step under a policy that holds the body,
    /// because the drains are uneven — see <see cref="HomeostatSettings.Drain"/>, where that
    /// rotation is the same property the rank codes were built for.
    /// </remarks>
    public int Outcomes => _settings.Needs;

    /// <inheritdoc/>
    public void Do(int? doing)
    {
        if (doing is { } which)
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(which, _settings.Needs);

        if (doing is { } low) ArgumentOutOfRangeException.ThrowIfNegative(low);

        _pending = doing;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The state is read before the step and the outcome after it</b>, which is the only
    /// arrangement that makes the turn a claim about a consequence. Reading both after would
    /// report a body that had already been restored, and a commitment learning from that
    /// would be told what it did AND what it did it to, with nothing left to be wrong about.
    /// </remarks>
    public Turn<Bodily> Next()
    {
        var felt = Feels();
        var did = _pending;

        _pending = null;

        Step(did);

        return new Turn<Bodily> { Seen = new Bodily { Felt = felt, Did = did }, Outcome = Lowest };
    }

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
    /// <b>And, where the body can say so, where each one stands against the
    /// others.</b> A band cannot express <i>lowest</i>, which is the one fact this
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

    /// <summary>
    /// Which variable a felt code is about, or nothing where it is about none.
    /// </summary>
    /// <param name="code">One code out of <see cref="Feels"/>.</param>
    /// <remarks>
    /// <b>The inverse of the band half of <see cref="Feels"/></b>, as
    /// <see cref="Attended"/> is the inverse of <see cref="Attending"/>. A drive over this
    /// body reads how well off a variable is, and the only honest place to read that is the
    /// code the front end emitted — so the mapping back has to exist for a preference to be
    /// computable from what the learner feels rather than from <see cref="At"/>.
    /// </remarks>
    public static int? Sensed(Code code) =>
        code.Modality >= Need && code.Modality < Rank ? code.Modality - Need : null;

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
