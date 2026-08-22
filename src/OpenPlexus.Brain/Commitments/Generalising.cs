using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>
/// What proposes a scope naming no argument — <b>rung four's other half</b>, and the operator
/// <see cref="Unifying"/>'s matcher had nothing to match. Fork 102, gated by fork 97.
/// </summary>
/// <remarks>
/// <para>
/// Anti-unification, which is repair turned round. Repair takes one commitment and adds a
/// condition; this takes several that expect the same thing and differ in exactly one position
/// of an otherwise identical scope, and says the thing they share is a rule with a hole in it.
/// Every other operator here narrows, and <see cref="Abstracting"/> is the only other one that
/// does not.
/// </para>
/// <para>
/// <b>Promiscuous proposal and the gate doing the work</b>, which is the shape every admission
/// here has. Sibling groups are abundant and mostly noise: a hole punched on every one of them
/// is worse than the rules it replaces about nine times in ten, firing twice as often and
/// paying for it. So what decides is not how many siblings a hole covers — that column is flat
/// across two, three and four — but whether the values it covers are ALTERNATIVES.
/// </para>
/// <para>
/// <b>And that is a fact about the MOMENTS.</b> Which is why nothing in a population could
/// supply it: a commitment holds counts about its own firings and never about which codes
/// shared a moment. <see cref="Population.Sorts"/> is the vocabulary that can answer it, and asking
/// costs one dictionary hit: do the covered values share a coarser form.
/// <c>UnifyingYieldTests</c> holds the reading that says the lookup answers exactly what the
/// moments answer.
/// </para>
/// <para>
/// <b>A hole with no context beside it is a rule about nothing</b>, so a scope of one code is
/// never generalised. Blanking it leaves <i>whichever code of this kind, expect Y</i>, which
/// fires on every moment holding any code of that kind at all.
/// </para>
/// <para>
/// <b>The parent is ADDED and its siblings are left alone</b>, which is the add-only rule every
/// other operator here runs under. It is a new claim rather than a rewrite — it covers moments
/// none of its siblings did — so it starts blind and re-earns its record, and subsumption
/// decides what survives beside it on the ordinary evidence.
/// </para>
/// </remarks>
internal static class Generalising
{
    /// <summary>One rule with a hole in it, and what it was read off.</summary>
    /// <param name="Scope">The scope, with one entry naming a variable.</param>
    /// <param name="Expects">What it says should follow.</param>
    /// <param name="Covers">How many siblings the hole covers.</param>
    public readonly record struct Holed(ImmutableArray<Code> Scope, Code Expects, int Covers);

    /// <summary>Commitments one variable would cover, and where the variable goes.</summary>
    /// <param name="Hole">Which position of the shared scope varies.</param>
    /// <param name="Members">The commitments, which agree everywhere else.</param>
    public readonly record struct Sibling(int Hole, IReadOnlyList<Commitment> Members);

    /// <summary>Every group of commitments one variable would cover.</summary>
    /// <param name="all">The residents to read.</param>
    /// <param name="shortest">
    /// The shortest scope that may be generalised. <b>Two, wherever a proposal is meant</b> —
    /// a scope of one code with that code blanked becomes <i>whichever code of this kind,
    /// expect Y</i>, which fires on every moment holding any such code and is a rule about
    /// nothing. One is here so an instrument can show what the bar refuses.
    /// </param>
    /// <remarks>
    /// Keyed on the scope with one position blanked to its modality, which IS the hole. Two
    /// commitments land in one group exactly when one variable would cover both — and the
    /// VALUE is left out of the key while the modality stays in, because a variable is
    /// <i>whichever code of this kind</i>. Two rules differing in a word against a place share
    /// no rule with a hole in it; they happen to have the same length.
    /// </remarks>
    public static IReadOnlyList<Sibling> Siblings(
        IReadOnlyList<Commitment> all, int shortest = 2)
    {
        ArgumentNullException.ThrowIfNull(all);

        var groups = new Dictionary<string, List<Commitment>>(StringComparer.Ordinal);
        var holes = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var one in all)
        {
            if (one.Scope.Length < shortest) continue;

            for (var hole = 0; hole < one.Scope.Length; hole++)
            {
                var key = $"{one.Expects.Modality}:{one.Expects.Value}|{hole}|" + string.Join(
                    ",",
                    one.Scope.Select((code, at) => at == hole
                        ? $"?{code.Modality}"
                        : $"{code.Modality}:{code.Value}"));

                if (!groups.TryGetValue(key, out var members))
                {
                    groups[key] = members = [];
                    holes[key] = hole;
                }

                members.Add(one);
            }
        }

        return
        [
            .. groups
                .Where(one => one.Value.Count > 1)
                .OrderBy(one => one.Key, StringComparer.Ordinal)
                .Select(one => new Sibling(holes[one.Key], one.Value)),
        ];
    }

    /// <summary>The values one variable would have to stand for.</summary>
    /// <param name="group">The siblings.</param>
    public static IReadOnlyList<Code> Covered(Sibling group) =>
        [.. group.Members.Select(one => one.Scope[group.Hole]).Distinct()];

    /// <summary>Whether the vocabulary says those values are alternatives.</summary>
    /// <param name="group">The siblings.</param>
    /// <param name="sorts">The vocabulary of alternatives.</param>
    /// <remarks>
    /// <b>The gate, and it is one lookup.</b> A code with no coarser form answers no, so a
    /// vocabulary that has not reached this part of the alphabet refuses rather than guessing
    /// — which is what makes an empty <see cref="Categories"/> a control and not a failure.
    /// </remarks>
    public static bool Admits(Sibling group, Categories sorts)
    {
        ArgumentNullException.ThrowIfNull(sorts);

        var covered = Covered(group);

        return covered.Count > 1
            && sorts.Coarser(covered[0]) is { } coarser
            && covered.All(one => sorts.Coarser(one) == coarser);
    }

    /// <summary>The rule with a hole in it that a group of siblings proposes.</summary>
    /// <param name="group">The siblings.</param>
    public static Holed Rule(Sibling group)
    {
        var first = group.Members[0];

        return new Holed(
            [.. first.Scope.Select((code, at) => at == group.Hole
                ? Unifying.Any(code.Modality, 0)
                : code)],
            first.Expects,
            group.Members.Count);
    }

    /// <summary>Every rule with a hole the population and its vocabulary admit.</summary>
    /// <param name="all">The residents to read.</param>
    /// <param name="sorts">The vocabulary of alternatives, which is the gate.</param>
    /// <remarks>
    /// <para>
    /// <b>Every admitted proposal rather than the best one</b>, which is the opposite of
    /// <see cref="Abstracting"/>'s one name an ask. That rung ranks candidates by a z and takes
    /// the argmax, so a ceiling is what stops it walking the whole space. This one refuses all
    /// but a small fraction before ranking is reached — 38 of 2,418 on the multiplexer — so a
    /// ceiling would be a second gate over a set the first gate has already emptied.
    /// </para>
    /// <para>
    /// <b>Ordered by the scope's own name</b>, so two machines holding the same residents
    /// propose the same rules in the same order with nothing to ask each other.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Holed> Propose(IReadOnlyList<Commitment> all, Categories sorts)
    {
        ArgumentNullException.ThrowIfNull(sorts);

        var proposed = Siblings(all)
            .Where(group => Admits(group, sorts))
            .Select(Rule)
            .ToList();

        proposed.Sort((left, right) => Commitment
            .Name([.. left.Scope], left.Expects)
            .CompareTo(Commitment.Name([.. right.Scope], right.Expects)));

        return proposed;
    }
}
