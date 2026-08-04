using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The second body — <b>and the first world here where an act is enabled by an
/// act that helped nothing.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>`Homeostat` CANNOT SEE WHAT IS LEFT TO BUILD.</b> Every act there pays off
/// in the step it is taken, so a credit signal comparing one moment to the next
/// captures all of it — and eligibility traces, curiosity and rollout are all
/// built because one-step credit is insufficient. A world where it IS sufficient
/// cannot measure them.
/// </para>
/// <para>
/// <b>Here water reaches only the plant underfoot</b>, so getting to the plant
/// that needs it costs steps that improve nothing, and the watering itself lands a
/// step late. <b>The claims below are about the WORLD</b> — that it is winnable,
/// that it is not winnable by accident, and that one-step credit provably cannot
/// see part of it. Those have to hold before any arm run on it means anything.
/// </para>
/// </remarks>
public sealed class TendingTests(ITestOutputHelper output)
{
    private static TendingSettings World() => new();

    private static WalkSettings Dials => Fixture.Dials(stamina: 4.0);

    private const int Steps = 400;

    [Fact]
    public void Everything_dries_whether_or_not_anything_is_done()
    {
        // THE PROPERTY THAT MAKES IDLING COST, and the one that disqualified
        // survival as a score: standing still is the failure, not the safe option.
        var world = new Tending(World());
        var before = world.At.ToList();

        world.Step(null);

        Assert.All(Enumerable.Range(0, world.Plants),
            which => Assert.True(world.At[which] < before[which]));

        // AND THE FASTEST-DRYING ONE DRIES FASTEST, which is what makes spreading
        // water evenly the wrong thing to do.
        Assert.True(
            before[world.Plants - 1] - world.At[world.Plants - 1] > before[0] - world.At[0]);
    }

    [Fact]
    public void Water_reaches_only_what_is_underfoot_and_it_lands_a_step_late()
    {
        // THE TWO DELAYS, ASSERTED RATHER THAN DESCRIBED. Everything this world is
        // for rests on them.
        var world = new Tending(World());

        // DRY IT OUT FIRST, or the test measures the clamp instead of the delay:
        // a plant that is already full absorbs a watering and reads unchanged, so
        // the landing would be invisible and this would pass for a world with no
        // watering in it at all.
        for (var step = 0; step < 50; step++) world.Step(null);

        var wasHere = world.At[0];
        world.Step(2);

        // POURED AND NOT YET LANDED: the step it was poured on, it only dried.
        Assert.True(world.At[0] < wasHere,
            "watering took effect in the step it was poured, so there is no delay");

        var poured = world.At[0];
        world.Step(null);

        Assert.True(world.At[0] > poured,
            "the watering never landed at all");

        // AND IT LANDED WHERE THE BODY WAS STANDING, not where it was needed.
        Assert.Equal(0, world.Standing);
    }

    [Fact]
    public void Getting_somewhere_costs_steps_that_help_nothing()
    {
        // THE CLAIM THE WHOLE WORLD IS BUILT ON, AND IT IS ARITHMETIC RATHER THAN
        // a measurement: a move changes no moisture level at all, so ANY signal
        // that scores an act by what immediately followed it rates every move as
        // worthless — while the body cannot water the plant that needs it without
        // making them.
        var world = new Tending(World());

        var before = world.At.ToList();
        var was = world.Standing;

        world.Step(1);

        Assert.NotEqual(was, world.Standing);

        // Everything fell by exactly its drying rate and nothing else moved.
        Assert.All(Enumerable.Range(0, world.Plants), which =>
            Assert.Equal(before[which] - world.Dries(which), world.At[which], 10));
    }

    [Fact]
    public async Task The_world_is_winnable_and_not_by_accident()
    {
        // BOTH BOUNDS, OR THE WORLD MEASURES NOTHING — the same pair `Homeostat`
        // asserts, and here they have to be MEASURED rather than computed, because
        // travel time is part of the arithmetic and there is no closed form for it.
        using var oracle = new TendingRun(World(), Dials, seed: 1);
        using var random = new TendingRun(World(), Dials, seed: 1);
        using var still = new TendingRun(World(), Dials, seed: 1);

        var best = await oracle.RunAsync(Steps, Gardening.Best);
        var blind = await random.RunAsync(Steps, Gardening.Blind);
        var idle = await still.RunAsync(Steps, Gardening.Idle);

        output.WriteLine(best.ToString());
        output.WriteLine(blind.ToString());
        output.WriteLine(idle.ToString());

        Assert.True(best.Viable > 0.9,
            $"the ceiling could not hold the garden, so it is not winnable: "
            + $"{best.Viable:F4}");

        Assert.True(blind.Viable < best.Viable - 0.2,
            $"acting at random did nearly as well as the oracle, so this world "
            + $"discriminates nothing: {blind.Viable:F4} against {best.Viable:F4}");

        Assert.True(idle.Viable < 0.25,
            $"doing nothing held the garden for {idle.Viable:F4} of the run");

        // AND THE ORACLE ACTUALLY TRAVELS, which is what says the world is not
        // secretly solvable from one spot. A ceiling that never moved would mean
        // the delay and the geography were decoration.
        Assert.True(best.Travelling > 0.25,
            $"the oracle barely moved ({best.Travelling:F4}), so getting somewhere "
            + "is not part of this world after all");

        Assert.Empty(best.Complaints);
        Assert.Empty(blind.Complaints);
        Assert.Empty(idle.Complaints);
    }

    [Fact]
    public async Task Step_fours_arm_runs_here_and_this_is_its_second_world()
    {
        // THE POINT OF THE WORLD, AND THE READING IS DELIBERATELY OPEN. `Credited`
        // is the arm that beat the bar on `Homeostat` and it is the only claim in
        // this project resting on one world. What it does here is what this exists
        // to find out — and the plan predicts it should STRUGGLE, because a
        // one-step credit signal cannot see that a move was worth making.
        //
        // ASSERTED: it runs, it is wired, and it is read beside its silence. NOT
        // asserted: a threshold, which would be the answer decided in advance.
        using var walked = new TendingRun(World(), Dials, seed: 1);
        using var random = new TendingRun(World(), Dials, seed: 1);

        var credited = await walked.RunAsync(Steps, Gardening.Credited);
        var blind = await random.RunAsync(Steps, Gardening.Blind);

        output.WriteLine(credited.ToString());
        output.WriteLine(blind.ToString());

        Assert.True(credited.Silent < Steps,
            "the walk never proposed anything at all, so this arm is its own coin "
            + "toss and nothing about the graph is being measured");

        // THE GRAPH HAS TEMPORAL CELLS, which is what `Homeostat` silently did not
        // and is why nothing there could be asked what follows what. A world about
        // delayed consequences with no `after` cell would be the same fault twice.
        Assert.True(credited.Edges > 0);
        Assert.Empty(credited.Complaints);
    }

    /// <summary>
    /// Step 7 — <b>the credit reaching back past the step that earned it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>COVERAGE IS THE DIAGNOSED BOTTLENECK, FROM THREE DIRECTIONS.</b> The
    /// credit arm is silent on nearly every step here and on most steps of
    /// `Homeostat`; step 9 established that the silence cannot be cured by asking a
    /// WIDER question, because anything wide enough converges on the behaviour
    /// policy. <b>The remaining move is to widen what is WRITTEN.</b>
    /// </para>
    /// <para>
    /// <b>AND THE ONE THING THIS WORLD HAS THAT THE OTHER DOES NOT is a move —
    /// an act that improves nothing and is the only reason the next act can
    /// help.</b> A one-step signal must rate it worthless. A trace can credit it.
    /// </para>
    /// <para>
    /// <b>THE SPAN IS THE WORLD'S OWN ARITHMETIC</b> — crossing the garden, pouring,
    /// and waiting a step for it to land — so no constant is chosen, and a trace of
    /// ONE is <c>Credited</c> exactly.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Credit_that_reaches_back_against_credit_for_the_last_step_alone()
    {
        var arms = await Sweep.AcrossAsync(
            8,
            Arm("blind", Gardening.Blind),
            Arm("credited", Gardening.Credited),
            Arm("traced", Gardening.Traced),
            Arm("smeared", Gardening.Smeared),
            Arm("best", Gardening.Best));

        output.WriteLine(Sweep.Table(arms));

        foreach (var how in
            (Gardening[])[Gardening.Credited, Gardening.Traced, Gardening.Smeared])
        {
            using var run = new TendingRun(World(), Dials, seed: 1);
            var result = await run.RunAsync(Steps, how);

            output.WriteLine(
                $"{how,-9} silent={result.Silent,3}/{result.Steps} "
                + $"viable={result.Viable:F4} travelling={result.Travelling:F4} "
                + $"states={result.States} edges={result.Edges} msgs={result.Messages}");
        }

        // ASSERTED: THE TRACE ACTUALLY WROTE MORE THAN THE LAST STEP.
        //
        // This is the live trap about a check that is wired and unable to fire,
        // and it caught step 10's selective arm reproducing its control down to
        // the edge count. An arm that reaches further back must leave a bigger
        // graph behind it; if it does not, whatever the score says is about
        // something else.
        using var reaching = new TendingRun(World(), Dials, seed: 1);
        var traced = await reaching.RunAsync(Steps, Gardening.Traced);

        using var immediate = new TendingRun(World(), Dials, seed: 1);
        var oneStep = await immediate.RunAsync(Steps, Gardening.Credited);

        Assert.True(traced.Edges > oneStep.Edges,
            $"the trace wrote no more cells than crediting the last step alone "
            + $"({traced.Edges} against {oneStep.Edges}), so it is not reaching "
            + "back at all and its score is about something else");

        // AND THE RUN LENGTH, WHICH IS THE ONLY THING THAT SEPARATES "STUCK" FROM
        // "NOT YET". Step 4's arm on `Homeostat` was silent and short of the
        // ceiling, and quadrupling the run moved neither — which is what turned a
        // patience problem into a structural one. The same check has to be run
        // here before this arm is written up as refuted.
        foreach (var longer in (int[])[400, 1600])
        {
            using var run = new TendingRun(World(), Dials, seed: 1);
            var result = await run.RunAsync(longer, Gardening.Traced);

            output.WriteLine(
                $"traced {longer,5} steps silent={result.Silent,5}/{result.Steps} "
                + $"({result.Silent / (double)result.Steps:P0}) viable={result.Viable:F4} "
                + $"states={result.States} edges={result.Edges}");
        }

        Assert.Empty(traced.Complaints);
    }

    /// <summary>
    /// Step 8 — <b>the same reading said coarsely as well as finely, and it is the
    /// only likeness left that the graph did not compute.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>COVERAGE IS THE MEASURED BOTTLENECK AND IT GETS WORSE WITH
    /// EXPERIENCE.</b> Quadrupling a run here grows the states more than three
    /// times over while the credit cells grow less than half again, so a cell keyed
    /// on the state that earned it falls further behind the longer the body runs.
    /// Step 7 could not fix that by spreading credit further back, and step 9
    /// established it cannot be fixed by asking a wider question.
    /// </para>
    /// <para>
    /// <b>SO STATES HAVE TO STOP BEING ALL DISTINCT.</b> Two states differing in
    /// the fine band share the coarse one, and meet at that node — with nothing
    /// deciding they are similar and nothing derived from what the body did.
    /// </para>
    /// <para>
    /// <b>THE READING IS THE STATE COUNT BESIDE THE SILENCE.</b> Grains should cut
    /// the distinct states and the silence together; if the states fall and the
    /// silence does not, the coarse codes are being written and not walked.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Saying_it_coarsely_as_well_as_finely_against_saying_it_once()
    {
        var graded = World() with { Grains = 3 };

        var arms = await Sweep.AcrossAsync(
            8,
            Arm("blind", Gardening.Blind),
            Arm("credited", Gardening.Credited),
            Arm("credited+grained", Gardening.Credited, graded),
            Arm("traced+grained", Gardening.Traced, graded),
            Arm("best", Gardening.Best));

        output.WriteLine(Sweep.Table(arms));

        foreach (var (name, world, how) in
            (( string, TendingSettings, Gardening )[])
            [("fine", World(), Gardening.Credited),
             ("grained", graded, Gardening.Credited),
             ("grained+traced", graded, Gardening.Traced)])
        {
            using var run = new TendingRun(world, Dials, seed: 1);
            var result = await run.RunAsync(Steps, how);

            output.WriteLine(
                $"{name,-15} silent={result.Silent,3}/{result.Steps} "
                + $"viable={result.Viable:F4} states={result.States} "
                + $"edges={result.Edges} widest={result.Widest} msgs={result.Messages}");
        }

        using var fine = new TendingRun(World(), Dials, seed: 1);
        using var coarse = new TendingRun(graded, Dials, seed: 1);

        var sharp = await fine.RunAsync(Steps, Gardening.Credited);
        var blurred = await coarse.RunAsync(Steps, Gardening.Credited);

        // THE STATE COUNT CANNOT FALL AND EXPECTING IT TO WAS A MISREADING OF THE
        // INSTRUMENT. `Felt.Key` counts distinct code SETS, and grains ADD codes
        // without removing any — so two states that differ finely still differ as
        // sets, however much they now share. The count is unchanged by
        // construction and says nothing either way.
        //
        // WHAT THE MECHANISM ACTUALLY CLAIMS is that a coarse code is SHARED where
        // a fine one is not, and a shared code is a node many states meet at —
        // which shows up as a WIDER row, not a smaller state count.
        Assert.Equal(sharp.States, blurred.States);

        Assert.True(blurred.Widest > sharp.Widest,
            $"the coarse codes are not shared by anything ({blurred.Widest} against "
            + $"{sharp.Widest}), so they are a cost with no likeness in them");

        // AND THE HONEST READING OF THE SCORE: THIS WORLD CANNOT MEASURE ANY OF IT
        // YET. Every credit arm here is silent on 399 of 400 steps and scores
        // exactly what a coin toss scores, so what is being compared is four copies
        // of the same random policy. THE BOOTSTRAP DOMINATES EVERYTHING
        // DOWNSTREAM.
        //
        // THAT IS THE PLAN'S OWN RULE ABOUT A WORLD ABSORBING A CHANGE — at chance
        // or at its ceiling it says nothing — and it is the instrument at fault
        // rather than the arm. The garden has four plants at eight bands across
        // four positions, so the credit cell can never populate within a run; it
        // has to be shrunk until an arm can get off the ground, and only then
        // grown back.
        Assert.True(blurred.Silent > Steps * 0.9,
            "an arm here has stopped being nearly all coin toss, so this world can "
            + "now discriminate credit arms and the note above is out of date");

        Assert.Empty(blurred.Complaints);
    }

    /// <summary>One arm of a sweep, so two tables cannot drift apart.</summary>
    private static (string, Func<int, Task<double>>) Arm(
        string name, Gardening how, TendingSettings? world = null) =>
        (name, async seed =>
        {
            using var run = new TendingRun(world ?? World(), Dials, seed);
            return (await run.RunAsync(Steps, how)).Viable;
        });
}
