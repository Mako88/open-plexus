namespace OpenPlexus.Codes;

/// <summary>
/// Codes standing for sets of codes that are ALTERNATIVES, and the two directions they are
/// read in — <b><see cref="Commitments.Naming"/> turned the other way up.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The two vocabularies are the same shape and the opposite entailment.</b> Which is why
/// this is a type and not a list. A minted name means every member is present, so it
/// enters a moment when they all are and a scope may be rewritten over it with the record
/// carried. A category means ANY member is present, so it enters a moment when one is — and
/// a scope rewritten over it CLAIMS MORE THAN IT DID. Everything fork 85 says follows from
/// that one difference, and keeping both in one place is what stops the wrong fold being
/// reached for.
/// </para>
/// <para>
/// <b>So it is held by the front end</b> and read by the population, which is the seam fork 84
/// already drew. What the coarser form of a code is is a fact about how a stream is
/// being read, and the brain is told it in the same way it is told an alphabet. What the
/// brain does with it — whether a coarse code may enter a scope, and on what evidence — is
/// the brain's, and is <c>Population.Recast</c>.
/// </para>
/// <para>
/// <b>AND NOTHING HERE DERIVES ANYTHING.</b> <see cref="Alternating.From"/> finds the
/// groups from moments; this carries them. Two objects because a derivation that ran once
/// over four thousand sightings and a lookup asked on every code are not the same kind of
/// thing, and folding them together would make the second cost the first.
/// </para>
/// </remarks>
public sealed class Categories
{
    private readonly List<IReadOnlySet<Code>> _groups = [];
    private readonly Dictionary<Code, Code> _coarser = [];
    private readonly HashSet<Code> _known = [];

    /// <param name="groups">
    /// Sets of codes that are alternatives. <b>Derived by <see cref="Alternating.From"/> or
    /// handed over as a ceiling</b>, and the difference between those two is the whole of
    /// what a grid using this is measuring.
    /// </param>
    /// <remarks>
    /// <b>A code has at most one coarser form</b>, and the first group to claim it keeps it,
    /// because a code with two would make <see cref="Coarser"/> a choice rather than a lookup
    /// — a decision hiding in what was meant to be an alphabet. It was written when that
    /// could only fire on a hand-written table, <see cref="Alternating.From"/> returning
    /// disjoint groups by construction. <see cref="Learn"/> makes it ordinary: a group that
    /// grows arrives beside the group it grew out of, and its members are already claimed.
    /// </remarks>
    public Categories(IEnumerable<IReadOnlySet<Code>> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        foreach (var group in groups) Learn(group);
    }

    /// <summary>Takes a group of alternatives, and never gives one back.</summary>
    /// <param name="group">The codes that are alternatives.</param>
    /// <returns>Whether it was one this did not already carry.</returns>
    /// <remarks>
    /// <para>
    /// Add-only, and it is the same rule repair runs under one level up. A category's name IS
    /// its members, so a group that has grown is a different code from the group it grew out
    /// of — editing the group in place would rename it and orphan every scope that had been
    /// written over the old name. So the wider group arrives BESIDE the narrower one and both
    /// stay, in the way a repair adds a narrower commitment and never edits its parent.
    /// </para>
    /// <para>
    /// Both are then in the moment, because <see cref="Folded"/> reads every group it holds.
    /// So a scope may be written over either and the ordinary evidence decides between them,
    /// which is where a decision about which name is right belongs — subsumption already
    /// keeps the general one where the two are equally accurate.
    /// </para>
    /// <para>
    /// What it costs is vocabulary. A group that fills gradually mints a category at every
    /// size it passes through, so a run reports how many categories it holds beside whatever
    /// else it reports. That count is the arm's own refutation if it grows without paying.
    /// </para>
    /// <para>
    /// A member keeps the first category to claim it, which is what
    /// <see cref="Coarser"/> being a lookup rather than a choice requires. The earliest is the
    /// one with the most evidence behind it by the time anything asks, and the wider name is
    /// still reachable because the moment carries it.
    /// </para>
    /// </remarks>
    public bool Learn(IReadOnlySet<Code> group)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (group.Count < 2) return false;

        var category = Joined.Category(group);

        if (!_known.Add(category)) return false;

        _groups.Add(group);

        foreach (var member in group) _coarser.TryAdd(member, category);

        return true;
    }

    /// <summary>How many categories are carried.</summary>
    public int Count => _groups.Count;

    /// <summary>The groups themselves, in the order they arrived.</summary>
    public IReadOnlyList<IReadOnlySet<Code>> Groups => _groups;

    /// <summary>The category this code is a member of, or nothing.</summary>
    /// <param name="code">The code to ask about.</param>
    /// <remarks>
    /// <b>A category's own code has no coarser form</b>, and that is load-bearing rather than an
    /// omission. It is what makes the entailment one-directional, so nothing here can
    /// come to say that a category is a member of itself — and a rewrite that could would
    /// walk a scope up until it said nothing at all.
    /// </remarks>
    public Code? Coarser(Code code) => _coarser.TryGetValue(code, out var category) ? category : null;

    /// <summary>
    /// The moment with a code added for every category any of whose members is in it.
    /// </summary>
    /// <param name="moment">What is live. <b>Added to in place</b>, since both front ends
    /// that call this have just built it.</param>
    /// <remarks>
    /// <para>
    /// <b>Any and never all, which is the whole difference from rung five.</b> The members
    /// are alternatives and by construction never co-occur, so a fold demanding all of them
    /// would fire on nothing at all. <b>The plain code stays beside the category</b>, because
    /// emitting only the category would make <i>mary</i> and <i>john</i> the same word — and
    /// a general rule is worth having only while a particular one is still sayable.
    /// </para>
    /// <para>
    /// <b>One pass and not a fixed point, unlike <c>Naming.Fold</c>.</b> A category over
    /// categories is expressible and nothing mints one yet, so iterating would be machinery
    /// with no caller — and a loop that cannot turn twice is a loop written for a mechanism
    /// that does not exist.
    /// </para>
    /// </remarks>
    public HashSet<Code> Folded(HashSet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        foreach (var group in _groups)
            foreach (var member in group)
                if (moment.Contains(member))
                {
                    moment.Add(Joined.Category(group));
                    break;
                }

        return moment;
    }
}
