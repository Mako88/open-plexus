using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Fork 18 — <b>does the graph model its own effect on the world, once it has an
/// edge that can mean <i>then</i>?</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>IT WAS BLOCKED ON TEMPORAL EDGES AND THE BLOCKER IS GONE.</b>
/// <see cref="SnakeFrame.Did"/> is the move already taken and the view is the
/// world after it, but written as one flat occasion they became a <c>With</c>
/// pair — so the graph recorded that the view ACCOMPANIED the action, which is
/// indistinguishable from the view having been there when the action was chosen.
/// A walk from an action reached whatever co-occurred with it, and *what will the
/// world look like if I do X* was not a question the row could answer.
/// </para>
/// <para>
/// <b>The measurement is <see cref="Consequence"/>'s and is untouched.</b> Same
/// three arms, same prequential scoring. Only what the front end says about order
/// and what the prediction asks for change.
/// </para>
/// </remarks>
public sealed class Fork18Tests(ITestOutputHelper output)
{
    private static SnakeSettings World() => Fixture.Snake(energy: 80.0);

    private static WalkSettings Dials() => Fixture.Dials(foresight: 2.0);

    private const int Steps = 400;

    [Fact]
    public async Task An_action_said_to_come_first_is_recorded_as_coming_first()
    {
        // THE WIRING, BEFORE ANY CLAIM ABOUT WHAT IT BUYS. A dial declared,
        // documented, passed everywhere and connected to nothing is a named trap
        // here, and it has already caught one arm that survived three
        // measurements.
        using var flat = new SnakeRun(World(), Dials(), seed: 3);
        using var ordered = new SnakeRun(World(), Dials(), seed: 3, kinds: true);

        await flat.PlayAsync(120);
        await ordered.PlayAsync(120);

        // With no order said, nothing is temporal and every cell is `With`.
        Assert.Equal(0, Temporal(flat));

        // With the action said to come first, the action-to-view pairs are.
        Assert.True(Temporal(ordered) > 0,
            "the front end said the action came first and no temporal cell exists");
    }

    [Fact]
    public async Task Asking_what_follows_is_measured_against_asking_what_accompanies()
    {
        // THE EXPERIMENT. `Moved` is the honest number: how much further naming a
        // DIFFERENT action moves the prediction than asking the same question
        // twice does. The jitter floor is what makes it readable at all --
        // delivery is concurrent, so two identical broadcasts already land in
        // different places, and three earlier attempts to answer this failed for
        // want of that third arm.
        // SEVERAL SEEDS, BECAUSE A SMALL SAMPLE CAN LOOK LIKE A MECHANISM. That
        // is a named trap here — one seed with a collapsing echo read as a
        // discovery and turned out to be three questions — and a single snake
        // run supplies only as many questions as it survives steps.
        var moved = new Dictionary<bool, List<double>> { [false] = [], [true] = [] };
        var gap = new Dictionary<bool, List<double>> { [false] = [], [true] = [] };
        var asked = new Dictionary<bool, int> { [false] = 0, [true] = 0 };

        foreach (var seed in (int[])[3, 7, 11, 17, 23])
            foreach (var kinds in new[] { false, true })
            {
                using var run = new SnakeRun(World(), Dials(), seed, kinds: kinds);
                var result = await run.PlayAsync(Steps);

                moved[kinds].Add(result.Consequence.Moved);
                gap[kinds].Add(result.Consequence.Gap);
                asked[kinds] += result.Consequence.Asked;

                output.WriteLine(
                    $"seed={seed} kinds={kinds} asked={result.Consequence.Asked} " +
                    $"knowing={result.Consequence.Knowing:F4} " +
                    $"counterfactual={result.Consequence.Counterfactual:F4} " +
                    $"gap={result.Consequence.Gap:F4} " +
                    $"apart={result.Consequence.Apart:F4} " +
                    $"echoed={result.Consequence.Echoed:F4} " +
                    $"moved={result.Consequence.Moved:F4} " +
                    $"steps={result.Steps} temporal={run.TemporalCells} " +
                    $"msgs={result.Messages}");
            }

        output.WriteLine(
            $"MEAN moved off={moved[false].Average():F4} on={moved[true].Average():F4} | " +
            $"gap off={gap[false].Average():F4} on={gap[true].Average():F4} | " +
            $"asked off={asked[false]} on={asked[true]}");

        // WITHOUT TEMPORAL EDGES THE ACTION IS NOT IN THE WALK AT ALL, and that
        // is the standing finding rather than a hoped-for baseline: naming a
        // different action produces the identical prediction, so the graph is
        // predicting the next frame regardless of what the body does.
        Assert.All(moved[false], one => Assert.Equal(0.0, one, 6));
        Assert.All(gap[false], one => Assert.Equal(0.0, one, 6));

        // AND WITH THEM IT IS, ON EVERY SEED RATHER THAN ON THE MEAN. The mean
        // was what this was written to assert; five out of five is the stronger
        // claim and it is the one that was measured, so it is the one asserted.
        Assert.All(moved[true], one => Assert.True(one > 0.0, $"moved {one}"));
        Assert.All(gap[true], one => Assert.True(one > 0.0, $"gap {one}"));

        // AND THE HONEST SHAPE OF IT: the gap opens because naming a FALSE
        // action predicts worse, not because naming the true one predicts
        // better. That is discrimination rather than improved foresight, and
        // saying so here stops the number being read as the latter.
        Assert.True(asked[true] < asked[false],
            "the temporal arm asked at least as many questions, so the caveat "
            + "about a smaller sample no longer applies and should be removed");
    }

    /// <summary>
    /// How many temporal cells the run's graph holds.
    /// </summary>
    /// <remarks>
    /// <b>Counted from the row rather than inferred from a score</b>, because
    /// "the arm did nothing" and "the arm was never connected" are different
    /// findings and only one of them is worth reporting.
    /// </remarks>
    private static int Temporal(SnakeRun run) => run.TemporalCells;
}
