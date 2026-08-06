using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Learning;

/// <summary>
/// A node for the thing that would explain a group — <b>the first code here that
/// stands for something never observed.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY OTHER MINTER NAMES SOMETHING THAT WAS PRESENT.</b> <see cref="Chunk"/>
/// names a set that arrived, <see cref="Macro"/> an order that recurred,
/// <see cref="Stated"/> a relation that was stated, <c>Winnow</c> a reading that
/// was taken. This names the EXPLANATION for a group instead, and the thing it
/// stands for has no reading, was in no moment, and can be reached by no walk until
/// this puts it there.
/// </para>
/// <para>
/// <b>IT IS PRICED AS A SHORTCUT AND NEVER AS A PREDICTION, which is what makes it
/// safe where fork 21 is not.</b> A proposal that PREDICTS can be wrong about a
/// world; this says the same counts more cheaply and makes no claim about a world
/// at all — see <see cref="Paying.Cheaper"/>. It can be wasteful. It cannot be
/// false.
/// </para>
/// <para>
/// <b>AND MINTING IS INSEPARABLE FROM SUBSUMING.</b> Counts only ever rise here, so
/// adding a hub without dropping what it stands for leaves BOTH — every member's
/// row gains an entry, fan-out grows, and the description-length argument runs
/// backwards. <see cref="Subsume"/> is not an optimisation of this mechanism; it is
/// half of it.
/// </para>
/// </remarks>
public static class Posit
{
    /// <summary>
    /// The modality every posited hub is minted into.
    /// </summary>
    /// <remarks>
    /// <b>Its own, apart from <see cref="Chunk.Minted"/> and
    /// <see cref="Macro.Made"/>.</b> A walk narrowed to what a front end produced
    /// must not be answered with something nothing ever saw, which is the argument
    /// that gave a chunk its own modality and applies here with more force than
    /// anywhere: this code stands for a thing that was never there.
    /// </remarks>
    /// <remarks>
    /// <b>Private until something needs to ASK whether a code is a hub</b> — the
    /// same rule <see cref="Stated"/>'s modality follows, and the first walk that
    /// wants to rank an explanation apart from a thing is what makes it public.
    /// </remarks>
    private const byte Hub = 203;

    /// <summary>
    /// The code for the thing that would explain <paramref name="group"/>.
    /// </summary>
    /// <remarks>
    /// <b>SORTED, UNLIKE <see cref="Stated.Instance"/> AND FOR THE OPPOSITE
    /// REASON.</b> A relation instance names an ORDER — <c>gave(alice, bob)</c> is
    /// not <c>gave(bob, alice)</c> — where a hub names the SET that shares a cause,
    /// which is one fact however it is written. So this folds sorted, exactly as
    /// <c>Chunk</c> does, and every machine that finds the same group mints the
    /// same hub with nothing to ask.
    /// <para>
    /// <b>No clock, unlike an instance.</b> A hub is not an occasion; the same
    /// group discovered twice is the same explanation, and it must accumulate
    /// rather than fragment.
    /// </para>
    /// </remarks>
    public static Code Over(IReadOnlyCollection<Code> group)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (!Paying.Cheaper(group.Count))
            throw new ArgumentException(
                $"a group of {group.Count} is not cheaper said as a hub -- it would "
                + "cost a node and a row entry each to save less. See Paying.Cheaper",
                nameof(group));

        var hash = Agreed.Basis;

        foreach (var code in group.Order())
        {
            hash = Agreed.Fold(hash, code.Modality);
            hash = Agreed.Fold(hash, code.Value);
        }

        return new Code(Hub, Agreed.Mix(hash));
    }

    /// <summary>
    /// The moments that join a hub to what it explains.
    /// </summary>
    /// <remarks>
    /// <b>ONE SMALL MOMENT PER ARM, WHICH IS <see cref="Stated"/>'S RULE AND FOR
    /// ITS REASON.</b> An occasion pairs everything in it, so one moment holding
    /// the hub and every member would write member-to-member as well — re-creating
    /// by hand the very clique this exists to replace.
    /// <para>
    /// <b>Nothing here is fleeting.</b> A hub is a lasting explanation rather than
    /// an occasion, which is exactly what separates it from a relation instance.
    /// </para>
    /// </remarks>
    /// <param name="hub">The minted code.</param>
    /// <param name="group">What it explains.</param>
    /// <param name="at">The observing machine's clock.</param>
    public static ImmutableArray<Occasion> Star(
        Code hub, IReadOnlyCollection<Code> group, long at)
    {
        ArgumentNullException.ThrowIfNull(group);

        var arms = ImmutableArray.CreateBuilder<Occasion>(group.Count);

        foreach (var member in group)
            arms.Add(new Occasion { Onsets = [member, hub], Live = [], At = at });

        return arms.ToImmutable();
    }

    /// <summary>
    /// Drops the edges the hub now stands for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THIS IS THE HALF THAT MAKES THE ARITHMETIC TRUE, AND ONLY THE MINTER CAN
    /// DO IT.</b> The edges a hub subsumes are exactly those among the group, and
    /// that is known here and nowhere else. No eviction policy could find them: they
    /// are touched every moment the group occurs and carry the highest counts in the
    /// row, so neither recency nor frequency will ever drop them however redundant
    /// they have become.
    /// </para>
    /// <para>
    /// <b>BOTH SIDES, BECAUSE EACH NODE OWNS ITS OWN ROW.</b> Dropping <c>a→b</c>
    /// without <c>b→a</c> would leave a one-way edge that means <i>then</i> — see
    /// <see cref="Kind.After"/> — so a half-done subsumption does not shrink the
    /// graph, it changes what it says.
    /// </para>
    /// <para>
    /// <b>ONLY <see cref="Kind.With"/>.</b> A temporal or role cell between two
    /// members says something a hub does not stand for, and dropping it would lose
    /// a fact rather than a duplicate.
    /// </para>
    /// </remarks>
    /// <returns>How many entries were dropped.</returns>
    public static int Subsume(LocalClusters clusters, IReadOnlyCollection<Code> group)
    {
        ArgumentNullException.ThrowIfNull(clusters);
        ArgumentNullException.ThrowIfNull(group);

        var dropped = 0;

        foreach (var one in group)
            foreach (var other in group)
                if (one != other && clusters.For(one).Forget(other, Kind.With))
                    dropped++;

        return dropped;
    }
}
