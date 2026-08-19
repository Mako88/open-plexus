using OpenPlexus.Codes;
using OpenPlexus.Commitments;
using OpenPlexus.Machines;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A commitment inside another commitment's scope — <b>and what decides whether it can pay,
/// which is the world's property.</b>
/// </summary>
/// <param name="output">Where the rows go.</param>
/// <remarks>
/// <para>
/// <b>The identity that enters a moment is the one whose claim HELD</b>, and it is the
/// DECIDER rather than everything that fired. C1 settles that rather than the width: a
/// holder knows only its own firings, so a moment carrying them would carry different codes
/// on every machine — and only identical evidence converges on a name.
/// <see cref="Vote.By"/> is the fleet's verdict and is one code everywhere.
/// </para>
/// <para>
/// <b>An identity's recurrence is how often a scope holding it fires</b>, which
/// is what these two worlds are read for. A world answered by a handful of rules puts
/// the same identity in moment after moment; a world answered by hundreds puts a nearly
/// fresh one each round, and a code that cannot recur is a table row and no more — which is
/// this repo's own <c>Fleeting</c> finding arriving from the other side.
/// </para>
/// </remarks>
public sealed class NestingTests(ITestOutputHelper output)
{
    /// <summary>The spine world, translated as its own grids translate it.</summary>
    /// <param name="seed">What draws the houses and the walks.</param>
    private static IInput Roams(int seed) =>
        new Watching<Recited>(
            new Roaming(Fixture.House(Examining.Where), seed),
            new Joined(Joining.Resolved, resolution: 3, freshest: true),
            acting: _ => null);

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
    /// <b>A scope roots on a commitment's identity</b>, which is the architecture line
    /// having a mechanism rather than a property of the type.
    /// </summary>
    /// <remarks>
    /// <b>Read on the population and never on a score</b>, because the score does not move.
    /// The claim being made is that the meta level is REACHABLE, and a reading that waited
    /// for it to pay would be asserting something this world has already been measured not
    /// to give — see the concentration below for why.
    /// </remarks>
    [Fact]
    public void A_scope_roots_on_a_commitment_that_held()
    {
        var nesting = 0;
        var resident = 0;

        foreach (var seed in new[] { 1, 2, 3 })
        {
            var brain = new Brain(new CommittingSettings { Capacity = 20_000 }, seed);

            var tally = new Bench(Roams(seed), brain)
                .Run(10_000, sweep: 1000, target: 0.9, window: 2000);

            var rooted = brain.Held.All.Count(one => one.Scope.Any(Commitment.Names));

            output.WriteLine(
                $"seed {seed} | held {tally.Resident} | repaired {tally.Repaired} "
                + $"| rooting on an identity {rooted} | table {tally.Separations}");

            nesting += rooted;
            resident += tally.Resident;
        }

        Assert.True(nesting > 0,
            $"{resident} commitments were held over three seeds and not one scope holds "
            + "another commitment's identity, so nesting is a property of `Code` and not a "
            + "mechanism. Delete the carry-over in `Brain` with a revival row, and put the "
            + "architecture entry back to having no mechanism under it.");
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
