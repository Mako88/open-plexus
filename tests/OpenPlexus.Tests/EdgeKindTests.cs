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
    private static async Task<Bench> Moment(IReadOnlyDictionary<Code, int>? sequence)
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await new LocalRendezvous(bench.Local).JoinAsync(new Occasion
        {
            Onsets = [C(1), C(2)],
            Live = [],
            At = 7,
            Sequence = sequence,
        });

        return bench;
    }

    // ---- off changes nothing ----------------------------------------------

    // ---- THE TWO ARM TESTS THAT STOOD HERE ---------------------------------
    //
    // `With_kinds_off_a_carried_edge_lands_where_it_always_did` reproduced the
    // DEFECT -- a carried edge sharing the simultaneous cell -- and
    // `A_pair_met_both_ways_is_one_entry_off_and_two_on` compared one cell against
    // two. Step 6 became unconditional on 2026-08-04, so neither arm exists to
    // compare against and both tests are gone rather than rewritten.
    //
    // WHAT THEY ESTABLISHED, kept because the rest of this file rests on it: a
    // pair met simultaneously and then sequentially is ONE row entry when the
    // cells are shared and TWO when they are not, and sharing is what made a
    // deeper walk monotonically worse. The refutation table records the kinds as
    // the fix, and the remaining tests below assert what a temporal edge does now
    // that there is only one way for it to be written.


    // ---- what the kinds mean ----------------------------------------------

    [Fact]
    public async Task A_temporal_edge_is_still_written_one_way()
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));
        var rendezvous = new LocalRendezvous(bench.Local);

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
        // AN ORDER SAID INSIDE THE OCCASION IS NOT THE WINDOW, and the difference
        // survived the window becoming unconditional: this pair is sequential
        // because the FRONT END said so, not because one of them had stopped.
        var bench = await Moment(
            new Dictionary<Code, int> { [C(1)] = 0, [C(2)] = 1 });

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

        var asking = Fixture.Origin(C(1)) with { Through = Kind.After };

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

        var fired = node.Fire(Fixture.Origin(C(1)));

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

        var fired = node.Fire(Fixture.Origin(C(1)));

        // Two entries, and the second is refused because the chain already
        // carries C(2) after the first.
        Assert.Equal(2, node.Entries);
        Assert.Equal(2, fired.Outgoing.Length);
        Assert.All(fired.Outgoing, message => Assert.Equal(C(2), message.To));
    }

    // ---- a relation nobody compiled ---------------------------------------

    [Fact]
    public void A_relation_this_build_has_never_heard_of_is_a_cell_like_any_other()
    {
        // JOHN'S ASK, AND THE WHOLE OF IT. A static enum cannot grow, so every
        // relation the system could ever hold had to be decided by whoever last
        // compiled it -- which is the wrong place to decide what a front end is
        // allowed to say. `north-of` is not in this source anywhere.
        var north = Kind.Of("north-of");

        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));
        node.Note();
        node.Observe(C(2), 1.0, north);

        Assert.Equal(1.0, node.Together(C(2), north));

        // AND IT IS ITS OWN CELL rather than landing in the simultaneous one,
        // which is the property the whole of step 6 rests on.
        Assert.Equal(0.0, node.Together(C(2), Kind.With));
        Assert.Equal(1, node.Entered(north));
    }

    [Fact]
    public void And_a_question_can_be_narrowed_to_it()
    {
        // THE END-TO-END VERSION. Holding the cell is not the claim -- the claim is
        // that a walk can be restricted to a relation the build never knew about,
        // exactly as `Following()` restricts one to `After`. If this passes and the
        // one above fails there is a write path and no read path.
        var north = Kind.Of("north-of");

        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));
        node.Note();
        node.Observe(C(2), 1.0, Kind.With);
        node.Observe(C(3), 1.0, north);

        var fired = node.Fire(Fixture.Origin(C(1)) with { Through = north });

        Assert.Equal([C(3)], fired.Outgoing.Select(message => message.To));
        Assert.Equal(north, fired.Outgoing[0].Kind);
    }

    [Fact]
    public void The_five_built_in_relations_are_ordinary_calls_to_the_same_door()
    {
        // THE TRAP THIS CLOSES, AND IT IS A REAL ONE. If the built-in relations were
        // reserved numbers and minted ones were hashes, a front end naming its
        // relation "with" would get a DIFFERENT cell from the one every write in
        // this project already lands in -- two statistics under one word, which is
        // this design's recurring fault arriving by a new road. One rule instead:
        // a relation's identity is the hash of its name, always.
        Assert.Equal(Kind.With, Kind.Of("with"));
        Assert.Equal(Kind.After, Kind.Of("after"));
        Assert.Equal(Kind.Before, Kind.Of("before"));
        Assert.Equal(Kind.Helped, Kind.Of("helped"));
        Assert.Equal(Kind.Hindered, Kind.Of("hindered"));

        // AND DISTINCT NAMES ARE DISTINCT CELLS, without which the above is
        // satisfied by every name hashing to the same number.
        Assert.NotEqual(Kind.With, Kind.After);
        Assert.NotEqual(Kind.Of("north-of"), Kind.Of("south-of"));
    }

    [Fact]
    public void And_the_number_a_name_derives_to_is_fixed_forever()
    {
        // THE RED-BALL PROPERTY, APPLIED TO RELATIONS. Two machines must agree on
        // what `north-of` means with nothing to ask, so the arithmetic behind the
        // name is not free to move -- changing `Agreed` would silently renumber
        // every relation every machine has ever written, which is the same reason
        // `Kinds.Stride` is fixed and the same reason a fitted codebook is out.
        //
        // PINNED THROUGH `ToString` ON PURPOSE. The number is not public and should
        // not be: it is an identity, not a value anything reads.
        Assert.Equal("kind:5dc8b6d758901f40", Kind.Of("north-of").ToString());

        // AND THE FIVE SPELL THEMSELVES BACK OUT, so an assertion that fails prints
        // something a person can act on.
        Assert.Equal("after", Kind.After.ToString());
    }
}
