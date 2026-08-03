using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// Fork 2: the receiver weighs the edge it arrived on.
/// </summary>
/// <remarks>
/// The sender owns <c>together(me, you)</c> and puts it in the message; the
/// receiver divides by its own marginal. <b>Neither node reads the other's
/// data</b>, so nothing is fetched, gossiped or cached — which is what makes it
/// C1-legal by construction rather than by argument.
/// </remarks>
public sealed class ReceiverWeighingTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    private static WalkSettings Dials(double stamina = 10.0) => new()
    {
        Stamina = stamina,
        Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum,
        Horizon = 50,
    };

    private static Message Arriving(Code to, double together, double held = 10.0) => new()
    {
        Broadcast = BroadcastId.New(),
        ReturnTo = new MachineAddress("test"),
        To = to,
        Held = held,
        Chain = [C(9), to],
        Carried = 1.0,
        Together = together,
    };

    [Fact]
    public void A_node_needs_nothing_but_its_own_row_to_fire()
    {
        // THE WHOLE CLAIM, and it is now structural rather than asserted: there
        // is no way to hand a node another node's data, because `Fire` takes
        // only the message. `IMarginals` is gone with the sender arm.
        var node = new Node(C(1), Dials());
        for (var i = 0; i < 4; i++) node.Note();
        node.Observe(C(2));

        Assert.Single(node.Fire(Arriving(C(1), together: 4.0)).Outgoing);
    }

    [Fact]
    public void The_edge_is_weighed_from_the_senders_count_and_the_receivers_marginal()
    {
        // together 4 against a marginal of 4 is a weight of 1.0, costing 1.
        var strong = new Node(C(1), Dials());
        for (var i = 0; i < 4; i++) strong.Note();
        strong.Observe(C(2));

        // together 1 against the same marginal is 0.25, costing 4.
        var weak = new Node(C(1), Dials());
        for (var i = 0; i < 4; i++) weak.Note();
        weak.Observe(C(2));

        var afterStrong = strong.Fire(Arriving(C(1), together: 4.0));
        var afterWeak = weak.Fire(Arriving(C(1), together: 1.0));

        Assert.Equal(9.0, afterStrong.Outgoing.Single().Held, precision: 10);
        Assert.Equal(6.0, afterWeak.Outgoing.Single().Held, precision: 10);
    }

    [Fact]
    public void A_route_that_cannot_pay_for_its_own_arrival_dies_here()
    {
        var node = new Node(C(1), Dials());
        for (var i = 0; i < 100; i++) node.Note();
        node.Observe(C(2));

        // together 1 against a marginal of 100 is 0.01, costing 100.
        var fired = node.Fire(Arriving(C(1), together: 1.0, held: 10.0));

        Assert.Empty(fired.Outgoing);
        Assert.Equal(1, fired.Accounting.Deaths);
        Assert.Null(fired.Reached);
    }

    [Fact]
    public void A_route_that_can_pay_arrives_and_carries_the_edge_strength()
    {
        // The companion. Without it the test above passes for a node that
        // never lets anything through.
        var node = new Node(C(1), Dials());
        for (var i = 0; i < 4; i++) node.Note();
        node.Observe(C(2));

        var fired = node.Fire(Arriving(C(1), together: 2.0));

        Assert.NotNull(fired.Reached);
        Assert.Equal(0.5, fired.Reached.Score, precision: 10);
    }

    [Fact]
    public void A_sender_passes_its_own_count_for_each_partner()
    {
        var node = new Node(C(1), Dials());
        for (var i = 0; i < 4; i++) node.Note();
        for (var i = 0; i < 3; i++) node.Observe(C(2));
        node.Observe(C(3));

        var sent = node.Fire(Arriving(C(1), together: 4.0))
            .Outgoing.ToDictionary(m => m.To, m => m.Together);

        // Read from the sender's own row, which is the half it owns.
        Assert.Equal(3.0, sent[C(2)]);
        Assert.Equal(1.0, sent[C(3)]);
    }

    [Fact]
    public void The_walk_stays_bounded_on_the_receiver_arm_too()
    {
        // The bound has to survive the move, or fork 2 would have undone
        // fork 14. A clique where every weight is exactly 1.0 is the worst
        // case, and the budget still buys exactly its own number of hops.
        var dials = Dials(stamina: 4.0);
        var codes = Enumerable.Range(0, 12).Select(i => C((ulong)i)).ToArray();
        var nodes = codes.ToDictionary(c => c, c => new Node(c, dials));

        foreach (var code in codes)
        {
            for (var k = 0; k < 10; k++) nodes[code].Note();
            foreach (var other in codes)
                if (other != code)
                    for (var k = 0; k < 10; k++) nodes[code].Observe(other);
        }

        var frontier = new List<Message>
        {
            new()
            {
                Broadcast = BroadcastId.New(), ReturnTo = new MachineAddress("t"),
                To = codes[0], Held = 4.0, Chain = [codes[0]], Carried = 1.0,
            },
        };

        long messages = 0;
        var depth = 0;
        while (frontier.Count > 0 && messages < 5_000_000)
        {
            depth++;
            var next = new List<Message>();
            foreach (var message in frontier)
                foreach (var onward in nodes[message.To].Fire(message).Outgoing)
                {
                    next.Add(onward);
                    messages++;
                }

            frontier = next;
        }

        Assert.True(depth <= 5, $"reached depth {depth}");
        Assert.True(messages < 5_000, $"{messages} messages on a 12-clique");
    }

    [Fact]
    public void A_budget_that_cannot_afford_a_perfect_hop_refuses_the_whole_fan_out()
    {
        // The one prune a sender can still do, and it needs nothing from
        // anyone: a weight cannot exceed 1.0, so no hop costs less than 1.
        var node = new Node(C(1), Dials());
        for (var i = 0; i < 4; i++) node.Note();
        node.Observe(C(2));
        node.Observe(C(3));

        var broke = node.Fire(Arriving(C(1), together: 4.0, held: 1.5));
        Assert.Empty(broke.Outgoing);

        // The companion: a budget that CAN afford one still fans out.
        var solvent = node.Fire(Arriving(C(1), together: 4.0, held: 3.0));
        Assert.Equal(2, solvent.Outgoing.Length);
    }
}
