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
    public async Task Positing_a_hub_AND_dropping_what_it_stands_for()
    {
        // THE PAYOFF, AND THE ARM IS ON AGAINST A CONTROL RATHER THAN ON ALONE.
        // A hub added beside the clique it replaces makes every member's row WIDER,
        // because counts only ever rise -- so what is under test is not "does
        // minting help" but "does minting AND subsuming carry out the arithmetic".
        var scores = new Dictionary<bool, LatentResult>();

        foreach (var posit in (bool[])[false, true])
        {
            using var run = new LatentRun(World(channels: 8), Dials, seed: 1);
            var result = await run.RunAsync(Moments, posit: posit);

            scores[posit] = result;

            output.WriteLine(
                $"posit={posit,-5} accuracy={result.Accuracy:F4} "
                + $"widest={result.Widest} edges={result.Edges} nodes={result.Nodes} "
                + $"subsumed={result.Subsumed} traffic={result.Traffic:F0}");
        }

        // THE CONTROL DROPS NOTHING, or the arm is not the thing being measured.
        Assert.Equal(0, scores[false].Subsumed);

        // AND THE ARM ACTUALLY CARRIED IT OUT. Nought here with the arm on would
        // mean it minted nothing and every number below is the control twice.
        Assert.True(scores[true].Subsumed > 0,
            "the arm dropped no entries, so nothing was ever minted");

        // AND THE ARITHMETIC COMES TRUE. Measured at eight channels, seed 1:
        // 1,176 edges and 14,567 messages a question become 986 and 11,733, with
        // 610 entries subsumed and 27 hubs minted -- a SMALLER graph and a cheaper
        // walk out of ADDING nodes to it, which only works because the minting
        // dropped what it stands for.
        Assert.True(scores[true].Edges < scores[false].Edges,
            $"minting left {scores[true].Edges} edges against {scores[false].Edges} "
            + "-- the hub was added and nothing was taken away");

        Assert.True(scores[true].Traffic < scores[false].Traffic,
            $"minting cost {scores[true].Traffic:F0} messages a question against "
            + $"{scores[false].Traffic:F0} -- a hub that does not cut the fan-out "
            + "is paying for itself in the one currency it exists to save");

        // AND IT COST NO ACCURACY, which is what makes it compression rather than
        // damage. The sibling that was one hop is TWO through the hub, so this is
        // the walk affording the extra step rather than a free lunch.
        Assert.Equal(scores[false].Accuracy, scores[true].Accuracy, precision: 10);

        // THE HUBS ARE REAL NODES AND THERE ARE FEWER OF THEM THAN THE EDGES THEY
        // REPLACED, which is the description-length claim in one line.
        Assert.True(scores[true].Nodes > scores[false].Nodes,
            "no hub node ever entered the graph");
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
