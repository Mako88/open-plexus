using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How many recurring sets there are, how big, and how often they show.</summary>
public sealed record MotifSettings
{
    /// <summary>How many distinct codes exist.</summary>
    public int Symbols { get; init; } = 60;

    /// <summary>
    /// How many recurring sets. <b>Zero is the control</b> — the same stream with
    /// nothing in it that could ever be worth minting a code for.
    /// </summary>
    public int Motifs { get; init; } = 6;

    /// <summary>How many codes are in one set, and in one random moment.</summary>
    /// <remarks>
    /// <b>The same for both, deliberately.</b> A moment showing a set must be
    /// indistinguishable from a random one by its size alone, or the task is
    /// counting rather than recognising.
    /// </remarks>
    public int Size { get; init; } = 4;

    /// <summary>The share of moments that show a set rather than noise, in 0..1.</summary>
    public double Density { get; init; } = 0.5;
}

/// <summary>
/// A stream in which some sets of codes always arrive together.
/// </summary>
/// <remarks>
/// <para>
/// <b>The world for minting a name, and it was built before anything could.</b>
/// When a set of codes recurs, a code standing for the WHOLE set should come into
/// existence — which is what lets the alphabet GROW, where a quantiser alone fixes
/// it forever. That is rung five, and this world is where the redundancy it needs
/// is manufactured on purpose.
/// </para>
/// <para>
/// <b>The interesting number here is cost, not accuracy.</b> A graph with no
/// chunking can already complete a familiar set perfectly well — the codes
/// co-occur, so the counts are exactly what they should be. What it cannot do is
/// stop paying for it: a set of size S written as pairwise co-occurrence is
/// S(S-1) edge entries every time it recurs, where one minted node standing for
/// the set would be S. So the claim under test is compression, and the measurement
/// is the graph's size and the traffic per question — with accuracy present only
/// to prove the compression would not have cost anything.
/// </para>
/// <para>
/// <b>The minimum description length argument is the whole threshold.</b> A set
/// is worth minting when naming it costs less than describing it every time, which
/// is a property of how often it recurs and how big it is — not a constant
/// somebody set. This world varies both.
/// </para>
/// </remarks>
public sealed class Motif : IWorld<Coded>
{
    /// <summary>The one modality this world emits.</summary>
    public const byte Token = 70;

    private readonly MotifSettings _settings;
    private readonly List<ImmutableArray<Code>> _motifs = [];
    private readonly Random _rng;

    /// <param name="settings">The shape of the stream.</param>
    /// <param name="seed">The world's own generator.</param>
    public Motif(MotifSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Symbols);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Motifs);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Size, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Size, settings.Symbols);

        _settings = settings;
        _rng = new Random(seed);

        // The sets are drawn once and disjoint. Overlapping sets would make a
        // completion ambiguous for a reason that has nothing to do with chunking,
        // and the score would be measuring the overlap.
        var pool = Enumerable.Range(0, settings.Symbols).OrderBy(_ => _rng.Next()).ToList();

        for (var which = 0; which < settings.Motifs; which++)
        {
            var from = which * settings.Size;
            if (from + settings.Size > pool.Count) break;

            _motifs.Add([.. pool.Skip(from).Take(settings.Size).Select(Of)]);
        }
    }

    /// <summary>The recurring sets, in the order they were drawn.</summary>
    public IReadOnlyList<ImmutableArray<Code>> Motifs => _motifs;

    /// <summary>
    /// How many of a set's codes a moment shows — <b>half, rounded down, and at least
    /// one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Showing all but one makes the completion a lookup; showing one makes it a marginal.
    /// </para>
    /// <para>
    /// <b>And it is the axis rather than a constant</b>, because rung five's two readings
    /// need different lengths. A scope of two codes may be named and can never carry a
    /// name, so a name standing for a name is unreachable at any cue under three however
    /// much redundancy the stream holds. <see cref="MotifSettings.Size"/> is what moves it.
    /// </para>
    /// </remarks>
    public int Cue => Math.Max(1, _settings.Size / 2);

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The whole alphabet</b>, because the outcome is one symbol of it and a noise
    /// moment can be followed by any of them.
    /// </remarks>
    public int Outcomes => _settings.Symbols;

    /// <summary>
    /// The best a model that knew everything could score — <b>computed with no learning.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sets are disjoint, so a cue that belongs to a set names that set outright and
    /// nothing is left to work out. What remains is which of the <see cref="Cue"/> codes
    /// the set withheld arrived, and that is a uniform draw — so a motif moment is capped
    /// at one over the number withheld and no amount of learning moves it.
    /// </para>
    /// <para>
    /// A noise moment carries no information at all: its codes were drawn without
    /// replacement from the alphabet, so the outcome is uniform over whatever the cue did
    /// not show.
    /// </para>
    /// </remarks>
    public double Ceiling =>
        (Recurring / (double)(_settings.Size - Cue))
        + ((1.0 - Recurring) / (_settings.Symbols - Cue));

    /// <summary>
    /// Always naming the commonest outcome, which is one of the withheld members of a set.
    /// </summary>
    /// <remarks>
    /// <b>The control that matters here</b>, because a model that has learnt only which
    /// symbols are frequent lands on it. Every member a set withholds arrives at its own
    /// set's share of the motif moments, and every other symbol only ever arrives by noise.
    /// </remarks>
    public double Marginal =>
        (_motifs.Count == 0
            ? 0.0
            : Recurring / (_motifs.Count * (double)(_settings.Size - Cue)))
        + ((1.0 - Recurring) / _settings.Symbols);

    /// <summary>
    /// The share of moments that show a set, which is nought where there are none.
    /// </summary>
    /// <remarks>
    /// <b>Read off the sets rather than off the dial</b>, because the control turns the
    /// sets off and leaves <see cref="MotifSettings.Density"/> where it was. A bar computed
    /// from the dial would price a stream this world never draws.
    /// </remarks>
    private double Recurring => _motifs.Count == 0 ? 0.0 : _settings.Density;

    /// <summary>One moment: either a whole set, or that many codes at random.</summary>
    /// <returns>The codes shown, and which set they were if any.</returns>
    public (ImmutableArray<Code> Shown, int Which) Draw()
    {
        if (_motifs.Count > 0 && _rng.NextDouble() < _settings.Density)
        {
            var which = _rng.Next(_motifs.Count);
            return (_motifs[which], which);
        }

        // NOISE OF THE SAME SIZE, drawn without replacement so a random moment
        // never shows one code twice and is never smaller than a set.
        var drawn = Enumerable.Range(0, _settings.Symbols)
            .OrderBy(_ => _rng.Next())
            .Take(_settings.Size)
            .Select(Of);

        return ([.. drawn], -1);
    }

    /// <summary>
    /// A question about one set: what it shows, and what would complete it.
    /// </summary>
    public (ImmutableArray<Code> Asked, IReadOnlySet<Code> Wanted) Ask(int which)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(which);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(which, _motifs.Count);

        var (asked, wanted) = Split(_motifs[which]);

        return (asked, wanted.ToHashSet());
    }

    /// <summary>Where a moment stops being shown and starts being asked about.</summary>
    private (ImmutableArray<Code> Asked, IEnumerable<Code> Wanted) Split(
        ImmutableArray<Code> whole) =>
        ([.. whole.Take(Cue)], whole.Skip(Cue));

    /// <summary>
    /// One moment through the seam every world shares: the cue, and one code it withheld.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The completion put to the learner one code at a time</b>, because a moment settles
    /// on one outcome and a set withholds several. Which of them arrives is drawn uniformly,
    /// so the same cue is followed by a different answer from one showing to the next and
    /// the accuracy is capped at <see cref="Ceiling"/> by the world rather than by anything
    /// the population does.
    /// </para>
    /// <para>
    /// <b>And that cap is what gives repair a foothold</b>, which is what this world is for.
    /// A single cue code names its set outright, so no conjunction over the cue tells anyone
    /// which set is showing and a scope could never earn a second code from the sets alone.
    /// What it earns one from is noise: a set's code turns up in a random moment at the
    /// cue's share of the alphabet, and a second code of the same set is what strikes those
    /// moments out. So the scopes grow, they grow over codes that always co-occur, and rung
    /// five has redundancy to name.
    /// </para>
    /// <para>
    /// <b>The outcome is the symbol's own number</b> rather than a code, which is the shared
    /// alphabet <see cref="Turn{TSeen}.Outcome"/> asks for. A world minting its own outcome
    /// coding would be a world deciding what a brain answers in.
    /// </para>
    /// </remarks>
    public Turn<Coded> Next()
    {
        var (shown, _) = Draw();
        var (asked, wanted) = Split(shown);

        var withheld = wanted.ToList();

        return new Turn<Coded>
        {
            Seen = Coded.Of(asked),
            Outcome = (int)withheld[_rng.Next(withheld.Count)].Value,
        };
    }

    /// <summary>The code for one symbol.</summary>
    public static Code Of(int symbol)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(symbol);
        return new Code(Token, (ulong)symbol);
    }

    /// <summary>
    /// What the graph would hold if every set were minted as one node.
    /// </summary>
    /// <remarks>
    /// <b>The MDL target, computed rather than measured.</b> A set of size S seen
    /// as co-occurrence writes S(S-1) directed entries; a node standing for the
    /// set writes S, one to each member. This is what the graph's own edge count
    /// is compared against, and the gap is what step 3 would be buying.
    /// </remarks>
    public int Compressed => _motifs.Count * _settings.Size;

    /// <inheritdoc cref="Compressed"/>
    public int Uncompressed => _motifs.Count * _settings.Size * (_settings.Size - 1);

    /// <inheritdoc cref="MotifSettings.Size"/>
    public int Size => _settings.Size;
}
