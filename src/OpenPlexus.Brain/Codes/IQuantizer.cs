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
/// <b>Which is why a quantiser is built from the shared seed</b> and never fitted
/// to data. Two quantisers fitted on different samples of one stream agree
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
    /// <b>And <c>Commitments.Spanning</c> is what reads it</b>, which was not true for most
    /// of this branch. A commitment's scope is a set of codes and has nowhere to put a group,
    /// so the grouping is read beside the scope rather than inside it: genesis mints one scope
    /// over each thing, and no scope fires across two.
    /// </para>
    /// <para>
    /// <b>What a THING is stays the world's.</b> The mechanism cannot tell two answers
    /// apart: a part is an object in a scene on one world and a part of a sentence on
    /// another, and both arrive down this channel as an integer. That is one name over two
    /// ideas, which is the fault this repo has already been bitten by, and no check can reach
    /// it because what a grouping MEANS is a judgement about the world.
    /// </para>
    /// </remarks>
    IReadOnlyDictionary<Code, int>? Bind(TObservation observation) => null;

    /// <summary>
    /// What order those codes came in, when this front end can say. <b>Null by
    /// default</b>, which is every front end for which nothing came first.
    /// </summary>
    /// <remarks>
    /// <b>The order is a front-end job for the same reason segmentation is.</b>
    /// A phase cannot survive C2 — late, jittered, out-of-order messages are
    /// exactly what destroys an oscillator relationship — so the order has to
    /// travel INSIDE the moment, where lateness cannot reach it. Only the front end
    /// knows it: by the time codes reach a population they are a set. <b>Read by
    /// <c>Machines.Watching</c></b>, which turns it into the precedence codes rung
    /// three is made of — the seam <see cref="Forced"/> now shares. Defaulted so that adding it breaks
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
    /// <b>Its reader was the walk's intervened edge kind</b>, and it had none for the life
    /// of this branch — see <see cref="Bind"/>. Both halves were missing and they went in
    /// order. <i>I picked this without looking at the state</i> was unsayable while every
    /// world was watched; <c>Worlds.IActed</c> is that call, and
    /// <c>Worlds.Roaming</c> is the world that says it.
    /// </para>
    /// <para>
    /// <b>And the reader is <see cref="Intervened"/></b>, on rung three's seam. A moment
    /// carries a derived code beside each forced one, so a scope naming the plain code fires
    /// either way and a scope naming the derived one fires only under intervention — which
    /// makes the causal claim and the observational one two different scopes with their own
    /// statistics, where they used to be one scope with the evidence added together. Genesis
    /// may not root on one, exactly as it may not root on a precedence.
    /// </para>
    /// <para>
    /// <b>Repair reaches for it</b>, which is the half that had to be measured. A derivation
    /// nothing selects would be a channel with a reader that reads nothing, which is the same
    /// fault one layer in. On <c>Worlds.Roaming</c>'s effect question under a chooser
    /// that acts on a coin, 193 of 318 resident scopes name a doing — see
    /// <c>RoamingTests</c>, where the always-acting arm is printed beside it because acting
    /// every round makes provenance and <i>in the question</i> the same fact.
    /// </para>
    /// </remarks>
    IReadOnlySet<Code>? Forced(TObservation observation) => null;
}
