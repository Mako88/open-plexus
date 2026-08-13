using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Thinking;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The answer does not depend on which route got back first — <b>the budget for
/// a failure class this project had none for.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>C2 SAYS MESSAGES ARE LATE, JITTERED AND OUT OF ORDER, AND FORK 12 SAYS A
/// SEED REPRODUCES A RUN EXACTLY. Those two are only compatible if every fold
/// over arrivals is order-independent, and one of them was not.</b>
/// <c>Thought.Receive</c> kept the standing chain unless a new one was strictly
/// stronger, so two routes of EQUAL strength left the chain belonging to whichever
/// the network delivered first — and chain length is a sort key, so delivery order
/// reached the ranking through a comparison that looks like it could not carry it.
/// </para>
/// <para>
/// <b>MEASURED ON CLEVR, 2026-08-04, AND IT LOOKED LIKE NOISE RATHER THAN A
/// BUG.</b> Four runs of ONE seed with no code change between them: accuracy
/// 0.4286, 0.4361, 0.4511, 0.4624 — a spread of 0.034 against a margin above
/// chance of 0.066 to 0.100, so the wobble was about HALF the whole signal. The
/// graph was identical every time: 9,096,347 messages, 5,285 nodes, widest row
/// 701, to the digit. <b>Same graph, same traffic, different answers</b>, which is
/// what localised it to the read path.
/// </para>
/// <para>
/// <b>ONLY CLEVR COULD SHOW IT, and that is the general lesson.</b> Its widest row
/// is 701 against 6 to 36 everywhere else, so exact ties are common there and rare
/// everywhere else — five worlds reproduced bit-for-bit and hid it completely. A
/// defect that needs fan-out to appear will always be invisible in the small
/// constructed worlds, which is an argument for the one big world rather than
/// against the small ones.
/// </para>
/// <para>
/// <b>AND THE COMMENT ON <c>Ranked</c> ASSERTED THE PROPERTY IT DID NOT HAVE</b> —
/// "the order is deterministic and does not depend on which route happened to
/// land first" — three lines above the violation. It was true of the SORT and
/// false of what the sort read. A <c>cref</c> is not a call and a comment is not
/// an assertion; this file is the assertion.
/// </para>
/// </remarks>
public sealed class ArrivalOrderTests(ITestOutputHelper output)
{
    private static Code C(ulong value) => new(1, value);

    /// <summary>
    /// Arrivals built to TIE, because a tie is where order can get in.
    /// </summary>
    /// <remarks>
    /// <b>Equal <see cref="Arrival.Best"/> is the case that bit</b>, and equal
    /// scores across several endpoints is the case the summed fold can turn into a
    /// near-tie in the last bits. Distinct strengths would pass under the old code
    /// and prove nothing.
    /// </remarks>
    private static ImmutableArray<Arrival> Tying() =>
    [
        new() { Endpoint = C(10), Score = 0.5, Chain = [C(1), C(10)], Best = 0.5, Routes = 1 },
        new() { Endpoint = C(10), Score = 0.5, Chain = [C(2), C(10)], Best = 0.5, Routes = 1 },
        new() { Endpoint = C(10), Score = 0.5, Chain = [C(3), C(9), C(10)], Best = 0.5, Routes = 1 },
        new() { Endpoint = C(11), Score = 0.5, Chain = [C(4), C(11)], Best = 0.5, Routes = 1 },
        new() { Endpoint = C(11), Score = 0.5, Chain = [C(5), C(11)], Best = 0.5, Routes = 1 },
        new() { Endpoint = C(12), Score = 0.25, Chain = [C(6), C(12)], Best = 0.25, Routes = 1 },
        new() { Endpoint = C(12), Score = 0.25, Chain = [C(7), C(12)], Best = 0.25, Routes = 1 },
        new() { Endpoint = C(13), Score = 0.1, Chain = [C(8), C(13)], Best = 0.1, Routes = 1 },
    ];

    /// <summary>One thought, fed the same arrivals in the given order.</summary>
    private static IReadOnlyList<Arrival> Folded(IEnumerable<Arrival> arrivals, Accumulate ranking)
    {
        var thought = new Thought(BroadcastId.New(), 1, ranking);
        foreach (var arrival in arrivals) thought.Receive(arrival);

        return thought.Best(int.MaxValue);
    }

    /// <summary>How a ranking reads, so two of them can be compared as text.</summary>
    private static string Reading(IReadOnlyList<Arrival> ranked) =>
        string.Join(" ", ranked.Select(a => $"{a.Endpoint.Value}:{a.Chain.Length}:{a.Best:F4}"));

    [Theory]
    [InlineData(Accumulate.Sum)]
    [InlineData(Accumulate.Agreement)]
    public void Every_delivery_order_of_the_same_arrivals_ranks_the_same(Accumulate ranking)
    {
        var arrivals = Tying();
        var wanted = Reading(Folded(arrivals, ranking));

        output.WriteLine($"{ranking}: {wanted}");

        // EVERY ROTATION AND EVERY REVERSAL, rather than a couple of shuffles. The
        // failure needs a SPECIFIC pair to swap, so sampling orders at random can
        // miss it — and a check that can fail to fire is the named trap here.
        for (var turn = 0; turn < arrivals.Length; turn++)
        {
            var rotated = arrivals.Skip(turn).Concat(arrivals.Take(turn)).ToArray();

            Assert.Equal(wanted, Reading(Folded(rotated, ranking)));
            Assert.Equal(wanted, Reading(Folded(rotated.Reverse(), ranking)));
        }
    }

    [Fact]
    public void The_kept_chain_is_the_one_the_graph_chose_and_not_the_one_that_was_quick()
    {
        // THE DEFECT ITSELF, AT ITS SMALLEST. Two routes of equal strength reach
        // one endpoint; the shorter is the better explanation, and under `>` the
        // survivor was simply whichever landed first.
        var quick = new Arrival
        {
            Endpoint = C(10), Score = 0.5, Chain = [C(1), C(9), C(10)], Best = 0.5, Routes = 1,
        };

        var better = new Arrival
        {
            Endpoint = C(10), Score = 0.5, Chain = [C(2), C(10)], Best = 0.5, Routes = 1,
        };

        Assert.Equal(2, Folded([quick, better], Accumulate.Sum)[0].Chain.Length);
        Assert.Equal(2, Folded([better, quick], Accumulate.Sum)[0].Chain.Length);
    }

    [Fact]
    public void A_score_differing_only_in_the_last_bits_does_not_decide_a_ranking()
    {
        // THE SECOND HALF, AND IT IS THE SUM RATHER THAN THE COMPARISON. Folding is
        // done in arrival order and floating-point addition is not associative, so
        // the SAME routes arriving differently give scores apart in the last bits.
        // Two candidates that should tie must not be separated by that.
        var many = new List<Arrival>();

        // Chosen so the two endpoints receive identical contributions in different
        // orders — the sums are mathematically equal and need not be bitwise so.
        double[] parts = [0.1, 0.2, 0.30000000000000004, 0.7, 0.0001];

        for (var part = 0; part < parts.Length; part++)
        {
            many.Add(new Arrival
            {
                Endpoint = C(20), Score = parts[part],
                Chain = [C(100 + (ulong)part), C(20)], Best = 0.5, Routes = 1,
            });

            many.Add(new Arrival
            {
                Endpoint = C(21), Score = parts[parts.Length - 1 - part],
                Chain = [C(200 + (ulong)part), C(21)], Best = 0.5, Routes = 1,
            });
        }

        var ranked = Folded(many, Accumulate.Sum);

        output.WriteLine(
            $"{ranked[0].Endpoint.Value} scored {ranked[0].Score:R}, "
            + $"{ranked[1].Endpoint.Value} scored {ranked[1].Score:R}");

        // THE LOWER CODE FIRST, because the scores tie and the endpoint is the last
        // deterministic key. If the raw doubles were compared, whichever sum
        // happened to land a bit-width higher would win instead.
        Assert.Equal(20UL, ranked[0].Endpoint.Value);
        Assert.Equal(21UL, ranked[1].Endpoint.Value);
    }
}
