using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Where the wrong answers actually come from — <b>the question four sessions of arms
/// were aimed at without asking.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERY GATE, WEIGHING RULE AND SUBSUMPTION BAR TRIED HERE ATTACKS ONE BUCKET.</b>
/// They change which resident rule gets the seat, so they can only reach a round where a
/// correct rule was HELD and lost. If most failures are rounds where no correct rule was
/// in the room, every one of those arms was inert by construction — which fits a table of
/// level results better than any of the mechanisms did.
/// </para>
/// <para>
/// <b>AND THE PARTITION IS ONLY AVAILABLE ON A WORLD THAT CAN SAY WHAT IS TRUE.</b> A
/// sound commitment that fires is right by definition, so the split is exact rather than
/// estimated — which is why this file is the multiplexer's and cannot be `Arranged`'s.
/// </para>
/// </remarks>
public sealed class CensusTests(ITestOutputHelper output)
{
    private const long Rounds = 20_000;

    /// <param name="address">Address bits.</param>
    /// <param name="skew">How often a data bit is one, or zero to leave them even.</param>
    /// <param name="seed">The world's generator and the brain's.</param>
    private static Learned Run(
        int address,
        double skew,
        int seed,
        Weighing weighing = Weighing.Summing,
        Surprising surprising = Surprising.Unaccounted) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(
                new CommittingSettings { Weighing = weighing, Surprising = surprising }, seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>THE CENSUS COUNTS THE RUN'S OWN FAILURES AND NOT A SECOND OPINION ABOUT
    /// THEM.</b>
    /// </summary>
    /// <remarks>
    /// <b>THE VOTE IS RE-DERIVED BEFORE THE STEP, SO THIS IS THE CHECK THAT IT IS THE SAME
    /// VOTE.</b> Reading the population a second time is only sound if the reading agrees
    /// with what the loop did — a census that partitioned a DIFFERENT set of wrong rounds
    /// would look exactly as informative and mean nothing, which is this repo's oldest
    /// trap wearing an instrument. Rounds where nothing fired are wrong in neither count.
    /// </remarks>
    [Fact]
    public void The_census_partitions_exactly_the_rounds_the_run_scored_wrong()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var learned = Run(address, skew, seed: 1);
            var census = learned.Census;

            Assert.NotNull(census);

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | "
                + $"tally wrong {learned.Tally.Wrong,6} | census wrong {census.Wrong,6}");

            Assert.Equal(learned.Tally.Wrong, census.Wrong);
        }
    }

    /// <summary>
    /// <b>THE READING ITSELF: how much of the failure any vote rule could ever have
    /// reached.</b>
    /// </summary>
    [Fact]
    public void Where_the_wrong_answers_come_from()
    {
        output.WriteLine("outvoted — a sound rule for the right answer fired and lost");
        output.WriteLine("uncovered — nothing sound advocating the right answer fired");
        output.WriteLine("deeper — of the outvoted, lost to a LONGER scope");
        output.WriteLine("");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var learned = Run(address, skew, seed: 1);
            var census = learned.Census!;

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | "
                + $"wrong {census.Wrong,6} | outvoted {census.Outvoted,6} "
                + $"| uncovered {census.Uncovered,6} | deeper {census.Deeper,6} "
                + $"| reachable {census.Reachable,6:P1} "
                + $"| found {learned.Found}/{learned.Truths} "
                + $"| residents {learned.Resident}");
        }
    }

    /// <summary>
    /// <b>WHAT THE TWO KNOWN GATES COST AND BUY, read on the bucket that decides it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>BOTH ARMS HAVE BEEN COMPARED ON ACCURACY AND NEITHER HAS EVER BEEN READ ON
    /// COVERAGE.</b> <see cref="Surprising.Unaccounted"/> won on score and the reason
    /// given was that ungated genesis walks the whole <c>code → outcome</c> space — which
    /// is a statement about its COST. What it buys is rounds moved out of
    /// <see cref="Census.Uncovered"/>, and nothing has ever counted those.
    /// </para>
    /// <para>
    /// <b>AND THE GATE'S QUESTION IS ANSWERED BY ANY RULE AT ALL, WHICH IS THE SUSPECT.</b>
    /// <c>firing.Any(one =&gt; one.Expects == arrived)</c> is satisfied by a commitment that
    /// is unsound, inaccurate and outvoted — so one worthless rule proposing an outcome
    /// blocks covering on every round that outcome arrives, forever. This says whether
    /// removing the veto recovers the missing two thirds or merely buys them at a price
    /// nobody wants.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_two_genesis_gates_do_to_the_uncovered_bucket()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            foreach (var surprising in new[] { Surprising.Unaccounted, Surprising.AnyFailure })
            {
                var learned = Run(address, skew, seed: 1, surprising: surprising);
                var census = learned.Census!;

                output.WriteLine(
                    $"{address + (1 << address),2} bits skew {skew:F1} {surprising,-12} | "
                    + $"recent {learned.Recent:F3} | wrong {census.Wrong,6} "
                    + $"| outvoted {census.Outvoted,5} | uncovered {census.Uncovered,6} "
                    + $"| found {learned.Found,2}/{learned.Truths} "
                    + $"| sound {learned.Sound,4} | unsound {learned.Unsound,5} "
                    + $"| residents {learned.Resident,5} | minted {learned.Tally.Minted,6}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// <b>WHEN GENESIS STOPS, which the totals imply and no reading has ever shown.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>SEVENTEEN MINTS IN TWENTY THOUSAND ROUNDS IS NOT A RATE, IT IS AN EVENT.</b> The
    /// same total under three different vote rules says the vote does not touch genesis at
    /// all, and a total that small on a world wanting eight rules says covering is not
    /// running for most of the run. Whether it tails off or stops is the difference
    /// between a gate that is strict and a gate that is closed.
    /// </para>
    /// <para>
    /// <b>AND THE SUSPECT IS THE GATE'S OWN QUESTION.</b> <c>Surprising.Unaccounted</c>
    /// asks whether ANYTHING that fired proposed what arrived — not whether the vote was
    /// right. With two outcomes and promiscuous one-code minting, some rule proposes each
    /// of them within a few hundred rounds, after which nothing is ever unaccounted for
    /// again and covering can never fire however wrong the machine is.
    /// </para>
    /// </remarks>
    [Fact]
    public void When_genesis_stops_minting()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (2, 0.8), (3, 0.8) })
        {
            var brain = new Brain(new CommittingSettings(), seed: 1);

            var run = new MultiplexerRun(
                new MultiplexerSettings { Address = address, Skew = skew },
                brain, seed: 1, census: true);

            long ran = 0;
            var row = new List<string>();

            foreach (var upto in new long[] { 100, 500, 2_000, 10_000, 20_000 })
            {
                var learned = run.Run(upto - ran);
                ran = upto;

                row.Add($"{upto}:{learned.Tally.Minted}");
            }

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | minted by round — "
                + string.Join("  ", row));
        }
    }

    /// <summary>
    /// <b>WHICH BUCKET A VOTE RULE ACTUALLY MOVES — and the census says it should be able
    /// to move only one of them.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A VOTE DECIDES WHO GETS THE SEAT, SO IT CAN ONLY REACH THE OUTVOTED BUCKET —
    /// EXCEPT THAT IT ALSO STEERS REPAIR.</b> This repo already records that no change to
    /// the vote is only a readout, because covering and repair run on the rounds the
    /// WINNER got wrong. So a vote rule can move the uncovered bucket too, by changing
    /// what gets minted rather than by changing what gets read.
    /// </para>
    /// <para>
    /// <b>WHICH IS THE DIFFERENCE BETWEEN A BETTER READOUT AND A BETTER SEARCH, and no
    /// score can tell them apart.</b> If a rule that wins on the skewed world wins by
    /// lowering <see cref="Census.Uncovered"/>, then what it improved was coverage and
    /// calling it a vote rule describes where it is implemented rather than what it does.
    /// </para>
    /// </remarks>
    [Fact]
    public void Which_failures_each_vote_rule_moves_on_the_skewed_world()
    {
        foreach (var address in new[] { 2, 3 })
        {
            foreach (var weighing in new[]
                { Weighing.Summing, Weighing.Strongest, Weighing.Lifting })
            {
                var learned = Run(address, skew: 0.8, seed: 1, weighing);
                var census = learned.Census!;

                output.WriteLine(
                    $"{address + (1 << address),2} bits {weighing,-10} | "
                    + $"wrong {census.Wrong,6} | outvoted {census.Outvoted,5} "
                    + $"| uncovered {census.Uncovered,6} | deeper {census.Deeper,4} "
                    + $"| found {learned.Found,2}/{learned.Truths} "
                    + $"| residents {learned.Resident,4} "
                    + $"| minted {learned.Tally.Minted,6}");
            }
        }
    }
}
