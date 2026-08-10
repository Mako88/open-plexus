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
/// <summary>One line of a counted table, as it crosses.</summary>
public readonly record struct Tallied
{
    /// <summary>The code, or the first of the pair.</summary>
    public required Code Left { get; init; }

    /// <summary>The second of the pair, or nothing where this row is one code alone.</summary>
    /// <remarks>
    /// <b>ONE ROW TYPE AND NOT TWO, so a reader cannot receive half a table.</b> The two
    /// counts are meaningless apart — a pair's frequency is tested against what its members
    /// do separately — and two arrays are two chances for one to arrive without the other.
    /// </remarks>
    public required Code? Right { get; init; }

    /// <summary>How many scopes it turned up in.</summary>
    public required int Seen { get; init; }
}

/// <summary>A <see cref="Recurrence"/> as bytes, and the only form of it that travels.</summary>
public sealed record Counts
{
    /// <summary>How many scopes the sender counted.</summary>
    /// <remarks>
    /// <b>CARRIED RATHER THAN INFERRED, because it cannot be recovered from the rows.</b>
    /// The z the naming gate computes divides by this, so a reader that guessed it from
    /// the table would be testing a different hypothesis from the one the sender counted.
    /// </remarks>
    public required int Scopes { get; init; }

    /// <summary>Every code and every pair, ordered.</summary>
    public required ImmutableArray<Tallied> Rows { get; init; }
}

/// <summary>
/// How often each code and each pair of codes turns up across a set of scopes —
/// <b>everything the naming gate reads, and the only thing that has to cross a wire for
/// it to work.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>RUNG FIVE'S STATISTIC IS POPULATION-WIDE AND NOTHING ABOUT HOLDING MORE PER MACHINE
/// FIXES THAT.</b> Measured in <c>SplitNamingTests</c>: sharding one population three ways
/// leaves each holder thirty-six eligible scopes — far past every count the gate asks for
/// — and all three go silent, because the z it computes carries a factor of the square
/// root of the scope count. Splitting a population does not remove a redundancy; it
/// removes the ability to certify one.
/// </para>
/// <para>
/// <b>AND COUNTS ARE THE ONE THING THIS DESIGN ALREADY MERGES WITHOUT A COORDINATOR.</b>
/// Hits, misses and abstains are G-Counters for exactly this reason. These two tables are
/// the same shape, so <see cref="Absorb"/> is addition and the merged answer is EXACTLY
/// the whole population's — integers, so none of the floating-point caveat that
/// <see cref="Population.Decide"/> carries applies here.
/// </para>
/// <para>
/// <b>C1 IS KEPT BECAUSE A COUNT IS NOT A COMMITMENT.</b> What crosses is how often a pair
/// of codes co-occurred in the sender's scopes. A reader learns nothing about which
/// commitments those were, what they expect, or how accurate they are — it is told a
/// frequency, which is what the constraint has always permitted.
/// </para>
/// </remarks>
public sealed class Recurrence
{
    private readonly Dictionary<Code, int> _alone = [];
    private readonly Dictionary<(Code Left, Code Right), int> _together = [];

    /// <summary>How many scopes were counted.</summary>
    public int Scopes { get; private set; }

    /// <summary>How often each code appeared, by code.</summary>
    internal IReadOnlyDictionary<Code, int> Alone => _alone;

    /// <summary>How often each pair appeared together, by pair.</summary>
    internal IReadOnlyDictionary<(Code Left, Code Right), int> Together => _together;

    /// <summary>
    /// Whether rung five is offered this commitment at all — <b>its denominator, in one
    /// place because two copies is how a count comes to be read against the wrong total.</b>
    /// </summary>
    /// <param name="one">The commitment to ask about.</param>
    /// <param name="dials">The gate's numbers, for the experience floor.</param>
    /// <remarks>
    /// <para>
    /// <b>ONLY EXPERIENCED COMMITMENTS PROPOSE.</b> A scope minted this round is not
    /// evidence that anything recurs — it is evidence that covering ran.
    /// </para>
    /// <para>
    /// <b>AND A ONE-CODE SCOPE HAS NO PAIR TO CONTRIBUTE, WHICH MAKES THIS RUNG'S INPUT
    /// EXACTLY REPAIR'S SURVIVING OUTPUT.</b> Covering mints one code and nothing longer, so
    /// every scope this admits was reached by a specialisation — and how many there are is a
    /// fact about the repair budget and the repair timing rather than about redundancy.
    /// <b>A naming count read without this beside it is a repair result wearing an
    /// abstraction's name.</b>
    /// </para>
    /// </remarks>
    public static bool Eligible(Commitment one, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(one);
        ArgumentNullException.ThrowIfNull(dials);

        return one.Seen >= dials.Floor && one.Scope.Length >= 2;
    }

    /// <summary>Counts what one holder's commitments have in common.</summary>
    /// <param name="held">The commitments this holder has.</param>
    /// <param name="dials">The gate's numbers, for the experience floor.</param>
    /// <remarks>
    /// <b>Only experienced commitments propose</b> — see <see cref="Eligible"/>.
    /// </remarks>
    public static Recurrence Of(IEnumerable<Commitment> held, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(dials);

        var counted = new Recurrence();

        foreach (var scope in held
            .Where(one => Eligible(one, dials))
            .Select(one => one.Scope))
        {
            counted.Scopes++;

            foreach (var code in scope)
            {
                counted._alone.TryGetValue(code, out var seen);
                counted._alone[code] = seen + 1;
            }

            for (var left = 0; left < scope.Length; left++)
                for (var right = left + 1; right < scope.Length; right++)
                {
                    var pair = (scope[left], scope[right]);
                    counted._together.TryGetValue(pair, out var seen);
                    counted._together[pair] = seen + 1;
                }
        }

        return counted;
    }

    /// <summary>
    /// These counts in a shape a wire can carry, <b>ordered so the bytes are the same on
    /// every machine.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE WORKING TYPE CANNOT CROSS AND THAT WAS NOT NOTICED WHEN IT WAS BUILT.</b>
    /// <see cref="Recurrence"/> keys its pairs on a tuple and keeps both tables private, so
    /// a serialiser writes <c>Scopes</c> and drops everything else — which is worse than
    /// writing nothing, because a plausible number and no error is indistinguishable from a
    /// message that arrived. A merge on the far side then adds nothing to anything. That is
    /// the edge-kind fault in <see cref="Bus.Wire"/> exactly, which arrived the same way and
    /// was found by a round-trip test rather than by a failure.
    /// </para>
    /// <para>
    /// <b>AND ORDERED RATHER THAN WHATEVER THE DICTIONARY HELD.</b> Two machines writing
    /// the same counts must write the same bytes, or nothing downstream can compare them —
    /// and a dictionary's walk is not stable across processes. Fork 12 does not reach the
    /// merge, which is integer addition, but it reaches anything that hashes or logs what
    /// crossed.
    /// </para>
    /// </remarks>
    public Counts Written()
    {
        var rows = ImmutableArray.CreateBuilder<Tallied>(_alone.Count + _together.Count);

        foreach (var code in _alone.Keys.Order())
            rows.Add(new Tallied { Left = code, Right = null, Seen = _alone[code] });

        foreach (var pair in _together.Keys.OrderBy(one => one.Left).ThenBy(one => one.Right))
            rows.Add(new Tallied
            {
                Left = pair.Left,
                Right = pair.Right,
                Seen = _together[pair],
            });

        return new Counts { Scopes = Scopes, Rows = rows.MoveToImmutable() };
    }

    /// <summary>The counts those rows carry.</summary>
    /// <param name="counts">What arrived.</param>
    public static Recurrence From(Counts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        var counted = new Recurrence { Scopes = counts.Scopes };

        foreach (var row in counts.Rows)
        {
            if (row.Right is { } right) counted._together[(row.Left, right)] = row.Seen;
            else counted._alone[row.Left] = row.Seen;
        }

        return counted;
    }

    /// <summary>Folds another holder's counts into these.</summary>
    /// <param name="other">What the other holder counted.</param>
    /// <remarks>
    /// <b>MONOTONE AND COMMUTATIVE, SO ARRIVAL ORDER CANNOT BE READ OFF THE RESULT.</b>
    /// Integer addition composes exactly in any order, which is what makes this safe under
    /// C2 where <see cref="Population.Decide"/>'s sum is not. Fork 12 has cost this
    /// project twice and does not reach here.
    /// </remarks>
    public void Absorb(Recurrence other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Scopes += other.Scopes;

        foreach (var (code, seen) in other._alone)
        {
            _alone.TryGetValue(code, out var so_far);
            _alone[code] = so_far + seen;
        }

        foreach (var (pair, seen) in other._together)
        {
            _together.TryGetValue(pair, out var so_far);
            _together[pair] = so_far + seen;
        }
    }
}

public static class Abstracting
{
    /// <summary>The sub-scope most worth naming, or nothing if none has earned it.</summary>
    /// <param name="held">Every commitment resident.</param>
    /// <param name="dials">The gate's numbers.</param>
    public static ImmutableArray<Code>? Shared(IEnumerable<Commitment> held, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(dials);

        return Shared(Recurrence.Of(held, dials), dials);
    }

    /// <summary>The same question, asked of counts that may have come from many holders.</summary>
    /// <param name="counted">What recurred, from one holder or from all of them merged.</param>
    /// <param name="dials">The gate's numbers.</param>
    public static ImmutableArray<Code>? Shared(Recurrence counted, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(counted);
        ArgumentNullException.ThrowIfNull(dials);

        var scopes = counted.Scopes;

        if (scopes < 3) return null;

        var alone = counted.Alone;
        var together = counted.Together;

        if (together.Count == 0) return null;

        (Code, Code)? best = null;
        var strongest = 0.0;

        // ORDERED, BECAUSE THE WINNER WAS OTHERWISE WHICHEVER TIE THE DICTIONARY REACHED
        // FIRST. Two pairs with the same z resolved by hash order, which is stable within
        // a process and nothing at all across two -- and merging counts from several
        // holders is exactly what makes a dictionary's walk differ. Fork 12 by a door
        // nobody had opened yet, since before this there was only ever one table.
        foreach (var (pair, seen) in together.OrderBy(one => one.Key.Left).ThenBy(one => one.Key.Right))
        {
            // THE DESCRIPTION-LENGTH BAR. Naming a pair costs two entries to say what
            // it means and saves one in every scope that holds it, so it only repays
            // from three scopes up. Below that a name is a longer way of saying the
            // same thing.
            if (seen < 3) continue;

            var expected = alone[pair.Left] / (double)scopes
                * (alone[pair.Right] / (double)scopes);

            // A PAIR IN EVERY SCOPE HAS NO VARIANCE TO TEST AGAINST, and refusing it
            // for that would be perverse -- it is the strongest redundancy the
            // population can hold, and it is only reachable when repair chose BOTH
            // codes every single time. Nothing noise-like has that shape, so the
            // description-length bar decides alone.
            var z = expected >= 1.0
                ? double.PositiveInfinity
                : ((seen / (double)scopes) - expected)
                    / Math.Sqrt(expected * (1.0 - expected) / scopes);

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
