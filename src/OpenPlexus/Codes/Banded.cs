using System.Collections.Immutable;

namespace OpenPlexus.Codes;

/// <summary>
/// A real reading said as a band per dimension — <b><see cref="Grains"/> as a
/// front end</b>, and the half of step 8 that generalises along ONE axis.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not a stepping stone to <see cref="Winnow"/></b> and must not be
/// written up as one. Grains take a reading that has already been banded and
/// say it again more coarsely, so the hierarchy IS the similarity — but it is a
/// hierarchy per DIMENSION, and two readings differing a little in every
/// dimension share no band at any grain. <see cref="Winnow"/> reads the
/// dimensions TOGETHER. <b>They are two halves of one step</b> and neither is the
/// other's arm, so a body wanting both mounts both.
/// </para>
/// <para>
/// <b>It takes the part of the frame it reads</b>, which is why there is no
/// splitter. A body hands over everything it sensed and each front end
/// selects its own stream — see <see cref="Compound{TFrame}"/>. The selector is
/// the whole of what a router would have been.
/// </para>
/// <para>
/// <b>And it lives here rather than in the world.</b> The walk's tending world
/// banded its own moisture and called <see cref="Grains"/> itself, which is a
/// world deciding how it is coded — John's line, 2026-08-05: what the world IS
/// stays, how the brain THINKS goes. The bands and the grain are adapter
/// settings and belong on this side of it. That world is gone and the rule it
/// broke is not, which is why this paragraph outlived it.
/// </para>
/// </remarks>
/// <typeparam name="TFrame">What the body reads.</typeparam>
public sealed class Banded<TFrame> : IQuantizer<TFrame>
{
    private readonly Func<TFrame, IReadOnlyList<double>> _reading;
    private readonly byte _first;
    private readonly int _width;
    private readonly int _bands;
    private readonly int _grains;
    private readonly int _spans;

    /// <param name="reading">Which numbers of the frame this sense reads.</param>
    /// <param name="first">
    /// The modality the first dimension rides on. <b>Each dimension owns a block
    /// of them, one per grain</b>, so a coarse reading of one can never collide
    /// with a fine reading of another — see <see cref="Grains.Of"/>.
    /// </param>
    /// <param name="width">How many dimensions the reading has.</param>
    /// <param name="bands">How finely the finest grain cuts the range.</param>
    /// <param name="grains">How many times to say it again, more coarsely.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The block would not fit under 256, or the reading is not <paramref name="width"/> wide.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>The width is taken so the block can be checked.</b> And that check was the
    /// whole reason for adding it. A modality is ONE BYTE and a dimension owns
    /// <see cref="Spans"/> of them, so a reading of 128 dimensions at two spans runs
    /// out — and the arithmetic that assigns them is an unchecked cast, so it WRAPPED
    /// rather than failed. Dimension 0 and dimension 128 came out with identical
    /// codes, which means two different images were the same observation and nothing
    /// anywhere said so.
    /// </para>
    /// <para>
    /// <b>It was already known and guarded in exactly one world.</b> The walk's tending
    /// world refused more plants than its block held, and that guard sat there while
    /// <see cref="Machines.GradedRun"/> built the same type with no check at all. A
    /// defence mounted on one caller is the failure this repo keeps re-finding, so it
    /// moved here where it covers all of them — and the world it was copied from has
    /// since been deleted, which is exactly why it had to move.
    /// </para>
    /// <para>
    /// <b>And it is the ceiling on this front end reaching perception.</b> Roughly
    /// fifty dimensions at two spans is all a byte affords beside the modalities
    /// already spoken for — an eight-by-eight thumbnail and nothing wider.
    /// <see cref="Winnow"/> has no such ceiling because every code it emits rides on
    /// ONE modality, which is a structural difference between the two front ends and
    /// not a matter of degree.
    /// </para>
    /// </remarks>
    public Banded(
        Func<TFrame, IReadOnlyList<double>> reading,
        byte first,
        int width,
        int bands,
        int grains)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grains);

        var spans = Grains.Spans(bands, grains);
        var wants = first + (width * spans);

        if (wants > byte.MaxValue + 1)
            throw new ArgumentOutOfRangeException(
                nameof(width),
                $"{width} dimensions at {spans} span(s) from modality {first} needs "
                + $"{wants} modalities and a byte holds 256. Widen the grain, start the "
                + "block lower, or use a front end whose codes share one modality.");

        _reading = reading;
        _first = first;
        _width = width;
        _bands = bands;
        _grains = grains;
        _spans = spans;
    }

    /// <inheritdoc/>
    public byte Modality => _first;

    /// <summary>How many modalities each dimension uses.</summary>
    public int Spans => _spans;

    /// <inheritdoc/>
    /// <remarks>
    /// <b>The reading is clamped and never wrapped.</b> A value at or above the
    /// top of the range belongs in the top band; wrapping it would file the wettest
    /// plant with the driest, which is the one confusion a band must not make.
    /// </remarks>
    public IReadOnlyCollection<Code> Codify(TFrame observation)
    {
        var reading = _reading(observation);

        // Or the declared width is a promise nobody kept. The block was sized against
        // it at construction, so a reading that outgrows it wraps exactly as before
        // and the guard above would be decoration.
        if (reading.Count != _width)
            throw new ArgumentOutOfRangeException(
                nameof(observation),
                $"this sense was built for {_width} dimensions and was handed "
                + $"{reading.Count}.");

        var codes = ImmutableArray.CreateBuilder<Code>(reading.Count * _spans);

        for (var which = 0; which < reading.Count; which++)
        {
            var band = Math.Clamp((int)(reading[which] * _bands), 0, _bands - 1);

            codes.AddRange(
                Grains.Of((byte)(_first + (which * _spans)), band, _bands, _grains));
        }

        return codes.ToImmutable();
    }
}
