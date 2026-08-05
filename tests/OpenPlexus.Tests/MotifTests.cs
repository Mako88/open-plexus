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
    public async Task The_graph_pays_pairwise_and_the_minted_form_would_be_smaller()
    {
        // THE BASELINE FOR STEP 3, AND IT USED TO BE TWO FACTS. The first was that
        // the node count never exceeds the symbols the world emits, because the
        // quantiser fixes the alphabet and nothing could mint a code -- <b>that
        // half is gone, because minting is unconditional since 2026-08-05 and the
        // alphabet growing is now the mechanism working.</b> `Coined` is the
        // check that replaced it, and `MotifResult` fails a run that minted
        // nothing.
        //
        // WHAT REMAINS IS THE FACT THE MECHANISM EXISTS TO CHANGE: the graph holds
        // the sets pairwise, which is more entries than the minted form would
        // need, and it re-derives the completion by walking every time -- which is
        // what `Traffic` counts.
        using var run = new MotifRun(World(), Fixture.Dials(stamina: 4.0), seed: 1);
        var result = await run.RunAsync(600);

        output.WriteLine(result.ToString());

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

        // AGAINST A BACKGROUND, AND THIS TEST USED NOT TO HAVE ONE. A world where
        // a single set is the only thing that ever happens cannot tell a detector
        // from a counter: every code is in hand every round, so the marginals
        // EQUAL the joint and nothing is more frequent than chance. That is not a
        // quirk of the check -- it is true, and it is why the noise control was
        // minting 715 names while nothing here noticed. See `Chunk.Chance`.
        var background = new[] { Motif.Of(10), Motif.Of(11), Motif.Of(12) };

        Chunk.Substitution mine = default, theirs = default;

        for (var i = 0; i < 40; i++)
        {
            mine = one.Notice(set, set.ToHashSet());
            one.Notice(background, background.ToHashSet());

            theirs = other.Notice(shuffled, shuffled.ToHashSet());
            other.Notice(background, background.ToHashSet());
        }

        Assert.True(mine.Folded);

        // AN OCCASION IS A SET, so the order it arrived in cannot reach the name
        // -- and two machines that merged in different orders must still land on
        // the same code, which is why a name is hashed from its ORIGINALS rather
        // than from the two halves that happened to meet.
        // AS SEQUENCES. `ImmutableArray` compares its underlying array by
        // REFERENCE, so `Assert.Equal` on two of them formats the failure as a
        // collection and decides it on identity -- which reads as "collections
        // differ" over two printouts that are character for character the same.
        Assert.Equal(mine.Codes.AsEnumerable(), theirs.Codes.AsEnumerable());
        Assert.Equal(mine.Names.Keys.Order(), theirs.Names.Keys.Order());
        Assert.All(mine.Names.Keys, name => Assert.Equal(Chunk.Minted, name.Modality));
    }

    [Fact]
    public void A_name_may_not_cover_the_whole_moment()
    {
        // THE RULE THAT MADE THIS SUB-MOMENT, AND IT CLOSES TWO DEFECTS AT ONCE.
        // Substitution makes the name the onset and the members merely live, and
        // an occasion never pairs live with live -- so when the name covers
        // everything present, every member-to-member relation is destroyed and the
        // only entries written are name-to-member, which the name's own definition
        // already records. `Senses` fell 0.8621 to 0.4138 on exactly this: a
        // moment there is TWO codes, so every chunk was the whole moment and the
        // sight-sound edge it destroyed is the entire task.
        var pair = new Chunk();
        var two = new[] { Motif.Of(7), Motif.Of(8) };

        // However often it arrives. There is no count at which swallowing the
        // whole moment starts being a compression of it.
        for (var i = 0; i < 50; i++) Assert.False(pair.Notice(two, two.ToHashSet()).Folded);

        // AND A LONE CODE IS NEVER A CHUNK, at any count at all.
        var alone = new Chunk();
        var lone = new[] { Motif.Of(9) };
        for (var i = 0; i < 50; i++) Assert.False(alone.Notice(lone, lone.ToHashSet()).Folded);
    }

    [Fact]
    public void And_what_is_left_over_is_what_the_name_has_to_pair_with()
    {
        // THE COMPANION, AND WITHOUT IT THE RULE ABOVE READS AS "NEVER FOLD". A
        // moment with something left standing DOES fold, and what survives is the
        // name beside the remainder -- which is what gives a conjunction several
        // distinct origins to be a conjunction OF. `Accumulate.Agreement` read
        // exactly equal to `Sum` while one name stood for the whole moment.
        var chunk = new Chunk();
        var three = new[] { Motif.Of(1), Motif.Of(2), Motif.Of(3) };
        var background = new[] { Motif.Of(10), Motif.Of(11), Motif.Of(12) };

        // AGAINST A BACKGROUND. See `A_name_is_derived_from_the_members_and_never
        // _assigned` for why a set that is the only thing in the world can never
        // beat chance: with everything in hand every round, the marginals are the
        // joint.
        var folded = chunk.Notice(three, three.ToHashSet());

        for (var i = 0; i < 40 && !folded.Folded; i++)
        {
            chunk.Notice(background, background.ToHashSet());
            folded = chunk.Notice(three, three.ToHashSet());
        }

        Assert.True(folded.Folded);

        // TWO THINGS IN HAND, NOT ONE: a name covering a pair, and the code it
        // could not swallow.
        Assert.Equal(2, folded.Codes.Length);
        Assert.Single(folded.Names);
        Assert.Equal(2, folded.Names.Values.Single().Length);

        // AND EVERY ORIGINAL IS STILL ACCOUNTED FOR -- absorbed or standing, and
        // never dropped.
        Assert.Equal(
            three.Order(),
            folded.Absorbed.Concat(folded.Codes.Where(one => !folded.Names.ContainsKey(one))).Order());
    }

    [Fact]
    public void The_threshold_is_description_length_and_not_a_constant()
    {
        // NOTHING HERE WAS CHOSEN, which is the point: a constant nobody set doing
        // the cutting is already a refuted row. Naming wins when n(S-1) > S.
        //
        // THE CANDIDATE IS A PAIR, SO THE FIRST NAME ALWAYS COSTS THREE ARRIVALS
        // -- n > 2 at S = 2 -- however large the moment is. What buys the larger
        // sets is COMPOSITION rather than a lower threshold: once a pair has a
        // name the name is itself a candidate, and a merged set of three mints
        // when n(3-1) > 3, which is its SECOND arrival.
        var four = new Chunk();
        var set = new[] { Motif.Of(1), Motif.Of(2), Motif.Of(3), Motif.Of(4) };
        var background = new[] { Motif.Of(10), Motif.Of(11), Motif.Of(12) };

        // AND IT MUST ALSO BEAT CHANCE, so the first two arrivals mint nothing for
        // the description-length reason and a set with no background never mints
        // at all. See `Chunk.Chance`.
        Assert.False(four.Notice(set, set.ToHashSet()).Folded);
        Assert.False(four.Notice(set, set.ToHashSet()).Folded);

        var folded = four.Notice(set, set.ToHashSet());

        for (var i = 0; i < 40 && !folded.Folded; i++)
        {
            four.Notice(background, background.ToHashSet());
            folded = four.Notice(set, set.ToHashSet());
        }

        Assert.True(folded.Folded);

        // AND THE WHOLE MOMENT IS COVERED BY TWO NAMES RATHER THAN ONE, which is
        // the guard doing its work: folding to a single name would leave nothing
        // for it to be in relation with, so the second merge is refused and the
        // two halves pair with each other instead.
        Assert.Equal(2, folded.Codes.Length);
        Assert.Equal(2, folded.Names.Count);
    }

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
