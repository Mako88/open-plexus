using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;

namespace OpenPlexus.Tests;

/// <summary>
/// The window carries a departed code forward, so a thing that ended before the
/// next began can still be linked to it.
/// </summary>
public sealed class WindowTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    private static readonly WalkSettings Dials = new()
    {
        Stamina = 10.0, Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum, Horizon = 50,
    };

    private readonly HybridBus _bus = new();
    private readonly Ring _ring = new(seed: 42, replicas: 64);
    private readonly LocalClusters _local;
    private readonly LocalRendezvous _rendezvous;

    public WindowTests()
    {
        _local = new LocalClusters(_ring);
        _rendezvous = new LocalRendezvous(_local);

        foreach (var name in (string[])["a", "b", "c", "d"])
        {
            var address = new ClusterAddress(name);
            _ring.Join(address);
            _local.Include(new Cluster(address, _bus, _ring, Dials));
        }
    }

    private Node Node(Code code) => _local.For(code);

    // ---- what the window holds --------------------------------------------

    [Fact]
    public void A_departed_code_is_carried_for_its_span_and_no_longer()
    {
        var window = new Window(span: 2);
        window.Carry([C(1)], [], now: 10);

        Assert.Contains(C(1), window.Recent(11));
        Assert.Contains(C(1), window.Recent(12));

        // Three moments later it is gone: too long and everything is eventually
        // adjacent to everything, which is the density that makes a graph
        // unwalkable.
        Assert.DoesNotContain(C(1), window.Recent(13));
    }

    [Fact]
    public void A_span_of_zero_carries_nothing()
    {
        // The old behaviour exactly, which is what makes this an arm rather
        // than a replacement.
        var window = new Window(span: 0);
        window.Carry([C(1)], [], now: 10);

        Assert.DoesNotContain(C(1), window.Recent(11));
    }

    [Fact]
    public void A_code_that_comes_back_is_no_longer_departed()
    {
        // Leaving it in would let it be joined twice for one moment -- once as
        // live and once as recent.
        var window = new Window(span: 4);
        window.Carry([C(1)], [], now: 10);
        Assert.Contains(C(1), window.Recent(11));

        window.Carry([], [C(1)], now: 11);

        Assert.DoesNotContain(C(1), window.Recent(12));
    }

    // ---- what a carried code writes ---------------------------------------

    [Fact]
    public async Task The_past_records_the_future_and_not_the_reverse()
    {
        // THE ASYMMETRY IS THE WHOLE OF WHAT MAKES AN EDGE TEMPORAL. A
        // broadcast of what has just happened can walk forward to what usually
        // follows; a broadcast of what follows cannot walk back. Simultaneity
        // stays symmetric, because nothing came first.
        await _rendezvous.JoinAsync(new Occasion
        {
            Onsets = [C(9)], Live = [], Recent = [C(1)], At = 1,
        });

        Assert.Equal(1.0, Node(C(1)).Together(C(9)));
        Assert.Equal(0.0, Node(C(9)).Together(C(1)));
    }

    [Fact]
    public async Task Simultaneity_still_goes_both_ways()
    {
        // The companion, and the reason the test above is about direction
        // rather than about one of the writes being missing.
        await _rendezvous.JoinAsync(new Occasion
        {
            Onsets = [C(9)], Live = [C(2)], Recent = [], At = 1,
        });

        Assert.Equal(1.0, Node(C(9)).Together(C(2)));
        Assert.Equal(1.0, Node(C(2)).Together(C(9)));
    }

    [Fact]
    public async Task A_carried_code_that_is_present_again_is_not_joined_twice()
    {
        await _rendezvous.JoinAsync(new Occasion
        {
            Onsets = [C(9)], Live = [C(1)], Recent = [C(1)], At = 1,
        });

        // Once as live, and not a second time as recent.
        Assert.Equal(1.0, Node(C(1)).Together(C(9)));
    }

    [Fact]
    public async Task A_carried_code_does_not_note_an_occasion_it_was_not_in()
    {
        await _rendezvous.JoinAsync(new Occasion
        {
            Onsets = [C(9)], Live = [], Recent = [C(1)], At = 1,
        });

        // It had stopped, so it did not fire. The invariant still holds because
        // this edge is weighed by the RECEIVER's marginal, and the receiver is
        // an onset, which did note.
        Assert.Equal(0.0, Node(C(1)).Seen);
        Assert.Equal(1.0, Node(C(9)).Seen);
        Assert.True(Node(C(1)).Together(C(9)) <= Node(C(9)).Seen);
    }
}
