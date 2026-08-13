namespace OpenPlexus.Codes;

/// <summary>
/// A discrete reading said as one code — <b>the identity quantiser, for a stream
/// whose values have no similarity structure at all.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>WHICH TILE THE BODY STANDS ON IS NOT A MAGNITUDE.</b> Banding it would
/// claim tile 3 is nearer tile 4 than tile 40, and hashing it would claim the
/// same by another road — but a position index is a NAME, and the graph is what
/// learns which names go together. <b>A quantiser transports similarity and never
/// invents it</b>, so where there is none to transport the honest front end is
/// identity.
/// </para>
/// <para>
/// <b>IT IS NOT <see cref="Passthrough"/>, and the difference is which side of the
/// line the modality sits on.</b> Passthrough takes codes a world already minted;
/// this takes a NUMBER and mints the code itself, so the world hands over a fact
/// about its state and the brain decides what code carries it.
/// </para>
/// </remarks>
/// <typeparam name="TFrame">What the body reads.</typeparam>
public sealed class Marked<TFrame> : IQuantizer<TFrame>
{
    private readonly Func<TFrame, int?> _reading;
    private readonly byte _modality;

    /// <param name="reading">
    /// Which discrete value of the frame this sense reads. <b>Null is a stream
    /// that said nothing this moment</b>, which is an act not taken rather than an
    /// act taken with no name — a code that is absent cannot be in any scope, and a
    /// code standing for <i>nothing happened</i> would be in every one.
    /// </param>
    /// <param name="modality">The modality its code rides on.</param>
    public Marked(Func<TFrame, int?> reading, byte modality)
    {
        ArgumentNullException.ThrowIfNull(reading);

        _reading = reading;
        _modality = modality;
    }

    /// <inheritdoc/>
    public byte Modality => _modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(TFrame observation) =>
        _reading(observation) is { } value ? [new Code(_modality, (ulong)value)] : [];
}
