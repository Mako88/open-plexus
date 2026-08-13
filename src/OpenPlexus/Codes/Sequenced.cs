namespace OpenPlexus.Codes;

/// <summary>
/// What came immediately before what, as a code — <b>rung three, and it is a code rather
/// than a matcher, which is the whole of why it is small.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE LADDER'S THIRD RUNG IS <i>X THEN Y</i> RATHER THAN <i>X AND Y</i>, and the obvious
/// way to build it is the expensive one.</b> A scope entry saying <i>this one came after
/// that one</i> needs a scope that is no longer an array of codes, a
/// <see cref="Commitments.Commitment.Fires"/> that is no longer a subset test, a tally keyed
/// by pairs, a repair that proposes them, a subsumption that reads them and a wire format
/// that carries them. Every one of those is in the hottest path in the project.
/// </para>
/// <para>
/// <b>So the order is derived into a code instead, and nothing downstream changes at
/// all.</b> Matching, the tally, the discriminative gate, naming, subsumption and the wire
/// all read a set of codes, and a precedence IS one — so it is matched by the same subset
/// test and chosen by the same repair. <b>It is the trick
/// <see cref="Commitments.Unifying.Any"/> uses for a variable and
/// <see cref="Joining.Either"/> uses for an absence</b>, which is John's own rung-two
/// proposal moved to where it needs no new machinery.
/// </para>
/// <para>
/// <b>And it is the learner that derives it, which is the seam this sits the right side
/// of.</b> The front end reports order through
/// <see cref="IQuantizer{TObservation}.Order"/>, which is a fact about the signal on the
/// licence <see cref="Coded.Sequence"/> already carries. Turning that into <i>these two
/// stood this way round</i> is a derivation, and a front end doing it would be deciding
/// which relations exist.
/// </para>
/// <para>
/// <b>There is no dial, and that is a correction rather than the design.</b> This shipped
/// for one commit as a three-armed setting defaulting to OFF, justified by every recorded
/// number being reproduced — which is what this repo's own rules forbid twice over: an arm
/// only lives while it is compared, and a better brain beats intact numbers. The transitive
/// closure LOST its comparison on cost and is deleted with a revival row; with one arm left
/// there is nothing to switch. <b>It is inert wherever a front end reports no order, which
/// is a fact about the sense rather than a setting on the brain.</b>
/// </para>
/// </remarks>
public static class Sequenced
{
    /// <summary>The modality a precedence rides on.</summary>
    /// <remarks>
    /// <b>ITS OWN, BESIDE <see cref="Commitments.Commitment.Committed"/>,
    /// <see cref="Commitments.Naming.Meant"/> AND <see cref="Commitments.Unifying"/>'S.</b> A
    /// precedence is DERIVED from two codes rather than emitted by a sense, so a world able
    /// to produce one would be writing the learner's rules — the same reason a pattern may
    /// not be emitted.
    /// </remarks>
    public const byte Ordered = 208;

    /// <summary>What the precedence of one code over another is called, on every machine.</summary>
    /// <param name="first">What came first.</param>
    /// <param name="second">What came after it.</param>
    /// <remarks>
    /// <b>Derived from the pair and order-sensitive, so two machines reach the same code
    /// with nothing to ask</b> — the property every code here has. <see cref="Agreed"/>
    /// rather than <see cref="object.GetHashCode"/>, which is randomised per process, so a
    /// codebook resting on it would mean two machines quietly disagreeing about what a
    /// precedence IS.
    /// </remarks>
    public static Code Of(Code first, Code second)
    {
        var hash = Agreed.Fold(Agreed.Basis, first.Modality);

        hash = Agreed.Fold(hash, first.Value);
        hash = Agreed.Fold(hash, second.Modality);
        hash = Agreed.Fold(hash, second.Value);

        return new Code(Ordered, Agreed.Mix(hash));
    }

    /// <summary>Whether a code is a precedence rather than something a sense reported.</summary>
    /// <param name="code">The code to ask about.</param>
    /// <remarks>
    /// <b>What genesis reads, and it is the one gate this rung needs.</b> A precedence is a
    /// SPECIALISATION and never a root: <i>this stood before that</i> with no idea what
    /// either of them is about is a rule about grammar rather than about the world, and a
    /// population rooting on them fills with pairs the moment order arrives. See
    /// <see cref="Commitments.Population.Cover"/>, where the same argument already keeps a
    /// never-absent code from being a root.
    /// </remarks>
    public static bool Names(Code code) => code.Modality == Ordered;

    /// <summary>Every immediate precedence a moment's order entails.</summary>
    /// <param name="order">What position each code was at, from the front end.</param>
    /// <remarks>
    /// <para>
    /// <b>By distinct position and not by list index, which is where the first version was
    /// wrong.</b> A front end may report several codes at ONE position —
    /// the deleted <c>SnakeSense</c> put the action at nought and its whole view at one —
    /// and walking a sorted list pairwise would emit <i>this view code came before that
    /// one</i>, which the front end never said and which is false. Positions are grouped
    /// first, and every pair across two CONSECUTIVE groups is emitted.
    /// </para>
    /// <para>
    /// <b>Immediate and not transitive, and the closure is deleted rather than optional.</b>
    /// It reached the identical ceiling on <see cref="Worlds.Handing"/> for two and a half
    /// times the population. The revival row in the plan says what would bring it back: a
    /// world whose relation spans an intervening position, where this falls short.
    /// </para>
    /// <para>
    /// <b>Sorted, because a dictionary's order does not survive a run.</b> A moment whose
    /// contents depended on how a map happened to walk would be a difference nobody chose,
    /// which is what <c>DeterminismTests</c> exists to refuse.
    /// </para>
    /// </remarks>
    public static IEnumerable<Code> From(IReadOnlyDictionary<Code, int> order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Count < 2) yield break;

        var groups = order
            .GroupBy(one => one.Value)
            .OrderBy(group => group.Key)
            .Select(group => group.Select(one => one.Key).Order().ToList())
            .ToList();

        for (var at = 0; at < groups.Count - 1; at++)
        {
            foreach (var first in groups[at])
            {
                foreach (var second in groups[at + 1])
                {
                    // A CODE NEVER PRECEDES ITSELF, which is reachable where a front end
                    // reports one code at two positions -- a word said twice. Folding it in
                    // would put a code in every moment that word repeats in, which is a
                    // background code by a side door.
                    if (first == second) continue;

                    yield return Of(first, second);
                }
            }
        }
    }
}
