using System.Collections.Immutable;

namespace OpenPlexus.Codes;

/// <summary>
/// A real reading said as a band per dimension — <b><see cref="Grains"/> as a
/// front end, and the half of step 8 that generalises along ONE axis.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS IS NOT A STEPPING STONE TO <see cref="Winnow"/> AND MUST NOT BE
/// WRITTEN UP AS ONE.</b> Grains take a reading that has already been banded and
/// say it again more coarsely, so the hierarchy IS the similarity — but it is a
/// hierarchy per DIMENSION, and two readings differing a little in every
/// dimension share no band at any grain. <see cref="Winnow"/> reads the
/// dimensions TOGETHER. <b>They are two halves of one step and neither is the
/// other's arm</b>, so a body wanting both mounts both.
/// </para>
/// <para>
/// <b>IT TAKES THE PART OF THE FRAME IT READS, WHICH IS WHY THERE IS NO
/// SPLITTER.</b> A body hands over everything it sensed and each front end
/// selects its own stream — see <see cref="Compound{TFrame}"/>. The selector is
/// the whole of what a router would have been.
/// </para>
/// <para>
/// <b>AND IT LIVES HERE RATHER THAN IN THE WORLD.</b> <see cref="Worlds.Tending"/>
/// banded its own moisture and called <see cref="Grains"/> itself, which is a
/// world deciding how it is coded — John's line, 2026-08-05: what the world IS
/// stays, how the brain THINKS goes. The bands and the grain are adapter
/// settings and belong on this side of it.
/// </para>
/// </remarks>
/// <typeparam name="TFrame">What the body reads.</typeparam>
public sealed class Banded<TFrame> : IQuantizer<TFrame>
{
    private readonly Func<TFrame, IReadOnlyList<double>> _reading;
    private readonly byte _first;
    private readonly int _bands;
    private readonly int _grains;
    private readonly int _spans;

    /// <param name="reading">Which numbers of the frame this sense reads.</param>
    /// <param name="first">
    /// The modality the first dimension rides on. <b>Each dimension owns a block
    /// of them, one per grain</b>, so a coarse reading of one can never collide
    /// with a fine reading of another — see <see cref="Grains.Of"/>.
    /// </param>
    /// <param name="bands">How finely the finest grain cuts the range.</param>
    /// <param name="grains">How many times to say it again, more coarsely.</param>
    public Banded(
        Func<TFrame, IReadOnlyList<double>> reading, byte first, int bands, int grains)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grains);

        _reading = reading;
        _first = first;
        _bands = bands;
        _grains = grains;
        _spans = Grains.Spans(bands, grains);
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
