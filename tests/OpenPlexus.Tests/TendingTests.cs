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
}
