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
    public void One_agreement_level_across_the_candidates_FORCES_the_tie_with_Sum()
    {
        // THE OPEN DEFECT'S FOURTH EXPLANATION, AND THE FIRST ONE THAT DOES NOT
        // NEED A BUG. Agreement is compared first and falls through to strength
        // when it ties -- so candidates that all agree to the same degree are
        // ranked by strength alone, which IS `Sum`, to the last bit. Three
        // explanations were spent on why the two read exactly equal and none of
        // them asked whether the arm had anything to rank.
        var byAgreement = Thinking(Accumulate.Agreement);
        var bySum = Thinking(Accumulate.Sum);

        foreach (var thought in (Thought[])[byAgreement, bySum])
        {
            // Three candidates, three DIFFERENT origins, one origin each. The
            // agreement count is 1 everywhere and the strengths are far apart.
            thought.Receive(Reaching(C(1), C(10), 0.2));
            thought.Receive(Reaching(C(2), C(11), 0.7));
            thought.Receive(Reaching(C(3), C(12), 0.5));
        }

        Assert.Equal(1, byAgreement.Divides);

        Assert.Equal(
            bySum.Best(3).Select(one => one.Endpoint),
            byAgreement.Best(3).Select(one => one.Endpoint));
    }

    [Fact]
    public void And_it_reads_above_one_the_moment_they_differ()
    {
        // ARM ANYTHING THAT HAS ALWAYS READ ZERO -- the companion that stops the
        // check above passing for a statistic wired to a constant. Same shape as
        // the first test in this file, which is the case agreement is FOR.
        var thought = Thinking(Accumulate.Agreement);

        thought.Receive(Reaching(C(1), C(10), 0.1));
        thought.Receive(Reaching(C(2), C(10), 0.1));
        thought.Receive(Reaching(C(1), C(11), 0.9));

        Assert.Equal(2, thought.Divides);
    }

    [Fact]
    public void A_thought_that_reached_nowhere_divides_nothing()
    {
        // NOUGHT IS A DIFFERENT COMPLAINT FROM ONE, and the two must not be
        // confusable: one is an arm with nothing to rank, nought is a walk that
        // never arrived.
        Assert.Equal(0, Thinking(Accumulate.Agreement).Divides);
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
