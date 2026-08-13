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
    /// <para>
    /// <b>Segmentation is a front-end job.</b> A retina hands the cortex an
    /// already-grouped signal; nothing downstream has to work out which edges
    /// belonged to which object. Defaulted so that adding it breaks no existing
    /// quantiser and changes no existing measurement.
    /// </para>
    /// <para>
    /// <b>And nothing reads it since the walk went, which makes it a channel with no
    /// far end.</b> The walk's occasion was the only consumer; a commitment's scope is a
    /// SET of codes and has nowhere to put a group. It stays only until something
    /// decides between wiring it to rung four's binding and deleting it — and a front-end
    /// ability nothing can act on is the shape this repo keeps finding read as built.
    /// </para>
    /// </remarks>
    IReadOnlyDictionary<Code, int>? Bind(TObservation observation) => null;

    /// <summary>
    /// What order those codes came in, when this front end can say. <b>Null by
    /// default, which is every front end for which nothing came first.</b>
    /// </summary>
    /// <remarks>
    /// <b>The order is a front-end job for the same reason segmentation is.</b>
    /// A phase cannot survive C2 — late, jittered, out-of-order messages are
    /// exactly what destroys an oscillator relationship — so the order has to
    /// travel INSIDE the moment, where lateness cannot reach it. Only the front end
    /// knows it: by the time codes reach a population they are a set. <b>The one of
    /// these four with a reader</b> — <see cref="Machines.Trial{TSeen}"/> turns it into
    /// the precedence codes rung three is made of. Defaulted so that adding it breaks
    /// no existing quantiser and changes no existing measurement.
    /// </remarks>
    IReadOnlyDictionary<Code, int>? Order(TObservation observation) => null;

    /// <summary>
    /// Which of those codes name <i>this occasion</i> rather than a kind of
    /// thing, when this front end can say. <b>Null by default, which is every
    /// front end whose codes all recur.</b>
    /// </summary>
    /// <remarks>
    /// <b>Only the front end can know this</b>, for the same reason only it can
    /// segment: a code is opaque downstream, and "this one will never be seen again"
    /// is a fact about how it was minted. Defaulted so that adding it breaks no
    /// existing quantiser and changes no existing measurement.
    /// <para>
    /// <b>And it has no reader either — see <see cref="Bind"/>.</b> The walk's
    /// rendezvous refused to write an edge onto a code that would never recur;
    /// nothing on this side has the equivalent, and a commitment that never fires
    /// twice is disposed of by its own statistics rather than by being marked.
    /// </para>
    /// </remarks>
    IReadOnlySet<Code>? Fleeting(TObservation observation) => null;

    /// <summary>
    /// Which codes were ASSIGNED rather than selected. <b>Null by default, which
    /// is every occasion this design has ever written.</b>
    /// </summary>
    /// <remarks>
    /// <b>The channel that owes an argument against the others.</b> None of them can
    /// carry it: grouping is about objects, fleetingness about recurrence, and order
    /// about time. This is about how the moment came to happen at all, which is a fact
    /// nothing in the moment records — the difference between <c>P(y | x)</c> and
    /// <c>P(y | do(x))</c>, and no amount of counting the first yields the second.
    /// <para>
    /// <b>And its reader was the walk's intervened edge kind, so it has none — see
    /// <see cref="Bind"/>.</b> <i>I picked this without looking at the state</i> is
    /// something a body knows about what it did; nothing here can be told it, which is
    /// the same hole the plan records under <i>original thought</i>: every world is
    /// watched rather than acted in.
    /// </para>
    /// </remarks>
    IReadOnlySet<Code>? Forced(TObservation observation) => null;
}
