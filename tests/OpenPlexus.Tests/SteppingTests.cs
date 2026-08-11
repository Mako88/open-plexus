using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a repair that adds TWO codes at once buys, and what it costs —
/// <b>fork 74 built rather than argued about.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE CHAIN IS THE COST AND EVERY OTHER ARM ON THIS BENCH MAKES A STEP LIKELIER
/// RATHER THAN SHORTER.</b> A fresh child inherits no table, so it re-earns
/// <c>Floor</c> misses before it may add its next code; the repairs that ever buy a hard
/// round sit at the world's minimum sound depth, three codes at six bits and four at
/// eleven; genesis mints one. So a chain pays two or three floors and only its last rung
/// pays anything back, which is the same phenomenon as the scaling exponent seen from the
/// search's end.
/// </para>
/// <para>
/// <b>AND THE ONE READING TAKEN BEFORE THE MECHANISM CUTS AGAINST IT, WHICH IS WHY THIS
/// IS A GRID AND NOT A DEFAULT.</b> A parent's table predicts its child's first choice
/// under a third of the time where outcomes are even and 54.9% where they are skewed. That
/// is a majority in one place and a minority in two, and a majority settles nothing on its
/// own while the two outcomes cost differently: a right second code saves a floor, a wrong
/// one mints a child too narrow to be sound. Nothing has priced the second against the
/// first, and a share cannot — only a run holding both populations can.
/// </para>
/// <para>
/// <b>THE PREDICTION IS WRITTEN DOWN FIRST AND IT IS ABOUT DEPTH RATHER THAN SCORE.</b>
/// One code at a time reaches three in two steps and four in three; a pair reaches three
/// in one and then FIVE, overshooting the four eleven bits needs — which is exactly what
/// the naming loop did when it let repair step two codes at once. So this should pay at
/// six bits and overshoot at eleven, and if it pays at both then the depth account is
/// incomplete rather than confirmed.
/// </para>
/// <para>
/// <b>AND THE COLUMN THAT SETTLES IT IS <c>Census.Paying</c> WITH THE CARRIERS' MEAN SCOPE
/// BESIDE IT, NEVER ACCURACY.</b> Accuracy has a floor of four in five on the skewed world
/// and <c>found</c> and <c>sound</c> are reachable by rules firing where guessing already
/// works — this repo's own trap about a grid ranking arms on columns a skewed world raises
/// for free, and two grids have already walked into it.
/// </para>
/// </remarks>
public sealed class SteppingTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private const int Seeds = 6;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <param name="stepping">How many codes one repair adds.</param>
    /// <remarks>
    /// <b>THE RUN IS RETURNED BESIDE THE SCORE BECAUSE `Population.Paired` IS NOT ON
    /// <see cref="Learned"/>.</b> That record carries what every world shares; a count of
    /// repairs that took two codes is about one arm on one branch of the machine, and
    /// widening the shared record for it would put a column on every grid in the repo.
    /// </remarks>
    private static (Learned Learned, MultiplexerRun Run) Run(
        int address, double skew, int seed, Stepping stepping)
    {
        var run = new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(new CommittingSettings { Stepping = stepping }, seed),
            seed,
            census: true);

        return (run.Run(Rounds), run);
    }

    /// <summary>
    /// <b>THE READING: two arms, three worlds, and the ungameable columns.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WHAT A ONE-PASS STEP CAN ONLY DIFFER ON IS THE ROUNDS A SECOND CODE WAS
    /// CERTIFIABLE</b>, since <see cref="Stepping.Pair"/> degrades to
    /// <see cref="Stepping.OneCode"/> wherever <c>Repair.Runner</c> comes back empty. So a
    /// flat grid is ambiguous between <i>a two-code step is worthless</i> and <i>that set is
    /// small</i>, and the repair counts are printed beside the score to tell them apart —
    /// this repo's own trap about two arms scoring alike without being the same mechanism,
    /// which cost four grids in one session.
    /// </para>
    /// <para>
    /// <b>AND THE HIT RATE IS THE SEARCH'S OWN NUMBER RATHER THAN THE POPULATION'S.</b> Four
    /// to twenty-six per cent of repairs ever buy a hard round; if collapsing the chain
    /// works, that share rises because a step that lands on the minimum depth in one pass is
    /// a step that could pay, where every shorter one pays nothing by construction.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void What_a_repair_that_adds_two_codes_at_once_buys_and_what_it_costs()
    {
        output.WriteLine(
            "stepping | paying | carriers | hit rate | their mean scope "
            + "| sound | unsound | residents | repairs | took two | recent");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

            foreach (var stepping in new[] { Stepping.OneCode, Stepping.Pair })
            {
                var paying = new List<double>();
                var carried = new List<double>();
                var rate = new List<double>();
                var scope = new List<double>();
                var sound = new List<double>();
                var unsound = new List<double>();
                var resident = new List<double>();
                var repairs = new List<double>();
                var took = new List<double>();
                var recent = new List<double>();

                for (var seed = 1; seed <= Seeds; seed++)
                {
                    var (learned, run) = Run(address, skew, seed, stepping);
                    var census = learned.Census!;

                    // THE SHARE OF REPAIR ATTEMPTS THAT ACTUALLY TOOK A SECOND CODE, which
                    // is the only thing separating a mechanism that does nothing from one
                    // that almost never gets to run. Nought under `OneCode` by construction.
                    // AGAINST ATTEMPTS AND NOT BIRTHS: most attempts collide with a scope
                    // the population already holds, so `Repaired` as a denominator gives a
                    // share of thirty-nine.
                    took.Add(run.Held.Stepped == 0
                        ? 0.0
                        : run.Held.Paired / (double)run.Held.Stepped);

                    paying.Add(census.Paying);
                    carried.Add(census.Narrowed);
                    scope.Add(census.Codes);
                    sound.Add(learned.Sound);
                    unsound.Add(learned.Unsound);
                    resident.Add(learned.Resident);
                    repairs.Add(learned.Repaired);
                    recent.Add(learned.Recent);

                    rate.Add(learned.Repaired == 0
                        ? 0.0
                        : census.Narrowed / (double)learned.Repaired);
                }

                output.WriteLine(
                    $"{stepping,-8} | {Sweep.Spread(paying)} | {Sweep.Spread(carried, "F1")} "
                    + $"| {Sweep.Spread(rate)} | {Sweep.Spread(scope, "F2")} "
                    + $"| {Sweep.Spread(sound, "F1")} | {Sweep.Spread(unsound, "F1")} "
                    + $"| {Sweep.Spread(resident, "F1")} | {Sweep.Spread(repairs, "F0")} "
                    + $"| {Sweep.Spread(took)} | {Sweep.Spread(recent)}");
            }
        }

        // NO BAR. What a two-code step buys has never been measured, and a threshold written
        // before the first reading would be the answer rather than the finding. The
        // PREDICTION is on the class, where it can be read against the grid it was written
        // for rather than checked by an assertion that would have to be edited to pass.
    }

    /// <summary>
    /// <b>THE ARM IS WIRED AND IT BUILDS A DIFFERENT POPULATION — the check a score
    /// cannot make.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A DIAL CAN BE DECLARED, DOCUMENTED, PASSED EVERYWHERE AND CONNECTED TO NOTHING</b>,
    /// and a grid of two arms landing on top of each other reads exactly the same whether the
    /// mechanism is inert or unmounted. This asserts that the two arms DIFFER and never which
    /// way, because a prediction written into a wiring check fails two ways and reads the
    /// same.
    /// </para>
    /// <para>
    /// <b>AND IT ASSERTS ON <c>Population.Paired</c> RATHER THAN ON A SCORE, which is the
    /// difference between checking a wire and predicting a result.</b> That counter is nought
    /// under one code at a time by construction and positive under a pair the moment a second
    /// code reaches a scope — so it separates <i>unmounted</i> from <i>mounted and nothing to
    /// do</i>, where every downstream count could coincide for reasons of its own.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_pair_step_reaches_a_second_code_and_one_at_a_time_never_does()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.8) })
        {
            var (one, oneRun) = Run(address, skew, seed: 1, Stepping.OneCode);
            var (pair, pairRun) = Run(address, skew, seed: 1, Stepping.Pair);

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | one code "
                + $"born {one.Repaired,6} attempts {oneRun.Held.Stepped,6} "
                + $"took two {oneRun.Held.Paired,6} | pair "
                + $"born {pair.Repaired,6} attempts {pairRun.Held.Stepped,6} "
                + $"took two {pairRun.Held.Paired,6}");

            Assert.Equal(0, oneRun.Held.Paired);

            Assert.True(pairRun.Held.Paired > 0,
                "`Stepping.Pair` never added a second code, so either `Repair.Runner` "
                + "certifies nothing on this world or the arm is not wired");
        }
    }
}
