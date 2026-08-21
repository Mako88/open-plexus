using System.Collections.Immutable;

namespace OpenPlexus.Codes;

/// <summary>
/// A square reading winnowed one PATCH at a time, said twice — <b>the part, and the
/// part where it is.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is patch tokens, and the plan names it as the fix.</b> A pooled embedding
/// emits one vector for a whole picture: no parts, no arrangement, excellent at
/// <i>what is this a picture of</i> and unable ever to answer <i>what is where</i>.
/// <see cref="Winnowing"/> over a whole reading is the same shape of thing by another
/// road — a winner reads a scattered handful of pixels from everywhere at once, so the
/// tag it belongs to is a fact about the picture and not about any part of it.
/// </para>
/// <para>
/// <b>One codebook across every patch, which is the entire point.</b> The same
/// <see cref="Winnow"/> reads every patch, so a part that looks the same in two places
/// wins the same cells in both — and a code therefore MEANS that part, wherever it
/// turns up. That is the reusable symbol a bag-of-parts world cannot ask for and an
/// arrangement world can.
/// </para>
/// <para>
/// <b>And it says both things</b>, which is John's proposal arriving where it pays.
/// The plan asks for a front end emitting SEVERAL codes per reading so that near
/// readings overlap and the shared part becomes nameable. Here the two readings that
/// need to overlap are the same part in two places: each winner is emitted bare — <i>a
/// wedge, somewhere</i> — and again with its patch — <i>a wedge, in the third
/// patch</i>. The bare code is literally the shared sub-code of the two placed ones,
/// so what a scope can name includes the part independent of where it is.
/// </para>
/// <para>
/// <b>What it does not buy is rung four.</b> A placed code pins one patch, so
/// <i>whatever column the wedge is in, the ring is to its right</i> is still a
/// disjunction over patches and still unsayable. This makes the part transferable and
/// the position nameable; it does not make the position a VARIABLE, and pretending
/// otherwise is how the ladder's order becomes a bias nobody declared.
/// </para>
/// <para>
/// <b>The geometry is <see cref="Winnowing.Sheet"/>'s</b>, so the two arms differ in
/// exactly one place. A tiled front end with its own expansion ratio would be a
/// comparison between two projections as much as between two ways of cutting a picture
/// up, which is the fault the whole seam exists to prevent.
/// </para>
/// </remarks>
public sealed class Tiling : IQuantizer<IReadOnlyList<double>>, IQuantizer<Worlds.Crossed>
{
    private readonly Winnow _winnow;
    private readonly int _side;
    private readonly int _tile;
    private readonly int _across;
    private readonly ulong _cells;

    /// <summary>Whether a winner is said again with its patch.</summary>
    /// <remarks>
    /// <para>
    /// <b>An arm, and `LetteringTests` is where it is compared.</b> On a world whose thing
    /// MOVES the placed half cannot help: a word two pixels along emits none of the placed
    /// codes it emitted before, so no placed code can sit in a scope that is sound over
    /// position. What the reading adds is that it does not merely fail to help — a probe
    /// over both halves reads far below the same probe over the bare half alone, because the
    /// placed codes are twenty-five times as many features and every one of them is a
    /// position the withheld drawing does not use.
    /// </para>
    /// <para>
    /// <b>Defaulted to saying both</b>, so every reading taken before this existed is the
    /// reading it was. An arranged world whose cells never move is where the placed half was
    /// built for, and it stays the default until something measures it there too.
    /// </para>
    /// </remarks>
    private readonly bool _placed;

    /// <param name="modality">The modality these codes ride on.</param>
    /// <param name="side">How many pixels across the square reading is.</param>
    /// <param name="tile">How many pixels across one patch is. Must divide the side.</param>
    /// <param name="placed">
    /// Whether to say a winner a second time with the patch it sat in.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The patch does not divide the reading, or is too small to project from.
    /// </exception>
    public Tiling(byte modality, int side, int tile, bool placed = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(side, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(tile, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tile, side);

        // A patch that does not divide the reading would silently drop a strip off
        // two edges, and a part sitting in it would be invisible -- which reads, from
        // anywhere downstream, exactly like a learner that could not learn.
        if (side % tile != 0)
            throw new ArgumentOutOfRangeException(
                nameof(tile),
                $"a {tile}-pixel patch does not divide a {side}-pixel reading, so a "
                + "strip of it would never be looked at.");

        var (cells, reach, winners) = Winnowing.Sheet(tile * tile);

        _winnow = new Winnow(modality, tile * tile, cells, reach, winners);
        _side = side;
        _tile = tile;
        _across = side / tile;
        _cells = (ulong)cells;
        _placed = placed;

        Modality = modality;
    }

    /// <inheritdoc/>
    public byte Modality { get; }

    /// <summary>How many patches a reading is cut into.</summary>
    public int Patches => _across * _across;

    /// <summary>How many distinct things one patch's codebook has said.</summary>
    /// <remarks>
    /// <b>Per patch rather than per reading</b>, which is the number that matters here.
    /// A tiled front end collapses when it cannot tell two PARTS apart, and that shows
    /// up as a tag count near the number of distinct patches a world contains — which
    /// is small and knowable, unlike the number of distinct pictures.
    /// </remarks>
    public int Distinct => _winnow.Distinct;

    /// <summary>How many patches it has been handed.</summary>
    public long Emitted => _winnow.Emitted;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Two codes a winner, and the arithmetic keeps them apart forever.</b> A bare
    /// winner takes its own cell number; a placed one is offset by the patch, one block
    /// of cells per patch. Nothing can collide, and the bare code is arithmetically the
    /// residue of every placed one — which is what lets a scope name the part without
    /// naming the place.
    /// </remarks>
    public IReadOnlyCollection<Code> Codify(IReadOnlyList<double> observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (observation.Count != _side * _side)
            throw new ArgumentException(
                $"this front end reads a {_side}-by-{_side} square and was handed "
                + $"{observation.Count} numbers", nameof(observation));

        var codes = ImmutableArray.CreateBuilder<Code>();
        var patch = new double[_tile * _tile];

        for (var at = 0; at < Patches; at++)
        {
            var top = at / _across * _tile;
            var left = at % _across * _tile;

            for (var down = 0; down < _tile; down++)
            for (var across = 0; across < _tile; across++)
                patch[(down * _tile) + across] =
                    observation[((top + down) * _side) + left + across];

            foreach (var code in _winnow.Of(patch))
            {
                codes.Add(code);

                if (_placed)
                    codes.Add(new Code(Modality, ((ulong)(at + 1) * _cells) + code.Value));
            }
        }

        // A set, because a moment is one. Two patches holding the same part emit the
        // same bare code, and a duplicate in the list would make a moment's code count
        // -- which is reported beside every score as the cost of a front end -- depend
        // on how often a part recurred rather than on how much was said.
        return codes.Distinct().ToList();
    }

    /// <summary>The drawn half of a crossing moment, and nothing where none was drawn.</summary>
    /// <remarks>
    /// <b>A front end reads its own field and takes what it knows</b>, which is why a body
    /// needs no router — see <see cref="Compound{TFrame}"/>. A moment that is words alone is
    /// a moment this sense has nothing to say about, and saying nothing is what it does.
    /// </remarks>
    public IReadOnlyCollection<Code> Codify(Worlds.Crossed observation) =>
        observation.Shape is null ? [] : Codify(observation.Shape);
}
