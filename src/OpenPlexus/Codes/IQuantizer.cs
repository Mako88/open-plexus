namespace OpenPlexus.Codes;

/// <summary>
/// Turns one raw observation into the codes present in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The same input produces the same codes on every machine, forever.</b>
/// This is the red-ball property: a red ball seen today must produce what a
/// red ball produced last year, and on another machine, and from a PNG rather
/// than a JPEG.
/// </para>
/// <para>
/// <b>Which is why a quantiser is built from the shared seed and never fitted
/// to data.</b> Two quantisers fitted on different samples of one stream agree
/// about under 0.12 of items, and no amount of walking recovers that — two
/// machines would file the same red ball under different codes.
/// </para>
/// </remarks>
/// <typeparam name="TObservation">What this front end reads.</typeparam>
public interface IQuantizer<in TObservation>
{
    /// <summary>Which front end this is. Two modalities never collide.</summary>
    byte Modality { get; }

    /// <summary>
    /// The codes present in this observation. Several, not one.
    /// </summary>
    IReadOnlyCollection<Code> Codify(TObservation observation);
}
