using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Ranking belongs to the question, and this is where that was settled.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE DECISION AND ITS LOSING ALTERNATIVE.</b>
/// <see cref="Accumulate.Agreement"/> is right on a conjunction and harmful on an
/// indexed question, so it could never be a machine default. Two ways out: move
/// it onto the question, or fuse the two orders so nobody has to say. Fusing was
/// built first, because dissolving a dial beats moving one — and it lost.
/// </para>
/// <para>
/// <b>The fusion arm is deleted rather than parked</b>, per the standing rule,
/// so what survives here is the arithmetic that says why it could never have
/// worked and the check that the ranking now travels with the asker.
/// </para>
/// </remarks>
public sealed class RankingTests(ITestOutputHelper output)
{
    [Fact]
    public void Two_candidates_and_two_orders_that_invert_tie_under_fusion_for_any_damping()
    {
        // WHY FUSION FAILED, AS ARITHMETIC RATHER THAN AS A MEASUREMENT — which is
        // what makes the refutation general instead of a fact about one world.
        //
        // Reciprocal rank fusion scores a candidate by the sum of 1/(k + rank)
        // over the orders. With exactly two candidates and two orders that
        // disagree, one is ranked (0, 1) and the other (1, 0) — so both score
        // 1/k + 1/(k+1) EXACTLY, and no choice of k separates them. The tie then
        // falls to whatever breaks ties, which is an answer nobody chose.
        //
        // A binding question offers exactly two candidates and the two rankings
        // disagree on precisely the questions that are hard, so fusion is at its
        // most degenerate exactly where it was needed. Measured before it was
        // deleted: half of agreement's lift on the conjunction, and all of its
        // cost on binding.
        foreach (var damping in new[] { 1.0, 5.0, 60.0, 1000.0 })
        {
            var one = (1.0 / (damping + 0)) + (1.0 / (damping + 1));
            var other = (1.0 / (damping + 1)) + (1.0 / (damping + 0));

            Assert.Equal(one, other, 12);
        }
    }

    [Fact]
    public async Task The_same_machine_answers_two_questions_differently()
    {
        // THE MOVE ITSELF, ASSERTED. One `WalkSettings`, one graph, one world --
        // and the only thing that differs is what the asker said about its own
        // question. Before this, that choice lived on the machine and every
        // question a machine asked was ranked the same way.
        var dials = Fixture.Dials(stamina: 8.0) with { Pricing = Pricing.Sender };

        var world = new ComposedSettings
        {
            Values = 24, CodesPerValue = 3, Segmented = true, Tagged = true,
        };

        using var conjoined = new ComposedRun(world, dials, seed: 1, Accumulate.Agreement);
        using var summed = new ComposedRun(world, dials, seed: 1, Accumulate.Sum);

        var asking = await conjoined.RunAsync(400, Refer.Narrowed, every: 10);
        var plain = await summed.RunAsync(400, Refer.Narrowed, every: 10);

        output.WriteLine($"conjunction={asking.Accuracy:F4} strength={plain.Accuracy:F4}");

        Assert.True(asking.Accuracy > plain.Accuracy,
            $"saying it is a conjunction bought nothing, so the question has "
            + $"nothing to say: {asking.Accuracy} against {plain.Accuracy}");
    }

    [Fact]
    public void A_question_that_says_nothing_ranks_by_strength()
    {
        // THE DEFAULT IS THE CONTROL, and it has to stay the control: every number
        // taken before `Question` existed was taken under plain strength with no
        // grouping, and a different default would silently move all of them.
        var quiet = new Thinking.Question();

        Assert.Equal(Accumulate.Sum, quiet.Ranking);
        Assert.Null(quiet.Asking);
    }
}
