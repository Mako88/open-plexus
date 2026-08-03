using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// What the arms cost. <b>Not what they are worth</b> — see the note on
/// <see cref="Sweep"/> for why these runs cannot say that.
/// </summary>
public sealed class SweepTests
{
    private static Task<SweepRow> Run(int horizon, bool includeEmpty, int seed = 1) =>
        Sweep.OnceAsync(horizon, includeEmpty, StepCost.Best, seed, steps: 40);

    [Fact]
    public async Task The_horizon_is_what_bounds_the_flood()
    {
        var shallow = await Run(horizon: 2, includeEmpty: true);
        var deeper = await Run(horizon: 4, includeEmpty: true);

        // Same world, same seed, same everything else — so the walk really is
        // bounded by this constant and by nothing the design intended. Measured
        // at seed 1: 119 routes halted at horizon 2, 7,068 at horizon 4.
        Assert.Equal(shallow.Result.Steps, deeper.Result.Steps);
        Assert.True(deeper.Result.Halted > shallow.Result.Halted * 10,
            $"horizon 4 halted {deeper.Result.Halted} against horizon 2's {shallow.Result.Halted}");
    }

    [Fact]
    public async Task Withholding_empty_cells_costs_orders_of_magnitude_less()
    {
        var dense = await Run(horizon: 5, includeEmpty: true);
        var sparse = await Run(horizon: 5, includeEmpty: false);

        // FORK 8'S CHEAPEST CANDIDATE, and it needs no new mechanism — the arm
        // already exists. An occasion is a clique, so nine cell codes a frame
        // build a dense graph by construction and density is what makes path
        // enumeration explode. Measured at seed 1, horizon 5: 46,536 routes
        // halted with empty cells against 6 without.
        Assert.True(sparse.Result.Halted * 100 < dense.Result.Halted,
            $"sparse halted {sparse.Result.Halted} against dense {dense.Result.Halted}");

        Assert.True(sparse.Result.Nodes < dense.Result.Nodes);
    }

    [Fact]
    public async Task The_cheap_arm_still_lets_a_chain_cause_a_move()
    {
        // THE COMPANION, and without it the saving above means nothing: an arm
        // that costs nothing because it does nothing is not a saving.
        var sparse = await Run(horizon: 5, includeEmpty: false, seed: 3);

        Assert.True(sparse.Result.ChosenByChain > 0,
            $"no chain caused a move in {sparse.Result.Steps} steps");
    }
}
