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
/// <b>Two arms can peak at different budgets.</b> And this repo's own traps list says compare
/// peak to peak. <see cref="BudgetingTests"/> measured sixty-four against a budget that
/// cannot bind, and the ordering inverted: the free end holds 125.8 sound rules to 68.7,
/// mints the most names, carries 0.92 of the hard rounds to 0.79 — and its trailing accuracy
/// on an even world falls. Both readings are ENDS. Nothing between them has ever been run,
/// so a trade recorded as a choice may only be a dial read at two points.
/// </para>
/// <para>
/// <b>And the free end</b> is reached by a number rather than by the other enum, which is what
/// makes this one axis. <see cref="Budgeting.Children"/> is a free budget on every world
/// here — a child adds one code, so distinct children are capped by the vocabulary at
/// twenty-two against sixty-four — so it never refuses, and neither does
/// <see cref="Budgeting.Attempts"/> at <see cref="Unlimited"/>. Sweeping the COUNT keeps
/// <see cref="Budgeting"/> fixed, so a row moving is the budget moving and not two settings
/// at once.
/// </para>
/// <para>
/// <b>The prediction is written down first and it is specific enough to be wrong.</b> If
/// hard-round coverage and sound rules rise monotonically in the budget while trailing
/// accuracy on the even world falls monotonically, the trade is real, no number ranks it, and
/// the choice is John's. If instead there is an interior budget carrying the free end's
/// coverage at the shipped end's accuracy, the trade dissolves and that budget ships.
/// </para>
/// <para>
/// <b>Both worlds</b>, because the two halves of the trade live on different ones. The
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
    /// <b><see cref="Repairing.EveryRound"/> Is the shipped timing and is not swept here.</b>
    /// The budget read as inert under the old timing because the lineages that would have
    /// spent it were never blamed, so a grid over both timings would be re-running a question
    /// already answered rather than asking this one.
    /// </remarks>
    /// <param name="counting">What a parent's allowance is measured in.</param>
    private static Learned Run(
        int address,
        double skew,
        int budget,
        int seed,
        Budgeting counting = Budgeting.Attempts) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(
                new CommittingSettings
                {
                    Budget = budget,
                    Budgeting = counting,

                // AND `Forking` is pinned, which it was not when this dial shipped its new
                // value. A fixture inherits every dial it does not pin, and this grid sweeps
                // the one number whose meaning `Forking` decides -- under `Repeated` a parent
                // re-proposed the same child and the budget capped RE-DERIVATION; under
                // `Distinct` it caps the search. Left unpinned, the same rows would have
                // quietly changed question. Pinned to what runs, so the curve is about the
                // machine that exists; every reading recorded before it was taken under
                // `Repeated` and is owed a re-take.
                Forking = Forking.Distinct,
                }, seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>Seven budgets across the two ends already measured.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_an_interior_repair_budget_beats_both_measured_ends()
    {
        var budgets = Fixture.Budgets;

        // The grid must bracket what ships, or it is a curve about somebody else's brain.
        // A default moved out from under this list would leave every row still printing and
        // none of them standing on the running configuration -- the pass-shaped failure this
        // repo keeps finding, wearing a sweep. Cheaper as an assert than as a reading nobody
        // re-takes.
        Assert.Contains(new CommittingSettings().Budget, budgets);

        // Both widths, because the plan already carries this optimum as one that moves with
        // the relevant bits -- and a budget in ATTEMPTS is spent mostly on re-derivation,
        // whose rate is a function of the vocabulary. So a peak at one width is not a
        // number this brain gets to keep unless the other width agrees: a dial whose best
        // value moves with the world is a world reaching into the brain, which is the one
        // thing the constraints refuse outright.
        foreach (var (address, skew) in Fixture.Curve)
        {
            output.WriteLine($"=== {address + (1 << address)} bits, skew {skew:F1}, "
                + $"{Seeds} seeds, {Rounds} rounds, every-round repair ===");

            // And one cell that is not a level at all, which is why it is appended rather
            // than inserted. `Budgeting.Earned` pays one attempt per `Floor` misses and does
            // not read `Budget`, so it belongs on this curve as a row and nowhere on its
            // x-axis. It sits with a free budget where a parent is often wrong and with a cap
            // where it is not, measured on `Arranged` -- and this is where the columns skew
            // cannot game say whether the same holds at four widths.
            foreach (var (cell, budget, counting) in
                budgets.Select(one => (
                    one == Unlimited ? "free" : one.ToString(),
                    one,
                    Budgeting.Attempts))
                    .Append(("earned", Unlimited, Budgeting.Earned)))
            {
                // One run per seed, shared by every reading below -- the same discipline
                // `BudgetTests` records. Seven readings asked independently would run one
                // configuration seven times and print one measurement as seven.
                var once = new Dictionary<int, Learned>();

                Learned Cached(int seed)
                {
                    if (!once.TryGetValue(seed, out var learned))
                        once[seed] = learned = Run(address, skew, budget, seed, counting);

                    return learned;
                }

                foreach (var reading in new (string What, Func<Learned, double> Of)[]
                {
                    // First, because it is the one reading skew cannot game. A true rule
                    // firing on a round the base rate gets wrong is what `found` and
                    // `recent` both fail to separate on a tilted world.
                    ("paying", one => one.Census!.Paying),
                    // And the rounds the coverage is the complement of, because a rise in
                    // `paying` is consistent with the quantity story and does not test it. A
                    // child fires only where its added code is present, so it covers a SUBSET
                    // of what its parent was right about -- if that is why two thirds of wrong
                    // answers have no sound advocate, then buying more children buys more
                    // advocates and this column falls as the budget rises. If it is flat while
                    // `paying` climbs, the coverage is coming from somewhere else and the
                    // quantity story is wrong.
                    ("uncovered", one => one.Census!.Uncovered),
                    ("sound", one => one.Sound),
                    ("named", one => one.Named),
                    ("stacked", one => one.Stacked),
                    // The two denominators, because a naming count without them is a repair
                    // result wearing an abstraction's name. Rung five is offered only scopes
                    // of two codes or more past the floor, and covering mints one code and
                    // nothing longer -- so its whole input is repair's surviving output, and
                    // the budget moves that directly.
                    ("eligible", one => one.Eligible),
                    ("stackable", one => one.Stackable),
                    // Beside every score, because an accuracy can be hit by memorising.
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
