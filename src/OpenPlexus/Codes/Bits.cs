using System.Collections.Immutable;

namespace OpenPlexus.Codes;

/// <summary>
/// A row of whole numbers said as one code per position and value.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE SIMPLEST TRANSLATION THERE IS, AND IT LIVES HERE RATHER THAN IN A
/// WORLD.</b> A world that minted its own codes would be deciding what the brain
/// perceives, which is the mixing this arrangement exists to prevent — so even the
/// trivial pipe is a pipe.
/// </para>
/// <para>
/// <b>One code per (position, value) and never one per position.</b> A code standing
/// for a position alone could only say *this position exists*, which is true of every
/// reading and separates nothing.
/// </para>
/// <para>
/// <b>AND THE PACKING NEEDED A WIDTH, WHICH IT DID NOT HAVE.</b> This said <i>whole
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
    private readonly byte? _coarse;

    /// <param name="modality">The modality these codes ride on.</param>
    /// <param name="stride">
    /// One more than the largest value a position may hold. <b>Two, which is a bit.</b>
    /// </param>
    /// <param name="coarse">
    /// A second modality to emit the POSITION on with its value thrown away, or nothing to
    /// emit one code a reading as this always has.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>THE COARSE CODE IS A COARSER CUT ALONG THE SAME AXIS, WHICH IS THE ONLY KIND OF
    /// FRONT-END CHANGE THIS DESIGN PERMITS.</b> Fixing a failure by changing the feature
    /// BASIS is refused outright — a new feature is a minted name above the codes, never a
    /// new thing to read. Reading the same position less finely is resolution, and
    /// resolution is what a front end is allowed to vary.
    /// </para>
    /// <para>
    /// <b>AND WHAT IT BUYS IS A SHARED PART WHERE THERE WAS NONE.</b> <i>Bit three is
    /// zero</i> and <i>bit three is one</i> have nothing in common while a code carries
    /// position and value fused together, so rung five cannot name the thing they share —
    /// and the multiplexer's whole concept is <i>these positions are the address, whatever
    /// they say</i>. With this the shared part IS a code, and naming can reach it.
    /// </para>
    /// <para>
    /// <b>WHAT IT COSTS IS SEARCH, SAID BEFORE IT IS RUN.</b> Every moment carries twice the
    /// codes, so the repair table is twice as wide and every candidate count doubles — which
    /// loosens no bar, because the correction divides by the candidates considered. It is a
    /// cost in time and memory rather than in the gate's honesty.
    /// </para>
    /// </remarks>
    public Bits(byte modality, int stride = 2, byte? coarse = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, 2);

        _modality = modality;
        _stride = stride;
        _coarse = coarse;
    }

    /// <inheritdoc/>
    public byte Modality => _modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(IReadOnlyList<int> observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var codes = ImmutableArray.CreateBuilder<Code>(
            observation.Count * (_coarse is null ? 1 : 2));

        for (var which = 0; which < observation.Count; which++)
        {
            codes.Add(Of(_modality, which, observation[which], _stride));

            // THE POSITION ITSELF, WHICH IS THE SAME READING CUT LESS FINELY. It rides on its
            // own modality rather than on a spare value of this one, because a code's meaning
            // has to be recoverable from it forever and packing two kinds of thing into one
            // block is how a byte wrapped and made two pictures one observation.
            if (_coarse is { } position) codes.Add(new Code(position, (ulong)which));
        }

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

        // THE COLLISION, REFUSED RATHER THAN PRODUCED. A value at or above the stride
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
