using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// Voting on snake, and the answer is that snake never needed it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The mechanism existed on `SensesRun` and `BindingRun` and not here</b>,
/// and the standing caveat was that every snake number was therefore a lower
/// bound taken at the noisy end. <b>Measured 2026-08-03, and that caveat was
/// wrong.</b>
/// </para>
/// <para>
/// The walk disagrees with itself on <b>0.0018 ± 0.0018 of steps</b> over 20
/// seeds — against roughly one question in eight on the senses world. Snake's
/// action alphabet is three turns over a graph of a few dozen nodes, so the
/// top-ranked action wins by a margin that delivery order does not overturn;
/// the senses world ranks 36 touch codes packed far closer together.
/// </para>
/// </remarks>
public sealed class VotingTests
{
    private static SnakeSettings World => new()
    {
        Width = 15, Height = 15, Sight = 2,
        StartingEnergy = 200, EnergyPerStep = 1, EnergyPerFood = 50,
    };

    private static WalkSettings Dials => new()
    {
        Stamina = 8.0, Foresight = 2.0, Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum, Horizon = 50,
    };

    private static async Task<RunResult> PlayAsync(int seed, int votes)
    {
        using var run = new SnakeRun(World, Dials, seed);
        return (await run.ReportAsync(200, votes: votes)).Result;
    }

    [Fact]
    public async Task Asking_three_times_actually_asks_three_times()
    {
        // THE WIRING CHECK, AND IT IS THE WHOLE REASON THIS TEST EXISTS. Voting
        // changes no outcome here, and "changes nothing" is exactly what a
        // parameter connected to nothing looks like -- which has happened in this
        // project before, to a stamina dial that survived a build, 155 tests and
        // three measurements. So the traffic is asserted, not the outcome.
        var once = await PlayAsync(seed: 3, votes: 1);
        var thrice = await PlayAsync(seed: 3, votes: 3);

        Assert.True(thrice.Messages > once.Messages,
            $"three votes carried {thrice.Messages} messages against one vote's {once.Messages}, "
            + "so the extra thoughts never happened");
    }

    [Fact]
    public async Task One_vote_can_never_disagree_with_itself()
    {
        // The companion. Without it `Disagreed` could be counting anything at
        // all and the test below would still pass.
        var once = await PlayAsync(seed: 3, votes: 1);

        Assert.Equal(0, once.Disagreed);
        Assert.True(once.Steps > 0);
    }

    [Fact]
    public async Task The_snake_walk_agrees_with_itself_and_that_is_why_voting_is_off()
    {
        // MEASURED AT 0.0018 +- 0.0018 OF STEPS OVER 20 SEEDS, which is one
        // standard error from zero. Bounded loosely rather than pinned, because
        // this is a concurrency number and pinning it would be asserting how busy
        // the machine is -- the trap that made the suite serial in the first
        // place.
        var disagreement = await Sweep.ArmAsync("disagreed per step", 8, async seed =>
        {
            var run = await PlayAsync(seed, votes: 3);
            return run.Steps == 0 ? 0.0 : run.Disagreed / (double)run.Steps;
        });

        Assert.True(disagreement.Mean < 0.05,
            $"{disagreement} — the walk disagrees with itself far more than it used to, "
            + "and snake numbers taken at one vote are no longer safe");
    }
}
