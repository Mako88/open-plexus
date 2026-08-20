using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Where a repair dies — <b>the half of the diagnosis the census pointed at and could not
/// see.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The census says the missing rules are conjunctions</b> and repair is the only thing
/// that makes them. Genesis is one code wide, so its whole reachable space is the
/// vocabulary times the outcomes and it saturates in the opening few hundred rounds.
/// Everything after that is repair, and on the worlds where coverage is worst repair runs
/// at a fifth the rate it does where coverage is fine. Nothing said why.
/// </para>
/// <para>
/// <b>Five gates stand between a wrong commitment and a child</b>, and only the last was ever
/// counted. <c>Blamed</c> and <c>Unseparated</c> are per-ROUND and speak only for
/// candidates that survived everything else — so a run whose gates refuse almost
/// everything reports the same <c>Wanting</c> as a run whose language is too weak, and
/// those are opposite diagnoses. The ladder's own trigger has been reading a number
/// conditioned on gates nobody counted.
/// </para>
/// </remarks>
public sealed class RefusalTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static Learned Run(int address, double skew, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(new CommittingSettings(), seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>The five refusals add up to every candidate</b>, which is what makes the shares
    /// readable.
    /// </summary>
    /// <remarks>
    /// <b>A partition that does not partition is a pie chart of a different pie.</b> Each
    /// wrong commitment is charged to the FIRST gate that refused it, so the five counts
    /// and the search must total the candidates exactly — and if a sixth gate is ever
    /// added to <c>Repair</c> without being counted here, this is what goes red.
    /// </remarks>
    [Fact]
    public void Every_repair_candidate_is_charged_to_exactly_one_gate()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var tally = Run(address, skew, seed: 1).Tally;

            var parted = tally.AtFloor + tally.AtBudget + tally.AtCovered
                + tally.AtImproving + tally.Searched;

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | "
                + $"candidates {tally.Candidates,8} | parted {parted,8}");

            Assert.Equal(tally.Candidates, parted);
        }
    }

    /// <summary>
    /// <b>The same reading with seeds under it</b>, because one seed will happily invert.
    /// </summary>
    /// <remarks>
    /// <b>The shares are large enough that the single-seed row would probably hold,</b> which
    /// is not a reason not to count. This repo has a trap saying error bars come
    /// before ordering every time, and a budget refusing nine candidates in ten is exactly
    /// the sort of number that gets quoted for months.
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public async Task Where_repairs_die_with_seeds_under_it()
    {
        const int Seeds = 8;

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            foreach (var reading in new (string What, Func<Tally, double> Of)[]
            {
                ("floor", one => one.AtFloor / (double)Math.Max(one.Candidates, 1)),
                ("budget", one => one.AtBudget / (double)Math.Max(one.Candidates, 1)),
                ("searched", one => one.Searched / (double)Math.Max(one.Candidates, 1)),
                ("repaired", one => one.Repaired),
            })
            {
                var arm = await Sweep.ArmAsync(
                    reading.What,
                    Seeds,
                    seed => Task.FromResult(reading.Of(Run(address, skew, seed).Tally)));

                output.WriteLine(
                    $"{address + (1 << address),2} bits skew {skew:F1} {reading.What,-9} | "
                    + $"{arm.Mean,9:F4} +/-{arm.StdErr:F4} | n={arm.Seeds}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>THE READING: which gate is deciding the run.</b>
    /// </summary>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Where_repairs_die()
    {
        output.WriteLine("of every firing commitment that expected the wrong thing —");
        output.WriteLine("floor: too few misses · budget: forked too often");
        output.WriteLine("covered: a child already covers it · improving: forking never paid");
        output.WriteLine("searched: reached the candidate search at all");
        output.WriteLine("");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var learned = Run(address, skew, seed: 1);
            var tally = learned.Tally;
            var all = (double)Math.Max(tally.Candidates, 1);

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | "
                + $"candidates {tally.Candidates,8} "
                + $"| floor {tally.AtFloor / all,6:P1} "
                + $"| budget {tally.AtBudget / all,6:P1} "
                + $"| covered {tally.AtCovered / all,6:P1} "
                + $"| improving {tally.AtImproving / all,6:P1} "
                + $"| searched {tally.Searched / all,6:P1} "
                + $"| repaired {tally.Repaired,5} "
                + $"| wanting {tally.Wanting,6:P1} "
                + $"| found {learned.Found,2}/{learned.Truths}");
        }
    }
}
