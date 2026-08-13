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
/// <b>THE WORLD FOR MINTING A NAME, AND IT WAS BUILT BEFORE ANYTHING COULD.</b>
/// When a set of codes recurs, a code standing for the WHOLE set should come into
/// existence — which is what lets the alphabet GROW, where a quantiser alone fixes
/// it forever. That is rung five, and this world is where the redundancy it needs
/// is manufactured on purpose.
/// </para>
/// <para>
/// <b>THE INTERESTING NUMBER HERE IS COST, NOT ACCURACY.</b> A graph with no
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
public sealed class Motif
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

        // THE SETS ARE DRAWN ONCE AND DISJOINT. Overlapping sets would make a
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
    /// The chance a blind guess completes a set.
    /// </summary>
    /// <remarks>
    /// <b>Over the codes that are NOT in the cue</b>, because the cue is struck
    /// out of the candidates before anything is ranked — so a blind draw is over
    /// what is left, not over the whole alphabet.
    /// </remarks>
    public double Chance =>
        _motifs.Count == 0 || _settings.Symbols <= Cue
            ? 0.0
            : (_settings.Size - Cue) / (double)(_settings.Symbols - Cue);

    /// <summary>How many of a set's codes a question shows.</summary>
    /// <remarks>
    /// <b>Half, rounded down, and at least one.</b> Showing all but one makes the
    /// completion a lookup; showing one makes it a marginal.
    /// </remarks>
    private int Cue => Math.Max(1, _settings.Size / 2);

    /// <summary>One moment: either a whole set, or that many codes at random.</summary>
    /// <returns>The codes shown, and which set they were if any.</returns>
    public (ImmutableArray<Code> Shown, int Which) Next()
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

        var motif = _motifs[which];

        return ([.. motif.Take(Cue)], motif.Skip(Cue).ToHashSet());
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
    /// <b>THE MDL TARGET, COMPUTED RATHER THAN MEASURED.</b> A set of size S seen
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
