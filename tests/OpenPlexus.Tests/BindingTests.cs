using OpenPlexus.Worlds;

namespace OpenPlexus.Tests;

/// <summary>
/// The world this architecture provably cannot do, and the prediction registered
/// before it was ever run.
/// </summary>
/// <remarks>
/// <para>
/// <b>PRE-REGISTERED, 2026-08-03, BEFORE THE FIRST EXECUTION: the unbound arm
/// scores EXACTLY AT CHANCE.</b> Not poorly — at chance, because the two
/// situations it is asked to tell apart are literally the same input. If it
/// scored meaningfully above chance, the model of this architecture written in
/// the handoff would be wrong and the four borrowings planned on top of it would
/// need revisiting before any of them was built.
/// </para>
/// <para>
/// <b>Measured, 16 seeds, stamina 12: 0.5240 ± 0.0268 against a chance of
/// 0.5000</b> — nine tenths of a standard error — while the control arm, which
/// differs only in a fact the counts can hold, scores 0.9167 ± 0.0095. Nearly
/// fourteen standard errors apart, on identical input.
/// </para>
/// <para>
/// <b>Re-baselined 2026-08-03 once <see cref="Seeds.Apart"/> reached
/// <see cref="Sweep"/>.</b> The first reading was 0.5064 ± 0.0213 against
/// 0.9247 ± 0.0072; the claim is unchanged and every error bar is wider, which
/// is what removing correlated seeds is supposed to do.
/// </para>
/// <para>
/// <b>The proof is the world tests, not the accuracy.</b> Two scenes with
/// opposite bindings emit the identical code sequence, which is asserted rather
/// than argued; the accuracy is what that identity looks like from the far end of
/// the system.
/// </para>
/// </remarks>
public sealed class BindingTests
{
    private static BindingSettings World(bool bound, int concepts = 8, int codes = 3) =>
        Fixture.Binding(bound, concepts, codes);

    // ---- what the world is, asserted rather than described ------------------

    [Fact]
    public void The_two_arms_see_the_identical_input()
    {
        // THE PROOF, AND EVERYTHING ELSE HERE IS A DEMONSTRATION OF IT. The bound
        // world and the unbound world at the same seed emit the same codes in the
        // same order, scene after scene. Only which shape is answerable for which
        // colour differs -- and that lives nowhere in what the machine receives.
        var bound = new Binding(World(bound: true), seed: 1);
        var unbound = new Binding(World(bound: false), seed: 1);

        for (var i = 0; i < 2_000; i++)
            Assert.Equal(bound.Next().Codes, unbound.Next().Codes);
    }

    [Fact]
    public void And_the_answer_differs_about_half_the_time()
    {
        // THE COMPANION, and without it the test above passes for two worlds that
        // are simply the same world. Same input, different truth, on about half
        // the scenes -- which is what makes the task a coin flip from inside.
        var bound = new Binding(World(bound: true), seed: 1);
        var unbound = new Binding(World(bound: false), seed: 1);

        var differed = 0;
        for (var i = 0; i < 1_000; i++)
            if (!bound.Next().Shapes.SequenceEqual(unbound.Next().Shapes)) differed++;

        Assert.InRange(differed, 400, 600);
    }

    [Fact]
    public void The_binding_coin_is_fair_and_its_spread_is_honest()
    {
        // THE TRAP THAT COST A FALSE FIVE-SIGMA RESULT, 2026-08-03, kept as a
        // check so it cannot come back. A seeded Random in .NET normalises by
        // magnitude, so consecutive seeds produce nearly the same stream: over
        // seeds 1..8 the swap count landed in 19..23 of 39, a spread of about 1.3
        // where a fair coin gives 3.1. A standard error taken ACROSS those seeds
        // is then far too small, and this world's headline read as five sigma
        // below chance when it was sitting on it.
        //
        // So both halves are asserted. Fair on its own is not enough -- the
        // broken seeding was fair too, and still wrong.
        var counts = Enumerable.Range(1, 32).Select(seed =>
        {
            var world = new Binding(World(bound: false), seed);
            var swaps = 0;

            for (var moment = 0; moment < 400; moment++)
            {
                var scene = world.Next();

                // The questions are asked on every tenth scene, so that is the
                // subsample whose fairness actually decides a score.
                if (moment % 10 == 0 && moment != 0 && scene.Shapes[0] != scene.Colours[0])
                    swaps++;
            }

            return (double)swaps;
        }).ToList();

        var measured = new Measured { Arm = "swap rate", Values = counts };

        // Fair: 39 draws a seed, so half is 19.5 and the pooled error is small.
        Assert.True(Math.Abs(measured.Mean - 19.5) < 3 * measured.StdErr + 0.5,
            $"{measured} against 19.5 of 39");

        // AND HONEST: a fair coin over 39 draws has a standard deviation of 3.12,
        // so anything much under that means the seeds are agreeing with each
        // other rather than sampling.
        var spread = measured.StdErr * Math.Sqrt(counts.Count);
        Assert.True(spread > 2.0,
            $"the per-seed spread is {spread:F2} where a fair coin gives 3.12, " +
            "so these seeds are not independent");
    }

    [Fact]
    public void A_scene_is_two_objects_of_two_different_kinds()
    {
        var world = new Binding(World(bound: false), seed: 2);

        for (var i = 0; i < 1_000; i++)
        {
            var scene = world.Next();

            Assert.Equal(2, scene.Objects);
            Assert.Equal(4, scene.Codes.Count);
            Assert.NotEqual(scene.Colours[0], scene.Colours[1]);

            // Both objects' shapes are present, and they are the two kinds in the
            // scene. The binding permutes them; it never introduces a third.
            Assert.Equal([.. scene.Colours.Order()], [.. scene.Shapes.Order()]);
        }
    }

    [Fact]
    public void A_scene_shows_two_colours_and_two_shapes()
    {
        var world = new Binding(World(bound: false), seed: 2);

        for (var i = 0; i < 500; i++)
        {
            var attributes = world.Next().Codes
                .GroupBy(code => code.Modality)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.Equal(2, attributes[Binding.Colour]);
            Assert.Equal(2, attributes[Binding.Shape]);
        }
    }

    [Fact]
    public void Two_concepts_never_share_a_code()
    {
        var world = new Binding(World(bound: false, concepts: 6, codes: 4), seed: 3);

        var all = (from attribute in (byte[])[Binding.Colour, Binding.Shape]
                   from concept in Enumerable.Range(0, 6)
                   from code in world.Of(attribute, concept)
                   select code).ToArray();

        Assert.Equal(all.Length, all.Distinct().Count());
        Assert.All(all, code => Assert.Equal(
            (int)(code.Value / 1000), Binding.Concept(code)));
    }

    // ---- what it measures ---------------------------------------------------

    /// <summary>
    /// Grouping in the occasion, the index in the question, and the edge weighed
    /// from the sender's end. <b>The three together are what lift it.</b>
    /// </summary>
    private static BindingSettings Bound => Fixture.Binding(segmented: true, tagged: true);

    [Fact]
    public void A_tag_without_its_group_is_refused_rather_than_accepted()
    {
        // AN ARM THAT LOOKS DISTINCT AND IS NOT is how this project has fooled
        // itself before. An ungrouped tag pairs with every code in the scene, so
        // it indexes nothing and the arm would quietly measure the untagged one.
        Assert.Throws<ArgumentException>(() =>
            new Binding(World(bound: false) with { Tagged = true }, seed: 1));
    }

}
