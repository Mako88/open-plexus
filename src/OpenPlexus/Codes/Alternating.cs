namespace OpenPlexus.Codes;

/// <summary>
/// Codes that are ALTERNATIVES, found in what was seen rather than handed over — <b>fork
/// 97's category, derived.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, and it is two clauses rather than one.</b> His account is a set of things
/// standing in the same relations to everything else; the clause that has to go with it is
/// that they NEVER CO-OCCUR. Without it the same rule groups a wheel and a door — both
/// stand in relations to a car — and that is a co-firing bundle, which is rung five and
/// folds the opposite way.
/// </para>
/// <para>
/// <b>So the test is exclusion and shared company, and both are facts about the moments.</b>
/// Nothing here reads a world's tables, a vocabulary or an outcome. Two codes that have
/// never been seen together and that keep the same company are alternatives, and that is
/// arithmetic over what arrived.
/// </para>
/// <para>
/// <b>And it is why the individual and the category are one mechanism rather than two.</b>
/// The looks a thing wears across sightings never co-occur — a basket at two moments does
/// not co-occur with itself, which is what makes rung five structurally unable to reach it —
/// and they keep the same company by construction. So a thing's appearances ARE a category,
/// and minting one is what collapses a rule per appearance into a rule.
/// </para>
/// <para>
/// <b>What it cannot do, said before anything reads it: it cannot separate two things that
/// are substitutable.</b> Twins wear one look by construction, so every statistic over the
/// moments is the same for both and a proposal covering both is what this returns. That is
/// not a fault to repair here — it is exactly what a PAYING gate is for, and the proposal
/// being wrong is how the gate finds out.
/// </para>
/// </remarks>
public static class Alternating
{
    /// <summary>
    /// The groups of alternatives in a stream of moments.
    /// </summary>
    /// <param name="moments">What was seen, each moment a set of codes.</param>
    /// <param name="company">
    /// How much of a code's company must be shared before two codes count as keeping the
    /// same. <b>A dial, and it is the front end's rather than the brain's</b> — what counts
    /// as the same company is a fact about how finely a stream is being read, which is the
    /// same standing <see cref="Winnow"/>'s resolution carries.
    /// </param>
    /// <param name="floor">
    /// How many times a code must have been seen before it may join a group. <b>Because a
    /// code seen once has never co-occurred with anything and keeps whatever company it
    /// arrived in</b>, so it clears both clauses trivially and would group with everything.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Greedy, and its order is the codes' own rather than the stream's.</b> Two machines
    /// seeing the same moments in different orders must reach the same groups or a category
    /// means one thing here and another there — the rule <c>Agreed</c> stands on, and the
    /// reason a fitted quantiser is refused.
    /// </para>
    /// <para>
    /// <b>A member must clear both clauses against every member already in, never against
    /// the first alone.</b> A chain of pairwise-similar codes reaches arbitrarily far, which
    /// is single-link clustering's own failure and would return one group holding the whole
    /// alphabet.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlySet<Code>> From(
        IEnumerable<IReadOnlySet<Code>> moments, double company, int floor)
    {
        ArgumentNullException.ThrowIfNull(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);

        var seen = new Dictionary<Code, int>();
        var withs = new Dictionary<Code, HashSet<Code>>();

        foreach (var moment in moments)
            foreach (var one in moment)
            {
                seen[one] = seen.GetValueOrDefault(one) + 1;

                if (!withs.TryGetValue(one, out var kept)) withs[one] = kept = [];

                foreach (var other in moment) if (!other.Equals(one)) kept.Add(other);
            }

        return Grouped(
            seen, floor, withs,
            (mine, theirs, group) => Shared(withs[mine], withs[theirs], group) >= company);
    }

    /// <summary>
    /// The same question asked of a stream that has an ORDER — <b>fork 106, John's, and the
    /// one clause a bag of moments cannot carry.</b>
    /// </summary>
    /// <param name="moments">What was seen, in the order it was seen.</param>
    /// <param name="adhesion">
    /// How many times more often than chance two codes must turn up near each other in TIME.
    /// <b>A ratio against what independent codes would have done, so it is not a level about
    /// this world</b> — the same shape rung five's independence bar has, and the reason a
    /// share of shared company would not do here.
    /// </param>
    /// <param name="floor">
    /// <inheritdoc cref="From" path="/param[@name='floor']"/>
    /// </param>
    /// <param name="span">
    /// How many moments either side count as near. <b>The window is what makes time
    /// readable at all</b>, and one is the smallest thing that is not the moment itself.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Exclusion stays in the moment and company moves to the window, which is the whole
    /// construction.</b> Widening the exclusion would refuse exactly the codes this is for —
    /// a thing seen twice running shows two of its own looks in adjacent moments, so they
    /// co-occur in any window and would fail the clause that makes them alternatives.
    /// </para>
    /// <para>
    /// <b>And the test is adhesion rather than shared company, because shared company is
    /// exactly what twins have.</b> Two twins wear one look, so their landmarks keep the same
    /// company however wide the window is — that is <see cref="From"/>'s measured limit. What
    /// runs give is that a thing's OWN codes turn up beside each other far more often than
    /// chance and a twin's never do, which is a statement about a pair rather than about the
    /// company either keeps.
    /// </para>
    /// <para>
    /// <b>So a uniform stream must return nothing, and that is the control rather than a
    /// failure.</b> Where sightings are drawn independently, every pair adheres at chance, so
    /// this refuses everything <see cref="From"/> would have found. The two are not
    /// substitutes: one reads space and one reads time.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlySet<Code>> Over(
        IEnumerable<IReadOnlySet<Code>> moments, double adhesion, int floor, int span)
    {
        ArgumentNullException.ThrowIfNull(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(span);

        var stream = moments.ToList();

        var seen = new Dictionary<Code, int>();
        var withs = new Dictionary<Code, HashSet<Code>>();
        var near = new Dictionary<(Code Mine, Code Theirs), int>();

        for (var at = 0; at < stream.Count; at++)
            foreach (var one in stream[at])
            {
                seen[one] = seen.GetValueOrDefault(one) + 1;

                if (!withs.TryGetValue(one, out var kept)) withs[one] = kept = [];

                foreach (var other in stream[at]) if (!other.Equals(one)) kept.Add(other);

                // Counted once a moment an other, not once a neighbour. A code appearing in
                // both moments either side of this one is one piece of evidence about
                // adjacency, and counting it twice would let a long run of one thing
                // manufacture its own significance.
                var window = new HashSet<Code>();

                for (var step = Math.Max(0, at - span);
                    step <= Math.Min(stream.Count - 1, at + span);
                    step++)
                    if (step != at) window.UnionWith(stream[step]);

                foreach (var other in window)
                    if (!other.Equals(one))
                    {
                        near.TryGetValue((one, other), out var already);
                        near[(one, other)] = already + 1;
                    }
            }

        // Chance is the product of the two marginals over the window's own width, so what is
        // being asked is whether the pair turns up beside each other more than two codes of
        // those frequencies would have. Without the width the bar would be a claim about how
        // wide the window is rather than about the stream.
        var total = (double)stream.Count;
        var width = (2 * span) + 1.0;

        return Grouped(
            seen, floor, withs,
            (mine, theirs, _) =>
            {
                near.TryGetValue((mine, theirs), out var beside);

                var expected = seen[mine] / total * (seen[theirs] / total) * width * total;

                return expected > 0.0 && beside / expected >= adhesion;
            });
    }

    /// <summary>The greedy grouping both derivations share.</summary>
    /// <param name="seen">How often each code turned up.</param>
    /// <param name="floor">How often before a code may join a group.</param>
    /// <param name="withs">What each code shared a MOMENT with — the exclusion clause.</param>
    /// <param name="keeps">Whether two codes keep close enough company to be alternatives.</param>
    /// <remarks>
    /// <para>
    /// <b>Greedy, and its order is the codes' own rather than the stream's.</b> Two machines
    /// seeing the same moments in different orders must reach the same groups or a category
    /// means one thing here and another there — the rule <c>Agreed</c> stands on, and the
    /// reason a fitted quantiser is refused.
    /// </para>
    /// <para>
    /// <b>A member must clear both clauses against every member already in, never against
    /// the first alone.</b> A chain of pairwise-similar codes reaches arbitrarily far, which
    /// is single-link clustering's own failure and would return one group holding the whole
    /// alphabet.
    /// </para>
    /// <para>
    /// <b>And the exclusion clause is here rather than in either caller, because it is the
    /// half of John's account that is not negotiable.</b> What the two derivations differ in
    /// is what counts as keeping the same company; that alternatives never co-occur is the
    /// same claim in both.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<IReadOnlySet<Code>> Grouped(
        Dictionary<Code, int> seen,
        int floor,
        Dictionary<Code, HashSet<Code>> withs,
        Func<Code, Code, IReadOnlySet<Code>, bool> keeps)
    {
        var codes = seen.Where(one => one.Value >= floor).Select(one => one.Key).Order().ToList();

        var taken = new HashSet<Code>();
        var groups = new List<IReadOnlySet<Code>>();

        foreach (var one in codes)
        {
            if (taken.Contains(one)) continue;

            var group = new HashSet<Code> { one };

            foreach (var other in codes)
            {
                if (taken.Contains(other) || group.Contains(other)) continue;

                if (group.All(member =>
                    !withs[member].Contains(other) && keeps(member, other, group)))
                    group.Add(other);
            }

            if (group.Count < 2) continue;

            foreach (var member in group) taken.Add(member);

            groups.Add(group);
        }

        return groups;
    }

    /// <summary>How much of two codes' company is the same.</summary>
    /// <param name="mine">One code's company.</param>
    /// <param name="theirs">The other's.</param>
    /// <param name="group">
    /// What is already in the group. <b>Excluded from both sides</b>, because members are
    /// alternatives and therefore never each other's company — counting their absence as a
    /// difference would penalise exactly the codes that belong together.
    /// </param>
    private static double Shared(
        IReadOnlySet<Code> mine, IReadOnlySet<Code> theirs, IReadOnlySet<Code> group)
    {
        var both = 0;
        var either = 0;

        foreach (var one in mine)
        {
            if (group.Contains(one)) continue;

            either++;
            if (theirs.Contains(one)) both++;
        }

        foreach (var one in theirs)
            if (!group.Contains(one) && !mine.Contains(one)) either++;

        return either == 0 ? 0.0 : both / (double)either;
    }
}

/// <summary>
/// Another front end, with a code added for every category any of whose members is in the
/// moment — <b>the ANY fold, which is what makes a category not a name.</b>
/// </summary>
/// <typeparam name="TObservation">What the world hands over.</typeparam>
/// <remarks>
/// <b>A decorator, so the categories are an axis on every world rather than a feature of
/// one.</b> <see cref="Joined"/> carries its own because it was built before this existed
/// and its arms cross with the categories; anything reaching a world that is already coded
/// needs the fold without the text machinery around it.
/// </remarks>
/// <param name="inner">The front end this wraps.</param>
/// <param name="categories">
/// The vocabulary of alternatives. <b>The SAME object the population is handed</b>, since a
/// category the front end folds and one the brain may rewrite over have to be the same code
/// or the rewrite names something no moment ever holds.
/// </param>
public sealed class Sorted<TObservation>(IQuantizer<TObservation> inner, Sorting categories)
    : IQuantizer<TObservation>
{
    /// <inheritdoc/>
    public byte Modality => inner.Modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(TObservation observation) =>
        categories.Folded(new HashSet<Code>(inner.Codify(observation)));

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Bind(TObservation observation) => inner.Bind(observation);

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Order(TObservation observation) => inner.Order(observation);

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(TObservation observation) => inner.Fleeting(observation);

    /// <inheritdoc/>

    /// <inheritdoc/>

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(TObservation observation) => inner.Forced(observation);
}
