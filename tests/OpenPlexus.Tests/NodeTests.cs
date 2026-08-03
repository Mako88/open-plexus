using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// A node, tested with no bus, no cluster and no network.
/// </summary>
/// <remarks>
/// That this is possible at all is why <see cref="Node.Fire"/> returns its
/// outgoing messages instead of sending them.
/// </remarks>
public sealed class NodeTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    private static WalkSettings Dials(
        StepCost cost = StepCost.Best,
        Refuel refuel = Refuel.Strength,
        ArrivalValue value = ArrivalValue.Strength,
        double stamina = 1.0,
        double charge = 0.0,
        int horizon = 6) => new()
        {
            Stamina = stamina,
            Cost = cost,
            Charge = charge,
            Refuel = refuel,
            Value = value,
            Accumulate = Accumulate.Sum,
            Horizon = horizon,

            // PINNED TO THE ARM THESE TESTS ARE ABOUT, which is not the same as
            // pinning to a configuration nobody uses. Every test here hands a
            // `Marginals` stub, and only the sender arm reads one. The receiver
            // arm -- the default, and the one the runs use -- has its own file.
            Weighing = Weighing.Sender,
        };

    /// <summary>A message that has already walked, so the node is not an origin.</summary>
    private static Message Arriving(Code to, double held = 10.0, params Code[] before) => new()
    {
        Broadcast = new BroadcastId(Guid.Empty),
        ReturnTo = new MachineAddress("test"),
        To = to,
        Held = held,
        Chain = [.. before, to],
        Carried = 1.0,
    };

    // ---- learning ---------------------------------------------------------

    [Fact]
    public void Observing_a_partner_is_the_connection_forming()
    {
        var node = new Node(C(1), Dials());

        Assert.Equal(0.0, node.Together(C(2)));
        Assert.Empty(node.Partners());

        node.Observe(C(2));
        node.Observe(C(2));

        Assert.Equal(2.0, node.Together(C(2)));
        Assert.Equal([C(2)], node.Partners().ToArray());
    }

    [Fact]
    public void Noting_moves_only_the_marginal()
    {
        var node = new Node(C(1), Dials());

        node.Note();
        node.Note();

        Assert.Equal(2.0, node.Seen);
        Assert.Empty(node.Partners());
    }

    [Fact]
    public void A_code_cannot_be_its_own_partner()
    {
        var node = new Node(C(1), Dials());

        Assert.Throws<ArgumentException>(() => node.Observe(C(1)));
    }

    // ---- the dials refuse contradictions ----------------------------------

    [Fact]
    public void A_charge_without_constant_pricing_is_refused()
    {
        // An argument that silently does nothing is a sweep arm that looks
        // distinct and is not.
        Assert.Throws<ArgumentException>(
            () => new Node(C(1), Dials(cost: StepCost.Best, charge: 0.5)));
    }

    [Fact]
    public void Constant_pricing_without_a_charge_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Node(C(1), Dials(cost: StepCost.Constant, charge: 0.0)));
    }

    // ---- firing -----------------------------------------------------------

    [Fact]
    public void A_misrouted_message_is_refused_rather_than_answered()
    {
        var node = new Node(C(1), Dials());

        Assert.Throws<ArgumentException>(
            () => node.Fire(Arriving(C(99)), new Marginals()));
    }

    [Fact]
    public void A_node_with_nowhere_to_go_reports_one_death()
    {
        var node = new Node(C(1), Dials());

        var fired = node.Fire(Arriving(C(1), before: C(0)), new Marginals());

        Assert.Empty(fired.Outgoing);
        Assert.Equal(1, fired.Accounting.Deaths);
        Assert.Equal(0, fired.Accounting.Splits);
    }

    [Fact]
    public void Three_survivors_are_two_splits()
    {
        var node = new Node(C(1), Dials());
        foreach (var partner in (Code[])[C(2), C(3), C(4)]) node.Observe(partner);
        var marginals = new Marginals().Set(C(2), 1).Set(C(3), 1).Set(C(4), 1);

        var fired = node.Fire(Arriving(C(1), before: C(0)), marginals);

        // One route became three, so the live count moves by the difference.
        // A split is not the birth of three new routes.
        Assert.Equal(3, fired.Outgoing.Length);
        Assert.Equal(2, fired.Accounting.Splits);
        Assert.Equal(0, fired.Accounting.Deaths);
    }

    [Fact]
    public void A_node_already_in_the_chain_is_not_walked_again()
    {
        var node = new Node(C(1), Dials());
        node.Observe(C(2));
        node.Observe(C(3));
        var marginals = new Marginals().Set(C(2), 1).Set(C(3), 1);

        var visited = node.Fire(Arriving(C(1), before: C(2)), marginals);

        Assert.DoesNotContain(C(2), visited.Outgoing.Select(m => m.To));

        // THE COMPANION. Without it this passes whenever Fire emits nothing at
        // all, which is exactly how a disconnected mechanism looks correct.
        var fresh = node.Fire(Arriving(C(1), before: C(9)), marginals);
        Assert.Contains(C(2), fresh.Outgoing.Select(m => m.To));
    }

    [Fact]
    public void The_chain_grows_by_the_partner_that_was_reached()
    {
        var node = new Node(C(1), Dials());
        node.Observe(C(2));
        var marginals = new Marginals().Set(C(2), 1);

        var fired = node.Fire(Arriving(C(1), before: C(0)), marginals);

        Assert.Equal([C(0), C(1), C(2)], fired.Outgoing.Single().Chain.ToArray());
    }

    // ---- the connection test: perturb the input, assert the output moves ---

    [Fact]
    public void A_stronger_edge_carries_more()
    {
        var marginals = new Marginals().Set(C(2), 10);

        var weak = new Node(C(1), Dials());
        weak.Observe(C(2));

        var strong = new Node(C(1), Dials());
        for (var i = 0; i < 5; i++) strong.Observe(C(2));

        var weakly = weak.Fire(Arriving(C(1), before: C(0)), marginals).Outgoing.Single();
        var strongly = strong.Fire(Arriving(C(1), before: C(0)), marginals).Outgoing.Single();

        Assert.True(strongly.Carried > weakly.Carried,
            $"carried {strongly.Carried} should beat {weakly.Carried}");
    }

    /// <summary>
    /// The measured claim the whole weighting rests on, at the real
    /// proportions: a word on 845 occasions, each of its codes on 60, and a
    /// distractor present on all 3,845.
    /// </summary>
    [Fact]
    public void Forward_weighting_refuses_the_ever_present_distractor()
    {
        var code = C(2);
        var distractor = C(3);

        var word = new Node(C(1), Dials());
        for (var i = 0; i < 60; i++) word.Observe(code);
        for (var i = 0; i < 845; i++) word.Observe(distractor);

        var marginals = new Marginals().Set(code, 60).Set(distractor, 3845);

        var fired = word.Fire(Arriving(C(1), before: C(0)), marginals);
        var sent = fired.Outgoing.ToDictionary(m => m.To, m => m.Carried);

        // The distractor co-occurs fourteen times more often and still loses,
        // because it predicts nothing in particular. Every symmetrising rule
        // admits it — 0 of 24 refused it.
        Assert.True(sent[code] > sent[distractor],
            $"rare code {sent[code]} should beat distractor {sent[distractor]}");
    }

    // ---- pricing ----------------------------------------------------------

    [Fact]
    public void Best_pricing_never_lets_a_route_gain_budget()
    {
        var node = new Node(C(1), Dials(cost: StepCost.Best));
        node.Observe(C(2));
        for (var i = 0; i < 4; i++) node.Observe(C(3));
        var marginals = new Marginals().Set(C(2), 4).Set(C(3), 4);

        var arriving = Arriving(C(1), held: 10.0, before: C(0));
        var fired = node.Fire(arriving, marginals);

        Assert.NotEmpty(fired.Outgoing);
        Assert.All(fired.Outgoing, m => Assert.True(m.Held <= arriving.Held,
            $"budget rose from {arriving.Held} to {m.Held}"));
    }

    [Fact]
    public void Local_pricing_lets_an_above_mean_step_gain_budget()
    {
        // THE REFUTATION, RE-RUN. About half a node's edges are above its own
        // mean, so a route taking above-mean steps gains budget forever and
        // reaches everything, and a walk that reaches everything has answered
        // nothing. Kept as an arm because a refutation that cannot be re-run
        // is a claim.
        var node = new Node(C(1), Dials(cost: StepCost.Local));
        node.Observe(C(2));
        for (var i = 0; i < 4; i++) node.Observe(C(3));
        var marginals = new Marginals().Set(C(2), 4).Set(C(3), 4);

        var arriving = Arriving(C(1), held: 10.0, before: C(0));
        var fired = node.Fire(arriving, marginals);

        Assert.Contains(fired.Outgoing, m => m.Held > arriving.Held);
    }

    // ---- the horizon ------------------------------------------------------

    [Fact]
    public void A_route_that_has_walked_its_whole_horizon_stops_there()
    {
        var node = new Node(C(1), Dials(horizon: 3));
        node.Observe(C(2));
        var marginals = new Marginals().Set(C(2), 1);

        // EXACTLY at the horizon, not one past it. A ratio test on the cost
        // survives an off-by-one here; this does not, and an extra hop is worth
        // roughly sevenfold on a dense graph.
        var atLimit = node.Fire(Arriving(C(1), before: [C(8), C(9)]), marginals);

        Assert.Empty(atLimit.Outgoing);
        Assert.Equal(1, atLimit.Accounting.Halted);
        Assert.Equal(1, atLimit.Accounting.Deaths);
    }

    [Fact]
    public void A_route_one_short_of_the_horizon_takes_its_last_step()
    {
        // The companion. Without it the test above passes for a node that never
        // emits anything, and the horizon would look like it worked while
        // actually killing everything.
        var node = new Node(C(1), Dials(horizon: 3));
        node.Observe(C(2));
        var marginals = new Marginals().Set(C(2), 1);

        var oneShort = node.Fire(Arriving(C(1), before: C(9)), marginals);

        Assert.Single(oneShort.Outgoing);
        Assert.Equal(0, oneShort.Accounting.Halted);
        Assert.Equal(3, oneShort.Outgoing[0].Chain.Length);
    }

    [Fact]
    public void An_ordinary_death_is_not_a_halt()
    {
        // A route that ran out of anywhere to go and a route cut off by the
        // horizon are both deaths, and reporting them as the same thing would
        // hide the constant. Measured separately for exactly that reason.
        var node = new Node(C(1), Dials());

        var stranded = node.Fire(Arriving(C(1), before: C(9)), new Marginals());

        Assert.Equal(1, stranded.Accounting.Deaths);
        Assert.Equal(0, stranded.Accounting.Halted);
    }

    // ---- valuing ----------------------------------------------------------

    [Fact]
    public void Lift_divides_by_the_receivers_own_marginal()
    {
        var common = new Node(C(1), Dials(value: ArrivalValue.Lift));
        for (var i = 0; i < 4; i++) common.Note();

        var reached = common.Fire(Arriving(C(1), before: C(0)), new Marginals()).Reached;

        // Its own marginal, which it owns. PPMI's global occasion total is the
        // same for every candidate, so it cancels in a ranking and never has to
        // be known -- which is what makes this C1-legal where PPMI is not.
        Assert.Equal(0.25, reached!.Score);
    }

    [Fact]
    public void Strength_values_an_arrival_by_what_it_carried()
    {
        var common = new Node(C(1), Dials(value: ArrivalValue.Strength));
        for (var i = 0; i < 4; i++) common.Note();

        var reached = common.Fire(Arriving(C(1), before: C(0)), new Marginals()).Reached;

        Assert.Equal(1.0, reached!.Score);
    }

    [Fact]
    public void An_origin_has_not_arrived_anywhere()
    {
        var node = new Node(C(1), Dials());

        // A route that has not travelled reached nothing, and counting the
        // origin as an arrival would let every broadcast answer with itself.
        Assert.Null(node.Fire(Arriving(C(1)), new Marginals()).Reached);
    }

    // ---- refuelling -------------------------------------------------------

    [Fact]
    public void Surprise_pays_more_for_an_unlikely_edge_than_strength_does()
    {
        var marginals = new Marginals().Set(C(2), 100);

        Node Built(Refuel refuel)
        {
            var node = new Node(C(1), Dials(cost: StepCost.Constant, charge: 0.001, refuel: refuel));
            node.Observe(C(2));
            return node;
        }

        var arriving = Arriving(C(1), held: 1.0, before: C(0));
        var byStrength = Built(Refuel.Strength).Fire(arriving, marginals).Outgoing.Single();
        var bySurprise = Built(Refuel.Surprise).Fire(arriving, marginals).Outgoing.Single();

        // A weight of 0.01 pays 0.01 as strength and -log2(0.01) as surprise.
        Assert.True(bySurprise.Held > byStrength.Held);

        // And the SCORE is untouched by how the route was funded. The two are
        // separate quantities and this is the assertion that keeps them so.
        Assert.Equal(byStrength.Carried, bySurprise.Carried);
    }
}
