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
        // IT LEAVES IT EXACTLY AS MUCH TO DO. `Doubt` buys the same lift with the
        // cap on as with it off, to ten places, so the two are orthogonal: the
        // entries `Doubt` corrects SURVIVE the cap. A mid-rank code under Zipf is
        // touched recently AND evidenced thinly, and recency cannot see that.
        var free = await ArmAsync("free", Tail, cap: Fixture.Unbounded);
        var doubted = await ArmAsync("free, doubted", Tail, cap: Fixture.Unbounded, doubt: 8.0);
        var capped = await ArmAsync("capped", Tail, cap: 32);
        var both = await ArmAsync("capped, doubted", Tail, cap: 32, doubt: 8.0);

        var alone = doubted.Scored.Mean - free.Scored.Mean;
        var after = both.Scored.Mean - capped.Scored.Mean;

        output.WriteLine($"doubt buys {alone:F4} free, {after:F4} capped");

        // THE LIFT IS REAL, or the equality below is two nothings agreeing.
        Assert.True(alone > 0.0,
            $"`Doubt` stopped paying on the tail at all ({alone:F4}), so this "
            + "test compares two absences and says nothing about either");

        Assert.Equal(alone, after, precision: 10);
    }
}
