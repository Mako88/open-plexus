using OpenPlexus.Graph;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The outstanding half of the recurring fault: <b>the row entry that ranks a
/// partner AND prices the hop to it.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="WalkSettings.Doubt"/> SPLIT THE ARITHMETIC AND LEFT THE
/// STATISTIC.</b> What an edge is believed and what it costs became two
/// expressions, and both still read <c>together / seen</c> — so evidence still
/// set the budget, and no result under either could be attributed to one job
/// alone. <see cref="Toll.Traffic"/> charges from the row's WIDTH instead, which
/// is what the budget is actually spent on.
/// </para>
/// <para>
/// <b>COMPARING THE TWO AT ONE STAMINA WOULD BE MEASURING STAMINA.</b> Under
/// <see cref="Toll.Evidence"/> the dial counts perfect hops; under
/// <see cref="Toll.Traffic"/> it counts bits, and a row of eleven costs 4.46 of
/// them. Held at 8 the traffic arm buys one hop where the control buys eight,
/// so a straight swap measures depth and reports it as pricing — which is the
/// live trap about a dial measured at one setting of another, in its purest
/// form.
/// </para>
/// <para>
/// <b>SO THE ARMS ARE MATCHED ON WHAT THEY SPEND.</b> Each toll is swept up its
/// own stamina ladder, and the reading is accuracy against messages: the
/// question a price dial can answer is <i>what does a thousand messages buy</i>,
/// and nothing else.
/// </para>
/// </remarks>
public sealed class TollTests(ITestOutputHelper output)
{
    /// <summary>
    /// Stamina ladders that land the two arms in the same spending range.
    /// </summary>
    /// <remarks>
    /// <b>The traffic arm's ladder is longer because its unit is smaller.</b> A
    /// hop priced in bits costs three to five where a well-evidenced hop priced
    /// inversely costs one or two, so equal numbers on the two dials are not
    /// comparable and equal SPEND is what the table is read on.
    /// </remarks>
    private static readonly double[] Evidence = [2.0, 4.0, 8.0, 12.0];

    /// <summary>
    /// <b>THE LADDER IS SHORT BECAUSE BOTH TOLLS RUN AWAY AT DEPTH, and that is
    /// not a fact about this dial.</b> A hop costs around three under either on a
    /// row of six, so a stamina in the thirties buys nine hops of a fan-out of
    /// six under BOTH — tens of millions of messages, and the run never lands.
    /// The comparison lives where the walk is affordable.
    /// </summary>
    private static readonly double[] Traffic = [4.0, 8.0, 12.0, 16.0];

    private sealed record Point(string Arm, double Stamina, double Accuracy, long Messages);

    /// <summary>
    /// <b>The reading, and it is a curve rather than a number.</b>
    /// </summary>
    /// <remarks>
    /// <b>Reported and not asserted on, deliberately.</b> Which toll wins is what
    /// this is for finding out; an assertion written before the first run would be
    /// the answer decided in advance. What IS asserted is that both arms still
    /// answer above chance at every rung — a toll that starved the walk everywhere
    /// would be a bound, not a price.
    /// </remarks>
    [Fact]
    public async Task What_a_thousand_messages_buys_under_each_toll()
    {
        var points = new List<Point>();

        foreach (var stamina in Evidence)
            points.Add(await SensesAsync("evidence", Toll.Evidence, stamina));

        foreach (var stamina in Traffic)
            points.Add(await SensesAsync("traffic", Toll.Traffic, stamina));

        output.WriteLine($"{"arm",-10} {"stamina",8} {"acc",8} {"msgs",10} {"acc/kmsg",10}");

        foreach (var point in points)
            output.WriteLine(
                $"{point.Arm,-10} {point.Stamina,8:F1} {point.Accuracy,8:F4} "
                + $"{point.Messages,10} {point.Accuracy / (point.Messages / 1000.0),10:F4}");

        Reaches(points, chance: 0.0833);
    }

    /// <summary>
    /// The world with the widest rows on the board — <b>where a toll priced in
    /// row width should differ most from one priced in evidence.</b>
    /// </summary>
    /// <remarks>
    /// <b>AND WHERE STEP 3'S OPEN QUESTION LIVES.</b> A minted node is a hub by
    /// construction and <see cref="Pricing.Receiver"/> refuses hubs, which is the
    /// unverified reading of why chunking costs a little accuracy. `Motif` is the
    /// world chunking was measured on, so it is where the two dials can be put
    /// together.
    /// </remarks>
    [Fact]
    public async Task And_the_same_on_the_world_with_the_widest_rows()
    {
        var points = new List<Point>();

        // SHORT LADDERS ON BOTH SIDES, AND THE REASON IS THE TOLL ITSELF.
        // `Motif` has a widest row of thirty-six and plenty of narrow ones, and a
        // traffic-priced hop through a narrow row costs about two — so a stamina
        // of twenty buys ten hops through the sparse part of this world and the
        // run does not land. That is the dial doing what it is for and it is also
        // why the ladder stops here: see the note on `Toll.Traffic` about stamina
        // no longer capping depth uniformly.
        foreach (var stamina in new[] { 2.0, 4.0, 6.0, 8.0 })
            points.Add(await MotifAsync("evidence", Toll.Evidence, stamina));

        foreach (var stamina in new[] { 4.0, 6.0, 8.0, 10.0 })
            points.Add(await MotifAsync("traffic", Toll.Traffic, stamina));

        output.WriteLine($"{"arm",-10} {"stamina",8} {"acc",8} {"msgs",10}");

        foreach (var point in points)
            output.WriteLine(
                $"{point.Arm,-10} {point.Stamina,8:F1} {point.Accuracy,8:F4} {point.Messages,10}");

        Reaches(points, chance: 0.0345);
    }

    /// <summary>
    /// Each toll answers above chance <b>somewhere on its ladder</b>.
    /// </summary>
    /// <remarks>
    /// <b>PER ARM AND NOT PER RUNG, BECAUSE A RUNG IS ALLOWED TO BE TOO SHALLOW.</b>
    /// The bottom of the evidence ladder scores nothing at all on `Senses` — a walk
    /// with two hops of budget reaches almost nowhere, which is the fork 20 finding
    /// and not a defect in a toll. What would be a real failure is a toll that
    /// cannot answer at ANY depth, which is a bound wearing a price's clothes.
    /// </remarks>
    private static void Reaches(IReadOnlyList<Point> points, double chance)
    {
        foreach (var arm in points.GroupBy(point => point.Arm))
            Assert.True(arm.Max(point => point.Accuracy) > chance,
                $"{arm.Key} never clears chance at any depth on its ladder");
    }

    private static async Task<Point> SensesAsync(string arm, Toll toll, double stamina)
    {
        using var run = new SensesRun(
            Fixture.Senses(concepts: 12),
            Fixture.Dials(stamina) with { Toll = toll },
            seed: 3);

        var result = await run.RunAsync(300, every: 10).ConfigureAwait(false);
        return new Point(arm, stamina, result.Accuracy, result.Messages);
    }

    private static async Task<Point> MotifAsync(string arm, Toll toll, double stamina)
    {
        using var run = new MotifRun(
            new MotifSettings { Symbols = 60, Motifs = 6, Size = 4, Density = 0.5 },
            Fixture.Dials(stamina) with { Toll = toll },
            seed: 3);

        var result = await run.RunAsync(300, every: 10).ConfigureAwait(false);
        return new Point(arm, stamina, result.Accuracy, result.Messages);
    }
}
