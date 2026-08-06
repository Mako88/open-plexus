using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>
/// What is worth giving a name to, and whether anything is.
/// </summary>
/// <remarks>
/// <para>
/// <b>TWO BARS AND NEVER ONE, WHICH IS `Paying`'S FINDING CARRIED OVER RATHER THAN
/// ITS CODE.</b> Description length alone minted 715 names on a pure-noise control
/// against 245 on structured data — because a shorter description of noise is still
/// a shorter description. So a name has to pay for itself AND beat what independent
/// scopes would have produced anyway.
/// </para>
/// <para>
/// <b>PAIRS, AND BIGGER SETS BY RECURSION RATHER THAN BY SEARCH.</b> A named pair is
/// a code, so it can be half of the next pair — which reaches a set of four in two
/// steps and costs nothing to look for. Searching all subsets directly is
/// exponential, and the recursion is also the thing worth having: it is what makes
/// a second level of structure possible at all.
/// </para>
/// </remarks>
public static class Abstracting
{
    /// <summary>The sub-scope most worth naming, or nothing if none has earned it.</summary>
    /// <param name="held">Every commitment resident.</param>
    /// <param name="dials">The gate's numbers.</param>
    /// <remarks>
    /// <b>Only experienced commitments propose.</b> A scope minted this round is not
    /// evidence that anything recurs — it is evidence that covering ran.
    /// </remarks>
    public static ImmutableArray<Code>? Shared(IEnumerable<Commitment> held, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(dials);

        var scopes = held
            .Where(one => one.Seen >= dials.Floor && one.Scope.Length >= 2)
            .Select(one => one.Scope)
            .ToList();

        if (scopes.Count < 3) return null;

        var alone = new Dictionary<Code, int>();
        var together = new Dictionary<(Code, Code), int>();

        foreach (var scope in scopes)
        {
            foreach (var code in scope)
            {
                alone.TryGetValue(code, out var seen);
                alone[code] = seen + 1;
            }

            for (var left = 0; left < scope.Length; left++)
                for (var right = left + 1; right < scope.Length; right++)
                {
                    var pair = (scope[left], scope[right]);
                    together.TryGetValue(pair, out var seen);
                    together[pair] = seen + 1;
                }
        }

        if (together.Count == 0) return null;

        (Code, Code)? best = null;
        var strongest = 0.0;

        foreach (var (pair, seen) in together)
        {
            // THE DESCRIPTION-LENGTH BAR. Naming a pair costs two entries to say what
            // it means and saves one in every scope that holds it, so it only repays
            // from three scopes up. Below that a name is a longer way of saying the
            // same thing.
            if (seen < 3) continue;

            var expected = alone[pair.Item1] / (double)scopes.Count
                * (alone[pair.Item2] / (double)scopes.Count);

            // A PAIR IN EVERY SCOPE HAS NO VARIANCE TO TEST AGAINST, and refusing it
            // for that would be perverse -- it is the strongest redundancy the
            // population can hold, and it is only reachable when repair chose BOTH
            // codes every single time. Nothing noise-like has that shape, so the
            // description-length bar decides alone.
            var z = expected >= 1.0
                ? double.PositiveInfinity
                : ((seen / (double)scopes.Count) - expected)
                    / Math.Sqrt(expected * (1.0 - expected) / scopes.Count);

            if (z <= strongest) continue;

            strongest = z;
            best = pair;
        }

        if (best is not { } chosen) return null;

        // CORRECTED FOR THE PAIRS LOOKED AT, exactly as repair's bar is. Taking the
        // best of thousands of candidates clears any fixed bar on chance alone, and
        // that is the failure this whole gate exists against.
        return Normal.Tail(strongest) * together.Count <= dials.Alpha
            ? [chosen.Item1, chosen.Item2]
            : null;
    }
}
