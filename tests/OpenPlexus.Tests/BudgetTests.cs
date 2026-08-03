using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// Fork 24 — a machine hunting for its own stamina.
/// </summary>
/// <remarks>
/// <b>Tested against a MADE-UP world with a known right answer</b>, so the
/// controller can be wrong in a way that is visible. A controller only ever
/// exercised through the real graph is one whose failures look like the graph's.
/// </remarks>
public sealed class BudgetTests
{
    private static Budgeting Settings(int window = 20, double worth = 0.02, double most = 64.0) =>
        new() { Window = window, Worth = worth, Most = most };

    /// <summary>
    /// A world where budgets below <paramref name="enough"/> reach nothing and
    /// budgets at or above it always reach — the plateau, with a sharp knee.
    /// </summary>
    private static bool Reaches(double stamina, double enough) => stamina >= enough;

    private static Budget Settle(double start, double enough, int rounds, Budgeting? settings = null)
    {
        var budget = new Budget(start, settings ?? Settings());

        for (var i = 0; i < rounds; i++)
            budget.Reached(Reaches(budget.Next(), enough));

        return budget;
    }

    [Fact]
    public void It_climbs_to_a_budget_that_reaches()
    {
        // Started far below what the world needs.
        var budget = Settle(start: 1.0, enough: 8.0, rounds: 2_000);

        Assert.True(budget.Stamina >= 8.0, $"settled at {budget.Stamina}, which reaches nothing");
    }

    [Fact]
    public void It_falls_back_from_a_budget_that_is_bigger_than_it_needs()
    {
        // THE HALF THAT MATTERS, and the reason the rule is asymmetric. Accuracy
        // is flat from stamina 8 to 24 on the senses world while messages rise
        // twenty-three fold, so a controller that only ever climbed would be
        // paying that and reporting success.
        var budget = Settle(start: 64.0, enough: 8.0, rounds: 2_000);

        Assert.True(budget.Stamina <= 16.0, $"settled at {budget.Stamina}, well past what it needed");
    }

    [Fact]
    public void Where_it_settles_does_not_depend_on_where_it_started()
    {
        // If the answer moved with the starting guess, the guess would still be
        // the constant and this would only have hidden it.
        var low = Settle(start: 1.0, enough: 8.0, rounds: 4_000).Stamina;
        var high = Settle(start: 64.0, enough: 8.0, rounds: 4_000).Stamina;

        Assert.Equal(low, high);
    }

    [Fact]
    public void A_world_that_needs_more_settles_higher()
    {
        // The companion to every test above. Without it they all pass for a
        // controller hard-wired to return 8.
        var cheap = Settle(start: 8.0, enough: 2.0, rounds: 4_000).Stamina;
        var dear = Settle(start: 8.0, enough: 32.0, rounds: 4_000).Stamina;

        Assert.True(dear > cheap, $"needing 32 settled at {dear}, needing 2 settled at {cheap}");
    }

    [Fact]
    public void It_probes_below_and_above_what_it_currently_holds()
    {
        // The mechanism, asserted directly: without a probe in both directions
        // the tests above could pass by drifting rather than by measuring.
        var budget = new Budget(8.0, Settings(window: 2));
        var tried = new List<double>();

        for (var i = 0; i < 6; i++)
        {
            tried.Add(budget.Next());
            budget.Reached(true);
        }

        Assert.Contains(4.0, tried);
        Assert.Contains(8.0, tried);
        Assert.Contains(16.0, tried);
    }

    [Fact]
    public void It_never_offers_a_budget_that_cannot_afford_one_hop()
    {
        // A hop costs at least 1 because a weight cannot exceed 1.0, so anything
        // below that buys nothing and the downward probe would measure the same
        // nothing forever.
        var budget = new Budget(1.0, Settings(window: 2));

        for (var i = 0; i < 100; i++)
        {
            Assert.True(budget.Next() >= 1.0, $"offered {budget.Next()}");
            budget.Reached(true);
        }
    }

    [Fact]
    public void It_stops_at_the_ceiling()
    {
        // The backstop. An unbounded climb is the one failure that takes the
        // process with it, because message cost explodes with the budget.
        var budget = Settle(start: 2.0, enough: 1_000.0, rounds: 4_000, Settings(most: 32.0));

        Assert.Equal(32.0, budget.Stamina);
    }

    [Fact]
    public void A_flat_world_is_driven_all_the_way_down()
    {
        // Nothing is ever gained by spending more, so the smallest budget wins.
        // This is the asymmetry doing its job in the extreme case.
        var budget = Settle(start: 64.0, enough: 0.0, rounds: 4_000);

        Assert.Equal(1.0, budget.Stamina);
    }

    [Fact]
    public void It_reports_whether_it_ever_moved()
    {
        // A controller that never moved and one that converged instantly look
        // identical from the outside otherwise -- which is the "wired up?"
        // question this project keeps having to ask.
        var moved = Settle(start: 1.0, enough: 8.0, rounds: 2_000);
        var already = Settle(start: 64.0, enough: 1_000.0, rounds: 2_000, Settings(most: 64.0));

        Assert.True(moved.Moves > 0);
        Assert.Equal(0, already.Moves);
    }
}
