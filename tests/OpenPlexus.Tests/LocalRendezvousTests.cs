using System.Collections.Immutable;
using OpenPlexus.Bus;
using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;

namespace OpenPlexus.Tests;

/// <summary>
/// Where connections are formed. A connection is a count, so everything here is
/// about which counts move and which do not.
/// </summary>
public sealed class LocalRendezvousTests
{
    private static Code C(ulong value) => new(Modality: 1, value);

    private static readonly WalkSettings Dials = new()
    {
        Stamina = 10.0,
        Cost = StepCost.Best,
        Refuel = Refuel.Strength,
        Value = ArrivalValue.Strength,
        Accumulate = Accumulate.Sum,
            Horizon = 6,
    };

    private readonly HybridBus _bus = new();
    private readonly Ring _ring = new(seed: 42, replicas: 64);
    private readonly LocalClusters _local;
    private readonly LocalRendezvous _rendezvous;

    public LocalRendezvousTests()
    {
        _local = new LocalClusters(_ring);
        _rendezvous = new LocalRendezvous(_local);

        // Several clusters, so codes really are spread and the join has to
        // reach across them rather than into one dictionary.
        foreach (var name in (string[])["a", "b", "c", "d"])
        {
            var address = new ClusterAddress(name);
            _ring.Join(address);
            _local.Include(new Cluster(address, _bus, _ring, Dials, new LocalMarginals(_local)));
        }
    }

    private ValueTask Join(Code[] onsets, params Code[] live) =>
        _rendezvous.JoinAsync(new Occasion
        {
            Onsets = [.. onsets],
            Live = [.. live],
            At = 0,
        });

    private Node Node(Code code) => _local.For(code);

    // ---- silence ----------------------------------------------------------

    [Fact]
    public async Task An_occasion_with_no_onset_writes_nothing()
    {
        await Join([], C(1), C(2));

        // A stable scene is silent. Sampling on a tick is what manufactures the
        // ever-present distractor, and this is where that is refused.
        Assert.Equal(0.0, Node(C(1)).Seen);
        Assert.Equal(0.0, Node(C(1)).Together(C(2)));
    }

    [Fact]
    public async Task An_occasion_with_an_onset_writes_something()
    {
        // The companion. Without it the test above passes for a rendezvous that
        // never writes at all.
        await Join([C(3)], C(1), C(2));

        Assert.Equal(1.0, Node(C(1)).Seen);
        Assert.Equal(1.0, Node(C(3)).Together(C(1)));
    }

    // ---- what a join writes ------------------------------------------------

    [Fact]
    public async Task An_onset_joins_with_everything_live_in_both_directions()
    {
        await Join([C(9)], C(1), C(2));

        // Each side writes its own row; a node holding both directions would be
        // keeping data it does not own.
        Assert.Equal(1.0, Node(C(9)).Together(C(1)));
        Assert.Equal(1.0, Node(C(1)).Together(C(9)));
        Assert.Equal(1.0, Node(C(9)).Together(C(2)));
        Assert.Equal(1.0, Node(C(2)).Together(C(9)));
    }

    [Fact]
    public async Task Everything_present_notes_the_occasion_including_what_was_already_live()
    {
        await Join([C(9)], C(1), C(2));

        Assert.Equal(1.0, Node(C(9)).Seen);
        Assert.Equal(1.0, Node(C(1)).Seen);
        Assert.Equal(1.0, Node(C(2)).Seen);
    }

    [Fact]
    public async Task Two_codes_that_were_both_already_live_do_not_gain_a_count()
    {
        await Join([C(9)], C(1), C(2));

        // They coincided whenever they started, and that was counted then.
        // Incrementing again on every unrelated onset would inflate exactly the
        // stable background the weighting has to refuse.
        Assert.Equal(0.0, Node(C(1)).Together(C(2)));

        // The companion is the onset-to-live assertion above: something DID get
        // written in the same call.
    }

    [Fact]
    public async Task Two_onsets_in_one_frame_are_one_coincidence_not_two()
    {
        await Join([C(8), C(9)], C(1));

        Assert.Equal(1.0, Node(C(8)).Together(C(9)));
        Assert.Equal(1.0, Node(C(9)).Together(C(8)));

        // And each still reaches the live code.
        Assert.Equal(1.0, Node(C(8)).Together(C(1)));
        Assert.Equal(1.0, Node(C(9)).Together(C(1)));
    }

    [Fact]
    public async Task A_lone_onset_notes_itself_and_pairs_with_nothing()
    {
        await Join([C(9)]);

        Assert.Equal(1.0, Node(C(9)).Seen);
        Assert.Empty(Node(C(9)).Partners());
    }

    // ---- the invariant the weighting rests on ------------------------------

    [Fact]
    public async Task A_shared_count_never_exceeds_either_marginal()
    {
        // THE PROPERTY THAT MAKES AN EDGE WEIGHT MEAN ANYTHING. A partner is
        // scored together(here, other) / seen(other), so a shared count above
        // the marginal would score over 1.0 and make the ever-present
        // background the strongest partner in the graph.
        var rng = new Random(7);
        var alive = new List<Code>();

        for (var step = 0; step < 400; step++)
        {
            var onsets = Enumerable.Range(0, rng.Next(1, 4)).Select(_ => C((ulong)rng.Next(12))).Distinct().ToArray();
            var live = alive.Except(onsets).ToArray();
            await Join(onsets, live);

            alive = [.. live.Concat(onsets).Distinct()];
            if (alive.Count > 6) alive.RemoveRange(0, alive.Count - 6);
        }

        for (var i = 0UL; i < 12; i++)
        {
            var node = Node(C(i));
            foreach (var partner in node.Partners())
            {
                Assert.True(node.Together(partner) <= Node(partner).Seen,
                    $"together({i},{partner.Value})={node.Together(partner)} " +
                    $"exceeds seen({partner.Value})={Node(partner).Seen}");
            }
        }
    }

    [Fact]
    public async Task A_code_present_through_many_onsets_gathers_a_large_marginal()
    {
        // The companion to the invariant, and the reason it holds: a persistent
        // code's marginal grows with what happens around it, which is what
        // makes it a weak partner rather than a hub.
        var background = C(1);

        for (var i = 2UL; i < 30; i++) await Join([C(i)], background);

        Assert.Equal(28.0, Node(background).Seen);
        Assert.All(Node(background).Partners(),
            partner => Assert.Equal(1.0, Node(background).Together(partner)));
    }

    [Fact]
    public async Task The_background_is_therefore_a_weaker_partner_than_a_rare_one()
    {
        // End to end: the counts the rendezvous writes are the ones the forward
        // weighting needs. Nothing here asserts the weight directly -- it
        // asserts the two numbers it divides.
        var background = C(1);
        var rare = C(2);

        for (var i = 10UL; i < 30; i++) await Join([C(i)], background);
        await Join([rare], background);
        await Join([C(50)], background, rare);

        var asker = Node(C(50));

        var backgroundWeight = asker.Together(background) / Node(background).Seen;
        var rareWeight = asker.Together(rare) / Node(rare).Seen;

        Assert.True(rareWeight > backgroundWeight,
            $"rare {rareWeight} should beat background {backgroundWeight}");
    }

    // ---- reach -------------------------------------------------------------

    [Fact]
    public async Task The_join_reaches_across_clusters()
    {
        var codes = Enumerable.Range(1, 8).Select(i => C((ulong)i)).ToArray();
        await Join([codes[0]], codes[1..]);

        // Confirms the spread is real, so the assertions above are not all
        // happening inside one dictionary.
        var owners = codes.Select(code => _ring.OwnerOf(code)).Distinct().Count();
        Assert.True(owners > 1, $"all eight codes landed on {owners} cluster");

        Assert.Equal(7, Node(codes[0]).Partners().Count);
    }

    [Fact]
    public void A_code_whose_owner_is_not_here_is_a_bug_rather_than_a_dropped_write()
    {
        var stranded = new Ring(seed: 42, replicas: 64);
        stranded.Join(new ClusterAddress("elsewhere"));
        var clusters = new LocalClusters(stranded);

        Assert.Throws<InvalidOperationException>(() => clusters.For(C(1)));
    }
}
