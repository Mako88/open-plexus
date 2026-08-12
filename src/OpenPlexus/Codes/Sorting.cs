namespace OpenPlexus.Codes;

/// <summary>
/// Codes standing for sets of codes that are ALTERNATIVES, and the two directions they are
/// read in — <b><see cref="Commitments.Naming"/> turned the other way up.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE TWO VOCABULARIES ARE THE SAME SHAPE AND THE OPPOSITE ENTAILMENT, WHICH IS WHY
/// THIS IS A TYPE AND NOT A LIST.</b> A minted name means every member is present, so it
/// enters a moment when they all are and a scope may be rewritten over it with the record
/// carried. A category means ANY member is present, so it enters a moment when one is — and
/// a scope rewritten over it CLAIMS MORE THAN IT DID. Everything fork 85 says follows from
/// that one difference, and keeping both in one place is what stops the wrong fold being
/// reached for.
/// </para>
/// <para>
/// <b>SO IT IS HELD BY THE FRONT END AND READ BY THE POPULATION, WHICH IS THE SEAM FORK 84
/// ALREADY DREW.</b> What the coarser form of a code is is a fact about how a stream is
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
public sealed class Sorting
{
    private readonly List<IReadOnlySet<Code>> _groups = [];
    private readonly Dictionary<Code, Code> _coarser = [];

    /// <param name="groups">
    /// Sets of codes that are alternatives. <b>Derived by <see cref="Alternating.From"/> or
    /// handed over as a ceiling</b>, and the difference between those two is the whole of
    /// what a grid using this is measuring.
    /// </param>
    /// <remarks>
    /// <b>A CODE BELONGS TO AT MOST ONE CATEGORY, AND THE FIRST GROUP TO CLAIM IT KEEPS
    /// IT.</b> <see cref="Alternating.From"/> returns disjoint groups by construction, so
    /// this only fires on a hand-written table — and a code with two coarser forms would
    /// make <see cref="Coarser"/> a choice rather than a lookup, which is a decision hiding
    /// in what was meant to be an alphabet.
    /// </remarks>
    public Sorting(IEnumerable<IReadOnlySet<Code>> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        foreach (var group in groups)
        {
            if (group.Count < 2) continue;

            var category = Joined.Category(group);

            _groups.Add(group);

            foreach (var member in group) _coarser.TryAdd(member, category);
        }
    }

    /// <summary>How many categories are carried.</summary>
    public int Count => _groups.Count;

    /// <summary>The groups themselves, in the order they arrived.</summary>
    public IReadOnlyList<IReadOnlySet<Code>> Groups => _groups;

    /// <summary>The category this code is a member of, or nothing.</summary>
    /// <param name="code">The code to ask about.</param>
    /// <remarks>
    /// <b>A CATEGORY'S OWN CODE HAS NO COARSER FORM, and that is load-bearing rather than an
    /// omission.</b> It is what makes the entailment one-directional, so nothing here can
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
    /// <b>ANY AND NEVER ALL, WHICH IS THE WHOLE DIFFERENCE FROM RUNG FIVE.</b> The members
    /// are alternatives and by construction never co-occur, so a fold demanding all of them
    /// would fire on nothing at all. <b>The plain code stays beside the category</b>, because
    /// emitting only the category would make <i>mary</i> and <i>john</i> the same word — and
    /// a general rule is worth having only while a particular one is still sayable.
    /// </para>
    /// <para>
    /// <b>ONE PASS AND NOT A FIXED POINT, unlike <c>Naming.Fold</c>.</b> A category over
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
