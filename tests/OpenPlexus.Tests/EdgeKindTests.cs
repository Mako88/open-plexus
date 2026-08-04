using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// A row entry is <c>(partner, kind)</c> and not <c>partner</c> — <b>step 6, and
/// the walk can finally tell <i>follows</i> from <i>accompanies</i>.</b>
/// </summary>
/// <remarks>
/// <b>THE ARM IS THE FIRST THING UNDER TEST.</b> Splitting a carried edge out of
/// the simultaneous cell moves counts that were already measured, so the claim
/// that matters most is the one that says nothing moves while it is off.
/// </remarks>
public sealed class EdgeKindTests
{
    private static Code C(ulong value) => Fixture.C(value);

    /// <summary>
    /// One moment holding <c>C(1)</c> and <c>C(2)</c>, with whatever the front end
    /// could say about their order.
    /// </summary>
    private static async Task<Bench> Moment(
        IReadOnlyDictionary<Code, int>? sequence, bool kinds = false)
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await new LocalRendezvous(bench.Local, kinds).JoinAsync(new Occasion
        {
            Onsets = [C(1), C(2)],
            Live = [],
            At = 7,
            Sequence = sequence,
        });

        return bench;
    }

    /// <summary>A broadcast standing on its first node, with budget to spend.</summary>
    private static Message Origin(Code code) => new()
    {
        Broadcast = BroadcastId.New(),
        ReturnTo = new MachineAddress("test"),
        To = code,
        Held = 10.0,
        Chain = [code],
        Carried = 1.0,
    };

    // ---- off changes nothing ----------------------------------------------

    [Fact]
    public async Task With_kinds_off_a_carried_edge_lands_where_it_always_did()
    {
        // EVERY MEASUREMENT TAKEN UP TO NOW WAS TAKEN THIS WAY. The window's
        // carried edge is added to the same cell as a simultaneous one, which is
        // the defect -- and reproducing it exactly is what keeps the old numbers
        // comparable with the new ones.
        var bench = new Bench(Fixture.Dials(stamina: 10.0));
        var rendezvous = new LocalRendezvous(bench.Local, kinds: false);

        await rendezvous.JoinAsync(new Occasion
        {
            Onsets = [C(9)], Live = [], Recent = [C(1)], At = 5,
        });

        Assert.Equal(1.0, bench.Node(C(1)).Together(C(9), Kind.With));
        Assert.Equal(0.0, bench.Node(C(1)).Together(C(9), Kind.After));
    }

    [Fact]
    public async Task A_pair_met_both_ways_is_one_entry_off_and_two_on()
    {
        // THE PRICE, IN THE ONE UNIT THAT MATTERS. Memory per edge is the
        // scaling wall and cost per thought is set by the widest row, so the
        // split has to be paid for in entries and is worth saying out loud.
        var off = new Bench(Fixture.Dials(stamina: 10.0));
        var on = new Bench(Fixture.Dials(stamina: 10.0));

        foreach (var (bench, kinds) in new[] { (off, false), (on, true) })
        {
            var rendezvous = new LocalRendezvous(bench.Local, kinds);

            // Met alongside, then met again as a predecessor.
            await rendezvous.JoinAsync(new Occasion
            {
                Onsets = [C(1), C(9)], Live = [], At = 1,
            });

            await rendezvous.JoinAsync(new Occasion
            {
                Onsets = [C(9)], Live = [], Recent = [C(1)], At = 2,
            });
        }

        Assert.Equal(1, off.Node(C(1)).Entries);
        Assert.Equal(2, on.Node(C(1)).Entries);

        // AND THE PAIR TOTALS THE SAME EITHER WAY, which is what says the split
        // moved the counts apart rather than losing or inventing one.
        Assert.Equal(2.0, off.Node(C(1)).Together(C(9)));
        Assert.Equal(2.0, on.Node(C(1)).Together(C(9)));

        // Separated: once alongside, once after.
        Assert.Equal(1.0, on.Node(C(1)).Together(C(9), Kind.With));
        Assert.Equal(1.0, on.Node(C(1)).Together(C(9), Kind.After));
    }

    // ---- what the kinds mean ----------------------------------------------

    [Fact]
    public async Task A_temporal_edge_is_still_written_one_way()
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));
        var rendezvous = new LocalRendezvous(bench.Local, kinds: true);

        await rendezvous.JoinAsync(new Occasion
        {
            Onsets = [C(9)], Live = [], Recent = [C(1)], At = 5,
        });

        // The past records the future; the future records nothing about the past.
        Assert.Equal(1.0, bench.Node(C(1)).Together(C(9), Kind.After));
        Assert.Equal(0.0, bench.Node(C(9)).Together(C(1), Kind.After));
        Assert.Equal(0.0, bench.Node(C(9)).Together(C(1), Kind.With));
    }

    [Fact]
    public async Task An_order_said_inside_the_occasion_makes_a_sequential_pair()
    {
        // THE GROUPS TRICK AGAIN: the front end says the order where lateness
        // cannot touch it, because a phase cannot survive C2.
        var bench = await Moment(new Dictionary<Code, int> { [C(1)] = 0, [C(2)] = 1 });

        Assert.Equal(1.0, bench.Node(C(1)).Together(C(2), Kind.After));
        Assert.Equal(0.0, bench.Node(C(2)).Together(C(1), Kind.After));

        // AND NOT SIMULTANEOUSLY, which is the point: one moment, but an order
        // inside it.
        Assert.Equal(0.0, bench.Node(C(1)).Together(C(2), Kind.With));
    }

    [Fact]
    public async Task A_sequence_needs_no_arm_because_no_history_carries_one()
    {
        // The asymmetry with the window, and the reason only one of them is
        // behind a flag: an occasion that says nothing about order produces
        // exactly the graph it always did.
        var bench = await Moment(
            new Dictionary<Code, int> { [C(1)] = 0, [C(2)] = 1 }, kinds: false);

        Assert.Equal(1.0, bench.Node(C(1)).Together(C(2), Kind.After));
    }

    [Fact]
    public async Task An_unranked_code_stays_simultaneous_with_everything()
    {
        // Additive, exactly as an ungrouped code pairs with everything: a front
        // end that can sequence some of what it emits is not forced to lie about
        // the rest.
        var bench = await Moment(new Dictionary<Code, int> { [C(1)] = 0 });

        Assert.Equal(1.0, bench.Node(C(1)).Together(C(2), Kind.With));
        Assert.Equal(1.0, bench.Node(C(2)).Together(C(1), Kind.With));
    }

    // ---- the supersession channel -----------------------------------------

    [Fact]
    public async Task A_cell_remembers_when_it_was_last_touched()
    {
        // THE SECOND CHANNEL, riding beside the count and never merged with it.
        // Nothing ranks by it yet; it is here because the row is widened once.
        var bench = new Bench(Fixture.Dials(stamina: 10.0));
        var rendezvous = new LocalRendezvous(bench.Local);

        await rendezvous.JoinAsync(new Occasion { Onsets = [C(1), C(2)], Live = [], At = 10 });
        await rendezvous.JoinAsync(new Occasion { Onsets = [C(1), C(2)], Live = [], At = 40 });

        // The count is a G-Counter and only ever climbs.
        Assert.Equal(2.0, bench.Node(C(1)).Together(C(2), Kind.With));

        // The clock is an LWW-Register and takes the later stamp.
        Assert.Equal(40, bench.Node(C(1)).When(C(2)));
    }

    [Fact]
    public void A_late_observation_cannot_drag_the_clock_backwards()
    {
        // C2 SAYS MESSAGES ARE OUT OF ORDER, so last-write-wins has to mean the
        // latest STAMP and not the last arrival -- otherwise the register
        // converges on whichever write happened to lose the race.
        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));

        node.Observe(C(2), 1.0, Kind.With, when: 90);
        node.Observe(C(2), 1.0, Kind.With, when: 20);

        Assert.Equal(90, node.When(C(2)));

        // AND THE COUNT TOOK BOTH, because the two channels do not merge. An
        // LWW join over the count would have discarded one of these.
        Assert.Equal(2.0, node.Together(C(2), Kind.With));
    }

    // ---- what the walk does with it ---------------------------------------

    [Fact]
    public void A_question_that_asks_what_follows_does_not_step_through_accompaniment()
    {
        // THE REVIVAL CONDITION, IN ONE ASSERTION. A deeper walk for prediction
        // was monotonically worse because every extra hop reached more things
        // that merely co-occurred and ranked them against the thing that came
        // next. This is what the row could not say before.
        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));
        node.Note();
        node.Observe(C(2), 1.0, Kind.With);
        node.Observe(C(3), 1.0, Kind.After);

        var asking = Origin(C(1)) with { Through = Kind.After };

        var fired = node.Fire(asking);

        Assert.Equal([C(3)], fired.Outgoing.Select(message => message.To));
        Assert.Equal(Kind.After, fired.Outgoing[0].Kind);
    }

    [Fact]
    public void A_question_that_says_nothing_walks_everything()
    {
        // Null is every question asked before kinds existed, and it has to stay
        // that way or every earlier measurement is measuring a filter nobody set.
        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));
        node.Note();
        node.Observe(C(2), 1.0, Kind.With);
        node.Observe(C(3), 1.0, Kind.After);

        var fired = node.Fire(Origin(C(1)));

        Assert.Equal(2, fired.Outgoing.Length);
    }

    [Fact]
    public void One_partner_met_two_ways_is_not_walked_to_twice()
    {
        // THE CYCLE CHECK IS ON THE PARTNER AND NOT ON THE ENTRY. A route that
        // reached B by accompaniment must not reach it again by sequence, or one
        // node appears twice in a chain that exists to say where the route went.
        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));
        node.Note();
        node.Observe(C(2), 1.0, Kind.With);
        node.Observe(C(2), 1.0, Kind.After);

        var fired = node.Fire(Origin(C(1)));

        // Two entries, and the second is refused because the chain already
        // carries C(2) after the first.
        Assert.Equal(2, node.Entries);
        Assert.Equal(2, fired.Outgoing.Length);
        Assert.All(fired.Outgoing, message => Assert.Equal(C(2), message.To));
    }
}
