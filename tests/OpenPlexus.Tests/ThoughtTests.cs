using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// One broadcast's arrivals and accounting, on the machine that started it.
/// </summary>
public sealed class ThoughtTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    private static readonly BroadcastId Mine = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly BroadcastId Other = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static Arrival Reaching(Code endpoint, double score, params Code[] chain) => new()
    {
        Endpoint = endpoint,
        Score = score,
        Chain = [.. chain, endpoint],
        Best = score,
        Routes = 1,
    };

    private static Thought Started(int origins = 1, Accumulate accumulate = Accumulate.Sum) =>
        new(Mine, origins, accumulate);

    private static ClusterAddress A(string name) => new(name);

    private static Report Reporting(ClusterAddress from, int handled, int splits, int deaths,
        params Routed[] sentInto) => new()
    {
        From = from,
        Handled = handled,
        SentInto = [.. sentInto],
        Arrivals = [],
        Accounting = new Accounting(Mine, splits, deaths),
    };

    // ---- fork 5: a departure is exact, not a question ---------------------

    [Fact]
    public void A_cluster_leaving_takes_the_routes_that_were_heading_into_it()
    {
        var thought = Started(origins: 3);
        thought.SentInto(A("alpha"), 2);
        thought.SentInto(A("beta"), 1);

        Assert.Equal(2, thought.InFlightTo(A("alpha")));
        Assert.True(thought.Balanced());

        // JOHN'S DESIGN. The origin knows how many of its routes were heading
        // into alpha, so a departure is not a question about whether it was
        // affected -- the loss is exact.
        Assert.Equal(2, thought.Lost(A("alpha")));

        Assert.Equal(1, thought.Live);
        Assert.Equal(2, thought.Deaths);
        Assert.Equal(0, thought.InFlightTo(A("alpha")));
    }

    [Fact]
    public void A_cluster_this_thought_never_used_takes_nothing()
    {
        // The companion. Without it the test above passes for a Lost that
        // writes off every route whatever died.
        var thought = Started(origins: 3);
        thought.SentInto(A("alpha"), 3);

        Assert.Equal(0, thought.Lost(A("elsewhere")));

        Assert.Equal(3, thought.Live);
        Assert.Equal(0, thought.Deaths);
    }

    [Fact]
    public void Losing_the_last_cluster_settles_the_thought()
    {
        // This is what the event bus was introduced for: the origin stops
        // waiting on routes that are never coming back, with no deadline
        // guessing on its behalf.
        var thought = Started(origins: 2);
        thought.SentInto(A("alpha"), 2);

        Assert.False(thought.Settled);
        thought.Lost(A("alpha"));

        Assert.True(thought.Settled);
        Assert.True(thought.Balanced());
    }

    [Fact]
    public void A_report_moves_routes_off_the_cluster_that_handled_them()
    {
        var thought = Started(origins: 2);
        thought.SentInto(A("alpha"), 2);

        thought.Receive(Reporting(A("alpha"), handled: 2, splits: 1, deaths: 0,
            new Routed(A("beta"), 3)));

        Assert.Equal(0, thought.InFlightTo(A("alpha")));
        Assert.Equal(3, thought.InFlightTo(A("beta")));
        Assert.Equal(3, thought.Live);

        // BALANCED IS NO LONGER A TAUTOLOGY. The live count comes from splits
        // and deaths; the in-flight counts come from the routing in each
        // report. Two independent quantities agreeing is a real check.
        Assert.True(thought.Balanced());
    }

    [Fact]
    public void Balanced_notices_when_the_routing_and_the_arithmetic_disagree()
    {
        var thought = Started(origins: 2);
        thought.SentInto(A("alpha"), 2);

        // Says it handled two and forked to three, but names nowhere they went.
        thought.Receive(Reporting(A("alpha"), handled: 2, splits: 1, deaths: 0));

        Assert.Equal(3, thought.Live);
        Assert.False(thought.Balanced());
    }

    // ---- accumulation -----------------------------------------------------

    [Fact]
    public void Sum_gathers_evidence_from_every_route()
    {
        var thought = Started(accumulate: Accumulate.Sum);

        thought.Receive(Reaching(C(9), 0.2, C(1)));
        thought.Receive(Reaching(C(9), 0.3, C(2)));

        var arrival = thought.Best(1).Single();
        Assert.Equal(0.5, arrival.Score, precision: 10);
        Assert.Equal(2, arrival.Routes);
    }

    [Fact]
    public void Max_keeps_only_the_strongest_route()
    {
        // The companion to the test above. Both arms run, so "sum accumulates"
        // is a difference from something rather than a description of the only
        // behaviour there is.
        var thought = Started(accumulate: Accumulate.Max);

        thought.Receive(Reaching(C(9), 0.2, C(1)));
        thought.Receive(Reaching(C(9), 0.3, C(2)));

        var arrival = thought.Best(1).Single();
        Assert.Equal(0.3, arrival.Score, precision: 10);
        Assert.Equal(2, arrival.Routes);
    }

    [Fact]
    public void The_explanation_is_the_strongest_chain_and_not_the_last_one()
    {
        var thought = Started();

        thought.Receive(Reaching(C(9), 0.9, C(1)));
        thought.Receive(Reaching(C(9), 0.1, C(2)));

        // A summed score is no route's strength, so reporting the last arrival
        // would make the explanation whichever branch happened to finish last.
        Assert.Equal([C(1), C(9)], thought.Best(1).Single().Chain.ToArray());
    }

    [Fact]
    public void A_stronger_chain_arriving_later_does_replace_the_explanation()
    {
        // Without this companion, the test above passes for a Thought that
        // simply never updates the chain at all.
        var thought = Started();

        thought.Receive(Reaching(C(9), 0.1, C(2)));
        thought.Receive(Reaching(C(9), 0.9, C(1)));

        Assert.Equal([C(1), C(9)], thought.Best(1).Single().Chain.ToArray());
    }

    // ---- ranking ----------------------------------------------------------

    [Fact]
    public void Best_ranks_by_score_and_cuts_where_the_caller_asks()
    {
        var thought = Started();

        thought.Receive(Reaching(C(1), 0.1));
        thought.Receive(Reaching(C(2), 0.9));
        thought.Receive(Reaching(C(3), 0.5));

        Assert.Equal([C(2), C(3)], thought.Best(2).Select(a => a.Endpoint).ToArray());
        Assert.Equal(3, thought.Best(10).Count);
    }

    [Fact]
    public void An_exact_tie_breaks_on_the_shorter_chain()
    {
        var thought = Started();

        thought.Receive(Reaching(C(1), 0.5, C(7), C(8)));
        thought.Receive(Reaching(C(2), 0.5, C(7)));

        Assert.Equal(C(2), thought.Best(1).Single().Endpoint);
    }

    [Fact]
    public void Best_is_readable_before_anything_has_settled()
    {
        // Continuous input means there is no moment between thoughts, so the
        // system acts on what has arrived so far and later arrivals refine it.
        var thought = Started(origins: 3);

        thought.Receive(Reaching(C(9), 0.4, C(1)));

        Assert.False(thought.Settled);
        Assert.Equal(C(9), thought.Best(1).Single().Endpoint);
    }

    // ---- accounting -------------------------------------------------------

    [Fact]
    public void A_thought_is_over_when_every_route_has_returned_or_died()
    {
        var thought = Started(origins: 1);
        Assert.False(thought.Settled);

        // One route became three, so the live count moves by the difference.
        thought.Receive(new Accounting(Mine, Splits: 2, Deaths: 0));
        Assert.Equal(3, thought.Live);
        Assert.False(thought.Settled);

        for (var i = 0; i < 3; i++) thought.Receive(new Accounting(Mine, Splits: 0, Deaths: 1));

        Assert.True(thought.Settled);
        Assert.True(thought.Balanced());
    }

    [Fact]
    public void The_accounting_stays_balanced_while_it_runs()
    {
        var thought = Started(origins: 2);
        thought.SentInto(A("alpha"), 2);

        thought.Receive(Reporting(A("alpha"), handled: 2, splits: 4, deaths: 0,
            new Routed(A("beta"), 6)));
        Assert.True(thought.Balanced());

        thought.Receive(Reporting(A("beta"), handled: 1, splits: 0, deaths: 1));
        Assert.True(thought.Balanced());

        Assert.Equal(4, thought.Splits);
        Assert.Equal(1, thought.Deaths);
        Assert.Equal(5, thought.Live);
    }

    [Fact]
    public void Another_broadcasts_accounting_is_refused()
    {
        var thought = Started();

        // Mixing two thoughts' death counts is exactly what the broadcast id
        // exists to prevent, and the Python has no equivalent.
        Assert.Throws<ArgumentException>(
            () => thought.Receive(new Accounting(Other, Splits: 1, Deaths: 0)));

        Assert.Equal(0, thought.Splits);
    }

    // ---- release ----------------------------------------------------------

    [Fact]
    public void Releasing_drops_the_state_and_a_late_arrival_is_ignored()
    {
        var thought = Started();
        thought.Receive(Reaching(C(9), 0.4, C(1)));
        Assert.Equal(1, thought.Endpoints);

        thought.Release();

        Assert.True(thought.Released);
        Assert.Empty(thought.Best(10));

        // C2 says late is normal, so an arrival after release is dropped rather
        // than refused — there is nothing left for it to refine.
        thought.Receive(Reaching(C(8), 0.9, C(1)));
        Assert.Empty(thought.Best(10));
    }

    [Fact]
    public void An_arrival_before_release_is_not_ignored()
    {
        // The companion. Without it, the test above passes for a Thought whose
        // Receive never did anything.
        var thought = Started();

        thought.Receive(Reaching(C(8), 0.9, C(1)));

        Assert.Single(thought.Best(10));
    }

    [Fact]
    public void A_thought_needs_somewhere_to_start()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Thought(Mine, 0, Accumulate.Sum));
    }
}
