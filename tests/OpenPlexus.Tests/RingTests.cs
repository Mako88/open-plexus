using OpenPlexus.Bus;
using OpenPlexus.Codes;

namespace OpenPlexus.Tests;

/// <summary>
/// The claim under test is that nobody has to be asked: two machines that have
/// never spoken agree about where every code lives.
/// </summary>
public sealed class RingTests
{
    private const int Replicas = 64;

    private static Code C(ulong value) => new(Modality: 1, value);
    private static ClusterAddress A(string name) => new(name);

    private static IEnumerable<Code> Codes(int count) =>
        Enumerable.Range(0, count).Select(i => C((ulong)i));

    private static Ring Ring(long seed, params string[] members)
    {
        var ring = new Ring(seed, Replicas);
        foreach (var member in members) ring.Join(A(member));
        return ring;
    }

    // ---- agreement without a coordinator ----------------------------------

    [Fact]
    public void Two_rings_with_the_same_seed_agree_about_every_code()
    {
        var here = Ring(seed: 42, "alpha", "beta", "gamma");
        var there = Ring(seed: 42, "alpha", "beta", "gamma");

        Assert.All(Codes(2000), code => Assert.Equal(here.OwnerOf(code), there.OwnerOf(code)));
    }

    [Fact]
    public void The_order_members_were_seen_in_does_not_survive()
    {
        // Every machine builds its ring from whatever sequence of joins it
        // happened to see, so insertion order must not reach the answer.
        var here = Ring(seed: 42, "alpha", "beta", "gamma");
        var there = Ring(seed: 42, "gamma", "alpha", "beta");

        Assert.All(Codes(2000), code => Assert.Equal(here.OwnerOf(code), there.OwnerOf(code)));
    }

    [Fact]
    public void A_different_seed_places_codes_differently()
    {
        // The companion. Without it, the two tests above pass for a ring that
        // ignores the seed entirely and always answers "alpha".
        var here = Ring(seed: 42, "alpha", "beta", "gamma");
        var there = Ring(seed: 43, "alpha", "beta", "gamma");

        Assert.Contains(Codes(2000), code => here.OwnerOf(code) != there.OwnerOf(code));
    }

    [Fact]
    public void Joining_the_same_cluster_twice_changes_nothing()
    {
        var once = Ring(seed: 42, "alpha", "beta");
        var twice = Ring(seed: 42, "alpha", "beta", "alpha");

        Assert.Equal(2, twice.Clusters.Count);
        Assert.All(Codes(500), code => Assert.Equal(once.OwnerOf(code), twice.OwnerOf(code)));
    }

    [Fact]
    public void A_code_has_no_owner_until_somebody_joins()
    {
        var ring = new Ring(seed: 42, Replicas);

        Assert.Throws<InvalidOperationException>(() => ring.OwnerOf(C(1)));
    }

    // ---- membership changes move as little as possible --------------------

    [Fact]
    public void When_a_cluster_leaves_only_its_own_codes_move()
    {
        var ring = Ring(seed: 42, "alpha", "beta", "gamma", "delta");
        var before = Codes(2000).ToDictionary(code => code, ring.OwnerOf);

        ring.Leave(A("gamma"));

        // This is the whole reason for a ring rather than a modulo: a departure
        // must not reshuffle the codes that had nothing to do with it.
        foreach (var (code, owner) in before)
        {
            if (owner == A("gamma")) continue;
            Assert.Equal(owner, ring.OwnerOf(code));
        }

        Assert.DoesNotContain(A("gamma"), ring.Clusters);
    }

    [Fact]
    public void A_departed_clusters_codes_do_move_somewhere_else()
    {
        // The companion. Without it the test above passes for a Leave that
        // does nothing at all.
        var ring = Ring(seed: 42, "alpha", "beta", "gamma", "delta");
        var orphans = Codes(2000).Where(code => ring.OwnerOf(code) == A("gamma")).ToArray();

        Assert.NotEmpty(orphans);

        ring.Leave(A("gamma"));

        Assert.All(orphans, code => Assert.NotEqual(A("gamma"), ring.OwnerOf(code)));
    }

    [Fact]
    public void Everything_that_moves_when_a_cluster_arrives_moves_to_it()
    {
        var ring = Ring(seed: 42, "alpha", "beta", "gamma");
        var before = Codes(2000).ToDictionary(code => code, ring.OwnerOf);

        ring.Join(A("delta"));

        var moved = 0;
        foreach (var (code, owner) in before)
        {
            var now = ring.OwnerOf(code);
            if (now == owner) continue;
            Assert.Equal(A("delta"), now);
            moved++;
        }

        Assert.True(moved > 0, "a new cluster took nothing at all");
        Assert.True(moved < before.Count / 2, $"a new cluster took {moved} of {before.Count}");
    }

    // ---- load ------------------------------------------------------------

    [Fact]
    public void Load_falls_within_a_factor_of_two_of_even()
    {
        var members = new[] { "alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta" };
        var ring = Ring(seed: 42, members);

        var share = Codes(20_000)
            .GroupBy(ring.OwnerOf)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(members.Length, share.Count);

        // MEASURED over 8 clusters and 20,000 codes, against an even 2,500:
        //
        //   replicas=16    min 2035   max 3448
        //   replicas=64    min 1837   max 3152
        //   replicas=256   min 2230   max 2883
        //   replicas=1024  min 2449   max 2617
        //
        // So the dial is real and 64 is NOT a good value for it -- the spread
        // at 64 is wider than at 16 on the low side, which is the sampling
        // noise a few dozen points still leaves. The bound below is loose
        // enough to survive the measured spread and tight enough to catch a
        // ring that has collapsed onto one cluster.
        var even = 20_000.0 / members.Length;
        Assert.All(share.Values, count =>
        {
            Assert.True(count > even / 2, $"a cluster holds only {count} of an even {even}");
            Assert.True(count < even * 2, $"a cluster holds {count} of an even {even}");
        });
    }

    [Fact]
    public void Adjacent_codes_do_not_land_together_by_accident()
    {
        // Codes for adjacent cells of one view are neighbouring values. Whether
        // similar codes SHOULD share a cluster is open fork 3 — a real
        // decision, which means it must not happen here by accident first.
        var ring = Ring(seed: 42, "alpha", "beta", "gamma", "delta");

        var owners = Codes(64).Select(ring.OwnerOf).Distinct().Count();

        Assert.Equal(4, owners);
    }
}
