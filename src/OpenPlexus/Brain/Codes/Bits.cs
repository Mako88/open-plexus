using System.Collections.Immutable;

namespace OpenPlexus.Codes;

/// <summary>
/// A row of whole numbers said as one code per position and value.
/// </summary>
/// <remarks>
/// <para>
/// <b>The simplest translation there is</b>, and it lives here rather than in a
/// world. A world that minted its own codes would be deciding what the brain
/// perceives, which is the mixing this arrangement exists to prevent — so even the
/// trivial pipe is a pipe.
/// </para>
/// <para>
/// <b>One code per (position, value) and never one per position.</b> A code standing
/// for a position alone could only say *this position exists*, which is true of every
/// reading and separates nothing.
/// </para>
/// <para>
/// <b>And the packing needed a width, which it did not have.</b> This said <i>whole
/// numbers</i> and packed <c>(position &lt;&lt; 1) | value</c>, so position one holding
/// nought and position nought holding two were THE SAME CODE — two attributes silently
/// conflated, and a learner blamed for it. Nothing had ever caught it because the only
/// caller was <see cref="Worlds.Multiplexer"/>, whose values are bits: the repo's own
/// trap about a guard mounted on one caller, arriving in the packing rather than in a
/// guard.
/// </para>
/// <para>
/// <b>So the stride is declared and the value is checked against it</b>, and it defaults
/// to two — which is byte-for-byte the arithmetic that was there, so no measurement
/// taken on a binary world moves.
/// </para>
/// </remarks>
public sealed class Bits : IQuantizer<IReadOnlyList<int>>
{
    private readonly byte _modality;
    private readonly int _stride;

    /// <param name="modality">The modality these codes ride on.</param>
    /// <param name="stride">
    /// One more than the largest value a position may hold. <b>Two, which is a bit.</b>
    /// </param>
    /// <remarks>
    /// <b>One code a reading</b>, and a second coarser one was built and deleted. Emitting
    /// the position with its value thrown away made the shared part of <i>bit three is
    /// zero</i> and <i>bit three is one</i> into a code — and it reached no scope, because
    /// genesis refuses a code that has never been absent and repair refuses one that
    /// separates nothing. See the plan's revival row: what is wanted is the coarser view
    /// where pairs are COUNTED, not where readings are emitted.
    /// </remarks>
    public Bits(byte modality, int stride = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, 2);

        _modality = modality;
        _stride = stride;
    }

    /// <inheritdoc/>
    public byte Modality => _modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(IReadOnlyList<int> observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var codes = ImmutableArray.CreateBuilder<Code>(observation.Count);

        for (var which = 0; which < observation.Count; which++)
            codes.Add(Of(_modality, which, observation[which], _stride));

        return codes.ToImmutable();
    }

    /// <summary>The code for one position holding one value.</summary>
    /// <param name="modality">The modality these codes ride on.</param>
    /// <param name="position">Which position.</param>
    /// <param name="value">What it holds.</param>
    /// <param name="stride">One more than the largest value a position may hold.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value does not fit the stride, which is the collision said out loud.
    /// </exception>
    public static Code Of(byte modality, int position, int value, int stride = 2)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, 2);

        // The collision, refused rather than produced. A value at or above the stride
        // lands on the next position's block, and the two readings become one
        // observation -- which is the `Tending` fault exactly: a byte that wrapped and
        // made two different pictures the same thing, silently.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, stride);

        return new Code(modality, (ulong)((position * stride) + value));
    }

    /// <summary>Which position a code stands for.</summary>
    /// <param name="code">A code this made.</param>
    /// <param name="stride">The stride it was made with.</param>
    public static int Position(Code code, int stride = 2) => (int)code.Value / stride;

    /// <summary>What value a code stands for.</summary>
    /// <param name="code">A code this made.</param>
    /// <param name="stride">The stride it was made with.</param>
    public static int Value(Code code, int stride = 2) => (int)code.Value % stride;
}
