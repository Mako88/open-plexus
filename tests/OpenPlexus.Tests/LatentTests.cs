using OpenPlexus.Graph;
using OpenPlexus.Learning;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// A world whose best explanation is never shown — <b>what a posited hub would be
/// minted over.</b>
/// </summary>
/// <remarks>
/// <b>EVERY CHANNEL REPORTS ONE HIDDEN STATE, so they co-occur constantly and none
/// of them causes any other.</b> The thing that would explain them has no code and
/// no walk can reach it. That is the shape `Thought.Grouped` was built for and
/// which no other world here has.
/// </remarks>
public sealed class LatentTests(ITestOutputHelper output)
{
    private static LatentSettings World(int channels = 6, int causes = 12) =>
        new() { Channels = channels, Causes = causes };

    private static WalkSettings Dials => Fixture.Dials(stamina: 4.0);

    private const int Moments = 400;

    [Fact]
    public void A_channel_never_shows_another_channels_code()
    {
        // THE WORLD, ASSERTED RATHER THAN DESCRIBED. If two channels could emit one
        // code the group would be an artefact of the coding rather than of the
        // hidden cause.
        var codes = Enumerable.Range(0, 6)
            .SelectMany(channel => Enumerable.Range(0, 12)
                .Select(cause => Latent.Shows(channel, cause)))
            .ToList();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void The_cause_itself_is_never_emitted()
    {
        // THE POINT OF THE WORLD. What explains the moment is not in the moment.
        var world = new Latent(World(), seed: 1);

        for (var moment = 0; moment < 50; moment++)
        {
            var (cause, shown) = world.Moment();

            Assert.Equal(6, shown.Length);
            Assert.All(shown, code => Assert.Equal(Latent.Seen, code.Modality));

            // Every channel agrees on the state, which is what makes them a group.
            for (var channel = 0; channel < shown.Length; channel++)
                Assert.Equal(Latent.Shows(channel, cause), shown[channel]);
        }
    }

    [Fact]
    public void A_world_too_narrow_to_pay_for_a_hub_is_refused()
    {
        // THREE CHANNELS HOLD THREE EDGES AGAINST A HUB'S THREE PLUS ONE, so a
        // world below four cannot exercise the thing this exists to measure -- and
        // a mechanism that did nothing would read as a mechanism that failed.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Latent(World(channels: 3), 1));
        Assert.False(Paying.Cheaper(3));
    }

    [Fact]
    public async Task The_hidden_channel_is_answered_far_above_chance()
    {
        // PAIRWISE COUNTS ALREADY DO THIS PERFECTLY WELL, which is the point: the
        // claim a hub makes here is about COST and not about reach, so the accuracy
        // is present to show the compression would not have cost anything.
        using var run = new LatentRun(World(), Dials, seed: 1);
        var result = await run.RunAsync(Moments);

        output.WriteLine(result.ToString());

        Assert.Empty(result.Complaints);
        Assert.True(result.Accuracy > result.Chance * 3,
            $"scored {result.Accuracy:F4} against chance {result.Chance:F4}");
    }

    [Fact]
    public async Task And_the_group_a_hub_would_be_minted_over_IS_FOUND()
    {
        // THE MEASUREMENT `Thought.Grouped` HAS BEEN MISSING. Every other world
        // here either has no latent structure or has one nobody stated, so a hub
        // was cheaper by arithmetic and unmeasurable in fact. Here the group is the
        // channels and the hub is the cause.
        using var run = new LatentRun(World(), Dials, seed: 1);
        var result = await run.RunAsync(Moments);

        output.WriteLine($"grouping={result.Grouping:F4} found={result.Found}/{result.Asked}");

        Assert.True(result.Grouping > 0.5,
            $"the walk found a mutually-reaching group on {result.Grouping:P0} of "
            + "questions, so the candidate a posited hub needs is mostly absent");
    }

    [Fact]
    public async Task And_the_cost_a_hub_would_attack_grows_with_the_channels()
    {
        // THE ARITHMETIC THE WHOLE CLAIM RESTS ON, MEASURED RATHER THAN ASSERTED.
        // Pairwise edges among k channels go as k(k-1) row entries and a hub is k,
        // so the saving grows with the width while the hub does not. If the widest
        // row does NOT grow here, a hub is solving a problem this world does not
        // have and the mechanism should be refused rather than tuned.
        var widths = new Dictionary<int, int>();

        foreach (var channels in (int[])[4, 8])
        {
            using var run = new LatentRun(World(channels), Dials, seed: 1);
            var result = await run.RunAsync(Moments);

            widths[channels] = result.Widest;

            output.WriteLine(
                $"channels={channels} widest={result.Widest} edges={result.Edges} "
                + $"traffic={result.Traffic:F0} accuracy={result.Accuracy:F4}");
        }

        Assert.True(widths[8] > widths[4],
            $"eight channels held a widest row of {widths[8]} against four's "
            + $"{widths[4]}, so the cost a hub would remove does not grow here");
    }
}
