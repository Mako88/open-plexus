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

    /// <summary>
    /// Which of those codes belong to which thing, when this front end can say.
    /// <b>Null by default, which is every front end that cannot.</b>
    /// </summary>
    /// <remarks>
    /// <b>Segmentation is a front-end job, not a graph job</b> — see
    /// <see cref="Learning.Occasion.Groups"/>. A retina hands the cortex an
    /// already-grouped signal; nothing downstream has to work out which edges
    /// belonged to which object. Defaulted so that adding it breaks no existing
    /// quantiser and changes no existing measurement.
    /// </remarks>
    IReadOnlyDictionary<Code, int>? Bind(TObservation observation) => null;

    /// <summary>
    /// Which of those codes name <i>this occasion</i> rather than a kind of
    /// thing, when this front end can say. <b>Null by default, which is every
    /// front end whose codes all recur.</b>
    /// </summary>
    /// <remarks>
    /// <b>Only the front end can know this</b>, for the same reason only it can
    /// segment: a code is opaque to the graph, and "this one will never be seen
    /// again" is a fact about how it was minted. See
    /// <see cref="Learning.Occasion.Fleeting"/> for what the rendezvous does with
    /// it. Defaulted so that adding it breaks no existing quantiser and changes
    /// no existing measurement.
    /// </remarks>
    IReadOnlySet<Code>? Fleeting(TObservation observation) => null;
}
