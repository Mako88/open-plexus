using OpenPlexus.Learning;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

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
public sealed class MotifTests(ITestOutputHelper output)
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

    [Fact]
    public async Task A_recurring_set_completes_itself_well_above_chance()
    {
        // THE POINT IS NOT THAT THIS IS HARD -- IT IS THAT IT IS EASY. The codes
        // co-occur, so the counts are exactly what they should be and a walk
        // completes a familiar set without any help. Establishing that is what
        // makes the cost measurement below mean something: step 3 is not being
        // asked to fix an accuracy, it is being asked to stop paying for one.
        using var run = new MotifRun(World(), Fixture.Dials(stamina: 4.0), seed: 1);
        var result = await run.RunAsync(600);

        output.WriteLine(result.ToString());

        Assert.True(result.Accuracy > result.Chance * 3,
            $"a familiar set did not complete: {result.Accuracy} against {result.Chance}");

        Assert.Empty(result.Complaints);
    }

    [Fact]
    public async Task The_control_has_nothing_worth_minting_and_scores_at_chance()
    {
        // THE SAME STREAM WITH NO STRUCTURE IN IT. Without this, the arm above
        // only says the harness can score a question.
        using var run = new MotifRun(
            World(motifs: 1, density: 0.0), Fixture.Dials(stamina: 4.0), seed: 1);

        var result = await run.RunAsync(600);

        output.WriteLine(result.ToString());

        Assert.True(result.Accuracy < 0.35,
            $"a set that never recurred was still completed: {result.Accuracy}");
    }

    [Fact]
    public async Task The_alphabet_cannot_grow_and_the_graph_pays_pairwise_forever()
    {
        // THE BASELINE FOR STEP 3, STATED AS TWO FACTS THE MECHANISM MUST CHANGE.
        //
        // First, the node count never exceeds the symbols the world emits: the
        // quantiser fixes the alphabet and nothing here can mint a code. Second,
        // the graph holds the sets pairwise -- more entries than the minted form
        // would need -- and it re-derives the completion by walking every time,
        // which is what `Traffic` counts.
        using var run = new MotifRun(World(), Fixture.Dials(stamina: 4.0), seed: 1);
        var result = await run.RunAsync(600);

        output.WriteLine(result.ToString());

        Assert.True(result.Nodes <= 60, $"the alphabet grew to {result.Nodes}");

        // THE SETS ALONE ALREADY COST MORE THAN THE MINTED FORM WOULD, before any
        // of the noise is counted.
        Assert.True(result.Edges > result.Compressed,
            $"the graph is no bigger than the minted form would be: "
            + $"{result.Edges} against {result.Compressed}");

        output.WriteLine(
            $"minting would hold {result.Compressed} entries for the sets; "
            + $"pairwise holds {result.Uncompressed}, and the whole graph "
            + $"{result.Edges} with noise. Each question costs {result.Traffic:F0} messages.");
    }

    // ---- step 3, built ------------------------------------------------------

    [Fact]
    public void A_name_is_derived_from_the_members_and_never_assigned()
    {
        // THE PROPERTY THAT MAKES MINTING LEGAL AT ALL. A counter would give two
        // machines different codes for the same set, and this whole design rests
        // on a code meaning the same thing everywhere forever -- the same
        // constraint that rules out a fitted codebook in step 8.
        var one = new Chunk();
        var other = new Chunk();

        var set = new[] { Motif.Of(3), Motif.Of(1), Motif.Of(2) };
        var shuffled = new[] { Motif.Of(2), Motif.Of(3), Motif.Of(1) };

        // Twice each, because the first arrival of a set has not yet paid for a
        // name -- see the description-length note on `Chunk`.
        one.Notice(set);
        other.Notice(shuffled);

        var mine = one.Notice(set);
        var theirs = other.Notice(shuffled);

        Assert.NotNull(mine);
        Assert.Equal(mine, theirs);

        // AN OCCASION IS A SET, so the order it arrived in cannot reach the name.
        Assert.Equal(Chunk.Minted, mine!.Value.Modality);
    }

    [Fact]
    public void The_threshold_is_description_length_and_not_a_constant()
    {
        // NOTHING HERE WAS CHOSEN, which is the point: a constant nobody set doing
        // the cutting is already a refuted row. Naming wins when n(S-1) > S, so a
        // set of four pays for itself on its second arrival and a pair never does.
        var four = new Chunk();
        var set = new[] { Motif.Of(1), Motif.Of(2), Motif.Of(3), Motif.Of(4) };

        Assert.Null(four.Notice(set));
        Assert.NotNull(four.Notice(set));

        // A PAIR IS NEVER WORTH A NAME. n(2-1) > 2 wants n > 2, so it mints on the
        // third -- naming a pair saves nothing until it is genuinely frequent.
        var two = new Chunk();
        var pair = new[] { Motif.Of(7), Motif.Of(8) };

        Assert.Null(two.Notice(pair));
        Assert.Null(two.Notice(pair));
        Assert.NotNull(two.Notice(pair));

        // AND A LONE CODE IS NEVER A CHUNK, at any count at all.
        var alone = new Chunk();
        for (var i = 0; i < 50; i++) Assert.Null(alone.Notice([Motif.Of(9)]));
    }

    [Fact]
    public async Task Minting_a_node_buys_the_traffic_it_was_supposed_to()
    {
        // THE NUMBER TO BEAT, AND IT IS COST AND NOT ACCURACY. A familiar set
        // already completes perfectly without any of this, so step 3 is not asked
        // to fix an accuracy -- it is asked to stop paying for one.
        var dials = Fixture.Dials(stamina: 4.0);

        using var flat = new MotifRun(World(), dials, seed: 1);
        using var chunked = new MotifRun(World(), dials, seed: 1, chunking: true);

        var without = await flat.RunAsync(600);
        var with = await chunked.RunAsync(600);

        output.WriteLine(without.ToString());
        output.WriteLine(with.ToString());
        output.WriteLine(
            $"traffic {without.Traffic:F0} -> {with.Traffic:F0}, "
            + $"widest {without.Widest} -> {with.Widest}, "
            + $"edges {without.Edges} -> {with.Edges}, "
            + $"accuracy {without.Accuracy:F4} -> {with.Accuracy:F4}");

        Assert.Empty(without.Complaints);
        Assert.Empty(with.Complaints);

        // THE ALPHABET GREW, which is the half of step 3 that nothing else here
        // can do -- and it grew by exactly the number of recurring sets.
        Assert.True(with.Nodes > without.Nodes,
            $"nothing was minted: {with.Nodes} against {without.Nodes}");

        Assert.Equal(without.Motifs, with.Coined);
    }

    [Fact]
    public async Task What_the_minting_costs_and_what_it_buys_over_seeds()
    {
        // ONE SEED IS NOT A RESULT, and this project's history is mostly claims
        // that did not survive their second sweep.
        var dials = Fixture.Dials(stamina: 4.0);

        var arms = await Sweep.AcrossAsync(
            6,
            ("traffic off", async seed =>
            {
                using var run = new MotifRun(World(), dials, seed);
                return (await run.RunAsync(600)).Traffic;
            }),
            ("traffic on", async seed =>
            {
                using var run = new MotifRun(World(), dials, seed, chunking: true);
                return (await run.RunAsync(600)).Traffic;
            }));

        var accuracy = await Sweep.AcrossAsync(
            6,
            ("accuracy off", async seed =>
            {
                using var run = new MotifRun(World(), dials, seed);
                return (await run.RunAsync(600)).Accuracy;
            }),
            ("accuracy on", async seed =>
            {
                using var run = new MotifRun(World(), dials, seed, chunking: true);
                return (await run.RunAsync(600)).Accuracy;
            }));

        var size = await Sweep.AcrossAsync(
            6,
            ("edges off", async seed =>
            {
                using var run = new MotifRun(World(), dials, seed);
                return (await run.RunAsync(600)).Edges;
            }),
            ("edges on", async seed =>
            {
                using var run = new MotifRun(World(), dials, seed, chunking: true);
                return (await run.RunAsync(600)).Edges;
            }));

        output.WriteLine(Sweep.Table(arms));
        output.WriteLine(Sweep.Table(accuracy));
        output.WriteLine(Sweep.Table(size));

        // THE CLAIM STEP 3 WAS ASKED FOR, AND IT IS THE ONLY ONE THAT LANDS.
        // Traffic per completion, and nothing else, is what the plan named.
        Assert.True(arms[1].Separation(arms[0]) > 5.0 && arms[1].Mean < arms[0].Mean,
            $"minting did not buy the traffic: {arms[1].Mean:F0} against "
            + $"{arms[0].Mean:F0}");

        // AND THE TWO IT DOES NOT, ASSERTED SO THEY CANNOT BE FORGOTTEN.
        //
        // THE GRAPH GETS BIGGER, NOT SMALLER. The arithmetic saving is real and
        // tiny: 72 pairwise entries for the sets against 24 minted, against a
        // whole graph of about 2,300. The noise dominates the row count, so six
        // new nodes carrying their own entries more than eat a 48-entry saving.
        // The MDL argument is about the SETS and the graph is mostly not sets.
        Assert.True(size[1].Mean > size[0].Mean,
            "the graph got smaller, so the storage half of the MDL argument has "
            + "started paying and this note is out of date");

        // AND IT COSTS ACCURACY. Completion now routes member -> name -> member
        // where it used to go member -> member, and the members no longer pair
        // with each other at all. THE LIKELY READING, UNVERIFIED: a minted node is
        // a hub BY CONSTRUCTION, and `Pricing.Receiver` exists to make arriving
        // somewhere popular expensive -- so step 3 mints exactly the shape the
        // weighting is built to refuse. `Pricing.Sender` on this world is what
        // would test it.
        Assert.True(accuracy[1].Mean < accuracy[0].Mean,
            "chunking stopped costing accuracy, so the hub reading above needs "
            + "re-running rather than repeating");
    }
}
