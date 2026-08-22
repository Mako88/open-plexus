using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// How concentrated the commitments that DECIDE are — <b>a question <see cref="Vote.By"/>
/// raised and nothing had answered.</b>
/// </summary>
/// <param name="output">Where the rows go.</param>
/// <remarks>
/// <para>
/// <b>Its own remark puts it as a fork in the road.</b> Either the deciders are the same
/// handful whatever the population does, or two runs differing by a sixth in residents and
/// returning identical withheld scores is a coincidence worth the same surprise. Every other
/// instrument here counts the POPULATION, and under the vote an expectation is worth its best
/// advocate and no more — so a population can be reshuffled at length while the same few
/// answer every question, and nothing would show it.
/// </para>
/// <para>
/// <b>The answer is the world's rather than the machine's</b>, which is what neither arm of
/// that fork expected. A world a handful of rules reach puts the same advocate in front of
/// round after round; a world reached by hundreds puts a nearly fresh one each time.
/// </para>
/// <para>
/// <b>And it is what priced nesting out.</b> A commitment's identity in a moment is a code a
/// scope can root on, and a scope holding one fires as often as that identity recurs — so the
/// spine world was the worst possible place to build it. See the plan's revival row.
/// </para>
/// </remarks>
public sealed class DecidingTests(ITestOutputHelper output)
{
    /// <summary>The spine world, translated as its own grids translate it.</summary>
    /// <param name="seed">What draws the houses and the walks.</param>
    private static IInput Roams(int seed) =>
        new Watching<Coded>(
            new Roaming(Fixture.House(Examining.Where), seed),
            new Joined(Joining.Resolved, resolution: 3, freshest: true),
            acting: Chooses.From(_ => null));

    /// <summary>The narrow world, whose answer a handful of rules reach.</summary>
    /// <param name="seed">What draws the addresses.</param>
    private static IInput Multiplexes(int seed) =>
        new Watching<IReadOnlyList<int>>(
            new Multiplexer(new MultiplexerSettings { Address = 2 }, seed),
            new Bits(Multiplexer.Bit));

    /// <summary>How often each commitment was the one whose claim held.</summary>
    /// <param name="world">What pushes moments.</param>
    /// <param name="brain">What answers them.</param>
    /// <param name="rounds">How many.</param>
    /// <remarks>
    /// <b>Driven directly rather than through <see cref="Bench"/></b>, because what is
    /// wanted is the per-round verdict and a tally is an aggregate. Nothing here scores, so
    /// no held-out examination is taken and none is implied.
    /// </remarks>
    private static async Task<List<int>> Deciding(IInput world, Brain brain, int rounds)
    {
        var deciders = new Dictionary<Code, int>();

        for (var round = 0; round < rounds; round++)
        {
            if (world.Push() is not { } moment) break;

            var answer = await brain.ReceiveAsync(
                moment, sweeping: round > 0 && round % 1000 == 0);

            if (answer.Vote.By is { } by)
                deciders[by] = deciders.GetValueOrDefault(by) + 1;
        }

        return [.. deciders.Values.OrderDescending()];
    }

    /// <summary>
    /// <b>Whether the decider is a handful or a crowd</b>, which is what says where a scope
    /// holding an identity could ever fire often enough to earn its row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A question <see cref="Vote.By"/> raised and nothing had answered.</b> Its own
    /// remark puts it as a fork in the road — either the deciders are the same handful
    /// whatever the population does, or two runs differing by a sixth in residents and
    /// returning identical scores is a coincidence. This says the answer is the world's.
    /// </para>
    /// <para>
    /// <b>And the bar is a ratio rather than a level</b>, so it is not a fact about either
    /// world's size. What is asserted is that the narrow world concentrates its deciders far
    /// harder than the spine does — which is the whole content of *where nesting could pay*
    /// and is what a level on either alone could not say.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task The_decider_is_a_handful_on_a_narrow_world_and_a_crowd_on_the_spine()
    {
        const int Rounds = 10_000;

        var narrow = await Deciding(
            Multiplexes(1), new Brain(new CommittingSettings { Capacity = 20_000 }, 1), Rounds);

        var spine = await Deciding(
            Roams(1), new Brain(new CommittingSettings { Capacity = 20_000 }, 1), Rounds);

        foreach (var (name, ranked) in new[] { ("multiplexer", narrow), ("roaming", spine) })
            output.WriteLine(
                $"{name,-12}| deciding {ranked.Sum()} | distinct {ranked.Count} "
                + $"| top5 {ranked.Take(5).Sum() / (double)ranked.Sum():F3} "
                + $"| top20 {ranked.Take(20).Sum() / (double)ranked.Sum():F3}");

        var here = narrow.Take(5).Sum() / (double)narrow.Sum();
        var there = spine.Take(5).Sum() / (double)spine.Sum();

        Assert.True(here > there * 3.0,
            $"the five commonest deciders carry {here:F3} of the narrow world's rounds "
            + $"against {there:F3} of the spine's, so the decider is not the world's "
            + "property after all — and the account of why a scope holding an identity "
            + "fires often on one and rarely on the other is wrong.");
    }
}
