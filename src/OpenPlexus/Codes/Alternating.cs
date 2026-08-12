namespace OpenPlexus.Codes;

/// <summary>
/// Codes that are ALTERNATIVES, found in what was seen rather than handed over — <b>fork
/// 97's category, derived.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S, AND IT IS TWO CLAUSES RATHER THAN ONE.</b> His account is a set of things
/// standing in the same relations to everything else; the clause that has to go with it is
/// that they NEVER CO-OCCUR. Without it the same rule groups a wheel and a door — both
/// stand in relations to a car — and that is a co-firing bundle, which is rung five and
/// folds the opposite way.
/// </para>
/// <para>
/// <b>SO THE TEST IS EXCLUSION AND SHARED COMPANY, AND BOTH ARE FACTS ABOUT THE MOMENTS.</b>
/// Nothing here reads a world's tables, a vocabulary or an outcome. Two codes that have
/// never been seen together and that keep the same company are alternatives, and that is
/// arithmetic over what arrived.
/// </para>
/// <para>
/// <b>AND IT IS WHY THE INDIVIDUAL AND THE CATEGORY ARE ONE MECHANISM RATHER THAN TWO.</b>
/// The looks a thing wears across sightings never co-occur — a basket at two moments does
/// not co-occur with itself, which is what makes rung five structurally unable to reach it —
/// and they keep the same company by construction. So a thing's appearances ARE a category,
/// and minting one is what collapses a rule per appearance into a rule.
/// </para>
/// <para>
/// <b>WHAT IT CANNOT DO, SAID BEFORE ANYTHING READS IT: it cannot separate two things that
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
    /// <b>GREEDY, AND ITS ORDER IS THE CODES' OWN RATHER THAN THE STREAM'S.</b> Two machines
    /// seeing the same moments in different orders must reach the same groups or a category
    /// means one thing here and another there — the rule <c>Agreed</c> stands on, and the
    /// reason a fitted quantiser is refused.
    /// </para>
    /// <para>
    /// <b>A MEMBER MUST CLEAR BOTH CLAUSES AGAINST EVERY MEMBER ALREADY IN, never against
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

                if (group.All(member => !withs[member].Contains(other)
                        && Shared(withs[member], withs[other], group) >= company))
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
/// <b>A DECORATOR, SO THE CATEGORIES ARE AN AXIS ON EVERY WORLD RATHER THAN A FEATURE OF
/// ONE.</b> <see cref="Joined"/> carries its own because it was built before this existed
/// and its arms cross with the categories; anything reaching a world that is already coded
/// needs the fold without the text machinery around it.
/// </remarks>
/// <param name="inner">The front end this wraps.</param>
/// <param name="categories">
/// Sets of codes that are ALTERNATIVES. <b>Derived by <see cref="Alternating.From"/> or
/// handed over as a ceiling</b>, and the difference between those two is the whole of what
/// a grid using this is measuring.
/// </param>
public sealed class Sorted<TObservation>(
    IQuantizer<TObservation> inner, IReadOnlyList<IReadOnlySet<Code>> categories)
    : IQuantizer<TObservation>
{
    /// <inheritdoc/>
    public byte Modality => inner.Modality;

    /// <inheritdoc/>
    public IReadOnlyCollection<Code> Codify(TObservation observation)
    {
        var moment = new HashSet<Code>(inner.Codify(observation));

        // ANY AND NEVER ALL, AND THE PLAIN CODE STAYS BESIDE THE CATEGORY. Members are
        // alternatives and by construction never co-occur, so a fold demanding all of them
        // would fire on nothing -- and emitting only the category would make two members the
        // same code, which is the specificity gradient collapsed at the front end.
        foreach (var category in categories)
            foreach (var member in category)
                if (moment.Contains(member))
                {
                    moment.Add(Joined.Category(category));
                    break;
                }

        return moment;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Bind(TObservation observation) => inner.Bind(observation);

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Order(TObservation observation) => inner.Order(observation);

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Fleeting(TObservation observation) => inner.Fleeting(observation);

    /// <inheritdoc/>
    public Graph.Kind? Relating(TObservation observation) => inner.Relating(observation);

    /// <inheritdoc/>
    public IReadOnlyDictionary<Code, int>? Filling(TObservation observation) => inner.Filling(observation);

    /// <inheritdoc/>
    public IReadOnlySet<Code>? Forced(TObservation observation) => inner.Forced(observation);
}
