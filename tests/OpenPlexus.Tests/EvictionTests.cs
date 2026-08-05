using OpenPlexus.Codes;
using OpenPlexus.Graph;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A bounded row — <b>the scaling wall's only answer, and this design's only
/// forgetting.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>COST PER THOUGHT IS SET BY THE WIDEST ROW.</b> <see cref="Node.Fire"/> emits
/// one message per ENTRY, so capping the row caps the fan-out — which turns
/// <i>cost grows with data forever</i> into <i>cost is constant</i>, the trick
/// approximate-nearest-neighbour indexes run at billions on.
/// </para>
/// <para>
/// <b>AND IT IS THE FIRST CONSUMER <see cref="Tie.When"/> HAS EVER HAD.</b> The
/// supersession channel has been written and read back since edge kinds landed and
/// no walk consulted it; the plan named its two consumers and left both unbuilt.
/// This is one of them.
/// </para>
/// <para>
/// <b>EVICT ON "NOT TOUCHED SINCE", NEVER BY ERODING A COUNT.</b> That distinction
/// is the whole of why forgetting is expressible here and decay is not: a count
/// that decreased would break the convergence the coordination-free design rests
/// on, and an entry that stops being RESIDENT does not — the number was never
/// revised, it was paged out.
/// </para>
/// </remarks>
public sealed class EvictionTests(ITestOutputHelper output)
{
    private static Code C(ulong value) => Fixture.C(value);

    private static Node Bounded(int cap) =>
        new(C(1), Fixture.Dials(stamina: 10.0) with { Row = cap });

    [Fact]
    public void An_unbounded_row_is_every_measurement_taken_before_this_existed()
    {
        // THE ARM'S OFF POSITION, ASSERTED RATHER THAN ASSUMED. Null is unbounded,
        // and a cap that quietly applied by default would silently change every
        // number this project has.
        var node = new Node(C(1), Fixture.Dials(stamina: 10.0));

        for (var partner = 2UL; partner < 50; partner++) node.Observe(C(partner), when: 1);

        Assert.Equal(48, node.Entries);
    }

    [Fact]
    public void A_bounded_row_never_exceeds_its_cap()
    {
        var node = Bounded(cap: 8);

        for (var partner = 2UL; partner < 200; partner++)
            node.Observe(C(partner), when: (long)partner);

        Assert.Equal(8, node.Entries);
    }

    [Fact]
    public void What_goes_is_what_was_touched_longest_ago()
    {
        // THE CHANNEL DOING ITS JOB. Recency and not count, not insertion order,
        // not arrival -- so a partner met once yesterday goes before one met once
        // this morning, and a heavily-counted partner nobody has seen in a while
        // goes before a thin one that is current.
        var node = Bounded(cap: 3);

        node.Observe(C(2), by: 100.0, when: 10);
        node.Observe(C(3), when: 20);
        node.Observe(C(4), when: 30);
        node.Observe(C(5), when: 40);

        Assert.Equal(3, node.Entries);

        // THE HEAVY ONE WENT, which is the part worth asserting: eviction is not
        // secretly ranking by evidence.
        Assert.Equal(0.0, node.Together(C(2)));
        Assert.Equal(1.0, node.Together(C(5)));
    }

    [Fact]
    public void Touching_an_entry_again_saves_it()
    {
        // WITHOUT THIS THE CHANNEL IS INSERTION ORDER WEARING A CLOCK. An entry
        // that keeps being met must survive newer ones, or the cap evicts exactly
        // the partners a node uses most.
        var node = Bounded(cap: 3);

        node.Observe(C(2), when: 10);
        node.Observe(C(3), when: 20);
        node.Observe(C(4), when: 30);

        // C(2) is the oldest -- and then it is met again, later than any of them.
        node.Observe(C(2), when: 40);
        node.Observe(C(5), when: 50);

        Assert.Equal(2.0, node.Together(C(2)));
        Assert.Equal(0.0, node.Together(C(3)));
    }

    [Fact]
    public void No_count_is_ever_reduced_on_the_way_out()
    {
        // THE CRDT CLAIM, AND IT IS THE ONE THAT MAKES THIS LEGAL WHERE DECAY IS
        // NOT. A survivor's count is exactly what it was; the evicted entry is
        // absent rather than diminished. Nothing anywhere observes a number going
        // down, which is the G-Counter property the whole design rests on.
        var node = Bounded(cap: 2);

        node.Observe(C(2), by: 5.0, when: 10);
        node.Observe(C(3), by: 7.0, when: 20);

        var before = node.Together(C(3));

        node.Observe(C(4), when: 30);

        Assert.Equal(before, node.Together(C(3)));
        Assert.Equal(0.0, node.Together(C(2)));

        // AND THE MARGINAL IS UNTOUCHED, which is what keeps `together <= seen`
        // holding and every hop costing at least one.
        Assert.Equal(2, node.Entries);
    }

    [Fact]
    public void Two_entries_stamped_alike_are_dropped_in_an_order_that_reproduces()
    {
        // FORK 12, AND IT IS WHY `Kind` HAD TO BECOME COMPARABLE. Every pair
        // written in ONE occasion shares a clock exactly, so on a full row the
        // entries competing to be dropped are routinely all stamped the same --
        // and picking among them by dictionary order would make a fixed seed stop
        // reproducing its run.
        var kept = new List<string>();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var node = Bounded(cap: 2);

            foreach (var partner in (ulong[])[7, 3, 9, 2, 5])
                node.Observe(C(partner), when: 100);

            kept.Add(string.Join(
                ",", node.Partners().Select(code => code.Value).Order()));
        }

        output.WriteLine(string.Join(" | ", kept));

        Assert.Single(kept.Distinct());
    }

    [Fact]
    public void An_entry_from_a_caller_with_no_clock_is_given_up_first()
    {
        // THE HONEST ORDERING FOR A STAMP OF NOUGHT: nothing is known about when
        // that entry was touched, so it is the first thing a bounded row gives up.
        // Said out loud because it means a bounded row and a clockless caller do
        // not belong together -- and every test in this suite that omits `when` is
        // exactly such a caller.
        var node = Bounded(cap: 2);

        node.Observe(C(2));
        node.Observe(C(3), when: 5);
        node.Observe(C(4), when: 6);

        Assert.Equal(0.0, node.Together(C(2)));
        Assert.Equal(1.0, node.Together(C(3)));
    }

    /// <summary>One cap, averaged over seeds.</summary>
    private static async Task<(double Accuracy, double Messages, double Widest)> CappedAsync(
        int cap)
    {
        double accuracy = 0.0, messages = 0.0, widest = 0.0;

        int[] seeds = [1, 2, 3, 5, 8, 13];

        foreach (var seed in seeds)
        {
            using var run = new Worlds.MotifRun(
                new Worlds.MotifSettings(),
                Fixture.Dials(stamina: 4.0) with { Row = cap },
                seed);

            var result = await run.RunAsync(600);

            accuracy += result.Accuracy;
            messages += result.Messages;
            widest += result.Widest;
        }

        return (accuracy / seeds.Length, messages / seeds.Length, widest / seeds.Length);
    }

    [Fact]
    public async Task Forgetting_is_survivable_and_a_third_of_the_row_is_free()
    {
        // THE BET THIS TESTS IS THE PLAN'S BIGGEST STANDING ONE: nothing can be
        // unlearned, only outvoted -- with eviction named as the expensive thing to
        // walk back if forgetting turns out to be necessary rather than optional.
        // Until now there was no way to find out, because the cap did not exist.
        //
        // THE ANSWER IS THAT IT IS FREE UNTIL IT IS NOT. `Motif`'s widest row runs
        // to the high forties; capped at sixteen the world still scores its
        // ceiling, on roughly half the edges and well under half the messages. The
        // graph was carrying a great deal it never used.
        var whole = await CappedAsync(Fixture.Unbounded);
        var third = await CappedAsync(16);

        output.WriteLine(
            $"none  acc={whole.Accuracy:F4} widest={whole.Widest:F0} "
            + $"msgs={whole.Messages:F0}");
        output.WriteLine(
            $"16    acc={third.Accuracy:F4} widest={third.Widest:F0} "
            + $"msgs={third.Messages:F0}");

        // THE CAP HAS TO BITE, or this measures nothing at all.
        Assert.True(whole.Widest > 24,
            $"the unbounded row is only {whole.Widest:F0} wide, so a cap of 16 is "
            + "barely a cap and the claim below is about nothing");

        // NOT WORSE, RATHER THAN EQUAL, AND THE DIRECTION IS WORTH NOTING WITHOUT
        // BEING CLAIMED. Over six seeds the bounded row scores slightly ABOVE the
        // unbounded one, which is what dropping stale partners would do if the row
        // were carrying noise -- but six seeds and no error bars is exactly the
        // sample that has fooled this project before, so the assertion is only that
        // eviction does not cost.
        Assert.True(third.Accuracy >= whole.Accuracy - 0.01,
            $"bounding the row cost accuracy: {third.Accuracy:F4} against "
            + $"{whole.Accuracy:F4}");

        Assert.True(third.Messages < whole.Messages * 0.75,
            $"bounding the row stopped paying for itself in traffic: "
            + $"{third.Messages:F0} against {whole.Messages:F0}");
    }

    [Fact]
    public async Task And_there_is_a_knee_below_it_rather_than_a_slope()
    {
        // WITHOUT THIS THE TEST ABOVE PASSES FOR A WORLD THAT NEVER NEEDED ITS ROW.
        // If accuracy held at every cap, the right reading would be that this world
        // is too easy to say anything about eviction -- so the cost has to appear
        // somewhere, and where it appears is the number worth having.
        var narrow = await CappedAsync(4);

        output.WriteLine($"4     acc={narrow.Accuracy:F4} msgs={narrow.Messages:F0}");

        Assert.True(narrow.Accuracy < 0.75,
            $"a row of four still scores {narrow.Accuracy:F4}, so this world does "
            + "not need its row at all and cannot say whether forgetting is safe");
    }
}
