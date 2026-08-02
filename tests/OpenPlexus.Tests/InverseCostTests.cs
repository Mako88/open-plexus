using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// John's inverse cost: a step costs <c>1/weight</c> of the edge it walks, and
/// nothing is paid back. What matters is that this bounds the walk where
/// <see cref="StepCost.Best"/> does not.
/// </summary>
public sealed class InverseCostTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    private sealed class Everyone(double seen) : IMarginals
    {
        public double SeenOf(Code code) => seen;
    }

    private static WalkSettings Dials(StepCost cost, double stamina = 4.0) => new()
    {
        Stamina = stamina,
        Cost = cost,
        Refuel = Refuel.Strength,
        Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum,

        // Far above the stamina on purpose: if the horizon fires first it hides
        // whether the economics bound anything.
        Horizon = 50,
    };

    /// <summary>A clique where every weight is exactly 1.0 — the worst case.</summary>
    private static (Dictionary<Code, Node> Nodes, IMarginals Marginals) Clique(int size, StepCost cost)
    {
        var dials = Dials(cost);
        var codes = Enumerable.Range(0, size).Select(i => C((ulong)i)).ToArray();
        var nodes = codes.ToDictionary(c => c, c => new Node(c, dials));

        foreach (var code in codes)
        {
            for (var k = 0; k < 10; k++) nodes[code].Note();
            foreach (var other in codes)
                if (other != code)
                    for (var k = 0; k < 10; k++) nodes[code].Observe(other);
        }

        return (nodes, new Everyone(10.0));
    }

    private static (long Messages, int Depth, bool Capped) Flood(
        Dictionary<Code, Node> nodes, IMarginals marginals, double stamina, long ceiling)
    {
        var frontier = new List<Message>
        {
            new()
            {
                Broadcast = BroadcastId.New(), ReturnTo = new MachineAddress("t"),
                To = C(0), Held = stamina, Chain = [C(0)], Carried = 1.0,
            },
        };

        long messages = 0;
        var depth = 0;
        while (frontier.Count > 0)
        {
            depth++;
            var next = new List<Message>();
            foreach (var message in frontier)
            {
                foreach (var onward in nodes[message.To].Fire(message, marginals).Outgoing)
                {
                    next.Add(onward);
                    messages++;
                }

                if (messages > ceiling) return (messages, depth, true);
            }

            frontier = next;
        }

        return (messages, depth, false);
    }

    [Fact]
    public void A_route_takes_at_most_its_budget_in_perfect_hops()
    {
        // EVERY HOP COSTS AT LEAST 1, because a weight cannot exceed 1.0. So a
        // budget of 4 buys four steps however perfect the path, and that is
        // what bounds the walk with no horizon involved.
        var (nodes, marginals) = Clique(12, StepCost.Inverse);

        var (messages, depth, capped) = Flood(nodes, marginals, stamina: 4.0, ceiling: 5_000_000);

        Assert.False(capped);
        Assert.Equal(4, depth);
        Assert.True(messages < 2_000, $"{messages} messages on a 12-clique");
    }

    [Fact]
    public void Best_does_not_bound_the_same_walk_at_all()
    {
        // THE COMPANION, and it is the refutation re-run rather than restated.
        // Same clique, same budget, same horizon: `Best` charges the strongest
        // edge and pays back the taken one, so on equal weights every route
        // breaks even exactly and nothing ever decays.
        var (nodes, marginals) = Clique(12, StepCost.Best);

        var (messages, _, capped) = Flood(nodes, marginals, stamina: 4.0, ceiling: 5_000_000);

        Assert.True(capped, $"Best terminated after {messages} messages, which it should not");
    }

    [Fact]
    public void A_bigger_budget_buys_more_hops()
    {
        // The budget reads as "how many perfect hops can I afford", which is a
        // scale to sweep rather than a number nobody chose.
        var (nodes, marginals) = Clique(8, StepCost.Inverse);

        Assert.Equal(2, Flood(nodes, marginals, stamina: 2.0, ceiling: 5_000_000).Depth);
        Assert.Equal(6, Flood(nodes, marginals, stamina: 6.0, ceiling: 5_000_000).Depth);
    }

    [Fact]
    public void A_weaker_edge_costs_more_than_a_strong_one()
    {
        var node = new Node(C(1), Dials(StepCost.Inverse, stamina: 10.0));

        // together = 4 against a marginal of 4 is weight 1.0, costing 1.
        // together = 1 against the same marginal is weight 0.25, costing 4.
        for (var i = 0; i < 4; i++) node.Observe(C(2));
        node.Observe(C(3));

        var fired = node.Fire(new Message
        {
            Broadcast = BroadcastId.New(), ReturnTo = new MachineAddress("t"),
            To = C(1), Held = 10.0, Chain = [C(9), C(1)], Carried = 1.0,
        }, new Everyone(4.0));

        var left = fired.Outgoing.ToDictionary(m => m.To, m => m.Held);

        Assert.Equal(9.0, left[C(2)], precision: 10);
        Assert.Equal(6.0, left[C(3)], precision: 10);
    }

    [Fact]
    public void A_route_that_cannot_pay_for_the_weak_edge_still_takes_the_strong_one()
    {
        // The companion to the test above: the difference in cost has to
        // actually decide something, not just show up in a number.
        var node = new Node(C(1), Dials(StepCost.Inverse, stamina: 10.0));
        for (var i = 0; i < 4; i++) node.Observe(C(2));
        node.Observe(C(3));

        var fired = node.Fire(new Message
        {
            Broadcast = BroadcastId.New(), ReturnTo = new MachineAddress("t"),
            To = C(1), Held = 2.0, Chain = [C(9), C(1)], Carried = 1.0,
        }, new Everyone(4.0));

        Assert.Equal([C(2)], fired.Outgoing.Select(m => m.To).ToArray());
    }

    [Fact]
    public async Task On_snake_the_horizon_never_fires_at_all()
    {
        // THE CONSTANT STOPS BEING NEEDED. Measured over 200 seeds with the
        // horizon at 50 and the stamina at 4: zero routes halted, so every
        // route ended by running out of budget rather than by hitting a wall
        // nobody chose. `Best` at a horizon of 4 halted 105,189 over the same
        // grid, and its behaviour is indistinguishable — 6.655 mean steps
        // against 6.590, either side of a standard error of about 0.42.
        var world = new SnakeSettings
        {
            Width = 15, Height = 15, Sight = 1,
            StartingEnergy = 60.0, EnergyPerStep = 1.0, EnergyPerFood = 30.0,
        };

        long halted = 0, steps = 0;
        for (var seed = 1; seed <= 30; seed++)
        {
            using var run = new SnakeRun(world, Dials(StepCost.Inverse), seed);
            var result = await run.PlayAsync(300);
            halted += result.Halted;
            steps += result.Steps;
        }

        Assert.Equal(0, halted);

        // The companion: routes really did walk, so the zero above is not the
        // zero you get from a flood that never left the origin.
        Assert.True(steps > 60, $"only {steps} steps ran");
    }

    [Fact]
    public void Refuelling_is_refused_under_a_cost_that_pays_nothing_back()
    {
        // An argument that silently does nothing is a sweep arm that looks
        // distinct and is not.
        Assert.Throws<ArgumentException>(() => new Node(C(1), new WalkSettings
        {
            Stamina = 4.0, Cost = StepCost.Inverse, Refuel = Refuel.Surprise,
            Value = ArrivalValue.Strength, Accumulate = Accumulate.Sum, Horizon = 50,
        }));
    }
}
