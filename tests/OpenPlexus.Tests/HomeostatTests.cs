using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The world for step 4, built before step 4.
/// </summary>
/// <remarks>
/// <b>It exists because survival was gameable.</b> Snake scored by staying alive
/// and circling wins that — it lives longest and eats least. Keeping variables in
/// bounds cannot be gamed the same way, because everything falls whether or not
/// anything is done, so standing still is the fastest way to fail rather than the
/// safe option.
/// </remarks>
public sealed class HomeostatTests(ITestOutputHelper output)
{
    private static HomeostatSettings World() => new();

    private static WalkSettings Dials => Fixture.Dials(stamina: 4.0);

    private const int Steps = 400;

    [Fact]
    public async Task This_world_holds_no_act_to_outcome_edge_AT_ALL()
    {
        // THE OPEN DEFECT'S DIAGNOSIS, AND IT NEEDS NO APPEAL TO NON-STATIONARITY.
        // `Span` is nought here and this front end states no order, so not one
        // `Kind.After` cell is ever written -- an act is joined to the state it was
        // taken IN and never to what followed it.
        //
        // SO THE CHAIN ARM IS ASKING WHICH ACT ACCOMPANIED STATES LIKE THIS. That
        // is a mirror of its own past policy with no outcome anywhere in it, which
        // is why more data makes it monotonically worse -- more data sharpens the
        // mirror -- and why the test below already records that every point it ever
        // scored came from the bootstrap's coin toss. The coin toss is the only
        // evidence in this world that nothing about the state selected.
        //
        // A CHECK THAT CANNOT FIRE READS AS ONE THAT PASSED, which is why this
        // asserts the zero rather than trusting the reading: `Plumbing.Temporal`
        // is new, and a counter wired to nothing would report nought here for the
        // wrong reason. `RhythmTests` is where it is armed against a world that
        // DOES carry a span.
        using var run = new HomeostatRun(World(), Dials, seed: 1);
        var result = await run.RunAsync(Steps, Attending.Chain);

        Assert.Equal(0, result.Plumbing.Temporal);

        // AND THE GRAPH IS NOT SIMPLY EMPTY, or the line above says nothing.
        Assert.True(result.Plumbing.Edges > 0, "the run wrote no edges at all");
    }

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void The_world_is_arithmetically_capable_and_not_trivially_so()
    {
        // BOTH BOUNDS, OR THE WORLD MEASURES NOTHING. Restoring less than
        // everything falls means nothing could hold it and the ceiling is
        // unreachable; restoring more than the fastest drain times the number of
        // needs means attending at random suffices and the ceiling is free.
        var world = new Homeostat(World());

        Assert.True(world.Restore > world.Falling,
            $"nothing could hold this body: restore {world.Restore} against "
            + $"fall {world.Falling}");

        Assert.True(world.Restore < world.Needs * world.Falls(world.Needs - 1),
            $"attending at random would hold this body, so it discriminates "
            + $"nothing: restore {world.Restore}");
    }

    [Fact]
    public void Everything_falls_whether_or_not_anything_is_done()
    {
        // THE PROPERTY THAT MAKES IDLING COST. Under survival, doing nothing was
        // the strategy; here it is the failure.
        var world = new Homeostat(World());
        var before = world.At.ToList();

        world.Step(null);

        Assert.All(Enumerable.Range(0, world.Needs),
            which => Assert.True(world.At[which] < before[which]));

        // AND THE FASTEST-FALLING ONE FALLS FASTEST, which is what makes spreading
        // attention evenly the wrong thing to do.
        Assert.True(
            before[world.Needs - 1] - world.At[world.Needs - 1] > before[0] - world.At[0]);
    }

    [Fact]
    public void A_drive_is_felt_as_a_band_and_not_read_as_a_number()
    {
        var world = new Homeostat(World());

        var felt = world.Feels();

        Assert.Equal(world.Needs, felt.Length);

        // ONE MODALITY PER VARIABLE, so the graph can tell hunger from thirst
        // without anything downstream knowing which is which.
        Assert.Equal(world.Needs, felt.Select(code => code.Modality).Distinct().Count());
    }

    // ---- what the graph does with it ---------------------------------------

    [Fact]
    public async Task Standing_still_is_the_fastest_way_to_fail()
    {
        // THE REFUTED ROW, CHECKED RATHER THAN ASSUMED. "Survival as the score --
        // circling wins: it lives longest and eats least." The revival condition
        // named homeostatic drives, and this is that condition being tested rather
        // than asserted.
        using var run = new HomeostatRun(World(), Dials, seed: 1);
        var idle = await run.RunAsync(Steps, Attending.Idle);

        output.WriteLine(idle.ToString());

        Assert.True(idle.Viable < 0.25,
            $"idling held the body for {idle.Viable} of the run");

        Assert.Empty(idle.Complaints);
    }

    [Fact]
    public async Task Attending_to_whatever_is_lowest_holds_the_body_and_random_does_not()
    {
        // THE CEILING AND THE CONTROL. Neither involves the graph: they say the
        // world is winnable, and winnable only by looking at it.
        using var best = new HomeostatRun(World(), Dials, seed: 1);
        using var blind = new HomeostatRun(World(), Dials, seed: 1);

        var lowest = await best.RunAsync(Steps, Attending.Lowest);
        var random = await blind.RunAsync(Steps, Attending.Blind);

        output.WriteLine(lowest.ToString());
        output.WriteLine(random.ToString());

        Assert.True(lowest.Viable > 0.9,
            $"the ceiling arm could not hold the body: {lowest.Viable}");

        Assert.True(lowest.Viable > random.Viable + 0.2,
            $"attending at random did nearly as well as attending to what is "
            + $"lowest, so the world does not discriminate: "
            + $"{random.Viable} against {lowest.Viable}");
    }

    // ---- step 4's blocker: the front end ------------------------------------

    /// <summary>What a felt state says about magnitude, as a comparable string.</summary>
    private static string Bands(IEnumerable<Code> felt) =>
        string.Join(",", felt.Where(code => code.Modality < Homeostat.Rank)
            .OrderBy(code => code.Modality).Select(code => code.Value));

    /// <summary>What it says about order. See <see cref="Homeostat.Standing"/>.</summary>
    private static string Positions(Homeostat body) =>
        string.Join(",", Enumerable.Range(0, body.Needs).Select(body.Standing));

    [Fact]
    public void A_band_cannot_say_which_is_lowest_and_a_rank_can()
    {
        // THE CEILING, ASSERTED RATHER THAN ARGUED, AND IT IS FORK 25's SHAPE IN A
        // SECOND WORLD. Two states of one body whose variables sit in the SAME
        // bands and in a DIFFERENT order. A front end that emits bands alone emits
        // the identical code set for both, so no amount of counting can separate
        // them -- and which is lowest is the only fact this world turns on.
        var ranked = new Homeostat(World() with { Ranked = true });

        var before = ranked.Feels();
        var wasStanding = Positions(ranked);

        // Everything falls, and attending to the first one puts it back on top.
        // One step is enough because the drains are uneven.
        ranked.Step(attend: 0);

        var after = ranked.Feels();
        var nowStanding = Positions(ranked);

        output.WriteLine($"bands {Bands(before)} -> {Bands(after)}");
        output.WriteLine($"ranks {wasStanding} -> {nowStanding}");

        // SAME BANDS. This is the state a banded front end cannot tell from the
        // one before it.
        Assert.Equal(Bands(before), Bands(after));

        // DIFFERENT ORDER, and it is the ordering that carries the answer.
        Assert.NotEqual(wasStanding, nowStanding);

        // ADDITIVE: the ranks are extra codes and the band codes are untouched, so
        // switching the arm off reproduces every earlier measurement exactly.
        var plain = new Homeostat(World()).Feels();
        Assert.Equal(plain.Length * 2, before.Length);
        Assert.All(plain, code => Assert.Contains(code, before));

        // AND THE ORDERING IS A PERMUTATION, never a near-miss: every position is
        // held by exactly one variable, so the front end cannot emit a rank no
        // variable holds -- a state the graph would learn about and the body can
        // never be in again.
        var standing = Enumerable.Range(0, ranked.Needs).Select(ranked.Standing).ToList();
        Assert.Equal(Enumerable.Range(0, ranked.Needs), standing.Order());
    }

    [Fact]
    public async Task The_ceiling_policy_stops_being_a_constant_to_the_graph()
    {
        // THE SECOND HALF OF THE DIAGNOSIS, AND THE SURPRISING ONE. Attending to
        // whatever is lowest holds the body so steady that every banded code sits
        // still: measured at EXACTLY ONE distinct state over four hundred steps.
        // The correct policy is, to the graph, a constant -- so there is no state
        // variation for a state-conditional association to attach to, and state
        // variety correlates with FAILURE rather than with information.
        //
        // Drains are uneven, so which variable is worst rotates while the values
        // barely move. The ordering varies exactly where the magnitudes do not.
        using var banded = new HomeostatRun(World(), Dials, seed: 1);
        using var ranked = new HomeostatRun(World() with { Ranked = true }, Dials, seed: 1);

        var flat = await banded.RunAsync(Steps, Attending.Lowest);
        var varied = await ranked.RunAsync(Steps, Attending.Lowest);

        output.WriteLine($"banded states={flat.States} viable={flat.Viable:F4}");
        output.WriteLine($"ranked states={varied.States} viable={varied.Viable:F4}");

        Assert.Equal(1, flat.States);
        Assert.True(varied.States > 1,
            "the ranked front end is as blind to the ceiling policy as the banded "
            + $"one, so it cannot be what step 4 was waiting on: {varied.States} states");

        // AND IT IS STILL THE CEILING. A front end that changed what the body can
        // do would be a different world rather than a better description of one.
        Assert.Equal(flat.Viable, varied.Viable, 6);

        Assert.Empty(varied.Complaints);
    }

    [Fact]
    public async Task What_the_ordering_is_worth_to_an_arm_that_has_to_act()
    {
        // THE QUESTION STEP 4 IS ACTUALLY ABOUT, and the two changes are separate
        // arms because they are two changes. `Chain` ranked asks whether being
        // able to EXPRESS the task is enough on its own.
        //
        // THE BAR IS BLIND AND NOT IDLE. Choosing by association already scores
        // below drawing at random, so beating idling would only say the
        // arithmetic works.
        var ranked = World() with { Ranked = true };

        var arms = await Sweep.AcrossAsync(
            5,
            ("blind", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Blind)).Viable;
            }),
            ("chain", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Chain)).Viable;
            }),
            ("chain+ranked", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                return (await run.RunAsync(Steps, Attending.Chain)).Viable;
            }),
            ("lowest", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Lowest)).Viable;
            }));

        output.WriteLine(Sweep.Table(arms));

        // THE CONFOUND THAT WOULD MAKE ALL OF THAT WORTHLESS, AND IT HAS TO BE
        // MEASURED RATHER THAN ARGUED AWAY. The bootstrap acts AT RANDOM when the
        // walk proposes nothing, so an arm that is silent more often is a blind
        // arm wearing the chain's name -- and moving TOWARDS the blind bar is
        // exactly what more silence would produce. A ranked front end doubles the
        // codes in an occasion, which is every reason to expect its walk to behave
        // differently, so this is not a remote possibility.
        var silence = await Sweep.AcrossAsync(
            5,
            ("chain silent", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                var result = await run.RunAsync(Steps, Attending.Chain);
                return result.Silent / (double)result.Steps;
            }),
            ("chain+ranked silent", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                var result = await run.RunAsync(Steps, Attending.Chain);
                return result.Silent / (double)result.Steps;
            }));

        output.WriteLine(Sweep.Table(silence));

        // AND THE SILENCE IS WHERE THE LIFT CAME FROM. Read the two tables
        // together rather than the first alone: the ranked arm looks better and
        // is silent three times as often, which is the same thing said twice.
        // `Every_point_the_chain_arm_scores_comes_from_its_own_coin_toss` spends
        // the budget to separate them, and the lift does not survive it.
        Assert.True(
            silence[1].Separation(silence[0]) > 2.0,
            "the ranked arm is no longer measurably more silent, so the confound "
            + "this table exists to expose has gone and the reading above can be "
            + "taken at face value again");

        // THE CEILING AND THE BAR STILL BRACKET EVERYTHING, or the world stopped
        // measuring what it measures and no row above means anything.
        var bar = arms.First(one => one.Arm == "blind");
        var ceiling = arms.First(one => one.Arm == "lowest");

        Assert.True(ceiling.Mean > bar.Mean + 0.2,
            $"the world stopped discriminating: {ceiling.Mean:F4} against {bar.Mean:F4}");

        // AND EVERY ARM THAT CONSULTS THE GRAPH IS STILL BELOW THE BAR, ranked or
        // not, credited or not. That is what step 4 has to move.
        foreach (var arm in arms.Where(one => one.Arm != "blind" && one.Arm != "lowest"))
            Assert.True(arm.Mean < bar.Mean,
                $"{arm.Arm} beat drawing at random ({arm.Mean:F4} against "
                + $"{bar.Mean:F4}), which is what step 4 is for -- if this fires, "
                + "the baseline has moved and the plan's step 4 needs rewriting");
    }

    [Fact]
    public async Task Every_point_the_chain_arm_scores_comes_from_its_own_coin_toss()
    {
        // THE CONTROL THAT KILLED THE RESULT, AND IT IS THE FINDING NOW.
        //
        // A ranked front end looked like it lifted the arm -- 0.1370 to 0.2350,
        // half the way to the blind bar. It is silent three times as often, and
        // the bootstrap acts AT RANDOM when the walk says nothing, so the arm
        // moved towards the random bar for the reason that would move it there
        // anyway. Buying the voice back with budget is the test.
        //
        // WHY IT GOES SILENT: a near-constant code concentrates its counts. With
        // bands alone every variable sits in band 4 for most of a run, so
        // `together(need, act)` piles onto a handful of pairs and the edge to an
        // action is fat and cheap. A code that VARIES spreads the same occasions
        // over many more pairs, every edge is thinner, a step costs 1/weight, and
        // routes starve before reaching an action. Same shape as the fleeting
        // index: a front end expressive enough to state the problem fragments the
        // statistics that made the walk affordable.
        //
        // AND SPENDING THE BUDGET ANSWERS IT. Silence falls, and so does the
        // score -- both front ends, monotonically. The graph's voice is not
        // merely uninformative here, it is ANTI-CORRELATED with what the body
        // needs, and every point this arm ever scored came from ignoring it.
        // BOTH FRONT ENDS AT BOTH BUDGETS, because comparing them at one stamina
        // would be measuring the stamina -- the trap this project has already
        // walked into with the plateau. Sixteen is not here: the ranked arm passed
        // three million messages at eight and the bus runs out of patience above
        // it, which is itself the cost being reported.
        // BOTH FRONT ENDS AT BOTH BUDGETS, because comparing them at one stamina
        // would be measuring the stamina -- the trap this project already walked
        // into with the plateau. Sixteen is not here: the ranked arm passed three
        // million messages at eight and the bus runs out of patience above it,
        // which is itself the cost being reported.
        var scores = new Dictionary<(bool Ranked, double Stamina), HomeostatResult>();

        foreach (var ranked in (bool[])[false, true])
            foreach (var stamina in (double[])[4.0, 8.0])
            {
                using var run = new HomeostatRun(
                    World() with { Ranked = ranked }, Fixture.Dials(stamina), seed: 1);

                var result = await run.RunAsync(Steps, Attending.Chain);
                scores[(ranked, stamina)] = result;

                output.WriteLine(
                    $"{(ranked ? "ranked" : "banded"),-6} stamina={stamina,-5} "
                    + $"silent={result.Silent,3}/{result.Steps} "
                    + $"viable={result.Viable:F4} "
                    + $"attended=[{string.Join(",", result.Attended)}] "
                    + $"states={result.States} edges={result.Edges} "
                    + $"widest={result.Widest} msgs={result.Messages}");

                Assert.Empty(result.Complaints);
            }

        using var idle = new HomeostatRun(World(), Dials, seed: 1);
        var doingNothing = await idle.RunAsync(Steps, Attending.Idle);

        // QUIETER IS WORSE, ON BOTH FRONT ENDS. This is the whole claim, and it is
        // asserted on each front end separately so that neither can carry it.
        foreach (var ranked in (bool[])[false, true])
            Assert.True(
                scores[(ranked, 8.0)].Silent < scores[(ranked, 4.0)].Silent
                && scores[(ranked, 8.0)].Viable < scores[(ranked, 4.0)].Viable,
                $"ranked={ranked}: spending the budget did not buy a quieter and "
                + "worse arm, so the score is no longer the coin toss and this "
                + "finding has expired");

        // AND AT THE POINT WHERE THE WALK DECIDES ALMOST EVERY STEP, CHOOSING BY
        // ASSOCIATION IS WORTH NO MORE THAN DOING NOTHING AT ALL. That is a
        // sharper statement than "below random" and it is the one to beat.
        Assert.True(scores[(false, 8.0)].Viable <= doingNothing.Viable + 0.02,
            $"the near-silent chain arm beat idling: "
            + $"{scores[(false, 8.0)].Viable:F4} against {doingNothing.Viable:F4}");

        // THE ORDERING IS NOT INERT, THOUGH, AND SAYING SO IS THE HONEST HALF.
        // Banded at this budget collapses onto one action and NEVER TOUCHES the
        // two fastest-draining variables; ranked spreads across all four and
        // leans on the fastest, which is the shape the ceiling has. It changes
        // what the body attends to, in the right direction, and does not turn
        // that into a body that survives.
        Assert.Contains(0, scores[(false, 8.0)].Attended);
        Assert.DoesNotContain(0, scores[(true, 8.0)].Attended);
    }

    [Fact]
    public async Task Credit_in_its_own_cell_against_credit_on_top_of_the_old_one()
    {
        // STEP 4'S SECOND ATTEMPT, AND THE ONE THING IT CHANGES IS CONTRAST.
        // Three credit arms failed by writing a heavier number into the cell that
        // already means "this was done here" -- which deepens the groove. This
        // writes a SECOND cell and walks that one instead, so the ranking is the
        // share of times an act helped rather than how often it was taken.
        var ranked = World() with { Ranked = true };

        var arms = await Sweep.AcrossAsync(
            12,
            ("blind", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Blind)).Viable;
            }),
            ("chain", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Chain)).Viable;
            }),
            ("lowest", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Lowest)).Viable;
            }),
            ("credited", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Credited)).Viable;
            }),
            ("credited+ranked", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                return (await run.RunAsync(Steps, Attending.Credited)).Viable;
            }),
            ("contested", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Contested)).Viable;
            }),
            ("contested+ranked", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                return (await run.RunAsync(Steps, Attending.Contested)).Viable;
            }));

        output.WriteLine(Sweep.Table(arms));

        var bar = arms.First(one => one.Arm == "blind");
        var contrasted = arms.First(one => one.Arm == "credited");

        // THE FIRST ARM IN THIS PROJECT TO BEAT DRAWING AT RANDOM. Everything that
        // consults the graph had scored below the bar, which is what step 4 exists
        // to fix.
        Assert.True(
            contrasted.Mean > bar.Mean && contrasted.Separation(bar) > 3.0,
            $"the credit cell stopped beating the bar: {contrasted.Mean:F4} "
            + $"against {bar.Mean:F4}");

        // THE CONTRAST BEING WHAT DID IT IS NO LONGER MEASURED HERE, AND THAT IS
        // DELIBERATE. `marked` wrote the same second cell unconditionally -- same
        // relation, same one-step staleness, differing by the CONDITION alone --
        // and it was the arm that ruled out "the extra cell" and "the staleness"
        // as explanations. It lost and was collapsed under the delete-the-loser
        // rule, so what it evidenced went with it: peak 0.3167 against a blind bar
        // of 0.3668, less than half `credited`'s 0.7347. The plan's table carries
        // the numbers and the revival condition. Anything reviving that arm has to
        // re-take this comparison rather than cite it.

        // AND THE NEGATIVE HALF STILL DOES NOT PAY, AFTER THREE SHAPES OF IT.
        // `Contested` writes `Kind.Hindered` when things got worse, so the CRDT
        // objection to punishment is gone -- two monotonic counters, convergence
        // untouched, one kind rather than a wider row.
        //
        // WHAT IT COST, AND THE ARC IS THE FINDING. Subtract-and-clamp scored
        // 0.5800 and left the walk silent on 394 of 400 steps; scaling the count
        // instead of cutting it moved nothing, 0.5806 and 387. Both fold the
        // discount into `Together`, which is STILL both the ranking and the price
        // of the hop -- so every discounted partner also became dearer to reach
        // and routes starved. That is this design's recurring fault, and `Doubt`
        // exists because the identical mistake was made once before.
        //
        // CARRYING IT ON THE MESSAGE AND DISCOUNTING THE SCORE ALONE RECOVERS MOST
        // OF IT: 0.6633, silence down to 371. So the `Doubt` precedent generalises
        // and the separation is worth having.
        //
        // IT IS STILL NOT AN IMPROVEMENT. Against the one-sided count it is about
        // 1.6 sigma down -- no longer clearly worse, and nowhere near better. On a
        // world where most acts are wrong most of the time, knowing WHICH ones
        // were wrong adds nothing the positive cell was not already saying.
        var contested = arms.First(one => one.Arm == "contested");

        Assert.True(contested.Mean < contrasted.Mean,
            $"subtracting the negative cell now beats the one-sided count "
            + $"({contested.Mean:F4} against {contrasted.Mean:F4}), so the "
            + "over-pruning note above has expired and needs re-running");

        // THE SILENCE CANNOT EXPLAIN IT, AND THE ARGUMENT NEEDS NO MEASUREMENT.
        // The bootstrap acts AT RANDOM, so mixing coin tosses into an arm pulls it
        // TOWARDS the blind bar and can never carry it past. An arm that is mostly
        // silent and still scores well above `blind` must be getting that from the
        // steps it did decide. Reported anyway, because the share matters for what
        // to do next: the credit cell is EMPTY until something has helped, so this
        // arm starts as pure coin toss and speaks only where it has learnt.
        foreach (var arm in (Attending[])[Attending.Chain, Attending.Credited, Attending.Contested])
        {
            using var run = new HomeostatRun(World(), Dials, seed: 1);
            var result = await run.RunAsync(Steps, arm);

            output.WriteLine(
                $"{arm,-9} silent={result.Silent,3}/{result.Steps} "
                + $"viable={result.Viable:F4} "
                + $"attended=[{string.Join(",", result.Attended)}] "
                + $"states={result.States} edges={result.Edges}");
        }
    }

    /// <summary>
    /// Several arms at one budget, swept over seeds and reported as a table.
    /// </summary>
    /// <remarks>
    /// <b>ONE PLACE, BECAUSE THE CLAIMS BELOW DIFFER ONLY IN STAMINA.</b> Two arms
    /// can peak two settings apart, so the whole point of these tests is that the
    /// SAME arms behave differently at different budgets — which is worth nothing
    /// if they are not the same arms.
    /// </remarks>
    private async Task<IReadOnlyList<Measured>> ArmsAsync(
        WalkSettings dials, int seeds, params Attending[] arms)
    {
        var swept = await Sweep.AcrossAsync(
            seeds,
            [.. arms.Select(arm => (
                arm.ToString().ToLowerInvariant(),
                new Func<int, Task<double>>(async seed =>
                {
                    using var run = new HomeostatRun(World(), dials, seed);
                    return (await run.RunAsync(Steps, arm)).Viable;
                })))]);

        output.WriteLine(Sweep.Table(swept));

        return swept;
    }

    [Fact]
    public async Task Inhibition_loses_at_the_budget_it_was_refuted_at_and_wins_above_it()
    {
        // THE REFUTED ROW, RE-RUN BECAUSE ITS EVIDENCE TURNED OUT TO BE TAKEN IN A
        // CORNER. "Inhibition on `Homeostat` ... still loses to the one-sided
        // count" was measured at one stamina, which is the stamina everything in
        // step 4 was measured at -- and `Choices` has since shown that at that
        // setting the walk reaches one action and the world cannot see a ranking.
        //
        // SO THE QUESTION IS WHETHER THE REFUTATION IS ABOUT INHIBITION OR ABOUT
        // THE BUDGET, and it is about the budget.
        var scored = new Dictionary<double, (double Credited, double Contested, double Sigma)>();

        foreach (var stamina in (double[])[4.0, 8.0, 12.0])
        {
            output.WriteLine($"--- stamina {stamina} ---");

            var arms = await ArmsAsync(
                Fixture.Dials(stamina), 24,
                Attending.Blind, Attending.Credited, Attending.Contested);

            var credited = arms.First(one => one.Arm == "credited");
            var contested = arms.First(one => one.Arm == "contested");

            scored[stamina] = (credited.Mean, contested.Mean, contested.Separation(credited));
        }

        // THE RECORDED REFUTATION REPRODUCES WHERE IT WAS TAKEN. Without this the
        // reversal below is just as likely to be the world having moved.
        Assert.True(scored[4.0].Contested < scored[4.0].Credited,
            $"inhibition no longer loses at the budget it was refuted at "
            + $"({scored[4.0].Contested:F4} against {scored[4.0].Credited:F4}), so "
            + "the row's evidence has expired for some reason other than this one");

        // AND IT REVERSES COMPLETELY ONE BUDGET UP. The one-sided count collapses
        // as the walk is allowed to reach further -- more budget reaches more
        // partners and nothing rules any of them out -- while the arm that can say
        // NOT THAT ONE keeps its footing. By stamina 12 the one-sided count is at
        // the blind bar and inhibition is still holding the body.
        Assert.True(scored[8.0].Sigma > 3.0 && scored[8.0].Contested > scored[8.0].Credited,
            $"inhibition does not beat the one-sided count at the wider budget: "
            + $"{scored[8.0].Contested:F4} against {scored[8.0].Credited:F4} at "
            + $"{scored[8.0].Sigma:F2} sigma");

        // THE SHAPE IS THE FINDING RATHER THAN THE WIN, and it is the second time
        // today the same shape has turned up: a mechanism that SUPPRESSES options
        // makes the budget stop mattering. The carried-edge discount flattened the
        // same cliff on `Rhythm` without raising its peak; this flattens it and
        // raises the peak.
        Assert.True(
            scored[12.0].Credited < scored[4.0].Credited - 0.2,
            $"the one-sided count no longer collapses with budget "
            + $"({scored[12.0].Credited:F4} against {scored[4.0].Credited:F4}), so "
            + "there is no cliff here and the claim above is about something else");

        Assert.True(
            scored[12.0].Contested > scored[4.0].Contested,
            $"inhibition stopped being budget-robust: {scored[12.0].Contested:F4} "
            + $"at stamina 12 against {scored[4.0].Contested:F4} at stamina 4");
    }

    [Fact]
    public async Task And_the_win_is_not_the_one_sided_count_being_handed_a_smaller_budget()
    {
        // THE CONFOUND, AND IT IS A REAL ONE. `Contested` reinforces on BOTH
        // outcomes where `Credited` reinforces only on improvement, so it joins
        // roughly twice as many occasions -- which raises `seen`, lowers every
        // weight, and makes every hop dearer. That is a smaller budget wearing a
        // different name, and it would produce exactly the shape measured above.
        //
        // THE TEST THAT SETTLES IT NEEDS NO NEW CODE: if inhibition were only a
        // budget change, its score could never EXCEED what the one-sided count
        // reaches at its own best budget. So find that best.
        var peaks = new Dictionary<string, (double Stamina, double Mean, double Err)>();

        foreach (var stamina in (double[])[2.0, 2.5, 3.0, 3.5, 4.0, 5.0, 6.0, 8.0])
        {
            output.WriteLine($"--- stamina {stamina} ---");

            var arms = await ArmsAsync(
                Fixture.Dials(stamina), 24, Attending.Credited, Attending.Contested);

            foreach (var arm in arms)
                if (!peaks.TryGetValue(arm.Arm, out var best) || arm.Mean > best.Mean)
                    peaks[arm.Arm] = (stamina, arm.Mean, arm.StdErr);
        }

        var oneSided = peaks["credited"];
        var inhibited = peaks["contested"];

        output.WriteLine(
            $"credited peaks {oneSided.Mean:F4} at stamina {oneSided.Stamina:F1}; "
            + $"contested peaks {inhibited.Mean:F4} at stamina {inhibited.Stamina:F1}");

        // THE TWO ARMS PEAK AT DIFFERENT BUDGETS, which is the whole reason a sweep
        // at one stamina got the wrong answer. This is the trap the plan already
        // names -- a dial measured at one setting of another may be measuring that
        // one -- and it is what the refuted row was built on.
        Assert.True(inhibited.Stamina > oneSided.Stamina,
            $"both arms now peak at the same budget ({oneSided.Stamina:F1}), so the "
            + "single-stamina sweep that produced the refutation was not obviously "
            + "wrong and this explanation needs re-reading");

        // AND INHIBITION'S PEAK CLEARS THE ONE-SIDED COUNT'S PEAK. A budget change
        // cannot do that: no setting of a dial makes an arm score above what that
        // same arm reaches at its own best setting. So the negative cell is buying
        // something the budget cannot.
        var apart = (inhibited.Mean - oneSided.Mean)
            / Math.Sqrt((inhibited.Err * inhibited.Err) + (oneSided.Err * oneSided.Err));

        output.WriteLine($"peak against peak: {apart:F2} sigma");

        // AND IT NO LONGER DOES, WHICH IS THE REVERSAL REVERSING. Inhibition's peak
        // reads 0.7259 against the one-sided count's 0.7434 -- MINUS 0.56 sigma, so
        // not beaten, but not distinguishable either. The claim that the negative
        // cell buys something a budget cannot is not supported by this measurement.
        //
        // THE ROW STANDS AS REFUTED UNTIL SOMETHING SHOWS OTHERWISE. That is the
        // conservative reading and it is the right one: "indistinguishable at peak"
        // is not evidence FOR the mechanism, and this reversal was the only thing
        // holding the row open. What it is not is evidence that inhibition is
        // harmful -- half a standard error the other way is nothing at all.
        //
        // ASSERTED AS INDISTINGUISHABLE RATHER THAN AS A LOSS, so that either
        // direction moving is news. John's call, 2026-08-05: adjust it now, and put
        // the two arms against each other properly when the baselines are redone.
        Assert.True(Math.Abs(apart) < 2.0,
            $"the two peaks have separated ({inhibited.Mean:F4} inhibited against "
            + $"{oneSided.Mean:F4} one-sided, {apart:F2} sigma). If inhibition is "
            + "ahead the reversal is back and the row reopens; if it is behind, the "
            + "negative cell is now actively costing something. Either wants "
            + "reading rather than this bar wants moving");
    }

    [Fact]
    public async Task Plain_association_peaks_below_the_bar_at_its_own_best_budget()
    {
        // THE RE-RUN THE INHIBITION REVERSAL MADE NECESSARY. Every claim in step 4
        // was taken at one stamina, and one of them turned out to be a comparison
        // between one arm's peak and another arm's way up. So the control gets the
        // same treatment: swept, and read at ITS OWN best budget.
        //
        // WHAT SURVIVES `Marked`'S COLLAPSE IS THE HALF THAT NEEDS NO SECOND CELL.
        // Plain association peaks BELOW the blind bar -- 0.2680 at stamina 2
        // against 0.3668 -- so `Credited`'s 0.7347 is not something any walk over
        // the ordinary cell was going to reach. That is worth asserting on its own:
        // it is what makes the credit cell the thing that changed rather than the
        // walking. The unconditioned control peaked at 0.3167 and is recorded in
        // the plan's table rather than run here.
        var peaks = new Dictionary<string, double>();

        foreach (var stamina in (double[])[2.0, 3.0, 4.0, 6.0, 8.0])
        {
            output.WriteLine($"--- stamina {stamina} ---");

            var arms = await ArmsAsync(
                Fixture.Dials(stamina), 24, Attending.Blind, Attending.Chain);

            foreach (var arm in arms)
                peaks[arm.Arm] = Math.Max(peaks.GetValueOrDefault(arm.Arm, 0.0), arm.Mean);
        }

        output.WriteLine(string.Join(
            "  ", peaks.Select(one => $"{one.Key}={one.Value:F4}")));

        Assert.True(peaks["chain"] < peaks["blind"],
            $"`chain` now beats the blind bar at its own best budget "
            + $"({peaks["chain"]:F4} against {peaks["blind"]:F4}), so walking the "
            + "ordinary cell is enough after all and step 4's premise needs "
            + "rewriting rather than re-asserting");
    }

    [Fact]
    public async Task Every_arm_but_one_gets_worse_the_further_it_is_allowed_to_reach()
    {
        // THE PATTERN UNDER TODAY'S RESULTS, AND IT IS THE SIMPLEST STATEMENT OF
        // THEM. `Blind` never consults the graph and is flat in the budget. EVERY
        // arm that does consult it declines as the walk is allowed to reach
        // further: more stamina reaches more partners, and a count that can only
        // say what HELPED rules none of them out, so the extra reach is all junk.
        //
        // `Contested` IS THE EXCEPTION, and it is the only arm here that can say
        // NOT THAT ONE. It still declines eventually -- nothing here is immune --
        // but it climbs first, peaks two budgets later, and peaks higher.
        var narrow = Fixture.Dials(stamina: 4.0);
        var wide = Fixture.Dials(stamina: 12.0);

        var near = await ArmsAsync(
            narrow, 24, Attending.Chain, Attending.Credited, Attending.Contested);

        var far = await ArmsAsync(
            wide, 24, Attending.Chain, Attending.Credited, Attending.Contested);

        double At(IReadOnlyList<Measured> arms, Attending arm) =>
            arms.First(one => one.Arm == arm.ToString().ToLowerInvariant()).Mean;

        foreach (var arm in (Attending[])[Attending.Chain, Attending.Credited])
        {
            output.WriteLine($"{arm,-10} {At(near, arm):F4} -> {At(far, arm):F4}");

            Assert.True(At(far, arm) < At(near, arm),
                $"`{arm}` stopped losing ground to a wider walk "
                + $"({At(far, arm):F4} against {At(near, arm):F4}), so the pattern "
                + "this test records has changed");
        }

        output.WriteLine(
            $"contested   {At(near, Attending.Contested):F4} -> "
            + $"{At(far, Attending.Contested):F4}");

        Assert.True(At(far, Attending.Contested) > At(near, Attending.Contested),
            $"inhibition no longer gains from a wider walk "
            + $"({At(far, Attending.Contested):F4} against "
            + $"{At(near, Attending.Contested):F4}), so it is no longer the "
            + "exception and today's reading of these results is wrong");
    }

    [Fact]
    public async Task The_credit_arm_reaches_its_level_fast_and_more_data_does_not_help()
    {
        // THE SILENCE HERE IS NOT THE SILENCE THE LAST TWO COMMITS WERE ABOUT.
        // `Chain` went quiet because routes could not AFFORD to reach an action --
        // a budget problem, and spending more bought the voice back. This arm goes
        // quiet because the credit cell is EMPTY until something has helped, and
        // no budget walks an edge that does not exist.
        //
        // SO THE PREDICTION WAS THAT A LONGER RUN WOULD FILL IT AND THE SCORE
        // WOULD RISE. IT IS REFUTED, AND BY THIS PROJECT'S OWN NAMED TRAP. At seed
        // 1 the score climbs 0.4150 -> 0.5938 -> 0.7025 across 400, 800 and 1600
        // steps, which reads exactly like a learning curve. Across six seeds the
        // rise is 0.2 sigma and there is no curve at all -- seed 1 is simply a bad
        // start that recovers. One seed showing a monotone trend is the shape a
        // small sample makes on its own.
        //
        // WHAT THAT MEANS: the arm reaches its level inside 400 steps and stays
        // there, silent 86% of the time, well below the ceiling. The gap is NOT
        // inexperience and will not close with more data.
        //
        // THE LIKELY REASON, AND IT IS THE NEXT THING TO ATTACK: distinct states
        // keep growing -- 92 at 400 steps, 207 at 1600 -- so a credit cell keyed
        // on the state it was earned in can never densely cover them. Nothing
        // carries what was learnt in one state across to a similar one, because
        // every generalisation here runs through similarity and there is none.
        // That is step 8's argument arriving from a third direction.
        // ACROSS SEEDS, because one seed showing a monotone rise is the shape a
        // small sample makes on its own -- a named trap here.
        var ranked = World() with { Ranked = true };

        var curve = await Sweep.AcrossAsync(
            6,
            ("400", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                return (await run.RunAsync(400, Attending.Credited)).Viable;
            }),
            ("800", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                return (await run.RunAsync(800, Attending.Credited)).Viable;
            }),
            ("1600", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                return (await run.RunAsync(1600, Attending.Credited)).Viable;
            }));

        output.WriteLine(Sweep.Table(curve));

        foreach (var steps in (int[])[400, 1600])
        {
            using var run = new HomeostatRun(ranked, Dials, seed: 1);
            var result = await run.RunAsync(steps, Attending.Credited);

            output.WriteLine(
                $"steps={steps,-5} silent={result.Silent,4}/{result.Steps} "
                + $"({result.Silent / (double)result.Steps:P0}) "
                + $"viable={result.Viable:F4} "
                + $"attended=[{string.Join(",", result.Attended)}] "
                + $"states={result.States} edges={result.Edges}");

            Assert.Empty(result.Complaints);
        }

        // NOT FLAT. IT FALLS, MONOTONICALLY, AND MORE DATA MAKES THIS ARM WORSE:
        //
        //    400   0.6450 +-0.0575
        //    800   0.4723 +-0.0353   2.6 sigma below 400
        //   1600   0.3844 +-0.0158   4.4 sigma below 400
        //
        // THE OLD ASSERTION COULD NOT SEE THE DIFFERENCE BETWEEN THIS AND THE GOOD
        // NEWS. It required `Separation < 2.0` and its message read "a longer run
        // now scores measurably BETTER -- the arm has started learning from data it
        // previously could not use". `Measured.Separation` is UNSIGNED, so a
        // collapse and a breakthrough trip the same bar and print the same
        // sentence. It reported a 40% fall as generalisation.
        //
        // SO THE DIRECTION IS ASSERTED SEPARATELY FROM THE SIZE. That an arm gets
        // worse with data is a defect worth someone's attention -- credit written
        // over a longer run is evidently accumulating something that misleads the
        // walk -- and it is recorded here rather than smoothed into a wider bar.
        Assert.True(curve[2].Mean < curve[0].Mean,
            $"the credit arm has stopped degrading with data ({curve[2].Mean:F4} at "
            + $"1600 against {curve[0].Mean:F4} at 400) -- that is GOOD NEWS and the "
            + "note above wants rewriting rather than repeating");

        // AND THE FALL IS MONOTONE, so it is a trend and not one bad length.
        Assert.True(curve[1].Mean < curve[0].Mean && curve[2].Mean < curve[1].Mean,
            $"the fall is no longer monotone ({curve[0].Mean:F4}, {curve[1].Mean:F4}, "
            + $"{curve[2].Mean:F4}), so whatever this arm is doing with more data "
            + "has changed shape and wants re-reading");

        // AND IT IS STILL SHORT OF THE CEILING at every length, so the room is
        // real and is not going to be filled by running for longer.
        Assert.All(curve, arm => Assert.True(arm.Mean < 0.95,
            $"{arm.Arm} reached the ceiling ({arm.Mean:F4}), so this world has "
            + "stopped having anything left to measure"));
    }

    [Fact]
    public async Task The_graph_has_no_reason_to_act_yet_and_the_baseline_says_so()
    {
        // THE BASELINE FOR STEP 4, AND IT IS EXPECTED TO BE POOR. Nothing tells
        // the walk what an action DOES -- it has only seen actions beside the
        // states they were taken in, so it reproduces whatever was done before in
        // a state rather than what would help. Drives are what would turn a felt
        // variable into a reason to act, and they are not built.
        //
        // This is recorded as a measurement rather than a target: when step 4
        // lands, this arm should move and the ceiling arm should not.
        using var run = new HomeostatRun(World(), Dials, seed: 1);
        var chain = await run.RunAsync(Steps, Attending.Chain);

        using var best = new HomeostatRun(World(), Dials, seed: 1);
        var lowest = await best.RunAsync(Steps, Attending.Lowest);

        using var blind = new HomeostatRun(World(), Dials, seed: 1);
        var random = await blind.RunAsync(Steps, Attending.Blind);

        output.WriteLine(chain.ToString());

        Assert.True(chain.Viable < lowest.Viable,
            $"the walk already matches the ceiling, which would mean step 4 has "
            + $"nothing to add: {chain.Viable} against {lowest.Viable}");

        // AND THE GRAPH IS ACTUALLY DECIDING SOME OF IT, or this arm is measuring
        // its own random fallback. See the note on the bootstrap in HomeostatRun.
        Assert.True(chain.Silent < chain.Steps,
            "the walk never once proposed an action, so this arm is the fallback");

        output.WriteLine(
            $"chain={chain.Viable:F4} blind={random.Viable:F4} lowest={lowest.Viable:F4}, "
            + $"and the walk decided {chain.Steps - chain.Silent} of {chain.Steps} steps");

        Assert.Empty(chain.Complaints);
    }
}
