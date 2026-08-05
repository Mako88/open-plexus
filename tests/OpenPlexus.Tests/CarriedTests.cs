using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// What a CARRIED pair is worth against a simultaneous one — <b>the window's
/// standing revival condition, resolved rather than parked.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ROW SAYS <i>followed</i> IS EVIDENCE AS STRONG AS <i>accompanied</i>,
/// AND IT IS NOT.</b> Two codes joined by the window were never in one moment —
/// one had already stopped — so counting that at full weight is the same
/// overstatement <see cref="Learning.Occasion.Weight"/> already refuses for an
/// occasion that was merely concluded.
/// </para>
/// <para>
/// <b>THE ARM COMPARES ON SPEND AND NOTHING ELSE.</b> A discount lowers
/// <c>together</c> and leaves <c>seen</c> alone, so under
/// <see cref="Toll.Evidence"/> — where a hop costs <c>1 / weight</c> — it makes
/// every carried hop dearer and the walk shallower. <b>A shallower walk is
/// something a smaller <c>Stamina</c> also buys</b>, so an arm read at one stamina
/// against another at the same stamina is reading the budget. The plan names this
/// trap and names <c>Toll</c> as its sharpest case; this is the same case.
/// </para>
/// <para>
/// <b>`Rhythm` IS WHERE IT HAS MOST TO LOSE.</b> Nothing here is ever simultaneous
/// with anything, so EVERY edge this world holds is a carried one and the discount
/// touches all of them.
/// </para>
/// </remarks>
public sealed class CarriedTests(ITestOutputHelper output)
{
    private static RhythmSettings World() =>
        new() { Symbols = 12, Period = 5, Violations = 0.1 };

    private const int Moments = 300;

    private static readonly int[] Seeds = [1, 2, 3, 5, 8, 13];

    private static async Task<(double Accuracy, double Messages, int Deepest)> ArmAsync(
        double carried, double stamina)
    {
        double accuracy = 0.0, messages = 0.0;
        var deepest = 0;

        foreach (var seed in Seeds)
        {
            using var run = new RhythmRun(World(), Fixture.Dials(stamina) with { Span = 1, Carried = carried }, seed);

            var result = await run.RunAsync(Moments);

            accuracy += result.Accuracy;
            messages += result.Messages;
            deepest = Math.Max(deepest, result.Plumbing.Deepest);
        }

        return (accuracy / Seeds.Length, messages / Seeds.Length, deepest);
    }

    [Fact]
    public async Task No_discount_reaches_a_point_an_undiscounted_walk_cannot_reach_cheaper()
    {
        // THE GRID, AND IT IS A GRID BECAUSE ONE ROW OF IT LIES. Swept at a fixed
        // stamina the discount reports buying accuracy at three-quarters of the
        // traffic, which is true and is not about the discount: it is a walk that
        // cannot afford to go deep, and a deeper walk for prediction is already
        // refuted as monotonically worse. So both dials move and only the frontier
        // means anything.
        var grid = new List<(double Carried, double Stamina, double Accuracy, double Messages)>();

        foreach (var carried in (double[])[1.0, 0.5, 0.25])
            foreach (var stamina in (double[])[1.5, 2.0, 3.0, 4.0, 8.0, 16.0])
            {
                var (accuracy, messages, _) = await ArmAsync(carried, stamina);
                grid.Add((carried, stamina, accuracy, messages));

                output.WriteLine(
                    $"carried={carried:F2} stamina={stamina,5:F1} "
                    + $"msgs={messages,9:F0} acc={accuracy:F4}");
            }

        var best = grid
            .Where(one => one.Carried >= 1.0)
            .OrderByDescending(one => one.Accuracy)
            .First();

        output.WriteLine(
            $"best undiscounted: stamina={best.Stamina:F1} "
            + $"{best.Accuracy:F4} at {best.Messages:F0} messages");

        // THE REVIVAL CONDITION, AND IT IS NOT MET. "Something that makes a carried
        // edge worth its row" would show up here as a discounted arm reaching at
        // least this accuracy for at most this traffic. Nothing does: the discount
        // moves along the frontier the budget already describes and does not push
        // it outward. The row entry costs exactly what it cost.
        var dominating = grid
            .Where(one => one.Carried < 1.0)
            .Where(one => one.Accuracy >= best.Accuracy && one.Messages <= best.Messages)
            .Select(one => $"carried={one.Carried:F2}@{one.Stamina:F1}")
            .ToList();

        Assert.True(dominating.Count == 0,
            "a discounted arm reached the best undiscounted accuracy for no more "
            + "traffic: " + string.Join(", ", dominating)
            + ". The window's revival condition is met and this row should say so.");
    }

    [Fact]
    public async Task The_wrong_setting_costs_everything_and_the_cliff_it_flattened_is_gone()
    {
        // THIS WAS THE ARM'S ONE REMAINING DEFENCE AND IT NO LONGER HOLDS. The claim
        // was that a dial whose WRONG setting costs nothing is a partial answer to
        // this design's recurring fault of dials wanting different values in
        // different worlds: undiscounted accuracy fell off a cliff as the budget
        // rose, and discounting flattened it.
        //
        // BOTH HALVES ARE GONE. There is no cliff -- the undiscounted arm is flat
        // across a fourfold budget -- and the wrong setting does not cost nothing,
        // it costs everything. What is asserted below is what is actually there.
        var spread = new Dictionary<double, List<double>>();
        var traffic = new Dictionary<double, List<double>>();
        var reach = new Dictionary<double, List<int>>();

        foreach (var carried in (double[])[1.0, 0.5])
        {
            spread[carried] = [];
            traffic[carried] = [];
            reach[carried] = [];

            foreach (var stamina in (double[])[4.0, 8.0, 16.0])
            {
                var (accuracy, messages, deepest) = await ArmAsync(carried, stamina);

                spread[carried].Add(accuracy);
                traffic[carried].Add(messages);
                reach[carried].Add(deepest);
            }

            output.WriteLine(
                $"carried={carried:F2} "
                + $"acc=[{string.Join(" ", spread[carried].Select(one => one.ToString("F4")))}] "
                + $"msgs=[{string.Join(" ", traffic[carried].Select(one => one.ToString("F0")))}] "
                + $"deepest=[{string.Join(" ", reach[carried])}]");
        }

        var plain = spread[1.0].Max() - spread[1.0].Min();

        // THERE IS NO CLIFF LEFT TO FLATTEN, AND THE REASON IS NOT ABOUT THIS DIAL.
        // Undiscounted accuracy was supposed to fall as the budget rose -- the walk
        // affords depth, depth reaches things that merely follow at a distance, and
        // prediction gets worse for thinking harder. Quadrupling the budget now
        // changes NOTHING: 0.8505 at stamina 4, 8 and 16, from the SAME 774 chains
        // at the SAME depth of two. Only the traffic moves, 15,922 to 27,278.
        //
        // BUDGET BUYS MESSAGES AND NOT REACH, which is a fact about the walk rather
        // than about the discount, and snake says the same thing from its own side:
        // depth two at every budget from 8 to 64. The cliff needed extra budget to
        // purchase extra depth and it no longer does.
        Assert.True(plain < 0.01,
            $"the undiscounted arm has become budget-sensitive again ({plain:F4} "
            + "across the sweep), so overspending costs something once more and the "
            + "cliff this dial was said to flatten is back -- which would mean the "
            + "walk has started buying depth with budget");

        // AND THE BUDGET REACHES THE WALK, so the flatness above is the walk's
        // answer and not a dial connected to nothing. Traffic rises by two thirds
        // across the same sweep.
        Assert.True(traffic[1.0][^1] > traffic[1.0][0] * 1.25,
            $"the budget stopped moving the traffic ({traffic[1.0][^1]:F0} against "
            + $"{traffic[1.0][0]:F0}), so this sweep is not reaching the walk at all");

        // AND THE DISCOUNT IS NOT A HARMLESS WRONG SETTING -- IT IS TOTAL. This test
        // existed to say that a dial whose wrong value costs nothing is a partial
        // answer to dials wanting different values in different worlds. On this
        // world the wrong value costs EVERYTHING: nought accuracy at every budget,
        // and `Deepest` nought, meaning not one chain completes.
        //
        // EVERY EDGE HERE IS A CARRIED EDGE -- one symbol a moment, so nothing is
        // ever simultaneous -- so discounting carried edges is a global halving of
        // every weight in the world rather than a discount on a class. The
        // refutation row already says this starves the walk; what it did not say is
        // that it starves it completely and at any budget.
        Assert.All(spread[0.5], one => Assert.Equal(0.0, one, precision: 10));
        Assert.All(reach[0.5], one => Assert.Equal(0, one));
    }

    [Fact]
    public async Task And_below_a_budget_the_plain_walk_manages_the_discounted_one_starves()
    {
        // THE COST, AND IT IS THE STANDING RISK REALISED. Anything that makes a hop
        // dearer starves the route that was already paying its way, and a starved
        // walk predicts NOTHING rather than predicting badly. At a stamina the
        // undiscounted arm walks perfectly well at, the discounted one cannot
        // afford its first hop and the world scores zero.
        var plain = await ArmAsync(carried: 1.0, stamina: 2.0);
        var cut = await ArmAsync(carried: 0.5, stamina: 2.0);

        output.WriteLine($"plain {plain.Accuracy:F4}   discounted {cut.Accuracy:F4}");

        Assert.True(plain.Accuracy > 0.5, $"the plain arm stopped walking too: {plain.Accuracy:F4}");
        Assert.Equal(0.0, cut.Accuracy);
    }
}
