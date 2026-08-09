using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A fixed fork count against a correctness condition — <b>John's rule, and the plan's
/// own note about `Budget`, which turn out to be the same proposal.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE CENSUS SAYS THE COUNT IS WHAT LIMITS THIS MACHINE AND THAT THE CONDITION HAS
/// NEVER RUN.</b> <see cref="CommittingSettings.Budget"/> refuses between half and nine
/// tenths of every repair candidate, worst exactly where nothing is learnt; and
/// <see cref="Mending"/> ships <see cref="Mending.Ungated"/>, so the two gates that ask
/// about a PARENT rather than about a tally read nought on every world. The dial census
/// already says what the honest driver would be — <i>whether a parent still has failures
/// no child covers, which the gate already computes and throws away</i>.
/// </para>
/// <para>
/// <b>SO THIS IS A CROSS AND NOT A REPLACEMENT.</b> A fixed count and a condition are two
/// answers to one question, and the cell that matters is unlimited-plus-condition: keep
/// refining while the parent still has failures nothing covers, and stop when it does
/// not. Deleting the count outright is the known-worse arm and is here as the bracket,
/// because unbounded repair over-specialises and this repo has measured that.
/// </para>
/// <para>
/// <b>AND `Found` IS THE READING RATHER THAN THE SCORE.</b> Unlimited repair can buy
/// accuracy by minting rules that fit what it has seen, so the count of the world's OWN
/// rules held exactly is the one a memorising arm cannot reach — reported beside
/// residents and unsound, which are what it would cost.
/// </para>
/// </remarks>
public sealed class BudgetTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    private const int Seeds = 8;

    /// <summary>No count limit at all, so the condition beside it is the only gate.</summary>
    private const int Unlimited = int.MaxValue;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="budget">How many children one parent may ever have.</param>
    /// <param name="mending">Which parents repair may touch.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static Learned Run(
        int address, double skew, int budget, Mending mending, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(
                new CommittingSettings { Budget = budget, Mending = mending }, seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>SIX CELLS, AND THE SHIPPED ONE IS A CORNER RATHER THAN THE MIDDLE.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_a_correctness_condition_beats_a_fixed_fork_count()
    {
        var cells = new (string Cell, int Budget, Mending Mending)[]
        {
            ("64 ungated", 64, Mending.Ungated),
            ("64 uncovered", 64, Mending.Uncovered),
            ("64 improving", 64, Mending.Improving),
            ("free ungated", Unlimited, Mending.Ungated),
            ("free uncovered", Unlimited, Mending.Uncovered),
            ("free improving", Unlimited, Mending.Improving),
        };

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} "
                + $"— the world holds {(1 << address) * 2} rules ---");

            foreach (var (cell, budget, mending) in cells)
            {
                // ONE RUN PER SEED, SHARED BY EVERY READING BELOW. Six metrics asked
                // independently would run the identical configuration six times and
                // report six identical runs as though they were evidence -- a third of an
                // hour spent to learn nothing, and a number that looks replicated and is
                // one measurement.
                var once = new Dictionary<int, Learned>();

                Learned Cached(int seed)
                {
                    if (!once.TryGetValue(seed, out var learned))
                        once[seed] = learned = Run(address, skew, budget, mending, seed);

                    return learned;
                }

                foreach (var reading in new (string What, Func<Learned, double> Of)[]
                {
                    // FIRST, BECAUSE `found` LED THIS GRID TO THE WRONG VERDICT ONCE.
                    // Every cell reported nought true rules and the conclusion drawn was
                    // that none of them learns anything -- which is what a run holding
                    // sound rules from a DIFFERENT correct rule set also reports. What
                    // cannot be gamed that way is whether a true rule fires on a round
                    // the base rate gets wrong.
                    ("paying", one => one.Census!.Paying),
                    ("found", one => one.Found),
                    ("recent", one => one.Recent),
                    ("uncovered", one => one.Census!.Uncovered),
                    ("unsound", one => one.Unsound),
                    ("residents", one => one.Resident),
                    ("repaired", one => one.Tally.Repaired),
                })
                {
                    await Fixture.ReadAsync(output, cell, Seeds, Cached, reading);
                }

                output.WriteLine("");
            }
        }
    }
}
