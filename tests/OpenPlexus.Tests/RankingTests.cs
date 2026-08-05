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
    public async Task The_same_machine_answers_two_questions_identically_and_that_is_the_defect()
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

        using var conjoined = new ComposedRun(world, dials with { Ranking = Accumulate.Agreement }, seed: 1);
        using var summed = new ComposedRun(world, dials with { Ranking = Accumulate.Sum }, seed: 1);

        var asking = await conjoined.RunAsync(400, Refer.Narrowed, every: 10);
        var plain = await summed.RunAsync(400, Refer.Narrowed, every: 10);

        output.WriteLine(
            $"conjunction: reference={asking.Reference:F4} accuracy={asking.Accuracy:F4}");
        output.WriteLine(
            $"strength:    reference={plain.Reference:F4} accuracy={plain.Accuracy:F4}");

        // READ OFF `Reference` AND NOT `Accuracy`, AND THE REASON IS STRUCTURAL
        // RATHER THAN A PREFERENCE. This compared `Accuracy` and the two arms tied
        // to sixteen digits, which is never a measurement -- but under
        // `Refer.Narrowed` it CANNOT be one. The answer comes from a SECOND
        // broadcast, made from the single index the first walk chose, and
        // `Accumulate.Agreement` counts how many DISTINCT ORIGINS reached an
        // endpoint. With one origin every candidate is reached by exactly one, the
        // comparison falls through to strength, and agreement is identical to `Sum`
        // by arithmetic. Asking the ranking to move that number is asking it to
        // discriminate where it is undefined.
        //
        // `Reference` is the first walk's, where the conjunction really is asked --
        // two attributes, two origin groups, and an index only the right one is
        // reached by twice. AND IT TIES EXACTLY TOO, 0.3846 against 0.3846, WHICH
        // IS THE OPEN DEFECT AND NOT A SECOND PROBLEM.
        //
        // PARKED, DELIBERATELY, AND ASSERTED RATHER THAN SKIPPED. `Accumulate.
        // Agreement` reading exactly equal to `Sum` is already the plan's one open
        // defect, with two explanations spent on it -- the minted name is not why
        // (it ties with chunking suppressed) and neither is arrival order. What is
        // added here is a THIRD thing it is not: it is not the narrowed question's
        // second broadcast either, because the first walk's own number ties as well.
        //
        // A skip would make this silent, which is the one thing this project does
        // not allow. So the tie is asserted: the suite stays green, the defect stays
        // visible, and THE DAY AGREEMENT STARTS DISCRIMINATING THIS FAILS AND SAYS
        // SO -- which is what anybody chasing it next needs.
        Assert.Equal(plain.Reference, asking.Reference, precision: 10);

        Assert.Equal(plain.Accuracy, asking.Accuracy, precision: 10);
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
