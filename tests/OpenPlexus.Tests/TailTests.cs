using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a skewed co-occurrence distribution does to the bill.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE WORLD SHAPE THE SCALING MEASUREMENT ASKED FOR.</b> Growing the alphabet
/// grew the graph and left the widest row alone, so cost per thought never moved
/// and the row cap never bit. The conclusion was that a wall needs a HEAVY TAIL
/// rather than more nodes — a few codes accompanying nearly everything, which is
/// text's shape and was no world's here. <see cref="SensesSettings.Skew"/> is that
/// shape, and this is what it bought.
/// </para>
/// <para>
/// <b>It is a dial rather than a new world, which is the standing rule.</b> Scale
/// and distribution are changes to a world; the task, the question and the chance
/// level are untouched, and clutter carries its own modality so it can never be an
/// answer. <see cref="SensesSettings.Clutter"/> set that precedent.
/// </para>
/// </remarks>
public sealed class TailTests(ITestOutputHelper output)
{
    private const int Moments = 400;

    private const int Seeds = 8;

    /// <summary>
    /// <b>Zipf's own exponent.</b> Rank frequency in text goes as <c>1/k</c>, and
    /// the point of this file is the shape real data has rather than the most
    /// extreme one the dial reaches.
    /// </summary>
    private const double Tail = 1.5;

    /// <summary>
    /// <b>Large, so the tail has somewhere to live.</b> A pool of four makes every
    /// clutter code ubiquitous whatever the exponent says — the skew needs ranks to
    /// be unequal ACROSS, and two thousand of them is what
    /// <see cref="ClutterTests"/> already measured uniformly.
    /// </summary>
    private const int Pool = 2000;

    /// <summary>One arm, with the cost left attached to the score.</summary>
    private readonly record struct Arm(
        Measured Scored, double Messages, double Nodes, double Widest);

    private async Task<Arm> ArmAsync(string name, double skew, int cap, double doubt = 0.0)
    {
        double messages = 0.0, nodes = 0.0, widest = 0.0;

        var scored = await Sweep.ArmAsync(name, Seeds, async seed =>
        {
            var world = Fixture.Senses(concepts: 12, clutter: 2, pool: Pool, skew: skew);

            using var run = new SensesRun(
                world,
                Fixture.Dials(stamina: 8.0) with { Row = cap, Doubt = doubt },
                seed);

            var result = await run.RunAsync(Moments, every: 10).ConfigureAwait(false);

            messages += result.Messages;
            nodes += result.Nodes;
            widest += result.Widest;

            return result.Accuracy;
        }).ConfigureAwait(false);

        output.WriteLine(
            $"{name,-18} acc={scored.Mean:F4}+-{scored.StdErr:F4} "
            + $"msgs={messages / Seeds,9:F0} nodes={nodes / Seeds,5:F0} "
            + $"widest={widest / Seeds,6:F1}");

        return new Arm(scored, messages / Seeds, nodes / Seeds, widest / Seeds);
    }

    /// <summary>
    /// Four claims off ONE set of runs.
    /// </summary>
    /// <remarks>
    /// <b>Asserts rather than tests, because the runs are the cost</b> — the same
    /// reason <see cref="ScalingTests"/> gives. Four arms at eight seeds is most of
    /// half a minute, and splitting them would take it four times for readings
    /// already in hand.
    /// </remarks>
    [Fact]
    public async Task A_heavy_tail_is_where_the_row_cap_finally_bites()
    {
        var flat = await ArmAsync("flat, free", skew: 0.0, cap: Fixture.Unbounded);
        var flatCapped = await ArmAsync("flat, capped", skew: 0.0, cap: 32);
        var tail = await ArmAsync("tail, free", Tail, cap: Fixture.Unbounded);
        var tailCapped = await ArmAsync("tail, capped", Tail, cap: 32);

        // THE PLAN'S ITEM, ANSWERED. The row cap has been inert in every world
        // this project has built -- `ScalingTests` asserts it, message for message,
        // at every alphabet size. Under a heavy tail it cuts the bill by better
        // than half.
        Assert.True(tailCapped.Messages < tail.Messages / 2.0,
            $"the cap took {tail.Messages:F0} to {tailCapped.Messages:F0}, which is "
            + "no longer the bound a tail is supposed to need");

        // AND IT IS FREE, WHICH IS THE PART THAT MAKES IT WORTH HAVING. Identical
        // to ten places across eight seeds: the walk visits a third of the row and
        // answers every question the same way. Evicting the least recently touched
        // partner removes cost the score was not using.
        Assert.Equal(tail.Scored.Mean, tailCapped.Scored.Mean, precision: 10);

        // THE CONTROL, AND IT IS WHY THIS IS ABOUT THE TAIL RATHER THAN THE CAP.
        // The same cap on the same world with the same clutter, drawn UNIFORMLY,
        // barely engages -- the widest row sits near the cap instead of five times
        // past it, so almost nothing is ever evicted.
        Assert.True(flatCapped.Messages > flat.Messages * 0.9,
            $"the cap now bites without a tail too ({flat.Messages:F0} to "
            + $"{flatCapped.Messages:F0}), so the skew is not what makes it bind "
            + "and this file is measuring the cap rather than the distribution");

        // AND THE AXIS IS NOT MERELY CHEAP, IT IS INVERTED. `ScalingTests` found
        // node count nearly free: sixty-four times the concepts, same widest row.
        // This is the other half and it is sharper -- the tail has a THIRD of the
        // nodes and better than TWICE the bill. A graph that grows by meeting more
        // things gets cheaper per thought; one that grows by meeting the same
        // things more unequally does not.
        Assert.True(tail.Nodes < flat.Nodes && tail.Messages > flat.Messages,
            $"nodes {flat.Nodes:F0}->{tail.Nodes:F0} and messages "
            + $"{flat.Messages:F0}->{tail.Messages:F0} no longer move in opposite "
            + "directions, so node count is back to being merely cheap");
    }

    [Fact]
    public async Task The_cap_and_doubt_do_not_repair_the_same_edge()
    {
        // A PREDICTION MADE FROM THE EVICTION RULE, AND REFUTED. Eviction drops the
        // LEAST RECENTLY TOUCHED entry; `Doubt` disbelieves the THINLY EVIDENCED
        // one. Under a heavy tail those looked like the same population -- a code
        // seen once, long ago -- so the cap should have left `Doubt` nothing to do.
        //
        // IT LEAVES IT NEARLY AS MUCH TO DO, AND "EXACTLY" NO LONGER HOLDS. The
        // entries `Doubt` corrects mostly SURVIVE the cap -- a mid-rank code under
        // Zipf is touched recently AND evidenced thinly, and recency cannot see
        // that -- but the two are not the perfectly separate populations this test
        // used to assert to ten decimal places.
        //
        //   arm                accuracy      msgs    widest
        //   free               0.775641   929,707     203.1
        //   free, doubted      0.820513   933,759     203.1
        //   capped             0.775641   271,485      32.0
        //   capped, doubted    0.814103   271,178      32.0
        //
        // THE STRONGER HALF WAS NEVER BEING ASSERTED AND NOW IS: the cap on its own
        // is FREE. Same accuracy to ten places against a third of the traffic, which
        // is the entire case for bounding the row and is a sharper claim than the
        // orthogonality one. What the cap costs, it costs only in company with
        // `Doubt`: 0.0385 of lift where the uncapped graph gets 0.0449, so a seventh
        // of what `Doubt` repairs is evicted before it can repair it.
        var free = await ArmAsync("free", Tail, cap: Fixture.Unbounded);
        var doubted = await ArmAsync("free, doubted", Tail, cap: Fixture.Unbounded, doubt: 8.0);
        var capped = await ArmAsync("capped", Tail, cap: 32);
        var both = await ArmAsync("capped, doubted", Tail, cap: 32, doubt: 8.0);

        var alone = doubted.Scored.Mean - free.Scored.Mean;
        var after = both.Scored.Mean - capped.Scored.Mean;

        output.WriteLine($"doubt buys {alone:F4} free, {after:F4} capped");
        output.WriteLine(
            $"free {free.Scored.Mean:F6} doubted {doubted.Scored.Mean:F6} "
            + $"capped {capped.Scored.Mean:F6} both {both.Scored.Mean:F6}");
        output.WriteLine(
            $"widest free {free.Widest:F1} capped {capped.Widest:F1} "
            + $"-- does the cap bite? {(Math.Abs(free.Widest - capped.Widest) > 0.5 ? "YES" : "NO")}");

        // THE LIFT IS REAL, or the equality below is two nothings agreeing.
        Assert.True(alone > 0.0,
            $"`Doubt` stopped paying on the tail at all ({alone:F4}), so this "
            + "test compares two absences and says nothing about either");

        // THE CAP ALONE IS FREE, TO TEN PLACES. This is the claim worth holding and
        // it was never held before -- eviction throws away two thirds of the traffic
        // and does not cost one answer.
        Assert.Equal(free.Scored.Mean, capped.Scored.Mean, precision: 10);

        Assert.True(capped.Messages < free.Messages * 0.5,
            $"the cap stopped paying for itself: {capped.Messages:F0} against "
            + $"{free.Messages:F0}");

        // AND MOST OF WHAT `Doubt` REPAIRS SURVIVES IT. Not all, which is the part
        // that changed: exact equality is refuted, so the bar is what the overlap
        // actually is rather than a claim that there is none.
        Assert.True(after > alone * 0.8,
            $"the cap now eats most of what `Doubt` repairs ({after:F4} capped "
            + $"against {alone:F4} free), so they are no longer near-orthogonal and "
            + "the two populations have merged");

        // AND THEY ARE NOT IDENTICAL EITHER, WHICH IS THE REFUTATION ASSERTED. If
        // this fails the populations have separated again and the orthogonality row
        // wants re-opening rather than this bar wants moving.
        Assert.NotEqual(alone, after, precision: 10);
    }
}
