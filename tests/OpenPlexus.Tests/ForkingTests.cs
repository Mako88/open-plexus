using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a parent's attempts buy when they are not allowed to arrive where it already is.
/// </summary>
/// <remarks>
/// <para>
/// <b>REPAIR IS DETERMINISTIC AND ITS TABLE MOVES BY ONE ENTRY A FIRING, so a parent
/// proposes the same child for thousands of rounds.</b> The argmax is stable, the code it
/// names stays in the tally because a commitment's table skips only its OWN scope, and
/// nothing anywhere asks whether this parent has been there before. Collisions run twenty to
/// fifty times the births at every majority rung, which is that fact counted.
/// </para>
/// <para>
/// <b>SO THE BUDGET HAS ALWAYS BEEN A RE-DERIVATION LIMIT, WHICH IS ALREADY A FINDING HERE
/// AND HAS NEVER BEEN ACTED ON.</b> A parent under two hundred and fifty-six spends nearly
/// all of it arriving where it already is. What has not been tried is spending those attempts
/// somewhere else.
/// </para>
/// <para>
/// <b>AND QUANTITY IS THE ONE ACCOUNT OF THE UNCOVERED ROUNDS THE EVIDENCE CONFIRMS.</b> A
/// child fires only where its added code is present, so covering what a parent is right about
/// takes MANY children, and <c>uncovered</c> falls monotonically as the budget rises. If the
/// attempts bought distinct children the search would be twenty to fifty times what every
/// number here was taken under, at the same budget.
/// </para>
/// <para>
/// <b>IT IS THE OPPOSITE HALF OF THE SEARCH FROM THE STEP LENGTH, WHICH IS WHY IT IS WORTH
/// TRYING AFTER THAT FAILED.</b> A two-code step made each attempt reach DEEPER and lost
/// coverage by overshooting the world's minimum sound depth; this makes each attempt reach
/// somewhere ELSE at the same depth. The chain's length does not change, so the failure that
/// closed fork 74 does not carry over.
/// </para>
/// <para>
/// <b>AND THE HAZARD IS NAMED BEFORE THE GRID: more distinct children is more population, and
/// this doc already carries a row about an arm that raised every count and lowered
/// coverage.</b> So the reading is <c>Census.Paying</c> with the residents and the carriers'
/// mean scope beside it, and never accuracy — which has a floor of four in five under skew.
/// </para>
/// </remarks>
public sealed class ForkingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private const int Seeds = 6;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <param name="forking">Whether a parent may propose a fork it has already made.</param>
    /// <param name="budget">How many times one commitment may separate, or nothing for the default.</param>
    private static (Learned Learned, MultiplexerRun Run) Run(
        int address, double skew, int seed, Forking forking, int? budget = null)
    {
        var dials = new CommittingSettings { Forking = forking };

        var run = new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(budget is null ? dials : dials with { Budget = budget.Value }, seed),
            seed,
            census: true);

        return (run.Run(Rounds), run);
    }

    /// <summary>Every repair that reached a scope the population already held.</summary>
    private static long Collisions(MultiplexerRun run) =>
        run.Held.Lineages.Values.Sum(one => one.Collided);

    /// <summary>
    /// <b>THE READING: two arms, three worlds, and the ungameable columns.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE COLLISION COUNT IS WHAT SAYS THE MECHANISM RAN, and it goes beside the score
    /// rather than instead of it.</b> Two arms landing together is ambiguous between <i>a
    /// distinct child is worth nothing</i> and <i>a parent rarely had a second candidate past
    /// the bar</i>, and only a count of what was BUILT tells those apart — the lesson a
    /// two-code step cost this bench a grid to learn.
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_parents_attempts_buy_when_they_may_not_land_where_it_has_been()
    {
        output.WriteLine(
            "forking  | paying | uncovered | carriers | hit rate | their mean scope "
            + "| sound | unsound | residents | born | collided | recent");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

            foreach (var forking in new[] { Forking.Repeated, Forking.Distinct })
            {
                var paying = new List<double>();
                var open = new List<double>();
                var carried = new List<double>();
                var rate = new List<double>();
                var scope = new List<double>();
                var sound = new List<double>();
                var unsound = new List<double>();
                var resident = new List<double>();
                var born = new List<double>();
                var collided = new List<double>();
                var recent = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var (learned, run) = Run(address, skew, seed, forking);
                    var census = learned.Census!;

                    paying.Add(census.Paying);

                    // THE COLUMN FORK 76 IS ABOUT, AND THE ONE THE BUDGET CURVE ALREADY
                    // MOVED. Rounds where nothing sound advocating the right answer fired
                    // fall from 1,354 to 472 as the budget rises, and quantity is the only
                    // account of them the evidence confirms -- so if distinct children buy
                    // anything, they buy it here and this is where it is read.
                    open.Add(census.Uncovered);
                    carried.Add(census.Narrowed);
                    scope.Add(census.Codes);
                    sound.Add(learned.Sound);
                    unsound.Add(learned.Unsound);
                    resident.Add(learned.Resident);
                    born.Add(learned.Repaired);
                    collided.Add(Collisions(run));
                    recent.Add(learned.Recent);

                    rate.Add(learned.Repaired == 0
                        ? 0.0
                        : census.Narrowed / (double)learned.Repaired);
                }

                output.WriteLine(
                    $"{forking,-8} | {Sweep.Spread(paying)} | {Sweep.Spread(open, "F0")} "
                    + $"| {Sweep.Spread(carried, "F1")} "
                    + $"| {Sweep.Spread(rate)} | {Sweep.Spread(scope, "F2")} "
                    + $"| {Sweep.Spread(sound, "F1")} | {Sweep.Spread(unsound, "F1")} "
                    + $"| {Sweep.Spread(resident, "F1")} | {Sweep.Spread(born, "F0")} "
                    + $"| {Sweep.Spread(collided, "F0")} | {Sweep.Spread(recent)}");
            }
        }

        // NO BAR. What a parent's attempts buy when they may not repeat has never been
        // measured, and a threshold written before the first reading would be the answer
        // rather than the finding.
    }


    /// <summary>
    /// <b>WHAT `Budget` MEANS ONCE A PARENT'S ATTEMPTS BUY DISTINCT CHILDREN — the first
    /// time this number has ever been a search limit.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 66 WAS CLOSED WITH THE ANSWER *A RE-DERIVATION LIMIT*, AND THAT ANSWER WAS
    /// CONDITIONAL ON A MECHANISM NOBODY HAD CHANGED.</b> Under the rule that ships a parent
    /// proposes the same child until its table drifts, so two hundred and fifty-six attempts
    /// buy two or three distinct children and the number cannot cap a search it is not
    /// running. Refuse a parent its spent codes and every attempt buys a NEW child, so the
    /// same dial becomes what its documentation always claimed.
    /// </para>
    /// <para>
    /// <b>AND IT DOES NOT BIND AT ITS SHIPPED VALUE, WHICH IS WHY THE LEVELS HERE ARE SMALL.</b>
    /// A child adds one code, so a parent's distinct children are capped by the vocabulary —
    /// twenty-two at eleven bits, against a budget of two hundred and fifty-six. Every level
    /// above the vocabulary is the same arm, and the interesting range is the one nobody could
    /// reach before.
    /// </para>
    /// <para>
    /// <b>THE READING IT IS FOR IS THE ELEVEN-BIT FLOOD.</b> Distinct forking takes six bits to
    /// a perfect score on every seed at the same population, and takes eleven bits to 1,144
    /// residents for 1.6 standard errors of coverage — every count rising while the reading
    /// barely moves, which is the shape of an arm this doc has already deleted once. If the
    /// re-derivation was acting as an accidental brake, a real cap is what replaces it.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_the_budget_caps_once_it_is_capping_a_search_rather_than_a_re_derivation()
    {
        output.WriteLine(
            "arm              | paying | uncovered | carriers | hit rate "
            + "| sound | unsound | residents | born | recent");

        foreach (var (address, skew) in new[] { (3, 0.0), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

            // THE BASELINE FIRST AND NAMED AS ONE. Every number this repo holds was taken
            // under it, so a column that does not beat it has bought nothing whatever it
            // does to the counts.
            foreach (var (arm, forking, budget) in new (string Arm, Forking Forking, int? Budget)[]
            {
                ("repeated 256", Forking.Repeated, null),
                ("distinct 2", Forking.Distinct, 2),
                ("distinct 4", Forking.Distinct, 4),
                ("distinct 8", Forking.Distinct, 8),
                ("distinct 256", Forking.Distinct, null),
            })
            {
                var paying = new List<double>();
                var open = new List<double>();
                var carried = new List<double>();
                var rate = new List<double>();
                var sound = new List<double>();
                var unsound = new List<double>();
                var resident = new List<double>();
                var born = new List<double>();
                var recent = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var (learned, _) = Run(address, skew, seed, forking, budget);
                    var census = learned.Census!;

                    paying.Add(census.Paying);
                    open.Add(census.Uncovered);
                    carried.Add(census.Narrowed);
                    sound.Add(learned.Sound);
                    unsound.Add(learned.Unsound);
                    resident.Add(learned.Resident);
                    born.Add(learned.Repaired);
                    recent.Add(learned.Recent);

                    rate.Add(learned.Repaired == 0
                        ? 0.0
                        : census.Narrowed / (double)learned.Repaired);
                }

                output.WriteLine(
                    $"{arm,-16} | {Sweep.Spread(paying)} | {Sweep.Spread(open, "F0")} "
                    + $"| {Sweep.Spread(carried, "F1")} | {Sweep.Spread(rate)} "
                    + $"| {Sweep.Spread(sound, "F1")} | {Sweep.Spread(unsound, "F1")} "
                    + $"| {Sweep.Spread(resident, "F1")} | {Sweep.Spread(born, "F0")} "
                    + $"| {Sweep.Spread(recent)}");
            }
        }

        // NO BAR. What this dial does once it caps a search has never been measured, and a
        // threshold written before the first reading would be the answer rather than the
        // finding.
    }

    /// <summary>
    /// <b>THE ARM IS WIRED AND WHAT IT CLAIMS TO REMOVE IS WHAT FALLS.</b>
    /// </summary>
    /// <remarks>
    /// <b>IT ASSERTS ON COLLISIONS RATHER THAN ON A SCORE, because that is the event the rule
    /// is about.</b> How much of the world was FOUND is printed beside it and asserted on by
    /// nothing — this repo's rule is that an accuracy is reported next to a count of the
    /// world's own rules held, and the grid that carries the score has residents and sound
    /// rules but not that. A parent refused its own spent codes cannot arrive at its own earlier
    /// child, so the count has to fall — and a run where it does not is unmounted rather than
    /// inert. Collisions do not reach nought, because two DIFFERENT parents can still reach
    /// one scope and nothing here forbids that.
    /// </remarks>
    [Fact]
    public void Refusing_a_parent_its_own_spent_codes_removes_the_collisions_it_was_making()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.8) })
        {
            var (repeated, repeatedRun) = Run(address, skew, seed: 1, Forking.Repeated);
            var (distinct, distinctRun) = Run(address, skew, seed: 1, Forking.Distinct);

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | repeated "
                + $"born {repeated.Repaired,6} collided {Collisions(repeatedRun),7} "
                + $"found {repeated.Found}/{repeated.Truths} | "
                + $"distinct born {distinct.Repaired,6} "
                + $"collided {Collisions(distinctRun),7} "
                + $"found {distinct.Found}/{distinct.Truths}");

            Assert.True(
                Collisions(distinctRun) < Collisions(repeatedRun),
                "refusing a parent the codes it has already forked on did not lower the "
                + "collisions, so either the ledger is not reaching the candidate walk or "
                + "the collisions were never a parent repeating itself");
        }
    }
}
