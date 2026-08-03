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
        Stamina = 4.0, Cost = StepCost.Inverse, Refuel = Refuel.Strength,
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
    public async Task The_accounting_holds_on_every_real_thought()
    {
        // THE INTEGRITY CHECK, RUN FOR REAL RATHER THAN IN A FIXTURE. The live
        // count comes from splits and deaths; the per-cluster in-flight counts
        // come from the routing named in each report. Two independent
        // quantities, so them agreeing says the routes really were where the
        // origin thought they were.
        //
        // Measured at 60 seeds: 256 thoughts, 0 unbalanced. Before the clamp
        // in `Thought.Move` was removed it was 100 of 256.
        var runs = await Over(30, Policy.Chain);

        Assert.Equal(0, runs.Sum(r => r.Unbalanced));

        // The companion: thoughts actually ran, so the zero above is not the
        // zero you get from never checking anything.
        Assert.True(runs.Sum(r => r.Steps - r.Silent) > 50);
    }

    [Fact]
    public async Task Fruit_is_rare_rather_than_impossible()
    {
        // THIS TEST USED TO ASSERT NOBODY EVER ATE, AND THAT WAS WRONG. It was
        // built from 30-seed samples, and at 200 seeds fruit does get taken:
        // 7 times under the chain, 3 under repeat, 0 under random. Asserting
        // the zero made a sample-size artefact look like a property, and a
        // large enough run would have turned it red for the right reason with
        // nobody watching.
        //
        // What is honest to assert is the rarity, so that a change which makes
        // eating COMMON is noticed rather than absorbed.
        //
        // AND THIS IS A TRIPWIRE, NOT A MECHANISM TEST. No mutation of the
        // production code turns it red: it asserts a property of the world and
        // the policy together, not of anything one class does. Loosening the
        // bound passes trivially, which is what a one-sided assertion does.
        // Kept for what it would catch -- a change that makes the system
        // suddenly competent, which nobody would want to discover by accident.
        var chain = await Over(20, Policy.Chain);

        Assert.True(chain.Sum(r => r.Ate) <= 3,
            $"fruit is no longer rare: {chain.Sum(r => r.Ate)} in 20 runs");
        Assert.True(chain.Sum(r => r.Steps) > 40, "the runs were too short to say anything");
    }
}
