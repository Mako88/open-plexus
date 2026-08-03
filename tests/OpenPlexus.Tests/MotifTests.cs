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
}
