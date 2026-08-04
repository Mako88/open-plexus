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

    private static async Task<(double Accuracy, double Messages)> ArmAsync(
        double carried, double stamina)
    {
        double accuracy = 0.0, messages = 0.0;

        foreach (var seed in Seeds)
        {
            using var run = new RhythmRun(
                World(), Fixture.Dials(stamina), seed, span: 1, carried: carried);

            var result = await run.RunAsync(Moments);

            accuracy += result.Accuracy;
            messages += result.Messages;
        }

        return (accuracy / Seeds.Length, messages / Seeds.Length);
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
                var (accuracy, messages) = await ArmAsync(carried, stamina);
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
    public async Task What_it_does_buy_is_a_dial_that_stops_punishing_a_wrong_setting()
    {
        // AND THIS IS WHY THE ARM IS NOT SIMPLY DELETED. Undiscounted, accuracy
        // falls off a cliff as the budget rises -- the walk can afford depth, depth
        // reaches more things that merely follow at a distance, and prediction gets
        // worse the more it is allowed to think. Discounted, the same sweep is
        // FLAT: the extra budget buys hops the walk then ranks too low to matter.
        //
        // A DIAL WANTING DIFFERENT VALUES IN DIFFERENT WORLDS IS THIS DESIGN'S
        // RECURRING FAULT, and a dial whose wrong setting costs nothing is a
        // partial answer to it. It is not the row's revival condition and should
        // not be recorded as one.
        var spread = new Dictionary<double, List<double>>();

        foreach (var carried in (double[])[1.0, 0.5])
        {
            spread[carried] = [];

            foreach (var stamina in (double[])[4.0, 8.0, 16.0])
            {
                var (accuracy, _) = await ArmAsync(carried, stamina);
                spread[carried].Add(accuracy);
            }

            output.WriteLine(
                $"carried={carried:F2} "
                + $"{string.Join(" ", spread[carried].Select(one => one.ToString("F4")))} "
                + $"range={spread[carried].Max() - spread[carried].Min():F4}");
        }

        var plain = spread[1.0].Max() - spread[1.0].Min();
        var cut = spread[0.5].Max() - spread[0.5].Min();

        Assert.True(plain > 0.05,
            $"overspending stopped costing the undiscounted arm ({plain:F4} across "
            + "the sweep), so there is no cliff here to flatten and this test is "
            + "measuring nothing");

        Assert.True(cut < plain / 4.0,
            $"the discounted arm is now as budget-sensitive as the plain one "
            + $"({cut:F4} against {plain:F4})");
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
