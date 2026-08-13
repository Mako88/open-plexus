using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The world for step 3, built before step 3.
/// </summary>
/// <remarks>
/// <b>Fork 21 mints edges; it should mint nodes.</b> When a set of codes recurs,
/// a code standing for the whole set should come into existence — which is what
/// would let the alphabet grow, where today the quantiser fixes it forever. What
/// this measures is the baseline `Chunk` will have to beat, and the number that
/// matters is cost rather than accuracy.
/// </remarks>
public sealed class MotifTests
{
    private static MotifSettings World(int motifs = 6, int size = 4, double density = 0.5) => new()
    {
        Symbols = 60, Motifs = motifs, Size = size, Density = density,
    };

    // ---- what the world is, asserted rather than described -----------------

    [Fact]
    public void The_sets_are_disjoint_and_a_noise_moment_is_the_same_size()
    {
        // OVERLAPPING SETS WOULD MAKE A COMPLETION AMBIGUOUS for a reason that has
        // nothing to do with chunking, and a noise moment of a different size
        // would make the task counting rather than recognising.
        var world = new Motif(World(), seed: 1);

        var seen = new HashSet<OpenPlexus.Codes.Code>();

        foreach (var motif in world.Motifs)
        {
            Assert.Equal(4, motif.Length);
            foreach (var code in motif) Assert.True(seen.Add(code), "the sets overlap");
        }

        for (var moment = 0; moment < 200; moment++)
            Assert.Equal(4, world.Next().Shown.Length);
    }

    [Fact]
    public void A_question_shows_half_a_set_and_wants_the_rest()
    {
        var world = new Motif(World(), seed: 1);
        var (asked, wanted) = world.Ask(0);

        Assert.Equal(2, asked.Length);
        Assert.Equal(2, wanted.Count);
        Assert.Empty(asked.Where(wanted.Contains));

        // AND THE WHOLE SET IS THE TWO TOGETHER, so nothing was dropped.
        Assert.Equal(world.Motifs[0].Order(), asked.Concat(wanted).Order());
    }

    [Fact]
    public void The_compression_target_is_arithmetic_and_not_a_measurement()
    {
        // A SET OF SIZE S WRITTEN AS CO-OCCURRENCE IS S(S-1) DIRECTED ENTRIES; a
        // node standing for the set is S. That gap is what step 3 buys, and it is
        // computed rather than measured so the graph can be compared against it.
        var world = new Motif(World(motifs: 6, size: 4), seed: 1);

        Assert.Equal(6 * 4, world.Compressed);
        Assert.Equal(6 * 4 * 3, world.Uncompressed);
    }

    // ---- what the graph does with it ---------------------------------------

    // ---- step 3, built ------------------------------------------------------

    // ---- WHAT CHUNKING BOUGHT, AND WHY THE MEASUREMENT IS GONE ------------
    //
    // TWO TESTS STOOD HERE AND BOTH COMPARED CHUNKING AGAINST ITS OWN ABSENCE:
    // `Minting_a_node_buys_the_traffic_it_was_supposed_to` and
    // `What_the_minting_costs_and_what_it_buys_over_seeds`. Step 3 became
    // unconditional on 2026-08-04 -- John's rule, you build it and it is ON -- so
    // both arms are now the same run and the comparison is not expressible.
    //
    // WHAT THEY FOUND, so it is not lost with them: minting cut traffic and cost
    // a little accuracy over six seeds, and the unverified reading of the
    // accuracy cost was that a minted node is a hub by construction and
    // `Pricing.Receiver` refuses hubs. `Toll.Traffic` is the arm that should
    // invert that, and it is a named alternative rather than a switch, so THAT
    // comparison is still expressible and is the one worth taking.
    //
    // The complaint list is what guards the mechanism now: `MotifResult` fails a
    // run that minted nothing at all, and one that minted more names than the
    // world has recurring sets.
}
