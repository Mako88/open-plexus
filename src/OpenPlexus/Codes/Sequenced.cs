namespace OpenPlexus.Codes;

/// <summary>
/// Whether a moment carries what came before what — <b>rung three, and it is a code rather
/// than a matcher, which is the whole of why it is small.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE LADDER'S THIRD RUNG IS <i>X THEN Y</i> RATHER THAN <i>X AND Y</i>, AND THE OBVIOUS
/// WAY TO BUILD IT IS THE EXPENSIVE ONE.</b> A scope entry saying <i>this one came after
/// that one</i> needs a scope that is no longer an array of codes, a
/// <see cref="Commitments.Commitment.Fires"/> that is no longer a subset test, a tally
/// keyed by pairs, a repair that proposes them, a subsumption that reads them and a wire
/// format that carries them. Every one of those is in the hottest path in the project.
/// </para>
/// <para>
/// <b>SO THE ORDER IS DERIVED INTO A CODE INSTEAD, AND NOTHING DOWNSTREAM CHANGES AT
/// ALL.</b> <see cref="Commitments.Population.Moment"/> already folds minted names into what
/// the front end said, and everything after it — matching, covering, the tally, repair,
/// subsumption, naming — reads the folded moment. A precedence folded in there is matched
/// by the same subset test, tallied by the same table, chosen by the same discriminative
/// gate and named by the same hash. <b>It is the trick <see cref="Commitments.Unifying.Any"/> uses for a
/// variable and <see cref="Joining.Either"/> uses for an absence</b>, which is John's own
/// rung-two proposal moved to where it needs no new machinery.
/// </para>
/// <para>
/// <b>AND IT IS THE LEARNER THAT DERIVES IT, WHICH IS THE SEAM THIS SITS THE RIGHT SIDE
/// OF.</b> The front end reports word order through
/// <see cref="IQuantizer{TObservation}.Order"/>, which is a fact about the signal on the
/// licence <see cref="Coded.Sequence"/> already carries. Turning that into <i>these two
/// stood this way round</i> is a derivation, and a front end doing it would be deciding
/// which relations exist. A handed-over version measured 1.000 on <see cref="Worlds.Handing"/>
/// and is an instrument rather than a mechanism for exactly that reason.
/// </para>
/// <para>
/// <b>WHAT IT COSTS IS THE THING TO WATCH, AND IT IS WHY THERE ARE TWO ARMS.</b> Every
/// ordered pair of a moment is quadratic in it, which lands straight on the object the plan
/// already says blows up — a tally entry per code seen while firing. Adjacency is linear and
/// says less. Which of them a world needs is a reading rather than a preference.
/// </para>
/// </remarks>
public enum Sequencing
{
    /// <summary>
    /// No order reaches the learner. <b>The default, so every number this repo has ever
    /// recorded is reproduced by it.</b>
    /// </summary>
    Never,

    /// <summary>
    /// One code for each pair that stood NEXT TO each other, in that order.
    /// </summary>
    /// <remarks>
    /// <b>LINEAR IN THE MOMENT, AND IT IS THE LOCAL CLAIM RATHER THAN THE CHEAP ONE.</b>
    /// <i>Immediately after</i> is what a sequence is made of; <see cref="Preceding"/> is
    /// its transitive closure and says nothing adjacency does not entail. Where a world
    /// needs the closure it will show as this arm falling short, which is a reading, and
    /// where it does not the closure is a quadratic price for nothing.
    /// </remarks>
    Adjacent,

    /// <summary>
    /// One code for every pair where the first came anywhere before the second.
    /// </summary>
    /// <remarks>
    /// <b>QUADRATIC IN THE MOMENT, WHICH IS THE COST THAT DECIDES WHETHER IT SHIPS.</b> It
    /// reaches a relation between words with anything at all between them —
    /// <i>whoever was asked about was mentioned before the answer</i> — which adjacency
    /// cannot say at any distance. The tally is per code seen while firing, so squaring the
    /// moment squares the object the plan already names as the one that blows up.
    /// </remarks>
    Preceding,
}

/// <summary>The code meaning <i>this one was said before that one</i>.</summary>
/// <remarks>
/// <b>ITS OWN MODALITY, BESIDE <see cref="Commitments.Commitment.Committed"/>,
/// <see cref="Commitments.Naming.Meant"/> AND <see cref="Commitments.Unifying"/>'S.</b> A precedence is
/// DERIVED from two codes rather than emitted by a sense, so a world able to produce one
/// would be writing the learner's rules — the same reason a pattern may not be emitted.
/// </remarks>
public static class Sequenced
{
    /// <summary>The modality a precedence rides on.</summary>
    public const byte Ordered = 208;

    /// <summary>What the precedence of one code over another is called, on every machine.</summary>
    /// <param name="first">What came first.</param>
    /// <param name="second">What came after it.</param>
    /// <remarks>
    /// <b>DERIVED FROM THE PAIR AND ORDER-SENSITIVE, so two machines reach the same code
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
    /// <b>WHAT GENESIS READS, AND IT IS THE ONE GATE THIS RUNG NEEDS.</b> A precedence is a
    /// SPECIALISATION and never a root: <i>this stood before that</i> with no idea what
    /// either of them is about is a rule about grammar rather than about the world, and a
    /// population rooting on them fills with pairs the moment order arrives. See
    /// <see cref="Commitments.Population.Cover"/>, where the same argument already keeps a
    /// never-absent code from being a root.
    /// </remarks>
    public static bool Names(Code code) => code.Modality == Ordered;

    /// <summary>Every precedence a moment's order entails, under one arm.</summary>
    /// <param name="order">What position each code was at, from the front end.</param>
    /// <param name="sequencing">Which arm.</param>
    /// <remarks>
    /// <b>SORTED BY POSITION FIRST, BECAUSE A DICTIONARY'S ORDER DOES NOT SURVIVE A RUN.</b>
    /// The codes come back in whatever order the map walks, and a moment whose contents
    /// depended on that would be a difference nobody chose — which is what
    /// <c>DeterminismTests</c> exists to refuse. Ties are broken by the code so that two
    /// front ends reporting one position for two codes agree about what that means.
    /// </remarks>
    public static IEnumerable<Code> From(
        IReadOnlyDictionary<Code, int> order, Sequencing sequencing)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (sequencing == Sequencing.Never || order.Count < 2) yield break;

        var placed = order.OrderBy(one => one.Value).ThenBy(one => one.Key).ToList();

        for (var first = 0; first < placed.Count - 1; first++)
        {
            var last = sequencing == Sequencing.Adjacent
                ? Math.Min(first + 2, placed.Count)
                : placed.Count;

            for (var second = first + 1; second < last; second++)
            {
                // A CODE NEVER PRECEDES ITSELF, and a front end reporting one code at two
                // positions is the case that makes that reachable -- a word said twice in
                // one sentence. Folding it in would put a code in every moment the word
                // appears twice in, which is a background code by a side door.
                if (placed[first].Key == placed[second].Key) continue;

                yield return Of(placed[first].Key, placed[second].Key);
            }
        }
    }
}
