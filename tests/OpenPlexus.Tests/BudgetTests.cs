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
/// <b>The census says the count is what limits this machine and that the condition has
/// never run.</b> <see cref="CommittingSettings.Budget"/> refuses between half and nine
/// tenths of every repair candidate, worst exactly where nothing is learnt; and
/// <see cref="Mending"/> ships <see cref="Mending.Ungated"/>, so the two gates that ask
/// about a PARENT rather than about a tally read nought on every world. The dial census
/// already says what the honest driver would be — <i>whether a parent still has failures
/// no child covers, which the gate already computes and throws away</i>.
/// </para>
/// <para>
/// <b>So this is a cross and not a replacement.</b> A fixed count and a condition are two
/// answers to one question, and the cell that matters is unlimited-plus-condition: keep
/// refining while the parent still has failures nothing covers, and stop when it does
/// not. Deleting the count outright is the known-worse arm and is here as the bracket,
/// because unbounded repair over-specialises and this repo has measured that.
/// </para>
/// <para>
/// <b>AND `Found` is the reading rather than the score.</b> Unlimited repair can buy
/// accuracy by minting rules that fit what it has seen, so the count of the world's OWN
/// rules held exactly is the one a memorising arm cannot reach — reported beside
/// residents and unsound, which are what it would cost.
/// </para>
/// <para>
/// <b>Every number this file has ever recorded was taken under a timing it no longer
/// runs.</b> And that is not a change anybody made here. These cells name
/// <see cref="CommittingSettings.Budget"/> and <see cref="Mending"/> and pin nothing else,
/// so the arms inherited <see cref="Repairing.AfterFailure"/> when it was the default and
/// inherit <see cref="Repairing.EveryRound"/> now. The recorded verdict — a free budget is
/// worse at every width — is therefore about a machine where repair waited for the vote.
/// </para>
/// <para>
/// <b>Which is exactly the condition that made the budget read as inert.</b> So the verdict is
/// suspect in a specific direction. Under the old timing the lineages that would have
/// spent a loosened budget were never blamed, so nothing was waiting on either the count or
/// the condition. <b>A gate asking whether a parent still has failures no child covers can
/// only bind where that parent gets blamed at all.</b> Re-taken, this grid is fork 67.
/// </para>
/// <para>
/// <b>And re-taken, the condition does not replace the count — it trades the same way the
/// count did.</b> On the even eleven-bit world the shipped count ungated holds the best
/// trailing accuracy of the six cells, and adding the condition costs it while buying
/// coverage and unsound rules together. Under skew the sign flips: free plus the condition
/// carries far more of the hard rounds at the count's own accuracy. <b>No cell leads on both
/// worlds</b>, which is the shape three dials before it had.
/// </para>
/// <para>
/// <b>So the fuel is wrong rather than the idea.</b> <i>Has a child covered this failure</i>
/// is a question about the POPULATION, so it is scarce exactly where blame is scarce and
/// abundant where blame already flows — which is why it helps under skew and costs on an
/// even world. A driver read off the commitment ITSELF has no such coupling; see fork 68,
/// where what a rung gains and what it spends are both facts about one rule.
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
                new CommittingSettings
                {
                    Budget = budget,
                    Mending = mending,

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
    /// <b>Six cells</b>, and the shipped one is a corner rather than the middle.
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Whether_a_correctness_condition_beats_a_fixed_fork_count()
    {
        // The count arm is the shipped budget and not the old one, which is the whole point
        // of a cross. This grid asks whether a CONDITION beats a COUNT, so the count has to
        // be the best count there is or the comparison is against a straw arm -- and
        // `BudgetCurveTests` measured that 64 was well below the level. Sixty-four's rows are
        // in this file's history and are not comparable anyway; they were taken under a
        // timing this test no longer runs.
        var counted = new CommittingSettings().Budget;

        var cells = new (string Cell, int Budget, Mending Mending)[]
        {
            ("shipped ungated", counted, Mending.Ungated),
            ("shipped uncovered", counted, Mending.Uncovered),
            ("shipped improving", counted, Mending.Improving),
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
                // One run per seed, shared by every reading below. Six metrics asked
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
                    // FIRST, BECAUSE `found` led this grid to the wrong verdict once.
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
