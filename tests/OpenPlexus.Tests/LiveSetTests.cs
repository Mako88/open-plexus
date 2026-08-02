using OpenPlexus.Codes;
using OpenPlexus.Learning;

namespace OpenPlexus.Tests;

/// <summary>
/// The stream is split by change, not by time. These assert that persistence
/// really is silent, and that silence is not simply the mechanism being off.
/// </summary>
public sealed class LiveSetTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    [Fact]
    public void A_first_frame_starts_everything_and_stops_nothing()
    {
        var live = new LiveSet();

        var changes = live.Update([C(1), C(2)], now: 0);

        Assert.Equal([C(1), C(2)], changes.Started.Order().ToArray());
        Assert.Empty(changes.Stopped);
    }

    [Fact]
    public void A_persisting_code_says_nothing_and_a_new_one_still_does()
    {
        var live = new LiveSet();
        live.Update([C(1)], now: 0);

        var changes = live.Update([C(1), C(2)], now: 1);

        // The companion half. Without it, an Update that had stopped working
        // entirely would pass the assertion above.
        Assert.Equal([C(2)], changes.Started.ToArray());
        Assert.Empty(changes.Stopped);
    }

    [Fact]
    public void A_wholly_unchanged_frame_is_silent()
    {
        var live = new LiveSet();
        live.Update([C(1), C(2)], now: 0);

        Assert.True(live.Update([C(1), C(2)], now: 1).Quiet);
    }

    [Fact]
    public void A_code_that_goes_away_stops()
    {
        var live = new LiveSet();
        live.Update([C(1), C(2)], now: 0);

        var changes = live.Update([C(1)], now: 5);

        Assert.Equal([C(2)], changes.Stopped.ToArray());
        Assert.Equal([C(1)], live.Live.ToArray());
    }

    [Fact]
    public void Persisting_does_not_reset_when_a_code_started()
    {
        var live = new LiveSet();
        live.Update([C(1)], now: 10);

        live.Update([C(1)], now: 20);
        live.Update([C(1)], now: 30);

        // Duration is the one thing this representation gains over a set of
        // moments, and refreshing the start time would silently make every
        // duration zero while every other test still passed.
        Assert.Equal(10, live.StartedAt(C(1)));
    }

    [Fact]
    public void A_code_that_returns_starts_again_at_the_new_time()
    {
        var live = new LiveSet();
        live.Update([C(1)], now: 10);
        live.Update([], now: 15);

        var changes = live.Update([C(1)], now: 40);

        Assert.Equal([C(1)], changes.Started.ToArray());
        Assert.Equal(40, live.StartedAt(C(1)));
    }

    [Fact]
    public void An_absent_code_has_no_start_time()
    {
        var live = new LiveSet();

        Assert.Throws<KeyNotFoundException>(() => live.StartedAt(C(1)));
    }
}
