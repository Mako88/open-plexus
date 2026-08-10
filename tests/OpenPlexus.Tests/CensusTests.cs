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
    /// <param name="surprising">What genesis mints on.</param>
    private static Learned Run(
        int address,
        double skew,
        int seed,
        Surprising surprising = Surprising.Unaccounted) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(
                new CommittingSettings { Surprising = surprising }, seed),
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
    /// reached, and whether what it holds covers the rounds guessing gets wrong.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE WALK OVER FOUR WORLDS RATHER THAN TWO, WHICH IS WHAT THE VOTE ARMS COST.</b>
    /// These were two readings because each crossed the four worlds with its own list of
    /// weighing arms; the arms are deleted, and what was left was the same four runs taken
    /// twice for two sets of columns. `DuplicationTests` refused it, correctly.
    /// </para>
    /// <para>
    /// <b>A DOZEN PERFECTLY ACCURATE TRUE RULES AND AN ANSWER KEY SCORING NOUGHT ARE THE
    /// SAME RUN.</b> On the skewed multiplexer every data bit is one four times in five,
    /// so <i>all four data bits are one</i> holds on about two rounds in five and entails
    /// the answer whatever the address selects. That rule is sound, never misses, and is
    /// not in the key — this repo's own trap about a single answer key marking the basis
    /// rather than the learner, walked into again.
    /// </para>
    /// <para>
    /// <b>AND IT STILL HAS NOT LEARNT THE WORLD, WHICH IS WHAT THE SECOND ROW
    /// SEPARATES.</b> That rule fires exactly when guessing the commoner answer already
    /// works, so it buys nothing. <see cref="Census.Paying"/> asks the question no
    /// alternative rule set can game: of the rounds where the base rate is WRONG, how many
    /// had a true rule present and firing.
    /// </para>
    /// </remarks>
    [Fact]
    public void Where_the_wrong_answers_come_from()
    {
        output.WriteLine("outvoted — a sound rule for the right answer fired and lost");
        output.WriteLine("uncovered — nothing sound advocating the right answer fired");
        output.WriteLine("deeper — of the outvoted, lost to a LONGER scope");
        output.WriteLine("hard — rounds whose answer was not the commonest one");
        output.WriteLine("carried — of those, how many had a sound rule fire and say so");
        output.WriteLine("");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (2, 0.8), (3, 0.8) })
        {
            var learned = Run(address, skew, seed: 1);
            var census = learned.Census!;

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | "
                + $"wrong {census.Wrong,6} | outvoted {census.Outvoted,6} "
                + $"| uncovered {census.Uncovered,6} | deeper {census.Deeper,6} "
                + $"| untested {census.Untested,6} "
                + $"| reachable {census.Reachable,6:P1} "
                + $"| found {learned.Found}/{learned.Truths} "
                + $"| residents {learned.Resident}");

            output.WriteLine(
                $"{"",16} | recent {learned.Recent:F3} | hard {census.Hard,6} "
                + $"| carried {census.Carried,6} | paying {census.Paying,7:P1} "
                + $"| sound {learned.Sound,3}");
        }
    }

    /// <summary>
    /// <b>THE SAME PARTITION ON THE WORLD WHOSE CEILING IS KNOWN IN ADVANCE — where an
    /// uncovered round means the opposite of what it means on the multiplexer.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE NUMBER, TWO DIAGNOSES, AND ONLY A SECOND WORLD SEPARATES THEM.</b> Every
    /// rule the multiplexer needs is a conjunction the scope language can say, so an
    /// uncovered round there is covering or repair failing to BUILD something that was
    /// available. On <see cref="Puzzle.Two"/> nothing shorter than a whole instance can
    /// soundly say yes — so an uncovered round is the LANGUAGE, and no gate, budget or
    /// vote could ever have fixed it.
    /// </para>
    /// <para>
    /// <b>WHICH MAKES THE PAIR THE CHECK RATHER THAN EITHER ROW.</b> A mechanism that
    /// lowers uncovered on the multiplexer and leaves it alone on the second puzzle is
    /// doing what it claims; one that lowers both is finding rules that cannot be true,
    /// and the soundness count beside it should say so.
    /// </para>
    /// </remarks>
    [Fact]
    public void Where_the_wrong_answers_come_from_on_a_world_with_a_known_ceiling()
    {
        foreach (var puzzle in new[] { Puzzle.One, Puzzle.Two, Puzzle.Three })
        {
            var learned = new MonkRun(
                new MonkSettings { Puzzle = puzzle, Withheld = 132 },
                new Brain(new CommittingSettings(), seed: 1),
                seed: 1,
                census: true).Run(Rounds);

            var census = learned.Census!;

            output.WriteLine(
                $"monk-{puzzle,-6} | recent {learned.Recent:F3} "
                + $"| wrong {census.Wrong,6} | outvoted {census.Outvoted,6} "
                + $"| uncovered {census.Uncovered,6} | reachable {census.Reachable,6:P1} "
                + $"| found {learned.Found,3}/{learned.Truths} "
                + $"| wanting {learned.Tally.Wanting,6:P1}");
        }
    }

    /// <summary>
    /// <b>WHETHER THE UNCOVERED BUCKET EMPTIES ONCE THE TRUE RULES ARE ALL HELD — the
    /// internal claim two instruments can check and neither can alone.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>AT SIX BITS UNGATED THE RUN ENDS HOLDING ALL EIGHT TRUE RULES AND STILL REPORTS
    /// THREE HUNDRED UNCOVERED ROUNDS.</b> Those eight partition the input space, so once
    /// they are resident every round must have a correct advocate — which means the
    /// uncovered rounds have to be EARLY ones, before the rules existed. That is the
    /// parsimonious reading and it was a guess until this counted.
    /// </para>
    /// <para>
    /// <b>AND IF IT IS WRONG THE FAULT IS IN MATCHING RATHER THAN IN LEARNING</b>, which
    /// no score could distinguish. A correct rule that is resident and does not fire on a
    /// moment it covers is a defect; a correct rule that did not exist yet is a run
    /// getting on with it. `Found` and `Uncovered` are computed by different code from
    /// different tables, so their agreement is a real check on both.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_uncovered_bucket_empties_as_the_true_rules_arrive()
    {
        foreach (var (address, surprising) in new[]
        {
            (2, Surprising.AnyFailure),
            (2, Surprising.Unaccounted),
            (3, Surprising.AnyFailure),
        })
        {
            var brain = new Brain(
                new CommittingSettings { Surprising = surprising }, seed: 1);

            var run = new MultiplexerRun(
                new MultiplexerSettings { Address = address },
                brain, seed: 1, census: true);

            long ran = 0;
            var row = new List<string>();

            foreach (var upto in new long[] { 2_000, 5_000, 10_000, 15_000, 20_000 })
            {
                // EACH CALL REPORTS ITS OWN SEGMENT, because the census accumulates per
                // run rather than per population -- the same property that made the
                // genesis reading possible without touching the loop.
                var learned = run.Run(upto - ran);
                ran = upto;

                row.Add($"{upto / 1000}k:{learned.Census!.Uncovered}/{learned.Found}");
            }

            output.WriteLine(
                $"{address + (1 << address),2} bits {surprising,-12} | "
                + "uncovered/found by segment — " + string.Join("  ", row));
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
                var brain = new Brain(
                    new CommittingSettings { Surprising = surprising }, seed: 1);

                var learned = new MultiplexerRun(
                    new MultiplexerSettings { Address = address, Skew = skew },
                    brain, seed: 1, census: true).Run(Rounds);

                var census = learned.Census!;

                // THE WHOLE SPACE GENESIS CAN EVER REACH IS ONE CODE WIDE, so counting the
                // one-code residents against it says whether covering stopped because a
                // gate refused or because there was nothing left to mint. Those are
                // opposite diagnoses and the mint total cannot tell them apart.
                var roots = brain.Held.All.Count(one => one.Scope.Length == 1);

                output.WriteLine(
                    $"{address + (1 << address),2} bits skew {skew:F1} {surprising,-12} | "
                    + $"recent {learned.Recent:F3} | wrong {census.Wrong,6} "
                    + $"| outvoted {census.Outvoted,5} | uncovered {census.Uncovered,6} "
                    + $"| found {learned.Found,2}/{learned.Truths} "
                    + $"| sound {learned.Sound,4} | unsound {learned.Unsound,5} "
                    + $"| residents {learned.Resident,5} | minted {learned.Tally.Minted,6} "
                    + $"| repaired {learned.Tally.Repaired,6} "
                    + $"| exhausted {learned.Exhausted,4} "
                    + $"| roots {roots,3}/{2 * 2 * (address + (1 << address))}");
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
    public void Which_failures_the_vote_leaves_behind_on_the_skewed_world()
    {
        // ONE ARM NOW, WHERE THIS ONCE CROSSED THREE. Two of them lost and are deleted, so
        // what is left is the partition itself -- and the partition is what the reading was
        // ever for: a wrong round is outvoted, uncovered, or deeper than the language
        // reaches, and those three want completely different work.
        foreach (var address in new[] { 2, 3 })
        {
            var learned = Run(address, skew: 0.8, seed: 1);
            var census = learned.Census!;

            output.WriteLine(
                $"{address + (1 << address),2} bits | "
                + $"wrong {census.Wrong,6} | outvoted {census.Outvoted,5} "
                + $"| uncovered {census.Uncovered,6} | deeper {census.Deeper,4} "
                + $"| found {learned.Found,2}/{learned.Truths} "
                + $"| residents {learned.Resident,4} "
                + $"| minted {learned.Tally.Minted,6}");
        }
    }

    /// <summary>
    /// <b>TAKING THE CENSUS DOES NOT CHANGE THE RUN, which is what buys its exemption
    /// from `ShapeTests`.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>`census` IS A CONSTRUCTOR PARAMETER ON A WORLD RUN AND THE DIAL GUARD IS RIGHT
    /// TO ASK ABOUT IT.</b> A world may say what it is looking at and never what to
    /// conclude, and this one hands over the world's own soundness check — an answer key,
    /// which is exactly the thing that may not reach a learner. What makes it admissible
    /// is that the key goes to the HARNESS and not to the brain, and that is a claim about
    /// wiring which a name in an allow-list cannot carry.
    /// </para>
    /// <para>
    /// <b>SO IT IS ASSERTED ON THE ANSWER RATHER THAN ON THE ROUTING.</b> Two runs from
    /// one seed, one censused and one not, and every number the learner produced has to
    /// match exactly — a single leak into the population shows up as a different rule
    /// count long before it shows up as a better score. This is also the check that would
    /// catch the census becoming load-bearing later, which is how an instrument quietly
    /// turns into a mechanism.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_censused_run_and_a_plain_one_learn_exactly_the_same_thing()
    {
        foreach (var (address, skew) in new[] { (2, 0.0), (2, 0.8), (3, 0.8) })
        {
            var settings = new MultiplexerSettings { Address = address, Skew = skew };

            Learned Once(bool census) => new MultiplexerRun(
                settings, new Brain(new CommittingSettings(), seed: 1), seed: 1, census)
                .Run(Rounds);

            var watched = Once(census: true);
            var plain = Once(census: false);

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | "
                + $"recent {plain.Recent:F4} | residents {plain.Resident} | "
                + $"repaired {plain.Repaired} | sound {plain.Sound}");

            Assert.Null(plain.Census);
            Assert.NotNull(watched.Census);

            Assert.Equal(plain.Recent, watched.Recent, 12);
            Assert.Equal(plain.Resident, watched.Resident);
            Assert.Equal(plain.Repaired, watched.Repaired);
            Assert.Equal(plain.Sound, watched.Sound);
            Assert.Equal(plain.Unsound, watched.Unsound);
            Assert.Equal(plain.Found, watched.Found);
            Assert.Equal(plain.Named, watched.Named);
        }
    }
}
