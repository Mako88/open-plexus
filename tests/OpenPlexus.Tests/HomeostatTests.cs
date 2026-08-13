using OpenPlexus.Codes;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The world for step 4, built before step 4.
/// </summary>
/// <remarks>
/// <b>It exists because survival was gameable.</b> Snake scored by staying alive
/// and circling wins that — it lives longest and eats least. Keeping variables in
/// bounds cannot be gamed the same way, because everything falls whether or not
/// anything is done, so standing still is the fastest way to fail rather than the
/// safe option.
/// </remarks>
public sealed class HomeostatTests(ITestOutputHelper output)
{
    private static HomeostatSettings World() => new();

    private const int Steps = 400;

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void The_world_is_arithmetically_capable_and_not_trivially_so()
    {
        // BOTH BOUNDS, OR THE WORLD MEASURES NOTHING. Restoring less than
        // everything falls means nothing could hold it and the ceiling is
        // unreachable; restoring more than the fastest drain times the number of
        // needs means attending at random suffices and the ceiling is free.
        var world = new Homeostat(World());

        Assert.True(world.Restore > world.Falling,
            $"nothing could hold this body: restore {world.Restore} against "
            + $"fall {world.Falling}");

        Assert.True(world.Restore < world.Needs * world.Falls(world.Needs - 1),
            $"attending at random would hold this body, so it discriminates "
            + $"nothing: restore {world.Restore}");
    }

    [Fact]
    public void Everything_falls_whether_or_not_anything_is_done()
    {
        // THE PROPERTY THAT MAKES IDLING COST. Under survival, doing nothing was
        // the strategy; here it is the failure.
        var world = new Homeostat(World());
        var before = world.At.ToList();

        world.Step(null);

        Assert.All(Enumerable.Range(0, world.Needs),
            which => Assert.True(world.At[which] < before[which]));

        // AND THE FASTEST-FALLING ONE FALLS FASTEST, which is what makes spreading
        // attention evenly the wrong thing to do.
        Assert.True(
            before[world.Needs - 1] - world.At[world.Needs - 1] > before[0] - world.At[0]);
    }

    [Fact]
    public void A_drive_is_felt_as_a_band_and_not_read_as_a_number()
    {
        var world = new Homeostat(World());

        var felt = world.Feels();

        Assert.Equal(world.Needs, felt.Length);

        // ONE MODALITY PER VARIABLE, so the graph can tell hunger from thirst
        // without anything downstream knowing which is which.
        Assert.Equal(world.Needs, felt.Select(code => code.Modality).Distinct().Count());
    }

    // ---- what the graph does with it ---------------------------------------

    // ---- step 4's blocker: the front end ------------------------------------

    /// <summary>What a felt state says about magnitude, as a comparable string.</summary>
    private static string Bands(IEnumerable<Code> felt) =>
        string.Join(",", felt.Where(code => code.Modality < Homeostat.Rank)
            .OrderBy(code => code.Modality).Select(code => code.Value));

    /// <summary>What it says about order. See <see cref="Homeostat.Standing"/>.</summary>
    private static string Positions(Homeostat body) =>
        string.Join(",", Enumerable.Range(0, body.Needs).Select(body.Standing));

    [Fact]
    public void A_band_cannot_say_which_is_lowest_and_a_rank_can()
    {
        // THE CEILING, ASSERTED RATHER THAN ARGUED, AND IT IS FORK 25's SHAPE IN A
        // SECOND WORLD. Two states of one body whose variables sit in the SAME
        // bands and in a DIFFERENT order. A front end that emits bands alone emits
        // the identical code set for both, so no amount of counting can separate
        // them -- and which is lowest is the only fact this world turns on.
        var ranked = new Homeostat(World() with { Ranked = true });

        var before = ranked.Feels();
        var wasStanding = Positions(ranked);

        // Everything falls, and attending to the first one puts it back on top.
        // One step is enough because the drains are uneven.
        ranked.Step(attend: 0);

        var after = ranked.Feels();
        var nowStanding = Positions(ranked);

        output.WriteLine($"bands {Bands(before)} -> {Bands(after)}");
        output.WriteLine($"ranks {wasStanding} -> {nowStanding}");

        // SAME BANDS. This is the state a banded front end cannot tell from the
        // one before it.
        Assert.Equal(Bands(before), Bands(after));

        // DIFFERENT ORDER, and it is the ordering that carries the answer.
        Assert.NotEqual(wasStanding, nowStanding);

        // ADDITIVE: the ranks are extra codes and the band codes are untouched, so
        // switching the arm off reproduces every earlier measurement exactly.
        var plain = new Homeostat(World()).Feels();
        Assert.Equal(plain.Length * 2, before.Length);
        Assert.All(plain, code => Assert.Contains(code, before));

        // AND THE ORDERING IS A PERMUTATION, never a near-miss: every position is
        // held by exactly one variable, so the front end cannot emit a rank no
        // variable holds -- a state the graph would learn about and the body can
        // never be in again.
        var standing = Enumerable.Range(0, ranked.Needs).Select(ranked.Standing).ToList();
        Assert.Equal(Enumerable.Range(0, ranked.Needs), standing.Order());
    }

}
