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
    /// What order those codes came in, when this front end can say. <b>Null by
    /// default, which is every front end for which nothing came first.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE ORDER IS A FRONT-END JOB FOR THE SAME REASON SEGMENTATION IS.</b>
    /// A phase cannot survive C2 — late, jittered, out-of-order messages are
    /// exactly what destroys an oscillator relationship — so the order has to
    /// travel INSIDE the occasion, where lateness cannot reach it. Only the front
    /// end knows it: by the time codes are in the graph they are a set. See
    /// <see cref="Learning.Occasion.Sequence"/>. Defaulted so that adding it
    /// breaks no existing quantiser and changes no existing measurement.
    /// </remarks>
    IReadOnlyDictionary<Code, int>? Order(TObservation observation) => null;

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

    /// <summary>
    /// Which relation this observation STATES, when this front end can say.
    /// <b>Null by default, which is every front end that observes things rather
    /// than relations between them.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE FIFTH CHANNEL, AND IT WAS THE ONE WITH NO ROUTE.</b>
    /// <see cref="Learning.Occasion.Roles"/> and <see cref="Graph.Kind.Role"/> were
    /// both built and neither could be reached: no method here produced them and
    /// <see cref="Coded"/> carried the other four, so nothing in the library ever
    /// wrote a role cell and every number measuring one came from an occasion a
    /// test constructed by hand. See <see cref="Filling"/> for the half that names
    /// the slots.
    /// </remarks>
    Graph.Kind? Relating(TObservation observation) => null;

    /// <summary>
    /// Which SLOT of that relation each code fills. <b>Null by default, and it
    /// needs <see cref="Relating"/> to mean anything.</b>
    /// </summary>
    /// <remarks>
    /// <b>IT SAYS WHAT IS BEING LOOKED AT AND NEVER WHAT TO CONCLUDE, which is the
    /// line every one of the other four stays on.</b> <i>This code fills slot two</i>
    /// is an observation a front end genuinely has — a parser knows its subject
    /// from its object, and a body knows which hand it reached with.
    /// <i>south-of is north-of reversed</i> is the fact under test, and handing that
    /// over would make the whole thing a lookup table.
    /// <para>
    /// <b>What it buys is a cell naming no argument</b>, so the same cell
    /// accumulates across every pair the relation was ever seen on and applies to
    /// pairs never seen — see <see cref="Graph.Kind.Role"/>.
    /// </para>
    /// </remarks>
    IReadOnlyDictionary<Code, int>? Filling(TObservation observation) => null;

    /// <summary>
    /// Which codes were ASSIGNED rather than selected. <b>Null by default, which
    /// is every occasion this design has ever written.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE SIXTH CHANNEL, AND IT OWES AN ARGUMENT AGAINST THE OTHER FIVE.</b>
    /// None of them can carry it: grouping is about objects, fleetingness about
    /// recurrence, order about time, and the two relation channels about what a
    /// moment STATES. This is about how the moment came to happen at all, which is
    /// a fact nothing in the moment records. See
    /// <see cref="Learning.Occasion.Forced"/> and <see cref="Graph.Kind.Meddled"/>
    /// — it is the difference between <c>P(y | x)</c> and <c>P(y | do(x))</c>, and
    /// no amount of counting the first yields the second.
    /// <para>
    /// <b>It stays on the line the other five stay on.</b> <i>I picked this without
    /// looking at the state</i> is something the body knows about what it did;
    /// what follows from it is left to the walk.
    /// </para>
    /// </remarks>
    IReadOnlySet<Code>? Forced(TObservation observation) => null;
}
