using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Whether the ranking dial can be dissolved rather than moved.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE DECISION THIS IS FOR.</b> <c>Accumulate.Agreement</c> is right on a
/// conjunction and harmful on an indexed question, so it cannot be promoted to a
/// default — and the recorded plan was to move it onto the question, beside the
/// grouping that already travels there. That works only if the asker knows which
/// kind of question it is asking, and it adds a thing every caller must now say.
/// </para>
/// <para>
/// <b>John's alternative: fuse the two orders by POSITION and let nobody say
/// anything.</b> If reciprocal rank fusion matches agreement where agreement wins
/// and matches strength where agreement loses, the dial has no reason to exist —
/// which is a better outcome than moving it, and the standing rule prefers fusing
/// the arms over sweeping them.
/// </para>
/// <para>
/// <b>The two worlds that disagree are the whole test.</b> Composition needs
/// agreement; binding is hurt by it. An arm that wins one and loses the other has
/// settled nothing.
/// </para>
/// </remarks>
public sealed class FusionTests(ITestOutputHelper output)
{
    private const int Repeats = 4;

    // ---- the world that NEEDS agreement ------------------------------------

    private static ComposedSettings Composing => new()
    {
        Values = 24, CodesPerValue = 3, Segmented = true, Tagged = true,
    };

    private static WalkSettings Composed(Accumulate ranking) =>
        Fixture.Dials(stamina: 8.0) with { Pricing = Pricing.Sender, Accumulate = ranking };

    private static Task<Measured> ComposingAsync(Accumulate ranking) =>
        Sweep.ArmAsync($"{ranking}", Repeats, async seed =>
        {
            using var run = new ComposedRun(Composing, Composed(ranking), seed);
            return (await run.RunAsync(400, Refer.Narrowed, every: 10).ConfigureAwait(false))
                .Accuracy;
        });

    // ---- the world that agreement HURTS ------------------------------------

    private static BindingSettings Binding => new()
    {
        Concepts = 8, CodesPerAttribute = 3, Bound = false,
        Segmented = true, Tagged = true, Fleeting = true,
    };

    private static Task<Measured> BindingAsync(Accumulate ranking) =>
        Sweep.ArmAsync($"{ranking}", Repeats, async seed =>
        {
            using var run = new BindingRun(
                Binding,
                Fixture.Dials(stamina: 12.0) with { Pricing = Pricing.Sender, Accumulate = ranking },
                seed);

            return (await run.RunAsync(400, every: 10).ConfigureAwait(false)).Accuracy;
        });

    [Fact]
    public async Task Fusing_recovers_only_part_of_what_agreement_wins()
    {
        var strength = await ComposingAsync(Accumulate.Sum);
        var agreement = await ComposingAsync(Accumulate.Agreement);
        var fused = await ComposingAsync(Accumulate.Fused);

        output.WriteLine("composition — the world that needs agreement");
        output.WriteLine(Sweep.Table([strength, agreement, fused]));

        // IT DOES LIFT, which is why this is a refutation rather than a null.
        Assert.True(fused.Mean > strength.Mean,
            $"fusing did nothing at all: {fused} against {strength}");

        // AND IT GIVES MOST OF THE LIFT BACK, which is the half that matters.
        Assert.True(fused.Mean < agreement.Mean,
            $"fusing matched agreement here, so the dial might dissolve after all: "
            + $"{fused} against {agreement}");
    }

    [Fact]
    public async Task And_pays_the_whole_of_what_agreement_costs()
    {
        var strength = await BindingAsync(Accumulate.Sum);
        var agreement = await BindingAsync(Accumulate.Agreement);
        var fused = await BindingAsync(Accumulate.Fused);

        output.WriteLine("binding — the world agreement hurts");
        output.WriteLine(Sweep.Table([strength, agreement, fused]));

        // THE VERDICT. Half the benefit and all of the cost is not the best of
        // both worlds; it is the worst of them, and it settles the open decision
        // in favour of moving the ranking onto the question after all.
        Assert.True(fused.Mean < strength.Mean,
            $"fusing did not cost what agreement costs: {fused} against {strength}");

        Assert.Equal(agreement.Mean, fused.Mean, 6);
    }

    [Fact]
    public void Two_candidates_and_two_orders_that_invert_tie_under_fusion_for_any_damping()
    {
        // WHY IT FAILED, AS ARITHMETIC RATHER THAN AS A MEASUREMENT — and this is
        // the part that makes the refutation general instead of a fact about one
        // world.
        //
        // Reciprocal rank fusion scores a candidate by the sum of 1/(k + rank)
        // over the orders. When there are exactly two candidates and the two
        // orders disagree, one candidate is ranked (0, 1) and the other is ranked
        // (1, 0) — so both score 1/k + 1/(k+1), EXACTLY, and no choice of k
        // separates them. The tie then falls to whatever breaks ties, which is
        // chain length and code order: an answer nobody chose.
        //
        // A binding question offers exactly two candidates and the two rankings
        // disagree on precisely the questions that are hard. So fusion is at its
        // most degenerate exactly where it was needed.
        foreach (var damping in new[] { 1.0, 5.0, 60.0, 1000.0 })
        {
            var one = (1.0 / (damping + 0)) + (1.0 / (damping + 1));
            var other = (1.0 / (damping + 1)) + (1.0 / (damping + 0));

            Assert.Equal(one, other, 12);
        }
    }

    [Fact]
    public async Task Fusing_is_ranking_only_and_leaves_the_traffic_untouched()
    {
        // THE RECURRING FAULT'S OWN DETECTOR, APPLIED TO THE NEW ARM. A ranking
        // dial must leave the message count EXACTLY unchanged on a fixed seed --
        // same walk, same places, different mind about what it found. See
        // DialTests: this is how `Doubt` was caught doing two jobs.
        var plain = Fixture.Dials(stamina: 8.0);

        using var sum = new SensesRun(Fixture.Senses(concepts: 12), plain, seed: 3);
        using var fused = new SensesRun(
            Fixture.Senses(concepts: 12), plain with { Accumulate = Accumulate.Fused }, seed: 3);

        var one = await sum.RunAsync(300, every: 10);
        var other = await fused.RunAsync(300, every: 10);

        Assert.Equal(one.Messages, other.Messages);
    }
}
