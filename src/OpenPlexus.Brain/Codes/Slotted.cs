namespace OpenPlexus.Codes;

/// <summary>
/// A row of whole numbers said one modality a position, so a value stands alone.
/// </summary>
/// <remarks>
/// <para>
/// <b>The other spelling of <see cref="Bits"/></b>, and the difference is what a variable
/// can reach. That one packs the position into the value, so position nought holding
/// <c>2</c> and position one holding <c>2</c> are two different code values under one
/// modality. This puts the position in the modality and leaves the value where it is, so
/// the two are the same value under two modalities.
/// </para>
/// <para>
/// <b>Which is the shape <c>Commitments.Unifying</c> matches.</b> A variable is
/// <i>whichever code of this modality</i>, and two entries carrying one name have to be
/// filled by one value. So <i>this position and that one hold the same thing</i> is a scope
/// under this spelling and is unsayable under the other, whatever the learner does.
/// </para>
/// <para>
/// <b>It costs a modality a position</b>, which is the reason it is not simply better. A
/// reading of any width would exhaust the byte, so this suits a row that is short and
/// declared — six attributes rather than a thousand pixels.
/// </para>
/// </remarks>
public sealed class Slotted : IQuantizer<IReadOnlyList<int>>
{
    private readonly byte _first;
    private readonly int _positions;

    /// <param name="first">The modality position nought rides on.</param>
    /// <param name="positions">
    /// How many positions a reading has. <b>Declared rather than taken from the first
    /// reading</b>, so a short row cannot quietly claim fewer modalities than the next one
    /// needs.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The positions do not fit above <paramref name="first"/> inside a byte.
    /// </exception>
    public Slotted(byte first, int positions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(positions);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(first + positions - 1, byte.MaxValue);

        _first = first;
        _positions = positions;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Position nought's, and it is the first of <see cref="Span"/> in a row.</b> A
    /// spelling with a modality a position has no single one, exactly as
    /// <see cref="Compound{TFrame}"/> has none; what needs to know reads
    /// <see cref="Code.Modality"/>.
    /// </remarks>
    public byte Modality => _first;

    /// <summary>How many modalities this spelling claims, starting at <see cref="Modality"/>.</summary>
    public int Span => _positions;

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The reading is wider than the positions declared, which would land two of them on one
    /// modality — the aliasing <see cref="Bits"/> carried once, arriving here by the other road.
    /// </exception>
    public IReadOnlyCollection<Code> Codify(IReadOnlyList<int> observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(observation.Count, _positions);

        var codes = new Code[observation.Count];

        for (var which = 0; which < observation.Count; which++)
            codes[which] = Of(_first, which, observation[which]);

        return codes;
    }

    /// <summary>The code for one position holding one value.</summary>
    /// <param name="first">The modality position nought rides on.</param>
    /// <param name="position">Which position.</param>
    /// <param name="value">What it holds.</param>
    public static Code Of(byte first, int position, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(first + position, byte.MaxValue);

        return new Code((byte)(first + position), (ulong)value);
    }

    /// <summary>Which position a code stands for.</summary>
    /// <param name="first">The modality position nought rides on.</param>
    /// <param name="code">A code this made.</param>
    public static int Position(byte first, Code code) => code.Modality - first;

    /// <summary>What value a code stands for.</summary>
    /// <param name="code">A code this made.</param>
    public static int Value(Code code) => (int)code.Value;
}
