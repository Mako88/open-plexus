using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether the chain is doing anything. The tests here assert the MECHANISM;
/// the comparison between policies is a measurement and lives in
/// docs/architecture.md with its seed count.
/// </summary>
public sealed class PolicyTests
{
    private static SnakeSettings World() => new()
    {
        Width = 15, Height = 15, Sight = 1,
        StartingEnergy = 60.0, EnergyPerStep = 1.0, EnergyPerFood = 30.0,
    };

    private static WalkSettings Dials() => new()
    {
        Stamina = 4.0, Cost = StepCost.Best, Refuel = Refuel.Strength,
        Value = ArrivalValue.Strength, Accumulate = Accumulate.Sum, Horizon = 4,
    };

    private static async Task<RunResult> Play(int seed, Policy policy)
    {
        using var run = new SnakeRun(World(), Dials(), seed, includeEmpty: false);
        return await run.PlayAsync(300, policy: policy);
    }

    private static async Task<List<RunResult>> Over(int seeds, Policy policy)
    {
        var results = new List<RunResult>();
        for (var seed = 1; seed <= seeds; seed++) results.Add(await Play(seed, policy));
        return results;
    }

    [Fact]
    public async Task The_chain_is_not_just_echoing_the_last_action()
    {
        // THE CHECK THAT DECIDES WHETHER "A CHAIN CAUSED A MOVE" MEANS ANYTHING.
        // The action joins the occasion it was taken in, so the last action is
        // tightly bound to the current view. A walk that only ever returned
        // what the body just did would make the claim true and empty.
        var live = await Over(10, Policy.Chain);

        var chose = live.Sum(r => r.ChosenByChain);
        var echoed = live.Sum(r => r.EchoedLast);

        Assert.True(chose > 0, "no chain caused a move at all");
        Assert.True(echoed < chose,
            $"every one of {chose} chain-chosen moves repeated the last action");

        // THE COMPANION, and it is not optional: "echoed is below chosen" is
        // satisfied perfectly by a counter that never increments, so without
        // this the anti-mirror check reads its own failure as a clean result.
        // Measured at 28 of 77 over ten seeds, against a chance rate of one in
        // four — so the chain does carry some momentum, and nothing like all.
        Assert.True(echoed > 0, "the echo counter never moved, so it proves nothing");
    }

    [Fact]
    public async Task Both_controls_ignore_the_chain_and_the_chain_arm_does_not()
    {
        // The three arms have to actually differ, or every comparison between
        // them is a comparison of one thing with itself.
        var chain = await Play(3, Policy.Chain);
        var random = await Play(3, Policy.Random);
        var repeat = await Play(3, Policy.Repeat);

        Assert.True(chain.ChosenByChain > 0);
        Assert.Equal(0, random.ChosenByChain);
        Assert.Equal(0, repeat.ChosenByChain);

        // And the controls are not each other.
        Assert.NotEqual(random.Steps, repeat.Steps);
    }

    [Fact]
    public async Task The_controls_still_learn_the_same_graph()
    {
        // The point of these controls over `blind`: `blind` stops the action
        // joining the occasion, which changes the GRAPH as well as the choice,
        // so it cannot isolate whether the chain helps. These change only the
        // choice.
        var random = await Play(3, Policy.Random);
        var blind = new SnakeRun(World(), Dials(), 3, includeEmpty: false);
        var cut = await blind.PlayAsync(300, blind: true);
        blind.Dispose();

        Assert.Equal(0, random.ChosenByChain);
        Assert.Equal(0, cut.ChosenByChain);

        // SAME POLICY, DIFFERENT GRAPH — which is exactly the confound. The
        // blind arm never lets an action code into an occasion, so it has
        // strictly fewer nodes to walk.
        Assert.True(cut.Nodes < random.Nodes,
            $"blind built {cut.Nodes} nodes against random's {random.Nodes}");
    }

    [Fact]
    public async Task The_chain_outlives_random_by_more_than_noise()
    {
        // Measured at 200 seeds: chain 6.575 +/- 0.408, random 3.990 +/- 0.272.
        // A gap of 2.585 against a combined standard error of 0.490 -- about
        // five standard errors, so it survives a much smaller sample than that
        // and this test uses 40 seeds.
        var chain = await Over(40, Policy.Chain);
        var random = await Over(40, Policy.Random);

        Assert.True(chain.Average(r => r.Steps) > random.Average(r => r.Steps) * 1.3,
            $"chain {chain.Average(r => r.Steps):F2} against random " +
            $"{random.Average(r => r.Steps):F2}");
    }

    [Fact]
    public async Task Repeating_the_last_action_cannot_outlive_the_board()
    {
        // WHY THE MEANS ARE NOT THE INTERESTING NUMBER. Walking straight from
        // the centre of a 15-wide board hits the wall, so this policy is capped
        // by geometry and never reaches 10 steps -- 0 of 200 measured. The
        // chain passed 10 steps in 62 of 200. Same mean, entirely different
        // shape, and the mean hides it.
        var repeat = await Over(40, Policy.Repeat);

        Assert.All(repeat, r => Assert.True(r.Steps <= 8, $"repeat survived {r.Steps}"));

        // The companion: the chain is not capped the same way.
        var chain = await Over(40, Policy.Chain);
        Assert.Contains(chain, r => r.Steps > 8);
    }

    [Fact]
    public async Task Nothing_has_eaten_a_fruit_under_any_policy()
    {
        // Recorded so no claim of competence can quietly be made. Thirty seeds
        // across three policies produced zero fruit; this checks a slice of it.
        foreach (var policy in (Policy[])[Policy.Chain, Policy.Random, Policy.Repeat])
            Assert.All(await Over(6, policy), r => Assert.Equal(0, r.Ate));
    }
}
