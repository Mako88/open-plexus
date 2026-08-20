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
/// <b>So the test is exclusion and shared company</b>, and both are facts about the moments.
/// Nothing here reads a world's tables, a vocabulary or an outcome. Two codes that have
/// never been seen together and that keep the same company are alternatives, and that is
/// arithmetic over what arrived.
/// </para>
/// <para>
/// <b>And it is why the individual and the category are one mechanism</b> rather than two.
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
public sealed class Alternating
{
    private readonly Dictionary<Code, int> _seen = [];
    private readonly Dictionary<Code, HashSet<Code>> _withs = [];
    private readonly Dictionary<(Code Mine, Code Theirs), int> _near = [];
    private readonly List<IReadOnlySet<Code>> _held = [];
    private readonly int _span;

    private int _next;
    private long _moments;
    private bool _settled;

    /// <param name="span">
    /// How many moments either side count as near, for <see cref="ByTime"/>. Fixed at
    /// construction because it decides how far back a moment has to be held, and a window
    /// widened part way through a stream would leave the earlier counts answering a narrower
    /// question than the later ones.
    /// </param>
    /// <remarks>
    /// <para>
    /// The derivation held open, so a brain may read it while it runs. The plan's entry is
    /// that deriving offline orphans every scope holding a category, and a live derivation is
    /// the half of that answer which is not about the store. <see cref="From"/> and
    /// <see cref="Over"/> are this object driven over a whole stream, so there is one
    /// implementation and every batch reading is unchanged by construction.
    /// </para>
    /// <para>
    /// Every count here is monotone. How often a code was seen and how often two turned up
    /// near each other are G-Counters, and what a code shared a moment with is a grow-only
    /// set, so two machines watching different streams merge with no coordinator. That is C1
    /// rather than a convenience, and it is why the accumulator is counts and the grouping is
    /// a pure function of them.
    /// </para>
    /// <para>
    /// What it costs is memory, and the cost is in the company rather than in the pairs. A
    /// code's company is every code it ever shared a moment with, which on a wide alphabet
    /// approaches the alphabet. Nothing here bounds it and no reading has priced it.
    /// </para>
    /// <para>
    /// A moment handed over is kept by reference for as long as the window needs it, so a
    /// caller that mutates a set it has already given up changes counts taken before the edit.
    /// </para>
    /// </remarks>
    public Alternating(int span = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(span);

        _span = span;
    }

    /// <summary>How many moments have been folded in.</summary>
    public long Moments => _moments;

    /// <summary>Folds one moment into the counts.</summary>
    /// <param name="moment">What was seen at once.</param>
    /// <remarks>
    /// The exclusion clause and the company are facts about this moment alone and are taken
    /// here. Adhesion is not: a moment cannot be scored against the moments after it until
    /// they arrive, so it is held until its forward window is complete and folded then. That
    /// lag is the whole difference between reading time online and reading it from a list,
    /// and it is one window deep.
    /// </remarks>
    public void Watch(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        if (_settled)
            throw new InvalidOperationException("a settled stream takes no more moments");

        _moments++;

        foreach (var one in moment)
        {
            _seen[one] = _seen.GetValueOrDefault(one) + 1;

            if (!_withs.TryGetValue(one, out var kept)) _withs[one] = kept = [];

            foreach (var other in moment) if (!other.Equals(one)) kept.Add(other);
        }

        _held.Add(moment);

        // A moment is ready once every moment that could be near it has arrived, and it is
        // never revisited after -- so what is held is the window and not the stream.
        while (_held.Count - _next > _span) Fold(_held.Count - 1);

        while (_next > _span)
        {
            _held.RemoveAt(0);
            _next--;
        }
    }

    /// <summary>Says no more moments are coming, so the last of them get their short window.</summary>
    /// <remarks>
    /// The experimenter's and never the learner's, and it exists so a run over a list reaches
    /// the counts a run over a list always did. A brain is never settled, which is what C4
    /// says; what it changes is the last few moments of a stream and nothing else.
    /// </remarks>
    public void Settle()
    {
        if (_settled) return;

        while (_next < _held.Count) Fold(_held.Count - 1);

        _settled = true;
    }

    /// <summary>Folds the next held moment against the window it now has.</summary>
    /// <param name="last">The furthest moment that may count as near, which the tail truncates.</param>
    private void Fold(int last)
    {
        var at = _next++;

        // Counted once a moment an other, not once a neighbour. A code appearing in both
        // moments either side of this one is one piece of evidence about adjacency, and
        // counting it twice would let a long run of one thing manufacture its own
        // significance.
        var window = new HashSet<Code>();

        for (var step = Math.Max(0, at - _span); step <= Math.Min(last, at + _span); step++)
            if (step != at) window.UnionWith(_held[step]);

        foreach (var one in _held[at])
            foreach (var other in window)
                if (!other.Equals(one))
                {
                    _near.TryGetValue((one, other), out var already);
                    _near[(one, other)] = already + 1;
                }
    }

    /// <summary>The groups read off what shares a moment, as <see cref="From"/> reads them.</summary>
    /// <param name="company">
    /// <inheritdoc cref="From" path="/param[@name='company']"/>
    /// </param>
    /// <param name="floor">
    /// <inheritdoc cref="From" path="/param[@name='floor']"/>
    /// </param>
    public IReadOnlyList<IReadOnlySet<Code>> BySpace(double company, int floor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, group) => Shared(_withs[mine], _withs[theirs], group) >= company);
    }

    /// <summary>The groups read off what turns up near in time, as <see cref="Over"/> reads them.</summary>
    /// <param name="adhesion">
    /// <inheritdoc cref="Over" path="/param[@name='adhesion']"/>
    /// </param>
    /// <param name="floor">
    /// <inheritdoc cref="From" path="/param[@name='floor']"/>
    /// </param>
    /// <remarks>
    /// Read before the stream is settled, this answers on the moments whose window is
    /// complete and on no others. What it reads is a prefix of what a settled run holds,
    /// which is what makes an unsettled reading early rather than wrong.
    /// </remarks>
    public IReadOnlyList<IReadOnlySet<Code>> ByTime(double adhesion, int floor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(floor);

        // Chance is the product of the two marginals over the window's own width, so what is
        // being asked is whether the pair turns up beside each other more than two codes of
        // those frequencies would have. Without the width the bar would be a claim about how
        // wide the window is rather than about the stream.
        var total = (double)_moments;
        var width = (2 * _span) + 1.0;

        return Grouped(
            _seen, floor, _withs,
            (mine, theirs, _) =>
            {
                _near.TryGetValue((mine, theirs), out var beside);

                var expected = _seen[mine] / total * (_seen[theirs] / total) * width * total;

                return expected > 0.0 && beside / expected >= adhesion;
            });
    }

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
    /// means one thing here and another there — the rule <c>Hashing</c> stands on, and the
    /// reason a fitted quantiser is refused.
    /// </para>
    /// <para>
    /// <b>A member must clear both clauses against every member already in</b>, never against
    /// the first alone. A chain of pairwise-similar codes reaches arbitrarily far, which
    /// is single-link clustering's own failure and would return one group holding the whole
    /// alphabet.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlySet<Code>> From(
        IEnumerable<IReadOnlySet<Code>> moments, double company, int floor)
    {
        ArgumentNullException.ThrowIfNull(moments);

        var watching = new Alternating();

        foreach (var moment in moments) watching.Watch(moment);

        return watching.BySpace(company, floor);
    }

    /// <summary>
    /// The same question asked of a stream that has an ORDER — <b>fork 106, John's, and the
    /// one clause a bag of moments cannot carry.</b>
    /// </summary>
    /// <param name="moments">What was seen, in the order it was seen.</param>
    /// <param name="adhesion">
    /// How many times more often than chance two codes must turn up near each other in TIME.
    /// <b>A ratio against what independent codes would have done</b>, so it is not a level about
    /// this world — the same shape rung five's independence bar has, and the reason a
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
    /// <b>Exclusion stays in the moment and company moves to the window</b>, which is the whole
    /// construction. Widening the exclusion would refuse exactly the codes this is for —
    /// a thing seen twice running shows two of its own looks in adjacent moments, so they
    /// co-occur in any window and would fail the clause that makes them alternatives.
    /// </para>
    /// <para>
    /// <b>And the test is adhesion rather than shared company</b>, because shared company is
    /// exactly what twins have. Two twins wear one look, so their landmarks keep the same
    /// company however wide the window is — that is <see cref="From"/>'s measured limit. What
    /// runs give is that a thing's OWN codes turn up beside each other far more often than
    /// chance and a twin's never do, which is a statement about a pair rather than about the
    /// company either keeps.
    /// </para>
    /// <para>
    /// <b>So a uniform stream must return nothing</b>, and that is the control rather than a
    /// failure. Where sightings are drawn independently, every pair adheres at chance, so
    /// this refuses everything <see cref="From"/> would have found. The two are not
    /// substitutes: one reads space and one reads time.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlySet<Code>> Over(
        IEnumerable<IReadOnlySet<Code>> moments, double adhesion, int floor, int span)
    {
        ArgumentNullException.ThrowIfNull(moments);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(span);

        var watching = new Alternating(span);

        foreach (var moment in moments) watching.Watch(moment);

        // The list has ended, so the last moments get the short window a list always gave
        // them. A brain never reaches this call, which is the one difference between the two
        // ways of driving the same counts.
        watching.Settle();

        return watching.ByTime(adhesion, floor);
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
    /// means one thing here and another there — the rule <c>Hashing</c> stands on, and the
    /// reason a fitted quantiser is refused.
    /// </para>
    /// <para>
    /// <b>A member must clear both clauses against every member already in</b>, never against
    /// the first alone. A chain of pairwise-similar codes reaches arbitrarily far, which
    /// is single-link clustering's own failure and would return one group holding the whole
    /// alphabet.
    /// </para>
    /// <para>
    /// <b>And the exclusion clause is here rather than in either caller.</b> Because it is the
    /// half of John's account that is not negotiable. What the two derivations differ in
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
public sealed class Sorted<TObservation>(IQuantizer<TObservation> inner, Categories categories)
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
