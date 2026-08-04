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
        // able to EXPRESS the task is enough on its own; `Topped` ranked asks
        // whether the credit -- refuted three times over on a front end where the
        // correct policy was a constant -- has something to attach to now.
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
            ("topped+ranked", async seed =>
            {
                using var run = new HomeostatRun(ranked, Dials, seed);
                return (await run.RunAsync(Steps, Attending.Topped)).Viable;
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
            ("topped", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Topped)).Viable;
            }),
            ("marked", async seed =>
            {
                using var run = new HomeostatRun(World(), Dials, seed);
                return (await run.RunAsync(Steps, Attending.Marked)).Viable;
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
        var control = arms.First(one => one.Arm == "marked");

        // THE FIRST ARM IN THIS PROJECT TO BEAT DRAWING AT RANDOM. Everything that
        // consults the graph had scored below the bar, which is what step 4 exists
        // to fix.
        Assert.True(
            contrasted.Mean > bar.Mean && contrasted.Separation(bar) > 3.0,
            $"the credit cell stopped beating the bar: {contrasted.Mean:F4} "
            + $"against {bar.Mean:F4}");

        // AND THE CONTRAST IS WHAT DID IT, WHICH IS THE ONLY REASON THE ROW ABOVE
        // MEANS ANYTHING. `marked` writes the same second cell, one step stale,
        // into the same relation, and walks it the same way -- it differs by the
        // CONDITION alone. It does not beat the bar and it does not beat `chain`.
        Assert.True(control.Mean < bar.Mean,
            $"writing the second cell unconditionally also beats the bar "
            + $"({control.Mean:F4}), so the gain is the extra cell or its "
            + "staleness rather than the contrast, and the claim is wrong");

        // AND THE NEGATIVE HALF OVER-PRUNES, WHICH IS A RESULT AND NOT A BUG.
        // `Contested` writes `Kind.Hindered` when things got worse and the walk
        // reads the difference, so the CRDT objection to punishment is gone -- two
        // monotonic counters, convergence untouched, one kind rather than a wider
        // row. It still loses to the one-sided version.
        //
        // WHY: only one of four acts is right at any moment here, so the negative
        // cell fills roughly three times faster than the positive one and drives
        // nearly every pair to nought or below -- where a hard clamp refuses to
        // walk it at all. Measured, the graph goes almost completely mute: silent
        // 394 of 400 steps against the credited arm's 330, which is a coin toss
        // wearing an arm's name.
        //
        // SO THE SHAPE IS WRONG RATHER THAN THE IDEA. A difference cuts; what this
        // wants is something that DOWN-WEIGHTS -- a ratio, or shrinkage in the
        // denominator the way `Doubt` already does it.
        var contested = arms.First(one => one.Arm == "contested");

        Assert.True(contested.Mean < contrasted.Mean,
            $"subtracting the negative cell now beats the one-sided count "
            + $"({contested.Mean:F4} against {contrasted.Mean:F4}), so the "
            + "over-pruning note above has expired and needs re-running");

        Assert.True(contrasted.Separation(control) > 3.0,
            $"the conditioned and unconditioned arms are no longer separable: "
            + $"{contrasted.Mean:F4} against {control.Mean:F4}");

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

        // FLAT, AND ASSERTED AS FLAT. Quadrupling the run does not move the score
        // by even one standard error, which is the finding -- if this ever starts
        // failing because a longer run scores BETTER, the arm has begun
        // generalising and the note above needs rewriting rather than repeating.
        Assert.True(curve[2].Separation(curve[0]) < 2.0,
            $"a longer run now scores measurably better ({curve[2].Mean:F4} at "
            + $"1600 against {curve[0].Mean:F4} at 400, "
            + $"{curve[2].Separation(curve[0]):F1} sigma) -- the arm has started "
            + "learning from data it previously could not use");

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
