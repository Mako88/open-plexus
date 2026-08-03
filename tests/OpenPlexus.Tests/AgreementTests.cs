using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;

namespace OpenPlexus.Tests;

/// <summary>
/// Ranking a candidate by how many distinct origins reached it, rather than by
/// how much strength arrived.
/// </summary>
/// <remarks>
/// <b>THIS IS WHAT A CONJUNCTIVE QUESTION ASKS FOR, AND `Sum` CANNOT SAY IT.</b>
/// Ask with two things at once and the thing you meant is the one BOTH reach.
/// Summed strength does not measure that: strength varies far more between routes
/// than the count of origins does, so one strong single-origin route outranks two
/// weak agreeing ones.
/// </remarks>
public sealed class AgreementTests
{
    private static Code C(ulong value) => Fixture.C(value);

    private static Thought Thinking(Accumulate accumulate) =>
        new(BroadcastId.New(), origins: 1, accumulate);

    /// <summary>
    /// An arrival at <paramref name="endpoint"/> that started at
    /// <paramref name="origin"/>.
    /// </summary>
    /// <remarks>
    /// <b>A chain begins at its origin and ends at the node addressed</b>, which
    /// is where the origin is read from — nothing extra is carried for this.
    /// </remarks>
    private static Arrival Reaching(Code origin, Code endpoint, double score) => new()
    {
        Endpoint = endpoint,
        Score = score,
        Chain = [origin, endpoint],
        Best = score,
        Routes = 1,
    };

    [Fact]
    public void Two_origins_agreeing_outrank_one_strong_route()
    {
        // THE WHOLE POINT, IN THE SMALLEST FORM IT HAS. `wanted` is reached by
        // two different origins, weakly. `loud` is reached by one, strongly
        // enough that a summed score would put it first.
        Code wanted = C(10), loud = C(11);

        var thought = Thinking(Accumulate.Agreement);

        thought.Receive(Reaching(C(1), wanted, 0.1));
        thought.Receive(Reaching(C(2), wanted, 0.1));
        thought.Receive(Reaching(C(1), loud, 0.9));

        Assert.Equal(wanted, thought.Best(1).Single().Endpoint);
        Assert.Equal(2, thought.Agreeing(wanted));
        Assert.Equal(1, thought.Agreeing(loud));
    }

    [Fact]
    public void And_summed_strength_gets_that_backwards()
    {
        // THE COMPANION, and without it the test above passes for a ranking that
        // happens to agree with `Sum` on this data. Same arrivals, same order.
        Code wanted = C(10), loud = C(11);

        var thought = Thinking(Accumulate.Sum);

        thought.Receive(Reaching(C(1), wanted, 0.1));
        thought.Receive(Reaching(C(2), wanted, 0.1));
        thought.Receive(Reaching(C(1), loud, 0.9));

        Assert.Equal(loud, thought.Best(1).Single().Endpoint);
    }

    [Fact]
    public void Many_routes_from_ONE_origin_are_one_piece_of_evidence()
    {
        // WHAT `Sum` OVER-COUNTS. Several routes from a single origin are one
        // thing arriving by several paths, not several things agreeing -- and a
        // conjunction is about the second. `Sum` adds them all.
        Code once = C(10), often = C(11);

        var thought = Thinking(Accumulate.Agreement);

        thought.Receive(Reaching(C(1), once, 0.1));
        thought.Receive(Reaching(C(2), once, 0.1));

        for (var route = 0; route < 8; route++)
            thought.Receive(Reaching(C(1), often, 0.5));

        Assert.Equal(once, thought.Best(1).Single().Endpoint);
        Assert.Equal(1, thought.Agreeing(often));
    }

    [Fact]
    public void With_one_origin_it_ranks_exactly_as_summed_strength_does()
    {
        // THE CONTROL THAT MAKES THE MEASUREMENTS COMPARABLE. Every question with
        // a single origin must be unaffected, or a change in a conjunction arm
        // could not be attributed to the conjunction. Measured on the composition
        // world too: the one-attribute arm is bit-identical under both.
        var byAgreement = Thinking(Accumulate.Agreement);
        var bySum = Thinking(Accumulate.Sum);

        foreach (var thought in (Thought[])[byAgreement, bySum])
        {
            thought.Receive(Reaching(C(1), C(10), 0.2));
            thought.Receive(Reaching(C(1), C(11), 0.7));
            thought.Receive(Reaching(C(1), C(12), 0.5));
        }

        Assert.Equal(
            byAgreement.Best(3).Select(a => a.Endpoint),
            bySum.Best(3).Select(a => a.Endpoint));
    }

    [Fact]
    public void An_endpoint_nothing_reached_agrees_with_nobody()
    {
        var thought = Thinking(Accumulate.Agreement);

        Assert.Equal(0, thought.Agreeing(C(99)));
    }

    [Fact]
    public void A_released_thought_forgets_who_agreed()
    {
        // The state is dropped, so nothing keeps growing on a thought nobody will
        // read -- the same reason the arrivals are cleared.
        var thought = Thinking(Accumulate.Agreement);
        thought.Receive(Reaching(C(1), C(10), 0.5));

        thought.Release();

        Assert.Equal(0, thought.Agreeing(C(10)));
    }
}
