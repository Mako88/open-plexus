using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// A world whose best explanation is never shown — <b>what a posited hub would be
/// minted over.</b>
/// </summary>
/// <remarks>
/// <b>Every channel reports one hidden state, so they co-occur constantly and none
/// of them causes any other.</b> The thing that would explain them has no code and
/// no walk can reach it. That is the shape `Thought.Grouped` was built for and
/// which no other world here has.
/// </remarks>
public sealed class LatentTests
{
    private static LatentSettings World(int channels = 6, int causes = 12) =>
        new() { Channels = channels, Causes = causes };

    private const int Moments = 400;

    [Fact]
    public void A_channel_never_shows_another_channels_code()
    {
        // The world, asserted rather than described. If two channels could emit one
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

}
