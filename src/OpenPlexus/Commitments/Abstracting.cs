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

/// <summary>
/// Which of rung five's bars stopped it, or none at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE PARTITION <see cref="Abstracting.Shared(Recurrence, CommittingSettings)"/> NEVER HAD, AND THE REASON A YIELD
/// COULD FALL WITH NO ACCOUNT OF WHY.</b> The gate answers a pair or nothing, so every
/// reading of it in this repo has been a count of the times it spoke — and a count of
/// silences says nothing whatever about which of four completely different things
/// happened. <c>Tally.Eligible</c> put a denominator under the successes; this puts one
/// under the failures.
/// </para>
/// <para>
/// <b>AND THE FOUR REFUSALS ARE NOT DEGREES OF ONE THING.</b> <see cref="Scarce"/> and
/// <see cref="Rare"/> are the population being too small or too varied to hold a
/// redundancy; <see cref="Independent"/> is a redundancy the counts say is not there;
/// <see cref="Uncertain"/> is one that is there and cannot be certified. Only the last
/// two are about the STATISTIC, and a mechanism moving between them is moving in a
/// direction no name count can show.
/// </para>
/// </remarks>
public enum Refused
{
    /// <summary>Nothing refused it, and a name was proposed.</summary>
    Nothing,

    /// <summary>Fewer than three eligible scopes existed to count over.</summary>
    Scarce,

    /// <summary>Eligible scopes existed and none of them contributed a pair.</summary>
    /// <remarks>
    /// <b>UNREACHABLE WHILE <see cref="Recurrence.Eligible"/> WANTS TWO CODES, and kept
    /// because that is a fact about the caller and not about this gate.</b> A reading that
    /// ever lands here is a reading taken over scopes some other rule admitted.
    /// </remarks>
    Unpaired,

    /// <summary>No pair recurred across the three scopes a name has to repay.</summary>
    Rare,

    /// <summary>
    /// A pair repaid and none was commoner than independent scopes would have made it.
    /// </summary>
    Independent,

    /// <summary>The strongest pair did not clear the bar once corrected for the search.</summary>
    Uncertain,
}

/// <summary>
/// Everything the naming gate read and what it did with it — <b>the gate's own working,
/// which is the only place its refusals can be told apart.</b>
/// </summary>
/// <remarks>
/// <b>A READING RATHER THAN A LOG, so it can be counted rather than eyeballed.</b> This
/// is what <see cref="Abstracting.Propose"/> returns on every ask, and
/// <see cref="Abstracting.Shared(Recurrence, CommittingSettings)"/> is the one field of it
/// the learner needs. Nothing here
/// decides anything; a value that ever gates a mechanism has stopped being an instrument.
/// </remarks>
public readonly record struct Proposed
{
    /// <summary>Eligible scopes the counts were taken over.</summary>
    public required int Scopes { get; init; }

    /// <summary>
    /// Distinct pairs considered — <b>the correction's multiplier, so it is a bar and not
    /// merely a workload.</b>
    /// </summary>
    public required int Candidates { get; init; }

    /// <summary>Of those, the pairs that recurred often enough to repay a name.</summary>
    public required int Repaying { get; init; }

    /// <summary>
    /// The best z among the repaying pairs, whatever its sign — <b>and not the best among
    /// the pairs that WON, which is what the selection loop reports.</b>
    /// </summary>
    /// <remarks>
    /// <b>SIGNED, BECAUSE THE INTERESTING FAILURE IS BELOW NOUGHT AND THE GATE CANNOT SAY
    /// SO.</b> Selection starts at zero and takes strict improvements, so a population
    /// whose every repaying pair is RARER than chance reads identically to one that had no
    /// repaying pair at all. Those are opposite findings.
    /// <see cref="double.NegativeInfinity"/> where nothing repaid.
    /// </remarks>
    public required double Strongest { get; init; }

    /// <summary>
    /// <see cref="Strongest"/> put through the tail and multiplied by
    /// <see cref="Candidates"/>, or <see cref="double.NaN"/> where there was nothing to
    /// correct.
    /// </summary>
    public required double Corrected { get; init; }

    /// <inheritdoc cref="Refused"/>
    public required Refused Refused { get; init; }

    /// <summary>The pair worth naming, or nothing.</summary>
    public required ImmutableArray<Code>? Named { get; init; }
}

/// <summary>
/// Whether rung five may name more than one thing per ask.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE CEILING NOBODY CHOSE.</b> <see cref="Population.Abstract"/> runs on the sweep
/// calendar and minted at most once a call, so a twenty-thousand-round run offered the rung
/// twenty chances and it already took eighteen of them. What a population has to name is a
/// fact about the population, and it was being metered by a clock it shares with widening,
/// subsumption and culling.
/// </para>
/// <para>
/// <b>AND <see cref="UntilRefused"/> IS ONLY BUILDABLE BECAUSE A NAMED PAIR STOPS BEING A
/// CANDIDATE.</b> Under the arm deleted one commit before this, the loop would have proposed
/// the same pair forever wherever a rewrite collided. The two findings compose into one
/// mechanism, and neither is a number.
/// </para>
/// </remarks>
public enum Minting
{
    /// <summary>One name an ask, whatever else is worth naming. What has always run.</summary>
    Once,

    /// <summary>Keep asking until the gate refuses.</summary>
    UntilRefused,
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
    /// <remarks>
    /// <b>ONE FIELD OF <see cref="Propose"/> AND NOT A SECOND IMPLEMENTATION.</b> Two copies of
    /// a gate is two chances for an instrument to describe a machine that is not running,
    /// which is the fault this repo has already paid for twice.
    /// </remarks>
    public static ImmutableArray<Code>? Shared(Recurrence counted, CommittingSettings dials) =>
        Propose(counted, dials).Named;

    /// <summary>
    /// The same question, and everything the gate looked at on the way to answering it.
    /// </summary>
    /// <param name="counted">What recurred, from one holder or from all of them merged.</param>
    /// <param name="dials">The gate's numbers.</param>
    /// <param name="named">
    /// What this population has already named, or nothing where the caller has no one
    /// vocabulary to speak of.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>A PAIR ALREADY NAMED IS NOT A CANDIDATE, AND THE ARM THAT ALLOWED ONE IS DELETED.</b>
    /// Proposing a pair twice mints nothing — a name's identity is its members — and this
    /// mints at most once an ask, so an ask spent on a known pair cannot add vocabulary. The
    /// old arm did that on a third of its asks at the shipped cadence. See the plan's
    /// revival row.
    /// </para>
    /// <para>
    /// <b>AND DROPPING THEM BEFORE THE CANDIDATE COUNT IS PRINCIPLE RATHER THAN GAIN.</b> The
    /// correction multiplies by what was searched, and a pair that could never be accepted
    /// was not searched — but the refusal counts did not move between the arms, so the whole
    /// of the measured effect was the ask being spent where it could still buy a name.
    /// </para>
    /// <para>
    /// <b>NOTHING PASSED MEANS NOTHING SKIPPED, which is what a merge and a test want.</b>
    /// There is no one population whose vocabulary it would be.
    /// </para>
    /// </remarks>
    public static Proposed Propose(
        Recurrence counted, CommittingSettings dials, Naming? named = null)
    {
        ArgumentNullException.ThrowIfNull(counted);
        ArgumentNullException.ThrowIfNull(dials);

        var scopes = counted.Scopes;

        if (scopes < 3) return Nothing(scopes, 0, Refused.Scarce);

        var alone = counted.Alone;
        var together = counted.Together;

        if (together.Count == 0) return Nothing(scopes, 0, Refused.Unpaired);

        (Code, Code)? best = null;
        var strongest = double.NegativeInfinity;
        var repaying = 0;
        var candidates = 0;

        // ORDERED, BECAUSE THE WINNER WAS OTHERWISE WHICHEVER TIE THE DICTIONARY REACHED
        // FIRST. Two pairs with the same z resolved by hash order, which is stable within
        // a process and nothing at all across two -- and merging counts from several
        // holders is exactly what makes a dictionary's walk differ. Fork 12 by a door
        // nobody had opened yet, since before this there was only ever one table.
        foreach (var (pair, seen) in together.OrderBy(one => one.Key.Left).ThenBy(one => one.Key.Right))
        {
            // A CODE CANNOT CO-OCCUR WITH ITSELF, AND SUCH A ROW WOULD WIN EVERY TIME.
            // `Recurrence.Of` cannot make one -- a scope is `Distinct().Order()` -- but
            // `From` takes whatever arrived, and a self-pair reads as a share of p against
            // an expectation of p squared, so its z is enormous and it takes the argmax.
            // The name minted for it then throws, because a name for fewer than two codes
            // says nothing. A sender's bug crashing a receiver, in the one type built to
            // cross a wire.
            if (pair.Left == pair.Right) continue;

            // A PAIR ALREADY NAMED CANNOT ADD VOCABULARY, so under `Fresh` it is not a
            // candidate -- and it is dropped BEFORE the count that the correction divides
            // among, because a candidate the gate would never accept is not one it searched.
            if (named is not null && named.Knows(Naming.Name([pair.Left, pair.Right]))) continue;

            candidates++;

            // THE DESCRIPTION-LENGTH BAR. Naming a pair costs two entries to say what
            // it means and saves one in every scope that holds it, so it only repays
            // from three scopes up. Below that a name is a longer way of saying the
            // same thing.
            if (seen < 3) continue;

            repaying++;

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

            // STRICT, SO A TIE GOES TO THE PAIR THE ORDERING REACHED FIRST rather than to
            // the last one written. Starting below nought rather than at it is what lets
            // the reading carry a negative peak; the choice below still needs a positive
            // one, so what the learner sees is unchanged.
            if (z <= strongest) continue;

            strongest = z;
            best = pair;
        }

        // AND `Unpaired` IS REACHABLE FROM `Fresh` AS WELL AS FROM A HAND-WRITTEN TABLE:
        // a population that has named every pair it holds has no candidate left, which is a
        // completely different state from holding no pair.
        if (candidates == 0) return Nothing(scopes, 0, Refused.Unpaired);

        if (repaying == 0) return Nothing(scopes, candidates, Refused.Rare);

        // A PAIR NO COMMONER THAN CHANCE IS NOT A REDUNDANCY, and the old loop could not
        // say that it had found one. Selection began at nought, so this case and the one
        // above both came back as no candidate at all.
        if (best is not { } chosen || strongest <= 0.0)
            return Nothing(scopes, candidates, Refused.Independent) with
            {
                Repaying = repaying,
                Strongest = strongest,
            };

        // CORRECTED FOR THE PAIRS LOOKED AT, exactly as repair's bar is. Taking the
        // best of thousands of candidates clears any fixed bar on chance alone, and
        // that is the failure this whole gate exists against.
        var corrected = Normal.Tail(strongest) * candidates;

        return new Proposed
        {
            Scopes = scopes,
            Candidates = candidates,
            Repaying = repaying,
            Strongest = strongest,
            Corrected = corrected,
            Refused = corrected <= dials.Alpha ? Refused.Nothing : Refused.Uncertain,
            Named = corrected <= dials.Alpha
                ? [chosen.Item1, chosen.Item2]
                : null,
        };
    }

    /// <summary>A reading that got no further than one of the bars before the statistic.</summary>
    private static Proposed Nothing(int scopes, int candidates, Refused why) =>
        new()
        {
            Scopes = scopes,
            Candidates = candidates,
            Repaying = 0,
            Strongest = double.NegativeInfinity,
            Corrected = double.NaN,
            Refused = why,
            Named = null,
        };
}
