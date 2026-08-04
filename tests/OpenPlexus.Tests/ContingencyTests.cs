using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Thinking;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// <c>P(better | act) − P(better | ¬act)</c> — <b>the base rate a hit rate cannot
/// see, and the last direction left that is not about reaching.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>EVERYTHING TRIED SO FAR CHANGED THE READ SIDE.</b> Paths, backoff, curiosity,
/// the trace, grains, the toll, count-based exploration: every one of them asked a
/// different question of the same cells, and every one scored worse than random.
/// <b>This changes WHAT IS WRITTEN</b> — a second tally on the node, split by
/// relation — and it is the only remaining direction that is not about the walk
/// reaching further.
/// </para>
/// <para>
/// <b>THE AUDIT IS AT THE UNIT LEVEL, WHICH IS WHERE AN ARITHMETIC CLAIM
/// BELONGS.</b> `SignalTests` gates a signal on two policies because a world is the
/// only thing that can say whether a run-level number discriminates. The claim here
/// is not about a run: it is that a hit rate and a contingency come apart, and a
/// constructed pair shows that completely where a world would show one sample of
/// it. <see cref="Learning.Surprise.Overreach"/> is audited the same way and for
/// the same reason.
/// </para>
/// <para>
/// <b>NOTHING DRIVES FROM THIS YET, AND THAT ORDER IS DELIBERATE.</b> Three
/// controllers in this project were built on signals nobody had audited and all
/// three failed for want of the audit rather than for want of the controller.
/// </para>
/// </remarks>
public sealed class ContingencyTests(ITestOutputHelper output)
{
    private static Code C(ulong value) => Fixture.C(value);

    private static readonly Code Act = C(2);

    /// <summary>A state node that occurred so often and improved so often.</summary>
    private static Node State(double occasions, double improved)
    {
        var node = new Node(C(1), Fixture.Dials());

        node.Note(occasions, Kind.With);
        if (improved > 0.0) node.Note(improved, Kind.Helped);

        return node;
    }

    /// <summary>An act taken from that state so often, and helping so often.</summary>
    private static void Taking(Node node, double taken, double helped)
    {
        node.Observe(Act, taken, Kind.With);
        if (helped > 0.0) node.Observe(Act, helped, Kind.Helped);
    }

    [Fact]
    public void The_same_hit_rate_is_worth_opposite_things_against_two_backgrounds()
    {
        // THE CLAIM IN ITS PUREST FORM, and everything else here is a corollary.
        // Both acts helped on twelve of the twenty occasions they were taken --
        // identical evidence, identical cell, identical `helped / taken` of 0.60.
        // In a world where things improve half the time that act is doing real
        // work; in one where they improve four times in five it is doing WORSE
        // than standing aside. A count of co-occurrence holds the same number for
        // both and has no way to tell them apart.
        var scarce = State(occasions: 100, improved: 50);
        var common = State(occasions: 100, improved: 80);

        Taking(scarce, taken: 20, helped: 12);
        Taking(common, taken: 20, helped: 12);

        var rate = scarce.Together(Act, Kind.Helped) / scarce.Together(Act, Kind.With);

        output.WriteLine($"hit rate  {rate:F4} on both");
        output.WriteLine($"scarce    {scarce.Contingency(Act):+0.0000;-0.0000}");
        output.WriteLine($"common    {common.Contingency(Act):+0.0000;-0.0000}");

        // THE HIT RATE REALLY IS IDENTICAL, or the two contingencies below are
        // separating something other than the background and this test is telling
        // a story about arithmetic it did not do.
        Assert.Equal(
            rate,
            common.Together(Act, Kind.Helped) / common.Together(Act, Kind.With));

        Assert.True(scarce.Contingency(Act) > 0.0,
            $"an act helping more often than the background reads {scarce.Contingency(Act):F4}");

        Assert.True(common.Contingency(Act) < 0.0,
            $"an act helping less often than the background reads {common.Contingency(Act):F4}");
    }

    [Fact]
    public void An_act_that_helps_exactly_as_often_as_doing_anything_else_reads_nought()
    {
        // SIX TIMES IN TEN LOOKS LIKE A GOOD ACT AND IS INDISTINGUISHABLE FROM
        // NOTHING. Things improved on sixty of the hundred occasions this state was
        // in, and on thirty of the fifty where this act was taken -- so the act
        // carries no information whatever about what happens next, and the cell
        // that says it was taken and helped thirty times says so at full volume.
        var node = State(occasions: 100, improved: 60);
        Taking(node, taken: 50, helped: 30);

        Assert.Equal(0.6, node.Together(Act, Kind.Helped) / node.Together(Act, Kind.With));
        Assert.Equal(0.0, node.Contingency(Act), precision: 10);
    }

    [Fact]
    public void And_one_that_helps_less_than_the_background_reads_negative()
    {
        // INHIBITION, ARRIVED AT BY SUBTRACTION RATHER THAN BY A SECOND CELL.
        // `Kind.Hindered` needs the world to say an act made things WORSE; this
        // needs nobody to say anything -- an act simply captures less of the
        // improvement than its share of the occasions, and the difference is
        // negative. Nothing in this design could say that from a one-sided count.
        var node = State(occasions: 100, improved: 60);
        Taking(node, taken: 40, helped: 16);

        output.WriteLine($"hit {node.Together(Act, Kind.Helped) / node.Together(Act, Kind.With):F4}");
        output.WriteLine($"ΔP  {node.Contingency(Act):+0.0000;-0.0000}");

        Assert.True(node.Contingency(Act) < -0.3);
    }

    [Fact]
    public void Every_part_only_ever_rises_and_the_difference_still_falls()
    {
        // THE PN-COUNTER ARGUMENT, ONE MORE TIME. This is the property the whole
        // coordination-free design rests on, and a contingency looks at first like
        // the thing that breaks it -- a number that goes DOWN as evidence arrives.
        // It does not: all four tallies are monotonic and only their difference
        // moves either way, exactly as `Kind.Hindered` is two G-Counters read as a
        // subtraction.
        var node = State(occasions: 100, improved: 50);
        Taking(node, taken: 20, helped: 12);

        var before = node.Contingency(Act);

        // MORE EXPERIENCE, ALL OF IT ADDITIVE: the state occurred thirty more
        // times and improved on twenty-five of them, and this act was taken on
        // none of them.
        node.Note(30.0, Kind.With);
        node.Note(25.0, Kind.Helped);

        var after = node.Contingency(Act);

        output.WriteLine($"{before:+0.0000;-0.0000} -> {after:+0.0000;-0.0000}");

        Assert.True(after < before,
            "the background rose and the act's share of it did not, so the "
            + "contingency has to fall");

        // AND NOTHING WAS TAKEN AWAY TO MAKE IT FALL.
        Assert.Equal(130.0, node.Noted(Kind.With));
        Assert.Equal(75.0, node.Noted(Kind.Helped));
    }

    [Fact]
    public void A_nought_can_mean_nothing_to_say_rather_than_no_effect()
    {
        // THE TRAP, NAMED WHERE IT LIVES. Two quite different situations read
        // exactly nought, and one of them is an act that worked EVERY SINGLE TIME.
        // Anything driving from this number has to report how often it was silent
        // beside whatever it scored, or a run of empty denominators reads as a run
        // of acts that made no difference -- which is the fallback-as-control-arm
        // trap wearing a new hat.
        var never = State(occasions: 100, improved: 50);

        Assert.Equal(0.0, never.Contingency(Act));

        var always = State(occasions: 20, improved: 20);
        Taking(always, taken: 20, helped: 20);

        // Taken on every occasion there was, and it helped on every one of them.
        // There is no ¬act to compare against and the reading is nought.
        Assert.Equal(1.0, always.Together(Act, Kind.Helped) / always.Together(Act, Kind.With));
        Assert.Equal(0.0, always.Contingency(Act));
    }

    [Fact]
    public void The_split_marginal_does_not_move_the_one_every_weight_divides_by()
    {
        // THE COMPATIBILITY CLAIM, AND IT IS THE ONE THAT WOULD SILENTLY INVALIDATE
        // EVERY EARLIER MEASUREMENT. `Seen` is the denominator of every edge weight
        // in the project. The split rides beside it and adds to it exactly as
        // before, so an arm measured last week is measuring the same graph.
        var node = new Node(C(1), Fixture.Dials());

        node.Note(5.0, Kind.With);
        node.Note(3.0, Kind.Helped);
        node.Note(2.0);

        Assert.Equal(10.0, node.Seen);
        Assert.Equal(5.0, node.Noted(Kind.With));
        Assert.Equal(3.0, node.Noted(Kind.Helped));

        // A CALLER WITH NOTHING TO SAY LEAVES THE SPLIT ALONE, so the totals do not
        // have to agree -- and `Contingency` reading nought off a node nobody split
        // is the honest answer rather than a bug.
        Assert.Equal(0.0, node.Noted(Kind.Of("unmentioned")));
    }

    [Fact]
    public async Task And_the_real_write_path_records_it()
    {
        // WIRED, NOT MERELY EXPRESSIBLE. Every claim above is arithmetic on a node
        // built by hand; this is the check that an ordinary join and a reinforce
        // land in different halves of the split, which is the only reason the
        // arithmetic ever sees two different numbers.
        using var bench = new Bench(Fixture.Dials());

        var occasion = new Occasion { Onsets = [C(1), Act], Live = [], At = 1 };

        await bench.Rendezvous.JoinAsync(occasion);

        Assert.Equal(1.0, bench.Node(C(1)).Noted(Kind.With));
        Assert.Equal(0.0, bench.Node(C(1)).Noted(Kind.Helped));

        await bench.Rendezvous.JoinAsync(occasion with { As = Kind.Helped, At = 2 });

        Assert.Equal(1.0, bench.Node(C(1)).Noted(Kind.With));
        Assert.Equal(1.0, bench.Node(C(1)).Noted(Kind.Helped));

        // AND `Seen` COUNTED BOTH, which is what it did before this existed.
        Assert.Equal(2.0, bench.Node(C(1)).Seen);
    }

    // ---- what the walk does with it ---------------------------------------

    /// <summary>
    /// Two acts whose hit rates rank them one way and whose contingencies rank
    /// them the other.
    /// </summary>
    /// <remarks>
    /// The rider is taken rarely and helps often — <b>and improvement is common
    /// enough that it would have happened anyway.</b> The worker is taken far more
    /// and helps a smaller share of the time, but nearly all the improvement there
    /// ever was happened under it.
    /// </remarks>
    private static Node Pair()
    {
        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));

        node.Note(100.0, Kind.With);
        node.Note(50.0, Kind.Helped);

        // Rider: taken 10, helped 7 -- hit rate 0.70.
        node.Observe(C(2), 10.0, Kind.With);
        node.Observe(C(2), 7.0, Kind.Helped);

        // Worker: taken 60, helped 36 -- hit rate 0.60.
        node.Observe(C(3), 60.0, Kind.With);
        node.Observe(C(3), 36.0, Kind.Helped);

        return node;
    }

    [Fact]
    public void The_sender_works_out_the_contrast_because_only_it_has_the_base_rate()
    {
        var node = Pair();

        var fired = node.Fire(Fixture.Origin(C(1)) with { Through = Kind.Helped, Contrasted = true });

        var contrast = fired.Outgoing.ToDictionary(one => one.To, one => one.Contrast);

        foreach (var (act, value) in contrast)
            output.WriteLine($"{act.Value}  ΔP {value:+0.0000;-0.0000}");

        // BOTH ARE POSITIVE -- both acts really do beat standing aside -- SO THIS IS
        // A CLAIM ABOUT ORDER AND NOT ABOUT SIGN.
        Assert.True(contrast[C(2)] > 0.0);
        Assert.True(contrast[C(3)] > contrast[C(2)],
            $"the act that captured nearly all the improvement reads "
            + $"{contrast[C(3)]:F4} against the rider's {contrast[C(2)]:F4}, so the "
            + "contrast is not seeing the background");

        // AND THE HIT RATE RANKS THEM THE OTHER WAY, which is the whole point --
        // without this the two orderings might agree and prove nothing.
        Assert.True(
            node.Together(C(2), Kind.Helped) / node.Together(C(2), Kind.With)
            > node.Together(C(3), Kind.Helped) / node.Together(C(3), Kind.With));
    }

    [Fact]
    public void And_a_question_that_does_not_ask_for_it_carries_nought()
    {
        // `Worthwhile` IS THIS ARM'S CONTROL, so it has to be untouched. A walk
        // that never asked for a contrast must not pay for one or be ranked by one,
        // or every number `Credited` ever produced is measuring something else.
        var fired = Pair().Fire(Fixture.Origin(C(1)) with { Through = Kind.Helped });

        Assert.NotEmpty(fired.Outgoing);
        Assert.All(fired.Outgoing, one => Assert.Equal(0.0, one.Contrast));
        Assert.All(fired.Outgoing, one => Assert.False(one.Contrasted));
    }

    /// <summary>
    /// A node with one partner onward, and a message already arriving at it.
    /// </summary>
    /// <remarks>
    /// <b>An ORIGIN is not weighed at all</b> — nothing arrived, so there is no
    /// edge to value — and everything below is about what the receiver does with
    /// the edge it came in on. So the chain has two codes in it.
    /// </remarks>
    private static (Node Node, Message Arriving) Receiving()
    {
        var node = new Node(C(2), Fixture.Dials(stamina: 10.0));
        node.Note(4.0, Kind.With);
        node.Observe(C(5), 1.0, Kind.With);

        return (node, Fixture.Origin(C(2)) with
        {
            Chain = [C(1), C(2)],
            Together = 4.0,
        });
    }

    [Fact]
    public void The_receiver_believes_it_less_and_is_never_charged_more_to_reach_it()
    {
        // THE RECURRING FAULT, CHECKED RATHER THAN ASSUMED. It has bitten four
        // times: a number that ranks a partner AND prices the hop to it means every
        // discount also starves the route, the walk falls quiet, and the change
        // reads as harmful when it merely made everything unreachable. `Doubt` and
        // `Kind.Hindered` are both on the score side of that line; so is this, and
        // the assertion is that the budget left is IDENTICAL.
        var (node, arriving) = Receiving();

        var plain = node.Fire(arriving);
        var halved = node.Fire(arriving with { Contrasted = true, Contrast = 0.5 });

        output.WriteLine($"score {plain.Reached!.Score:F4} -> {halved.Reached!.Score:F4}");
        output.WriteLine($"held  {plain.Outgoing[0].Held:F4} -> {halved.Outgoing[0].Held:F4}");

        Assert.Equal(plain.Reached!.Score * 0.5, halved.Reached!.Score, precision: 10);
        Assert.Equal(plain.Outgoing[0].Held, halved.Outgoing[0].Held, precision: 10);
    }

    [Fact]
    public void An_act_no_better_than_standing_aside_is_believed_nothing()
    {
        // THE INHIBITION, AND THE CLAMP THAT MAKES IT SAFE. A negative score would
        // not lower a ranking, it would INVERT it -- the more strongly an act is
        // contra-indicated the more negative it gets, and a sort would put the
        // worst act first. So it is clamped at nought, which is the same clamp and
        // the same reason as `Kind.Hindered`'s.
        var (node, plain) = Receiving();

        var arriving = plain with { Contrasted = true };

        Assert.Equal(0.0, node.Fire(arriving with { Contrast = 0.0 }).Reached!.Score);
        Assert.Equal(0.0, node.Fire(arriving with { Contrast = -0.9 }).Reached!.Score);
    }
}
