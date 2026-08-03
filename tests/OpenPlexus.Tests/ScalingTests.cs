using OpenPlexus.Graph;
using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// How the cost of a run grows with the length of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>MEASURED 2026-08-03, AND IT ANSWERED A QUESTION NOBODY HAD ASKED
/// PROPERLY.</b> The worry was that the graph gets too big. It does not: the
/// binding world holds forty-eight nodes at every run length, and the arm with
/// thirty-four times as many nodes carries a THIRD of the messages. <b>Cost is
/// set by the widest row, never by the node count</b>, because
/// <see cref="Node.Fire"/> snapshots the whole row and emits one message per
/// surviving partner.
/// </para>
/// <para>
/// <b>And on a fixed alphabet the walk already brakes itself, which was not
/// expected.</b> As a graph densifies, <c>together / seen</c> weakens, so under
/// inverse cost every hop gets dearer and routes die sooner — messages per scene
/// FALL with run length even as the graph grows. That is what this asserts,
/// because it is the property that would disappear silently.
/// </para>
/// </remarks>
public sealed class ScalingTests
{
    /// <summary>
    /// Stamina 12. <b>The same depth the binding measurements use</b> — a
    /// shallower walk would not fan out enough for any of this to be visible.
    /// </summary>
    private const double Deep = 12.0;

    private const int Repeats = 3;

    private static WalkSettings Dials(Pricing pricing) =>
        Fixture.Dials(Deep) with { Pricing = pricing };

    /// <summary>
    /// Both claims off ONE set of runs.
    /// </summary>
    /// <remarks>
    /// <b>Two asserts rather than two tests, because the runs are the cost.</b>
    /// Splitting them measured the longest arm twice and put half a minute on the
    /// suite for a second reading of a number already in hand.
    /// </remarks>
    [Fact]
    public async Task On_a_fixed_alphabet_the_walk_brakes_itself()
    {
        var early = await CostAsync(100);
        var middle = await CostAsync(200);
        var late = await CostAsync(400);

        // THE BRAKE, AND IT IS WHY NODE COUNT IS THE WRONG THING TO WORRY ABOUT.
        // A world whose alphabet cannot grow reaches a complete graph and stops:
        // four times the scenes buys nowhere near four times the fan-out.
        Assert.True(late.Widest < middle.Widest * 2,
            $"widest row went {early.Widest} to {middle.Widest} to {late.Widest}, "
            + "which is not the saturation inverse cost is supposed to produce");

        // THE MECHANISM BEHIND IT. Weights are `together / seen`, so a node that
        // has met everything scores every partner weakly -- and a weak edge costs
        // `1/weight` to walk. The walk therefore gets SHORTER as the graph fills
        // in, with nothing set to make it happen. If this ever reverses, the
        // economy has stopped self-limiting and every long run is on its own.
        Assert.True(late.PerScene < early.PerScene,
            $"{late.PerScene:F0} messages a scene at 400 against "
            + $"{early.PerScene:F0} at 100");
    }

    /// <summary>Messages per scene, and the widest row, averaged over seeds.</summary>
    /// <remarks>
    /// <b>The plain binding world under receiver pricing</b> — no grouping, no
    /// index, a fixed alphabet. Every number taken before the lift was taken
    /// here, and it is the densest graph this project builds.
    /// </remarks>
    private static async Task<(double PerScene, int Widest)> CostAsync(int scenes)
    {
        long messages = 0;
        var widest = 0;

        for (var seed = 1; seed <= Repeats; seed++)
        {
            using var run = new BindingRun(
                Fixture.Binding(), Dials(Pricing.Receiver), Seeds.Apart(seed, 0x5CA1));

            var result = await run.RunAsync(scenes, every: 10).ConfigureAwait(false);

            messages += result.Messages;
            widest = Math.Max(widest, result.Widest);
        }

        return (messages / (double)Repeats / scenes, widest);
    }
}
