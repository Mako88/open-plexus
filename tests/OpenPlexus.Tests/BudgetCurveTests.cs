using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The repair budget swept between its two measured ends — <b>the middle of a curve whose
/// endpoints disagree about what the run is for.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>TWO ARMS CAN PEAK AT DIFFERENT BUDGETS, AND THIS REPO'S OWN TRAPS LIST SAYS COMPARE
/// PEAK TO PEAK.</b> <see cref="BudgetingTests"/> measured sixty-four against a budget that
/// cannot bind, and the ordering inverted: the free end holds 125.8 sound rules to 68.7,
/// mints the most names, carries 0.92 of the hard rounds to 0.79 — and its trailing accuracy
/// on an even world falls. Both readings are ENDS. Nothing between them has ever been run,
/// so a trade recorded as a choice may only be a dial read at two points.
/// </para>
/// <para>
/// <b>AND THE FREE END IS REACHED BY A NUMBER RATHER THAN BY THE OTHER ENUM, WHICH IS WHAT
/// MAKES THIS ONE AXIS.</b> <see cref="Budgeting.Children"/> is a free budget on every world
/// here — a child adds one code, so distinct children are capped by the vocabulary at
/// twenty-two against sixty-four — so it never refuses, and neither does
/// <see cref="Budgeting.Attempts"/> at <see cref="Unlimited"/>. Sweeping the COUNT keeps
/// <see cref="Budgeting"/> fixed, so a row moving is the budget moving and not two settings
/// at once.
/// </para>
/// <para>
/// <b>THE PREDICTION IS WRITTEN DOWN FIRST AND IT IS SPECIFIC ENOUGH TO BE WRONG.</b> If
/// hard-round coverage and sound rules rise monotonically in the budget while trailing
/// accuracy on the even world falls monotonically, the trade is real, no number ranks it, and
/// the choice is John's. If instead there is an interior budget carrying the free end's
/// coverage at the shipped end's accuracy, the trade dissolves and that budget ships.
/// </para>
/// <para>
/// <b>BOTH WORLDS, BECAUSE THE TWO HALVES OF THE TRADE LIVE ON DIFFERENT ONES.</b> The
/// coverage and the sound rules are bought on the skewed world where the base rate pays
/// nothing; the accuracy is sold on the even world where it pays. A curve on one of them
/// would read as a clean win in whichever direction it was taken.
/// </para>
/// </remarks>
public sealed class BudgetCurveTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <summary>Matched to <see cref="BudgetingTests"/>, so the ends are comparable.</summary>
    private const int Seeds = 8;

    /// <summary>No count limit at all, which is the free end of the sweep.</summary>
    private const int Unlimited = int.MaxValue;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="budget">How many separation attempts one parent may ever spend.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    /// <remarks>
    /// <b><see cref="Repairing.EveryRound"/> IS THE SHIPPED TIMING AND IS NOT SWEPT HERE.</b>
    /// The budget read as inert under the old timing because the lineages that would have
    /// spent it were never blamed, so a grid over both timings would be re-running a question
    /// already answered rather than asking this one.
    /// </remarks>
    private static Learned Run(int address, double skew, int budget, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(new CommittingSettings { Budget = budget }, seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>SEVEN BUDGETS ACROSS THE TWO ENDS ALREADY MEASURED.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_an_interior_repair_budget_beats_both_measured_ends()
    {
        var budgets = new[] { 8, 16, 32, 64, 128, 256, Unlimited };

        // THE GRID MUST BRACKET WHAT SHIPS, OR IT IS A CURVE ABOUT SOMEBODY ELSE'S BRAIN.
        // A default moved out from under this list would leave every row still printing and
        // none of them standing on the running configuration -- the pass-shaped failure this
        // repo keeps finding, wearing a sweep. Cheaper as an assert than as a reading nobody
        // re-takes.
        Assert.Contains(new CommittingSettings().Budget, budgets);

        // BOTH WIDTHS, BECAUSE THE PLAN ALREADY CARRIES THIS OPTIMUM AS ONE THAT MOVES WITH
        // THE RELEVANT BITS -- and a budget in ATTEMPTS is spent mostly on re-derivation,
        // whose rate is a function of the vocabulary. So a peak at one width is not a
        // number this brain gets to keep unless the other width agrees: a dial whose best
        // value moves with the world is a world reaching into the brain, which is the one
        // thing the constraints refuse outright.
        foreach (var (address, skew) in new[] { (3, 0.8), (3, 0.0), (2, 0.8), (2, 0.0) })
        {
            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Seeds} seeds, {Rounds} rounds, every-round repair ===");

            foreach (var budget in budgets)
            {
                // ONE RUN PER SEED, SHARED BY EVERY READING BELOW -- the same discipline
                // `BudgetTests` records. Seven readings asked independently would run one
                // configuration seven times and print one measurement as seven.
                var once = new Dictionary<int, Learned>();

                Learned Cached(int seed)
                {
                    if (!once.TryGetValue(seed, out var learned))
                        once[seed] = learned = Run(address, skew, budget, seed);

                    return learned;
                }

                var cell = budget == Unlimited ? "free" : budget.ToString();

                foreach (var reading in new (string What, Func<Learned, double> Of)[]
                {
                    // FIRST, BECAUSE IT IS THE ONE READING SKEW CANNOT GAME. A true rule
                    // firing on a round the base rate gets wrong is what `found` and
                    // `recent` both fail to separate on a tilted world.
                    ("paying", one => one.Census!.Paying),
                    ("sound", one => one.Sound),
                    ("named", one => one.Named),
                    ("stacked", one => one.Stacked),
                    // THE TWO DENOMINATORS, BECAUSE A NAMING COUNT WITHOUT THEM IS A REPAIR
                    // RESULT WEARING AN ABSTRACTION'S NAME. Rung five is offered only scopes
                    // of two codes or more past the floor, and covering mints one code and
                    // nothing longer -- so its whole input is repair's surviving output, and
                    // the budget moves that directly.
                    ("eligible", one => one.Eligible),
                    ("stackable", one => one.Stackable),
                    // BESIDE EVERY SCORE, BECAUSE AN ACCURACY CAN BE HIT BY MEMORISING.
                    ("residents", one => one.Resident),
                    ("exhausted", one => one.Exhausted),
                    ("repaired", one => one.Repaired),
                    ("recent", one => one.Recent),
                })
                {
                    await Fixture.ReadAsync(output, cell, Seeds, Cached, reading);
                }

                output.WriteLine("");
            }
        }
    }
}
