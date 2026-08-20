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
/// <b>Every gate, weighing rule and subsumption bar tried here attacks one bucket.</b>
/// They change which resident rule gets the seat, so they can only reach a round where a
/// correct rule was HELD and lost. If most failures are rounds where no correct rule was
/// in the room, every one of those arms was inert by construction — which fits a table of
/// level results better than any of the mechanisms did.
/// </para>
/// <para>
/// <b>And the partition is only available</b> on a world that can say what is true. A
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
    /// <param name="subsuming">What it takes for a narrower rule to survive its parent.</param>
    private static Learned Run(
        int address,
        double skew,
        int seed,
        Surprising surprising = Surprising.Unaccounted,
        Subsuming subsuming = Subsuming.Weaker) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address, Skew = skew },
            new Brain(
                new CommittingSettings { Surprising = surprising, Subsuming = subsuming },
                seed),
            seed,
            census: true).Run(Rounds);

    /// <summary>
    /// <b>Whether a parent's table already knows</b> what its child will want — fork 74's
    /// precondition, and it changes nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A four-code truth costs three miss floors</b>, because a fresh child inherits no
    /// table and must re-earn one before it may add the next code. That is what makes the
    /// chain the cost it is: the repairs that pay sit at the world's minimum sound depth and
    /// every shorter step pays nothing by construction.
    /// </para>
    /// <para>
    /// <b>So the question is whether one table could have picked both.</b> When a child is
    /// born its parent's table has a runner-up; when that child later repairs it picks from
    /// its OWN table, re-earned over its own firings and therefore conditioned on the code
    /// the parent added. If the two agree, the second floor bought nothing that the first
    /// table did not already know and a one-pass step is a saving. If they differ, the
    /// conditioning is the whole point and no single pass can replace it.
    /// </para>
    /// <para>
    /// <b>And the answer means something either way</b>, which is why it is worth a run before a
    /// mechanism. Agreement makes fork 74 buildable; disagreement kills it and says why —
    /// the same reason a minted name overshoots, arriving through the search rather than
    /// through the vocabulary.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_a_parents_table_already_knows_what_its_child_will_want()
    {
        output.WriteLine("world            | agreed | differed | share the parent predicted");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (3, 0.8) })
        {
            var agreed = 0L;
            var differed = 0L;

            for (var seed = 1; seed <= 6; seed++)
            {
                var brain = new Brain(new CommittingSettings(), seed);

                new MultiplexerRun(
                    new MultiplexerSettings { Address = address, Skew = skew }, brain, seed)
                    .Run(Rounds);

                agreed += brain.Held.Agreed;
                differed += brain.Held.Differed;
            }

            var asked = agreed + differed;

            output.WriteLine(
                $"{address + (1 << address),2} bits skew {skew:F1} | {agreed,6} | {differed,8} "
                + $"| {(asked == 0 ? 0.0 : agreed / (double)asked),10:P1}");
        }

        // NO BAR. Whether a parent's table predicts its child's choice has never been
        // measured, and a threshold written before the first reading would be the answer
        // rather than the finding.
    }

    /// <summary>
    /// <b>Whether the chain is capped by its own intermediate rungs dying.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The repairs that pay sit at the world's minimum sound depth</b> — three codes at six
    /// bits, four at eleven — and repair adds one code at a time, so reaching one takes two or
    /// three separations. Every rung below the last is UNSOUND by construction: it pins some
    /// of what the truth needs and not all of it, so it is wrong on a share of its firings and
    /// its accuracy says so.
    /// </para>
    /// <para>
    /// <b>And an unsound child that is no better than its parent</b> is exactly what subsumption
    /// removes. Under <see cref="Subsuming.Weaker"/> the general rule survives wherever it
    /// is at least as accurate, and a child that has pinned one of three needed codes usually
    /// is not better yet. So the chain may be capped not by the search but by the ladder being
    /// kicked away halfway up.
    /// </para>
    /// <para>
    /// <b>Which is testable with an arm that already exists.</b>
    /// <see cref="Subsuming.Insignificant"/> demands the narrower rule be significantly better
    /// before the general one may take its place, and holds roughly twice the residents where
    /// it has been measured. If intermediate rungs dying is the cap, the share of repairs that
    /// ever buy a hard round rises under it. If it is flat, the chain is not being cut and the
    /// hit rate is about the search itself.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Whether_subsumption_is_cutting_the_chain_before_it_reaches_a_sound_depth()
    {
        output.WriteLine("subsuming     | carriers | repairs | ever paid | their mean scope");

        foreach (var (address, skew) in new[] { (2, 0.0), (3, 0.0), (3, 0.8) })
        {
            output.WriteLine($"--- {address + (1 << address)} bits, skew {skew:F1} ---");

            foreach (var rule in new[] { Subsuming.Weaker, Subsuming.Insignificant })
            {
                var paid = new List<double>();
                var scope = new List<double>();
                var carried = new List<double>();

                for (var seed = 1; seed <= 6; seed++)
                {
                    var learned = Run(address, skew, seed, subsuming: rule);
                    var census = learned.Census!;

                    carried.Add(census.Narrowed);
                    scope.Add(census.Codes);

                    paid.Add(learned.Repaired == 0
                        ? 0.0
                        : census.Narrowed / (double)learned.Repaired);
                }

                output.WriteLine(
                    $"{rule,-13} | {Sweep.Spread(carried, "F1")} | "
                    + $"{Sweep.Spread(paid)} | {Sweep.Spread(scope, "F2")}");
            }
        }

        // NO BAR. Whether the ladder is being kicked away has never been measured, and a
        // threshold written before the first reading would be the answer rather than the
        // finding.
    }

    /// <summary>
    /// <b>The census counts the run's own failures</b> and not a second opinion about
    /// them.
    /// </summary>
    /// <remarks>
    /// <b>The vote is re-derived before the step</b>, so this is the check that it is the same
    /// vote. Reading the population a second time is only sound if the reading agrees
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
    /// <b>THE READING ITSELF</b>: how much of the failure any vote rule could ever have
    /// reached, and whether what it holds covers the rounds guessing gets wrong.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One walk over four worlds rather than two</b>, which is what the vote arms cost.
    /// These were two readings because each crossed the four worlds with its own list of
    /// weighing arms; the arms are deleted, and what was left was the same four runs taken
    /// twice for two sets of columns. `DuplicationTests` refused it, correctly.
    /// </para>
    /// <para>
    /// <b>A dozen perfectly accurate true rules</b> and an answer key scoring nought are the
    /// same run. On the skewed multiplexer every data bit is one four times in five,
    /// so <i>all four data bits are one</i> holds on about two rounds in five and entails
    /// the answer whatever the address selects. That rule is sound, never misses, and is
    /// not in the key — this repo's own trap about a single answer key marking the basis
    /// rather than the learner, walked into again.
    /// </para>
    /// <para>
    /// <b>And it still has not learnt the world</b>, which is what the second row
    /// separates. That rule fires exactly when guessing the commoner answer already
    /// works, so it buys nothing. <see cref="Census.Paying"/> asks the question no
    /// alternative rule set can game: of the rounds where the base rate is WRONG, how many
    /// had a true rule present and firing.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
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

            // And which half of generate-and-test the uncovered rounds belong to. A round
            // where nothing expecting the right answer fired cannot be reached by narrowing
            // anything resident -- repair keeps the parent's expectation and a child fires
            // only where its parent does -- so it is a ceiling rather than a search problem,
            // and every gate, budget and timing arm on this bench is aimed at the other kind.
            output.WriteLine(
                $"{"",16} | carriers {census.Carriers,5} | of them from repair "
                + $"{census.Narrowed,5} | repairs made {learned.Repaired,6} "
                + $"| paid off {(learned.Repaired == 0 ? 0.0 : census.Narrowed / (double)learned.Repaired),6:P2} "
                + $"| their mean scope {census.Codes,5:F2}");

            output.WriteLine(
                $"{"",16} | uncovered {census.Uncovered,6} | unreachable "
                + $"{census.Unreachable,5} "
                + $"({(census.Uncovered == 0 ? 0.0 : census.Unreachable / (double)census.Uncovered):P1})"
                + $" | present but under the floor {census.Ineligible,6} "
                + $"({(census.Uncovered == 0 ? 0.0 : census.Ineligible / (double)census.Uncovered):P1})");
        }
    }

    /// <summary>
    /// <b>The same partition on the world whose ceiling is known</b> in advance — where an
    /// uncovered round means the opposite of what it means on the multiplexer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One number, two diagnoses, and only a second world separates them.</b> Every
    /// rule the multiplexer needs is a conjunction the scope language can say, so an
    /// uncovered round there is covering or repair failing to BUILD something that was
    /// available. On <see cref="Puzzle.Two"/> nothing shorter than a whole instance can
    /// soundly say yes — so an uncovered round is the LANGUAGE, and no gate, budget or
    /// vote could ever have fixed it.
    /// </para>
    /// <para>
    /// <b>Which makes the pair the check rather than either row.</b> A mechanism that
    /// lowers uncovered on the multiplexer and leaves it alone on the second puzzle is
    /// doing what it claims; one that lowers both is finding rules that cannot be true,
    /// and the soundness count beside it should say so.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
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
    /// <b>Whether the uncovered bucket empties once the true rules are all held</b> — the
    /// internal claim two instruments can check and neither can alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>At six bits ungated the run ends holding all eight true rules</b> and still reports
    /// three hundred uncovered rounds. Those eight partition the input space, so once
    /// they are resident every round must have a correct advocate — which means the
    /// uncovered rounds have to be EARLY ones, before the rules existed. That is the
    /// parsimonious reading and it was a guess until this counted.
    /// </para>
    /// <para>
    /// <b>And if it is wrong the fault is in matching</b> rather than in learning, which
    /// no score could distinguish. A correct rule that is resident and does not fire on a
    /// moment it covers is a defect; a correct rule that did not exist yet is a run
    /// getting on with it. `Found` and `Uncovered` are computed by different code from
    /// different tables, so their agreement is a real check on both.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
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
                // Each call reports its own segment, because the census accumulates per
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
    /// <b>What the two known gates cost and buy</b>, read on the bucket that decides it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Both arms have been compared on accuracy</b> and neither has ever been read on
    /// coverage. <see cref="Surprising.Unaccounted"/> won on score and the reason
    /// given was that ungated genesis walks the whole <c>code → outcome</c> space — which
    /// is a statement about its COST. What it buys is rounds moved out of
    /// <see cref="Census.Uncovered"/>, and nothing has ever counted those.
    /// </para>
    /// <para>
    /// <b>And the gate's question is answered by any rule at all</b>, which is the suspect.
    /// <c>firing.Any(one =&gt; one.Expects == arrived)</c> is satisfied by a commitment that
    /// is unsound, inaccurate and outvoted — so one worthless rule proposing an outcome
    /// blocks covering on every round that outcome arrives, forever. This says whether
    /// removing the veto recovers the missing two thirds or merely buys them at a price
    /// nobody wants.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
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

                // The whole space genesis can ever reach is one code wide, so counting the
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
    /// <b>WHEN GENESIS STOPS</b>, which the totals imply and no reading has ever shown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Seventeen mints in twenty thousand rounds is not a rate</b>, it is an event. The
    /// same total under three different vote rules says the vote does not touch genesis at
    /// all, and a total that small on a world wanting eight rules says covering is not
    /// running for most of the run. Whether it tails off or stops is the difference
    /// between a gate that is strict and a gate that is closed.
    /// </para>
    /// <para>
    /// <b>And the suspect is the gate's own question.</b> <c>Surprising.Unaccounted</c>
    /// asks whether ANYTHING that fired proposed what arrived — not whether the vote was
    /// right. With two outcomes and promiscuous one-code minting, some rule proposes each
    /// of them within a few hundred rounds, after which nothing is ever unaccounted for
    /// again and covering can never fire however wrong the machine is.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
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
    /// <b>Which bucket a vote rule actually moves</b> — and the census says it should be able
    /// to move only one of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A vote decides who gets the seat</b>, so it can only reach the outvoted bucket —
    /// except that it also steers repair. This repo already records that no change to
    /// the vote is only a readout, because covering and repair run on the rounds the
    /// WINNER got wrong. So a vote rule can move the uncovered bucket too, by changing
    /// what gets minted rather than by changing what gets read.
    /// </para>
    /// <para>
    /// <b>Which is the difference between a better readout and a better search.</b> And no
    /// score can tell them apart. If a rule that wins on the skewed world wins by
    /// lowering <see cref="Census.Uncovered"/>, then what it improved was coverage and
    /// calling it a vote rule describes where it is implemented rather than what it does.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Sweeps.Kind, Sweeps.Name)]
    public void Which_failures_the_vote_leaves_behind_on_the_skewed_world()
    {
        // One arm now, where this once crossed three. Two of them lost and are deleted, so
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
    /// <b>Taking the census does not change the run</b>, which is what buys its exemption
    /// from `ShapeTests`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>`census` is a constructor parameter</b> on a world run and the dial guard is right
    /// to ask about it. A world may say what it is looking at and never what to
    /// conclude, and this one hands over the world's own soundness check — an answer key,
    /// which is exactly the thing that may not reach a learner. What makes it admissible
    /// is that the key goes to the HARNESS and not to the brain, and that is a claim about
    /// wiring which a name in an allow-list cannot carry.
    /// </para>
    /// <para>
    /// <b>So it is asserted on the answer rather than on the routing.</b> Two runs from
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
