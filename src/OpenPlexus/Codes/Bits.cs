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
/// </remarks>
public sealed class Bits : IQuantizer<IReadOnlyList<int>>
{
    private readonly byte _modality;

    /// <param name="modality">The modality these codes ride on.</param>
    public Bits(byte modality) => _modality = modality;

    /// <inheritdoc/>
    public byte Modality => _modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(IReadOnlyList<int> observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var codes = ImmutableArray.CreateBuilder<Code>(observation.Count);

        for (var which = 0; which < observation.Count; which++)
            codes.Add(Of(_modality, which, observation[which]));

        return codes.ToImmutable();
    }

    /// <summary>The code for one position holding one value.</summary>
    /// <param name="modality">The modality these codes ride on.</param>
    /// <param name="position">Which position.</param>
    /// <param name="value">What it holds.</param>
    public static Code Of(byte modality, int position, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        return new Code(modality, (ulong)((position << 1) | value));
    }

    /// <summary>Which position a code stands for.</summary>
    /// <param name="code">A code this made.</param>
    public static int Position(Code code) => (int)(code.Value >> 1);

    /// <summary>What value a code stands for.</summary>
    /// <param name="code">A code this made.</param>
    public static int Value(Code code) => (int)(code.Value & 1);
}
