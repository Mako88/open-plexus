namespace OpenPlexus.Codes;

/// <summary>
/// <see cref="Winnow"/> as a translation, so it can stand where a band can.
/// </summary>
/// <remarks>
/// <para>
/// <b>SO THE FRONT END BECOMES AN ARM RATHER THAN A REWRITE.</b> Banding and
/// winnowing are two answers to the same question — how a number becomes symbols —
/// and a comparison between them is only honest if swapping one for the other changes
/// nothing else at all.
/// </para>
/// <para>
/// <b>A READING BELOW ABOUT TEN DIMENSIONS HAS TOO FEW DISTINCT WIRINGS to expand
/// into.</b> Six numbers sampled six at a time have exactly one, so every cell would
/// fire identically and the tag would separate nothing — <see cref="Sheet"/> caps the
/// sheet at what actually exists rather than producing that quietly.
/// </para>
/// </remarks>
public sealed class Winnowing : IQuantizer<IReadOnlyList<double>>
{
    /// <summary>How many cells the projection expands into, per dimension read.</summary>
    /// <remarks>
    /// <b>A constant of the design and not of a run, exactly as the projection itself
    /// is.</b> Making it a dial would invite fitting the front end to a world, which
    /// is the codebook the red-ball property forbids arriving by the back door.
    /// </remarks>
    private const int Expansion = 40;

    /// <summary>One winner per this many cells.</summary>
    private const int Sparsity = 20;

    private readonly Winnow _winnow;

    /// <param name="modality">The modality these codes ride on.</param>
    /// <param name="width">How many dimensions a reading has.</param>
    public Winnowing(byte modality, int width)
    {
        var (cells, reach, winners) = Sheet(width);

        _winnow = new Winnow(modality, width, cells, reach, winners);
        Modality = modality;
    }

    /// <inheritdoc/>
    public byte Modality { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(IReadOnlyList<double> observation) =>
        _winnow.Of(observation);

    /// <summary>How many distinct things this front end has said.</summary>
    /// <remarks>
    /// <b>PASSED THROUGH BECAUSE THE COLLAPSE IS INVISIBLE FROM THE OTHER SIDE.</b> A
    /// front end emitting one tag for every reading in the world looks, to whatever
    /// consumes it, exactly like a world with one thing in it — and the constructor's
    /// guard reads the DECLARED width, which real data routinely overstates. Reaching
    /// it required holding a <see cref="Winnow"/> rather than a translation, so nothing
    /// that took the arm could read it.
    /// </remarks>
    public int Distinct => _winnow.Distinct;

    /// <summary>How many readings it was handed.</summary>
    /// <remarks>
    /// <b>The denominator, and without it <see cref="Distinct"/> says nothing.</b>
    /// </remarks>
    public long Emitted => _winnow.Emitted;

    /// <summary>How wide a sheet a reading of this many dimensions can support.</summary>
    /// <param name="width">How many dimensions are read.</param>
    /// <remarks>
    /// <b>DERIVED, BECAUSE A FIXED GEOMETRY IS DEGENERATE ON A NARROW READING.</b> The
    /// fly projects fifty receptors into two thousand cells; the ratio is what
    /// preserves similarity on little data, and it needs dimensions to project FROM.
    /// A narrow world simply gets a small sheet, which is the honest answer.
    /// </remarks>
    public static (int Cells, int Reach, int Winners) Sheet(int width)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 3);

        var reach = Math.Clamp(width / 3, 2, width - 1);
        var wanted = width * Expansion;
        var cells = Math.Min(wanted, Wirings(width, reach, wanted));

        return (cells, reach, Math.Max(2, cells / Sparsity));
    }

    /// <summary>How many distinct wirings there are, giving up once past a ceiling.</summary>
    private static int Wirings(int inputs, int samples, int ceiling)
    {
        long total = 1;

        for (var step = 0; step < samples; step++)
        {
            total = total * (inputs - step) / (step + 1);
            if (total >= ceiling) return ceiling;
        }

        return (int)total;
    }
}
