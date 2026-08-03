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
