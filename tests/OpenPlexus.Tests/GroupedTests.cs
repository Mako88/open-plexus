using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// The candidate for a thing that was never observed — <b>a group of origins that
/// all reached each other, found at the machine that asked.</b>
/// </summary>
/// <remarks>
/// <b>EVERY MINTER IN THIS DESIGN NAMES SOMETHING THAT WAS PRESENT.</b> A chunk
/// names a set that arrived, a macro an order that recurred, a relation instance
/// something that was stated. A hub over a mutually-reaching group names the
/// EXPLANATION for one instead, which is the first thing here that is not a
/// recognition — and <see cref="Paying.Cheaper"/> prices it as a shortcut rather
/// than predicting anything, so it can be wasteful and cannot be false.
/// </remarks>
public sealed class GroupedTests
{
    private static Code C(ulong value) => Fixture.C(value);

    /// <summary>A thought asked from <paramref name="origins"/>.</summary>
    private static Thought Asking(params Code[] origins) =>
        new(BroadcastId.New(), origins.Length, Accumulate.Agreement, [.. origins]);

    private static Arrival Reaching(Code origin, Code endpoint) => new()
    {
        Endpoint = endpoint,
        Score = 0.5,
        Chain = [origin, endpoint],
        Best = 0.5,
        Routes = 1,
    };

    /// <summary>Every origin reaches every other. The whole point.</summary>
    private static Thought Mutual(int size)
    {
        var origins = Enumerable.Range(1, size).Select(one => C((ulong)one)).ToArray();
        var thought = Asking(origins);

        foreach (var from in origins)
            foreach (var to in origins)
                if (from != to)
                    thought.Receive(Reaching(from, to));

        return thought;
    }

    [Fact]
    public void Four_things_that_all_reach_each_other_are_worth_a_hub()
    {
        // SIX EDGES AMONG THEM AGAINST FOUR TO A HUB PLUS ONE TO DEFINE IT, so
        // this is the smallest group that pays. Nobody chose four -- it falls out
        // of the arithmetic.
        Assert.Equal(4, Mutual(4).Grouped().Length);
    }

    [Fact]
    public void And_a_triangle_is_not()
    {
        // THREE EDGES AGAINST THREE PLUS ONE. A triangle is real structure and
        // still not worth a name, which is the gate refusing something TRUE rather
        // than something false -- exactly what a description-length bar is for.
        Assert.Empty(Mutual(3).Grouped());
    }

    [Fact]
    public void A_group_that_does_NOT_all_reach_each_other_is_refused()
    {
        // THE CHECK THAT MATTERS. Four origins, but one of them reaches nobody --
        // so there is no group of four, and the three that remain do not pay.
        Code a = C(1), b = C(2), c = C(3), stranger = C(4);

        var thought = Asking(a, b, c, stranger);

        foreach (var from in (Code[])[a, b, c])
            foreach (var to in (Code[])[a, b, c])
                if (from != to)
                    thought.Receive(Reaching(from, to));

        Assert.Empty(thought.Grouped());
    }

    [Fact]
    public void One_stranger_does_not_cost_the_group_that_does_pay()
    {
        // PEELING HAS TO REMOVE THE OUTSIDER AND KEEP THE REST. Five origins where
        // four are mutual and one is not: the four still pay, and a version that
        // gave up on the whole set the moment one member failed would find nothing.
        var mutual = Enumerable.Range(1, 4).Select(one => C((ulong)one)).ToArray();
        var stranger = C(99);

        var thought = Asking([.. mutual, stranger]);

        foreach (var from in mutual)
            foreach (var to in mutual)
                if (from != to)
                    thought.Receive(Reaching(from, to));

        // The stranger was reached by one of them and reaches nobody back.
        thought.Receive(Reaching(mutual[0], stranger));

        Assert.Equal(mutual.AsEnumerable(), thought.Grouped().AsEnumerable());
    }

    [Fact]
    public void Removing_one_can_strand_another_and_the_peel_repeats()
    {
        // WHY ONE PASS IS NOT ENOUGH. `d` reaches only `e`, and `e` reaches only
        // `d` -- so dropping `e` for failing against the others must then drop
        // `d`, which was only mutual THROUGH `e`. A single sweep would leave `d`
        // standing and report a group that is not one.
        Code a = C(1), b = C(2), c = C(3), d = C(4), e = C(5);

        var thought = Asking(a, b, c, d, e);

        foreach (var from in (Code[])[a, b, c])
            foreach (var to in (Code[])[a, b, c])
                if (from != to)
                    thought.Receive(Reaching(from, to));

        thought.Receive(Reaching(d, e));
        thought.Receive(Reaching(e, d));

        // a, b, c survive and are only three, which does not pay.
        Assert.Empty(thought.Grouped());
    }

    [Fact]
    public void The_same_group_is_found_the_same_way_on_every_machine()
    {
        // THE RED-BALL PROPERTY, AND IT IS WHY THIS PEELS RATHER THAN SEARCHING.
        // Largest-clique is neither cheap nor unique: two machines could pick
        // different equally-large groups out of one structure and mint different
        // hubs for it. Peeling is deterministic.
        // AS SEQUENCES: two `ImmutableArray` values holding the same codes are not
        // the same array, and comparing the structs would pass for the wrong reason
        // or fail for one.
        Assert.Equal(Mutual(5).Grouped().AsEnumerable(), Mutual(5).Grouped().AsEnumerable());
    }

    [Fact]
    public void A_thought_that_reached_nowhere_groups_nothing()
    {
        Assert.Empty(Asking(C(1), C(2), C(3), C(4)).Grouped());
    }

    [Fact]
    public void An_endpoint_that_was_never_asked_from_is_not_a_member()
    {
        // THE GROUP IS PRICED OVER ORIGINS. Something the walk merely arrived at
        // is not a thing the question was about, and counting it would mint a hub
        // over a set nobody asked.
        var thought = Mutual(4);

        thought.Receive(Reaching(C(1), C(500)));
        thought.Receive(Reaching(C(2), C(500)));

        Assert.DoesNotContain(C(500), thought.Grouped());
    }

    [Fact]
    public void The_bar_is_arithmetic_and_not_a_constant_anybody_set()
    {
        // Said directly, because a chosen number is a refuted row's shape here.
        Assert.False(Paying.Cheaper(3));
        Assert.True(Paying.Cheaper(4));
        Assert.True(Paying.Cheaper(10));
    }
}
