using OpenPlexus.Graph;
using OpenPlexus.Worlds;

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
public sealed class Fork18Tests
{
    private static SnakeSettings World() => Fixture.Snake(energy: 80.0);

    private static WalkSettings Dials() => Fixture.Dials(foresight: 2.0);

    private const int Steps = 400;
    [Fact]
    public async Task An_action_said_to_come_first_is_recorded_as_coming_first()
    {
        // THE WIRING, AND IT IS ALL THAT IS LEFT TO ASSERT. A dial declared,
        // documented, passed everywhere and connected to nothing is a named trap
        // here, and it has already caught one arm that survived three
        // measurements — so the temporal cell is checked for directly.
        using var ordered = new SnakeRun(World(), Dials(), seed: 3);

        await ordered.PlayAsync(120);

        Assert.True(Temporal(ordered) > 0,
            "the front end said the action came first and no temporal cell exists");
    }

    // ---- WHAT THE ARM MEASURED, AND WHY THE MEASUREMENT IS GONE ------------
    //
    // `Asking_what_follows_is_measured_against_asking_what_accompanies` stood here
    // and ran twelve seeds against BOTH arms — the action ordered before the view,
    // and the flat occasion where it was not. Edge kinds became unconditional on
    // 2026-08-04 — John's rule, you build it and it is ON — so the flat arm does
    // not exist and the comparison is not expressible.
    //
    // WHAT IT ESTABLISHED, which is what closed this fork:
    //
    //   * WITH THE ORDER SAID, a walk from an action reaches what FOLLOWED it, and
    //     naming a different action moves the prediction further than asking the
    //     same question twice does. Without it every cell was `With`, so the walk
    //     reached whatever merely co-occurred and the prediction did not move.
    //
    //   * THE JITTER FLOOR WAS WHAT MADE IT READABLE. Delivery is concurrent, so
    //     two identical broadcasts already land in different places; three earlier
    //     attempts to answer this failed for want of that third arm.
    //
    //   * TWELVE SEEDS, BECAUSE A SMALL SAMPLE CAN LOOK LIKE A MECHANISM. At five,
    //     survival matched or beat the baseline on four and one seed looked like an
    //     outlier worth explaining. At twelve, survival was WORSE on five, twice
    //     severely — the outlier was the sample and not the seed. THE ORDER COSTS
    //     SURVIVAL AND BUYS PREDICTION, and that trade is the honest reading.
    //
    // The fork table records this fork as answered by edge kinds.

    private static int Temporal(SnakeRun run) => run.TemporalCells;
}
