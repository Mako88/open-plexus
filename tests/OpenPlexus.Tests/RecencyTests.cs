using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Preferring what is still true — <b>supersession's second consumer, and the only
/// answer this design has to a world that changes.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>NOTHING HERE DECAYS, SO A SUPERSEDED FACT IS AS LOUD AS A CURRENT ONE.</b> A
/// count that stopped rising still stands at whatever it reached. Eroding it is not
/// available — that is the convergence the whole coordination-free design rests on
/// — so the only move left is to let a QUESTION say it cares when.
/// </para>
/// <para>
/// <b>THE HARD PART IS THE SCALE, AND IT IS WHY THIS IS NOT A DIAL.</b> An age in
/// raw clock units means nothing without knowing what a unit is, so any threshold
/// over it wants a different value in every world — this design's recurring fault.
/// The row's OWN mean interval between writes removes the unit: a node that fires
/// constantly and one that fires rarely both read "one interval ago" alike.
/// </para>
/// </remarks>
public sealed class RecencyTests(ITestOutputHelper output)
{
    private static Code C(ulong value) => Fixture.C(value);

    /// <summary>A node whose partners were each met once, at the stamps given.</summary>
    private static Node Written(params long[] stamps)
    {
        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));

        node.Note(stamps.Length);

        for (var which = 0; which < stamps.Length; which++)
            node.Observe(C((ulong)which + 2), when: stamps[which]);

        return node;
    }

    private static IReadOnlyDictionary<Code, double> Freshness(Node node, bool recent)
    {
        var fired = node.Fire(Fixture.Origin(C(1)) with { Recent = recent });

        return fired.Outgoing.ToDictionary(one => one.To, one => one.Fresh);
    }

    [Fact]
    public void A_question_that_says_nothing_is_every_walk_taken_before_this_existed()
    {
        // THE ARM'S OFF POSITION. `Fresh` is not computed and the score is not
        // touched, so every measurement this project has stands unchanged.
        var node = Written(10, 20, 30);

        var fired = node.Fire(Fixture.Origin(C(1)));

        Assert.All(fired.Outgoing, one => Assert.False(one.Recent));
        Assert.All(fired.Outgoing, one => Assert.Equal(0.0, one.Fresh));
    }

    [Fact]
    public void The_newest_entry_is_wholly_believed_and_older_ones_less()
    {
        var node = Written(10, 20, 30);

        var fresh = Freshness(node, recent: true);

        foreach (var (code, value) in fresh.OrderBy(one => one.Key))
            output.WriteLine($"{code.Value} {value:F4}");

        Assert.Equal(1.0, fresh[C(4)], precision: 10);
        Assert.True(fresh[C(3)] < fresh[C(4)]);
        Assert.True(fresh[C(2)] < fresh[C(3)]);

        // AND NOTHING IS BELIEVED NOUGHT. A row is a record of what this node has
        // met and the oldest entry in it is still evidence -- the walk should
        // prefer the current one, not refuse the rest, which is what separates
        // this from eviction.
        Assert.True(fresh[C(2)] > 0.0);
    }

    [Fact]
    public void A_row_written_all_at_once_is_all_equally_current()
    {
        // THE READING THAT MATTERS MOST, AND A RANK-BASED SCHEME WOULD GET IT
        // WRONG. Normalising over the row's range forces a full spread however
        // narrow the range is, so the oldest of three entries written in the same
        // moment would be scored stale. They are not stale; they are simultaneous.
        var node = Written(7, 7, 7);

        Assert.All(Freshness(node, recent: true).Values, one => Assert.Equal(1.0, one));
    }

    [Fact]
    public void The_clock_s_units_do_not_change_the_answer()
    {
        // THE CLAIM THAT KEEPS THIS OFF THE DIAL LIST, and it is worth an assertion
        // rather than an argument. One world counts steps and another counts
        // milliseconds; the same history at a thousand times the scale must read
        // identically, or there is a hidden constant here wanting a value per world.
        var steps = Freshness(Written(10, 20, 30), recent: true);
        var millis = Freshness(Written(10_000, 20_000, 30_000), recent: true);

        foreach (var (code, value) in steps)
            Assert.Equal(value, millis[code], precision: 10);
    }

    [Fact]
    public void An_entry_touched_again_becomes_current_again()
    {
        // SUPERSESSION IN ONE ASSERTION. The pair met long ago and met again just
        // now is a CURRENT fact, and the channel is an LWW-Register precisely so
        // that the second meeting speaks for it.
        var node = Written(10, 20, 30);

        var before = Freshness(node, recent: true)[C(2)];

        node.Observe(C(2), when: 40);

        var after = Freshness(node, recent: true)[C(2)];

        output.WriteLine($"{before:F4} -> {after:F4}");

        Assert.True(after > before);
        Assert.Equal(1.0, after, precision: 10);
    }

    [Fact]
    public void It_moves_the_ranking_and_never_the_price()
    {
        // THE RECURRING FAULT, CHECKED RATHER THAN ASSUMED, FOR THE SIXTH TIME. A
        // number that ranks a partner AND prices the hop to it means every discount
        // also starves the route. The budget left must be IDENTICAL with the
        // preference on and off -- same walk, same places, different mind about
        // what it found.
        var node = Written(10, 20, 30);

        var plain = node.Fire(Fixture.Origin(C(1)));
        var dated = node.Fire(Fixture.Origin(C(1)) with { Recent = true });

        Assert.Equal(plain.Outgoing.Length, dated.Outgoing.Length);

        foreach (var (one, other) in plain.Outgoing.Zip(dated.Outgoing))
        {
            Assert.Equal(one.To, other.To);
            Assert.Equal(one.Held, other.Held, precision: 10);
        }
    }

    [Fact]
    public void And_a_stale_partner_scores_below_a_current_one_at_the_far_end()
    {
        // END TO END, because everything above is the SENDER's half. The receiver
        // is where `Fresh` becomes a score, and a walk that carried it and then
        // ignored it would pass every assertion in this file.
        var node = new Node(C(2), Fixture.Dials(stamina: 10.0));
        node.Note(4.0);
        node.Observe(C(5), when: 1);

        var arriving = Fixture.Origin(C(2)) with
        {
            Chain = [C(1), C(2)],
            Together = 4.0,
            Recent = true,
        };

        var current = node.Fire(arriving with { Fresh = 1.0 });
        var stale = node.Fire(arriving with { Fresh = 0.25 });

        output.WriteLine(
            $"score {current.Reached!.Score:F4} -> {stale.Reached!.Score:F4}");

        Assert.Equal(current.Reached!.Score * 0.25, stale.Reached!.Score, precision: 10);
    }
}
