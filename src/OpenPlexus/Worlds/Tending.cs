using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Worlds;

/// <summary>How many plants, how fast they dry, and how far a watering goes.</summary>
public sealed record TendingSettings
{
    /// <summary>How many plants must be kept damp at once, in a row.</summary>
    public int Plants { get; init; } = 4;

    /// <summary>
    /// The slowest plant's drying per step. <b>Plant <c>i</c> dries at
    /// <c>Drain × (i+1)</c></b>.
    /// </summary>
    /// <remarks>
    /// <b>UNEVEN FOR THE REASON <see cref="Homeostat"/> IS UNEVEN.</b> With equal
    /// rates, spreading water evenly would be correct and a policy that never
    /// looked at the plants would score as well as one that did.
    /// </remarks>
    public double Drain { get; init; } = 0.01;

    /// <summary>How much one watering restores, <b>one step after it is poured.</b></summary>
    public double Restore { get; init; } = 0.36;

    /// <summary>How dry a plant may get before it is out of bounds.</summary>
    public double Floor { get; init; } = 0.25;

    /// <summary>How many bands a moisture level is quantised into.</summary>
    public int Bands { get; init; } = 5;
}

/// <summary>
/// A body that must GO somewhere before it can do anything — <b>the world where
/// an act pays off later than the act that enabled it.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>WHY A SECOND BODY EXISTS AT ALL.</b> <see cref="Graph.Kind.Helped"/> is measured
/// on <see cref="Homeostat"/> alone, so every conclusion of step 4 is one world
/// old. That was the stated reason and it is the weaker one.
/// </para>
/// <para>
/// <b>THE REAL REASON: <see cref="Homeostat"/> CANNOT SEE THE THINGS LEFT TO
/// BUILD.</b> Every act there pays off in the step it is taken, so a credit signal
/// comparing one moment to the next captures all of it — which means eligibility
/// traces (step 7), curiosity (step 10) and rollout (step 11) have nothing to be
/// about. A world where one-step credit is SUFFICIENT cannot measure a mechanism
/// built because one-step credit is INSUFFICIENT.
/// </para>
/// <para>
/// <b>SO MOVING IS THE POINT.</b> Water reaches only the plant the body is
/// standing at, so reaching the plant that needs it takes steps that improve
/// nothing — and are the whole reason the next act can help. <b>An arm scoring
/// each act by what immediately followed it must rate every move as worthless</b>,
/// and that is the gap made visible rather than argued.
/// </para>
/// <para>
/// <b>AND THE WATERING ITSELF LANDS A STEP LATE</b>, so even the act that DOES
/// help does not help now. Two delays of different kinds: one instrumental, one
/// in the physics.
/// </para>
/// <para>
/// <b>Ashby's shape is kept exactly.</b> No reward function, no goal state, no
/// episode boundary — plants dry whether or not anything is done, so standing
/// still is the fastest way to fail and circling waters nothing. <b>Survival is
/// not the score</b>; the score is time spent with every plant in bounds, and a
/// run carries on through failure.
/// </para>
/// <para>
/// <b>NOTHING RELATIONAL IS HANDED OVER.</b> There is no rank here and no
/// <i>which is driest</i>: the body feels a band per plant and where it is
/// standing, both of which a real body knows about itself. <b>That is deliberate
/// after <c>Ranked</c></b>, which is the freebie this project is least comfortable
/// with — and the position varies on its own, so this world does not need one to
/// give a well-behaved run something to be conditional on.
/// </para>
/// </remarks>
public sealed class Tending
{
    /// <summary>Plant <c>i</c>'s moisture band rides on modality <c>Damp + i</c>.</summary>
    public const byte Damp = 140;

    /// <summary>Where the body is standing.</summary>
    public const byte Where = 139;

    /// <summary>What the body did.</summary>
    public const byte Did = 138;

    /// <summary>Step left, step right, or water what is underfoot.</summary>
    public const int Actions = 3;

    private readonly TendingSettings _settings;
    private readonly double[] _damp;

    /// <summary>What was poured last step and lands this one. <b>The delay.</b></summary>
    private int? _poured;

    private int _at;

    /// <param name="settings">The shape of the garden.</param>
    public Tending(TendingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Plants, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Bands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Drain);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Restore);

        // THE MODALITY BLOCK MUST NOT RUN OFF THE END, and a plant landing on
        // another world's modality would be one code meaning two things — this
        // design's recurring fault at its most literal.
        if (Damp + settings.Plants > byte.MaxValue + 1)
            throw new ArgumentOutOfRangeException(nameof(settings),
                $"{settings.Plants} plants do not fit in the modality block at {Damp}");

        _settings = settings;
        _damp = [.. Enumerable.Repeat(1.0, settings.Plants)];
    }

    /// <summary>How damp every plant is right now.</summary>
    public IReadOnlyList<double> At => _damp;

    /// <summary>Where the body is standing.</summary>
    public int Standing => _at;

    /// <inheritdoc cref="TendingSettings.Plants"/>
    public int Plants => _settings.Plants;

    /// <inheritdoc cref="TendingSettings.Restore"/>
    public double Restore => _settings.Restore;

    /// <summary>How fast plant <paramref name="which"/> dries.</summary>
    public double Dries(int which)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(which);
        return _settings.Drain * (which + 1);
    }

    /// <summary>Everything drying, added up.</summary>
    public double Drying => Enumerable.Range(0, _settings.Plants).Sum(Dries);

    /// <summary>Whether every plant is inside its bounds.</summary>
    public bool Viable => _damp.All(wet => wet >= _settings.Floor);

    /// <summary>
    /// Which plant is furthest from where it should be.
    /// </summary>
    /// <remarks>
    /// <b>THE ORACLE'S ANSWER, AND NOTHING IN THE GRAPH IS TOLD IT.</b> It exists
    /// so a run can be compared against the best a body could do with the same
    /// actions and the same travel.
    /// </remarks>
    public int Driest
    {
        get
        {
            var driest = 0;
            for (var which = 1; which < _damp.Length; which++)
                if (_damp[which] < _damp[driest]) driest = which;

            return driest;
        }
    }

    /// <summary>
    /// One step: <b>what was poured last step lands, everything dries, and the
    /// body does one thing.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE ORDER IS THE WHOLE DELAY.</b> A watering poured on this step is held
    /// and applied at the START of the next one, so the moment an act helps is
    /// never the moment it is taken — which is what step 7's trace is for and what
    /// <see cref="Homeostat"/> has no version of.
    /// </para>
    /// <para>
    /// <b>Drying happens whatever is chosen</b>, including moving and including
    /// doing nothing, so travel is never free.
    /// </para>
    /// </remarks>
    /// <param name="doing">
    /// <c>0</c> steps left, <c>1</c> steps right, <c>2</c> waters what is
    /// underfoot. Null does nothing at all.
    /// </param>
    /// <returns>Whether the garden is viable after the step.</returns>
    public bool Step(int? doing)
    {
        // WHAT WAS POURED LAST STEP LANDS NOW. Held rather than applied, which is
        // the delay this world exists for.
        if (_poured is { } wetted)
            _damp[wetted] = Math.Min(1.0, _damp[wetted] + _settings.Restore);

        _poured = null;

        for (var which = 0; which < _damp.Length; which++)
            _damp[which] = Math.Max(0.0, _damp[which] - Dries(which));

        switch (doing)
        {
            case 0: _at = Math.Max(0, _at - 1); break;
            case 1: _at = Math.Min(_damp.Length - 1, _at + 1); break;

            // POURED, NOT APPLIED. See above.
            case 2: _poured = _at; break;

            default: break;
        }

        return Viable;
    }

    /// <summary>
    /// What the body can feel about itself — <b>a band per plant, and where it is
    /// standing.</b>
    /// </summary>
    /// <remarks>
    /// <b>BOTH ARE FACTS A REAL BODY HAS ABOUT ITSELF</b>, which is the bar this
    /// project holds a front end to: moisture is a sense and position is
    /// proprioception. <b>Nothing here says which plant is driest</b>, and nothing
    /// says which plant is reachable — those are what the graph is for.
    /// </remarks>
    public ImmutableArray<Code> Feels()
    {
        var felt = new Code[_damp.Length + 1];

        for (var which = 0; which < _damp.Length; which++)
        {
            var band = (int)(_damp[which] * _settings.Bands);
            felt[which] = new Code(
                (byte)(Damp + which), (ulong)Math.Clamp(band, 0, _settings.Bands - 1));
        }

        felt[_damp.Length] = new Code(Where, (ulong)_at);

        return [.. felt];
    }

    /// <summary>The code for doing one thing.</summary>
    public static Code Doing(int what)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(what);
        return new Code(Did, (ulong)what);
    }

    /// <summary>Which thing an action code is about.</summary>
    public static int Done(Code code) => (int)code.Value;

    /// <summary>
    /// How long doing nothing lasts, in steps.
    /// </summary>
    /// <remarks>
    /// <b>The number that makes idling a failure rather than a strategy.</b> The
    /// fastest-drying plant reaches the floor first.
    /// </remarks>
    public int Idling =>
        (int)Math.Ceiling((1.0 - _settings.Floor) / Dries(_settings.Plants - 1));

    /// <summary>
    /// What the oracle would do: <b>go to the driest plant, and water it when
    /// standing on it.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE CEILING, AND IT IS NOT A POLICY THE GRAPH COULD LEARN FROM.</b> It
    /// reads the moisture levels as numbers and compares them, which is exactly
    /// the relational fact the front end refuses to hand over. It exists to say
    /// the world is winnable.
    /// </remarks>
    public int Best()
    {
        var driest = Driest;

        if (driest == _at) return 2;
        return driest < _at ? 0 : 1;
    }
}
