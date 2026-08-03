using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// Does the chain do anything, on the arm where runs are long enough to ask?
/// </summary>
/// <remarks>
/// <b>The headline is that SURVIVAL IS THE WRONG METRIC</b>, and these tests
/// exist mostly to keep that from being forgotten. See fork 10.
/// </remarks>
public sealed class Fork10Tests
{
    private static SnakeSettings World(double energy) => Fixture.Snake(energy: energy);

    private static WalkSettings Dials() => Fixture.Dials(foresight: 2.0);

    private static async Task<List<RunResult>> Over(int seeds, Policy policy, double energy)
    {
        var results = new List<RunResult>();
        for (var seed = 1; seed <= seeds; seed++)
        {
            using var run = new SnakeRun(World(energy), Dials(), seed);
            results.Add(await run.PlayAsync(1000, policy: policy));
        }

        return results;
    }

    [Fact]
    public async Task Repeating_a_turn_survives_by_circling_and_never_eats()
    {
        // THE RESULT THAT DISQUALIFIES SURVIVAL AS A SCORE. Under relative
        // actions, repeating Left or Right is a tight circle the snake can hold
        // forever — so it outlives everything and starves rather than dying.
        // Measured at 200 seeds with 200 energy: 133.71 mean steps, the best of
        // any arm, and TWO fruit in the whole grid against the chain's forty.
        var repeat = await Over(40, Policy.Repeat, energy: 200.0);

        Assert.True(repeat.Count(r => r.FinalEnergy <= 0.0) > repeat.Count / 2,
            "most repeat runs should end starved rather than by collision");
        Assert.True(repeat.Sum(r => r.Ate) <= 2, $"repeat ate {repeat.Sum(r => r.Ate)}");
    }

    [Fact]
    public async Task The_chain_eats_where_repeating_does_not()
    {
        // The companion, and the reason the test above is a finding rather than
        // a complaint: the arm that survives longest is the one that achieves
        // least. Measured at 200 seeds: chain 40 fruit, repeat 2.
        var chain = await Over(40, Policy.Chain, energy: 200.0);
        var repeat = await Over(40, Policy.Repeat, energy: 200.0);

        Assert.True(chain.Sum(r => r.Ate) > repeat.Sum(r => r.Ate) * 2,
            $"chain ate {chain.Sum(r => r.Ate)}, repeat ate {repeat.Sum(r => r.Ate)}");
    }

    [Fact]
    public async Task The_chain_outlives_random_by_far_more_than_noise()
    {
        // Measured at 200 seeds, 200 energy: 92.85 +/- 4.06 against
        // 37.41 +/- 1.76, which is about twelve standard errors. It survives a
        // much smaller sample than that.
        var chain = await Over(30, Policy.Chain, energy: 200.0);
        var random = await Over(30, Policy.Random, energy: 200.0);

        Assert.True(chain.Average(r => r.Steps) > random.Average(r => r.Steps) * 1.5,
            $"chain {chain.Average(r => r.Steps):F1} against random " +
            $"{random.Average(r => r.Steps):F1}");
    }

    [Fact]
    public async Task Raising_the_energy_stops_the_runs_being_clipped()
    {
        // At 60 energy the chain's longest run was exactly 60 — the runs were
        // censored by the cap rather than ended by the world, so no mean taken
        // there was a measurement of anything.
        var clipped = await Over(20, Policy.Chain, energy: 60.0);
        var roomy = await Over(20, Policy.Chain, energy: 200.0);

        Assert.True(clipped.Max(r => r.Steps) <= 60);
        Assert.True(roomy.Max(r => r.Steps) > 60, "still clipped at 200 energy");
    }
}
